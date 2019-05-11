using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.SqeHttpApi.Server.Services
{
    public interface IImagedObjectService
    {
        Task<ImagedObjectListDTO> GetImagedObjectsAsync(uint? userId, uint editionId);
        Task<ImagedObjectListDTO> GetImagedObjectsWithArtefactsAsync(uint? userId, uint editionId,
            bool withMasks = false);
        Task<ImagedObjectDTO> GetImagedObjectAsync(uint? userId, uint editionId, string imagedObjectId,
            bool withArtefacts = false, bool withMasks = false);
    }

    public class ImagedObjectService : IImagedObjectService
    {
        private readonly IImagedObjectRepository _repo;
        private readonly IImageRepository _imageRepo;
        private readonly IImageService _imageService;
        private readonly IArtefactRepository _artefactRepository;

        public ImagedObjectService(
            IImagedObjectRepository repo, 
            IImageRepository imageRepo, 
            IImageService imageService, 
            IArtefactRepository artefactRepository)
        {
            _repo = repo;
            _imageRepo = imageRepo;
            _imageService = imageService;
            _artefactRepository = artefactRepository;
        }

        public async Task<ImagedObjectListDTO> GetImagedObjectsAsync(uint? userId, uint editionId)
        {
            var imagedFragments = await _repo.GetImagedObjectsAsync(userId, editionId, null);

            if (imagedFragments == null)
            {
                throw new NotFoundException((uint)editionId);
            }
            var result = new ImagedObjectListDTO
            {
                imagedObjects = new List<ImagedObjectDTO>(),
            };
            var images = await _imageRepo.GetImagesAsync(userId, editionId, null); //send imagedFragment from here 

            var imageDict = new Dictionary<string, List<ImageDTO>>();
            foreach (var image in images)
            {
                var fragmentId = getImagedObjectId(image);
                if (!imageDict.ContainsKey(fragmentId))
                {
                    imageDict[fragmentId] = new List<ImageDTO>();
                }
                imageDict[fragmentId].Add(_imageService.ImageToDTO(image));
            }

            foreach (var i in imagedFragments)
            {
                if (imageDict.TryGetValue(i.Id, out var imagedFragment))
                    result.imagedObjects.Add(ImagedObjectModelToDTO(i, imagedFragment));
            }

            return result; 
        }
        
        public async Task<ImagedObjectListDTO> GetImagedObjectsWithArtefactsAsync(uint? userId, uint editionId,
            bool withMasks = false)
        {
            var result = await GetImagedObjectsAsync(userId, editionId);

            var artefacts = ArtefactDTOTransform.QueryArtefactListToArtefactListDTO(
                (await _artefactRepository.GetEditionArtefactListAsync(userId, editionId, withMasks)).ToList(), 
                editionId
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
                (imagedObject, artefactObjects) => new ImagedObjectDTO() // For each such grouping create an object that merges
                {
                    id = imagedObject.id, // the data from the imagedObject
                    recto = imagedObject.recto,
                    verso = imagedObject.verso,
                    artefacts = artefactObjects.ToList() // and the group of artefactObjects as a List.
                }).ToList(); // Return the IEnumerable array as a List. 

            return result; 
        }
        
        // TODO: Make this less wasteful by retrieving only the desired imaged object
        public async Task<ImagedObjectDTO> GetImagedObjectAsync(uint? userId, uint editionId, string imagedObjectId,
            bool withArtefacts = false, bool withMasks = false)
        {
            var result = (await GetImagedObjectsAsync(userId, editionId)).imagedObjects.First(x => x.id == imagedObjectId);
            if (withArtefacts)
            {
                var artefacts = ArtefactDTOTransform.QueryArtefactListToArtefactListDTO(
                    (await _artefactRepository.GetEditionArtefactListAsync(userId, editionId, withMasks)).ToList(), 
                    editionId
                    );

                foreach (var artefact in artefacts.artefacts)
                {
                    if (result.artefacts == null)
                        result.artefacts = new List<ArtefactDTO>();
                    if (result.id == artefact.imagedObjectId)
                        result.artefacts.Add(artefact);
                }
            }

            return result; 
        }
        
        private static string getImagedObjectId(DataAccess.Models.Image image)
        {
            return image.Institution + "-" 
                                     + image.Catlog1 
                                     + (string.IsNullOrEmpty(image.Catalog2) ? "" : "-" + image.Catalog2);
        }

        private static ImagedObjectDTO ImagedObjectModelToDTO(DataAccess.Models.ImagedObject model, List<ImageDTO> images)
        {
            var (recto, verso) = getSides(images);
            return new ImagedObjectDTO
            {
                id = model.Id.ToString(),
                recto = recto,
                verso = verso
            };
        }

        /// <summary>
        /// Get a tuple of DTO's for the recto and verso image set from a flat list of ImageDTO's.
        /// </summary>
        /// <param Name="images"></param>
        /// <returns></returns>
        private static (ImageStackDTO recto, ImageStackDTO verso) getSides(IEnumerable<ImageDTO> images)
        {
            var recto = new List<ImageDTO>();
            var verso = new List<ImageDTO>();
            int?  rectoMasterIndex = null;
            uint? rectoCatalogId = null;
            int?  versoMasterIndex = null;
            uint? versoCatalogId = null;
            
            // One loop over all the images
            foreach (var image in images)
            {
                // Build the recto and verso Lists based on the "Side" value
                switch (image.side)
                {
                    case "recto":
                        recto.Add(image);
                        rectoCatalogId = image.catalogNumber;
                        if (image.master)
                            rectoMasterIndex = recto.Count() - 1;
                        break;
                    case "verso":
                        verso.Add(image);
                        versoCatalogId = image.catalogNumber;
                        if (image.master)
                            versoMasterIndex = verso.Count() - 1;
                        break;
                }
            }
            
            // Null the objects if no images were found
            if (!recto.Any())
            {
                recto = null;
            }
            if (!verso.Any())
            {
                verso = null;
            }
            
            return (new ImageStackDTO
                    {
                        id = rectoCatalogId,
                        masterIndex = rectoMasterIndex,
                        images = recto
                    },
                    new ImageStackDTO
                    {
                        id = versoCatalogId,
                        masterIndex = versoMasterIndex,
                        images = verso
                    }
                );
        }
    }
}
