using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.API.Server.RealtimeHubs;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
    public interface IImagedObjectService
    {
        Task<ImagedObjectListDTO> GetImagedObjectsAsync(EditionUserInfo editionUser,
            List<string> optional = null);

        Task<ImagedObjectListDTO> GetImagedObjectsWithArtefactsAsync(EditionUserInfo editionUser,
            bool withMasks = false);

        Task<ImagedObjectDTO> GetImagedObjectAsync(EditionUserInfo editionUser,
            string imagedObjectId,
            List<string> optional = null);
    }

    public class ImagedObjectService : IImagedObjectService
    {
        private readonly IArtefactRepository _artefactRepository;
        private readonly IHubContext<MainHub, ISQEClient> _hubContext;
        private readonly IImageRepository _imageRepo;
        private readonly IImageService _imageService;
        private readonly IImagedObjectRepository _repo;

        public ImagedObjectService(
            IImagedObjectRepository repo,
            IImageRepository imageRepo,
            IImageService imageService,
            IArtefactRepository artefactRepository,
            IHubContext<MainHub, ISQEClient> hubContext)
        {
            _repo = repo;
            _imageRepo = imageRepo;
            _imageService = imageService;
            _artefactRepository = artefactRepository;
            _hubContext = hubContext;
        }

        // TODO: Fix this and GetImagedObjectsWithArtefactsAsync up to be more DRY and efficient.
        public async Task<ImagedObjectListDTO> GetImagedObjectsAsync(EditionUserInfo editionUser,
            List<string> optional = null)
        {
            ParseOptionals(optional, out var artefacts, out var masks);
            if (artefacts)
                return await GetImagedObjectsWithArtefactsAsync(editionUser, masks);

            var imagedObjects = await _repo.GetImagedObjectsAsync(editionUser, null);

            if (imagedObjects == null)
                throw new StandardExceptions.DataNotFoundException("imaged object", editionUser.EditionId, "edition");
            var result = new ImagedObjectListDTO
            {
                imagedObjects = new List<ImagedObjectDTO>()
            };
            var images = await _imageRepo.GetImagesAsync(editionUser, null); //send imagedFragment from here 

            var imageDict = new Dictionary<string, List<ImageDTO>>();
            foreach (var image in images)
            {
                if (!imageDict.ContainsKey(image.ObjectId)) imageDict[image.ObjectId] = new List<ImageDTO>();
                imageDict[image.ObjectId].Add(_imageService.ImageToDTO(image));
            }

            foreach (var i in imagedObjects)
                if (imageDict.TryGetValue(i.Id, out var imagedFragment))
                    result.imagedObjects.Add(ImagedObjectModelToDTO(i, imagedFragment));

            return result;
        }

        public async Task<ImagedObjectListDTO> GetImagedObjectsWithArtefactsAsync(EditionUserInfo editionUser,
            bool withMasks = false)
        {
            var result = await GetImagedObjectsAsync(editionUser);

            var artefacts = ArtefactDTOTransformer.QueryArtefactListToArtefactListDTO(
                (await _artefactRepository.GetEditionArtefactListAsync(editionUser, withMasks)).ToList(),
                editionUser.EditionId
            );

            // The code below takes two lists: one `result.imagedObjects` has many imaged objects, but no artefact information;
            // the second has a list of `artefacts.artefacts`, that need to be inserted into the imaged objects of `result.imagedObjects`.
            // The GroupJoin looks at the id of each imaged object and creates a group of artefacts based on the
            // agreement of `result.imagedObjects`.x.id = `artefacts.artefacts`.x.imagedObjectId (there may be several
            // instances of `artefacts.artefacts`.x that belong to a single `result.imagedObjects`.x).  The single
            // imaged object in the match is the first parameter of `(imagedObject, artefactObjects)` and the group of
            // artefacts is the second parameter in that lambda. The lambda function `(imagedObject, artefactObjects) => ...`
            // then creates an ImagedObjectDTO with the artefacts attribute populated from the grouped `artefactObjects`.
            result.imagedObjects = result.imagedObjects.GroupJoin(
                    artefacts.artefacts, // Take two lists: result.imagedObjects and artefacts.artefacts.
                    arg => arg.id, // Group them together where the imagedObject.id
                    arg => arg.imagedObjectId, // == the artefactObject.imagedObjectId (a one-to-many relationship).
                    (imagedObject, artefactObjects) =>
                        new ImagedObjectDTO // For each such grouping create an object that merges
                        {
                            id = imagedObject.id, // the data from the imagedObject
                            recto = imagedObject.recto,
                            verso = imagedObject.verso,
                            artefacts = artefactObjects.ToList() // and the group of artefactObjects as a List.
                        }
                )
                .ToList(); // Return the IEnumerable array as a List. 

            return result;
        }

        // TODO: Make this less wasteful by retrieving only the desired imaged object
        public async Task<ImagedObjectDTO> GetImagedObjectAsync(EditionUserInfo editionUser,
            string imagedObjectId,
            List<string> optional = null)
        {
            ParseOptionals(optional, out var artefacts, out var masks);
            var result =
                (await GetImagedObjectsAsync(editionUser)).imagedObjects.First(x => x.id == imagedObjectId);
            if (artefacts)
            {
                var artefactList = ArtefactDTOTransformer.QueryArtefactListToArtefactListDTO(
                    (await _artefactRepository.GetEditionArtefactListAsync(editionUser, masks)).ToList(),
                    editionUser.EditionId
                );

                foreach (var art in artefactList.artefacts)
                {
                    if (result.artefacts == null)
                        result.artefacts = new List<ArtefactDTO>();
                    if (result.id == art.imagedObjectId)
                        result.artefacts.Add(art);
                }
            }

            return result;
        }

        private static ImagedObjectDTO ImagedObjectModelToDTO(ImagedObject model, List<ImageDTO> images)
        {
            var (recto, verso) = GetSides(images);
            return new ImagedObjectDTO
            {
                id = model.Id,
                recto = recto,
                verso = verso
            };
        }

        /// <summary>
        ///     Get a tuple of DTO's for the recto and verso image set from a flat list of ImageDTO's.
        /// </summary>
        /// <param name="images">List of ImageDTO to be organized into recto/verso</param>
        /// <returns></returns>
        private static (ImageStackDTO recto, ImageStackDTO verso) GetSides(IEnumerable<ImageDTO> images)
        {
            var recto = new List<ImageDTO>();
            var verso = new List<ImageDTO>();
            int? rectoMasterIndex = null;
            uint? rectoCatalogId = null;
            int? versoMasterIndex = null;
            uint? versoCatalogId = null;

            // One loop over all the images
            foreach (var image in images)
                // Build the recto and verso Lists based on the "Side" value
                switch (image.side)
                {
                    case "recto":
                        recto.Add(image);
                        rectoCatalogId = image.catalogNumber;
                        if (image.master)
                            rectoMasterIndex = recto.Count - 1;
                        break;
                    case "verso":
                        verso.Add(image);
                        versoCatalogId = image.catalogNumber;
                        if (image.master)
                            versoMasterIndex = verso.Count - 1;
                        break;
                }

            return (recto.Any() // Check if we have any recto images, and return a null if we don't
                        ? new ImageStackDTO
                        {
                            id = rectoCatalogId,
                            masterIndex = rectoMasterIndex,
                            images = recto
                        }
                        : null,
                    verso.Any() // Check if we have any verso images, and return a null if we don't
                        ? new ImageStackDTO
                        {
                            id = versoCatalogId,
                            masterIndex = versoMasterIndex,
                            images = verso
                        }
                        : null
                );
        }

        private static void ParseOptionals(List<string> optionals, out bool artefacts, out bool masks)
        {
            artefacts = masks = false;
            if (optionals == null)
                return;
            artefacts = optionals.Contains("artefacts");
            if (!optionals.Contains("masks"))
                return;
            masks = true;
            artefacts = true;
        }
    }
}