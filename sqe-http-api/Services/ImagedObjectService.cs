using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.Backend.Server.DTOs;
using SQE.Backend.DataAccess;

namespace SQE.Backend.Server.Services
{
    public interface IImagedObjectService
    {
        Task<ImagedObjectListDTO> GetImagedObjects(uint? userId, uint editionId);
        Task<ImagedObjectListDTO> GetImagedObjectsWithArtefacts(uint? userId, uint editionId);
        Task<ImagedObjectDTO> GetImagedObject(uint? userId, uint editionId, string imagedObjectId);
        Task<ImagedObjectDTO> GetImagedFragment(uint? userId, uint scrollVersionId, string fragmentId);
    }
    public class ImagedObjectService : IImagedObjectService
    {
<<<<<<< HEAD:sqe-http-api/Services/ImagedFragmentsService.cs
        IImagedFragmentsRepository _repo;
        IImageRepository _imageRepo;
        IImageService _imageService;
        IArtefactService _artefactService;

        public ImagedFragmentsService(IImagedFragmentsRepository repo, IImageRepository imageRepo, IImageService imageService, IArtefactService artefactService)
=======
        private readonly IImagedObjectRepository _repo;
        private readonly IImageRepository _imageRepo;
        private readonly IImageService _imageService;
        private readonly IArtefactRepository _artefactRepository;

        public ImagedObjectService(
            IImagedObjectRepository repo, 
            IImageRepository imageRepo, 
            IImageService imageService, 
            IArtefactRepository artefactRepository)
>>>>>>> 6cc19a4187d1bfe5c70efc913e4adf5b324c1a4e:sqe-http-api/Services/ImagedObjectService.cs
        {
            _repo = repo;
            _imageRepo = imageRepo;
            _imageService = imageService;
<<<<<<< HEAD:sqe-http-api/Services/ImagedFragmentsService.cs
            _artefactService = artefactService;
=======
            _artefactRepository = artefactRepository;
>>>>>>> 6cc19a4187d1bfe5c70efc913e4adf5b324c1a4e:sqe-http-api/Services/ImagedObjectService.cs
        }

        async public Task<ImagedObjectListDTO> GetImagedObjects(uint? userId, uint editionId)
        {
            var imagedFragments = await _repo.GetImagedObjects(userId, editionId, null);

            if (imagedFragments == null)
            {
                throw new NotFoundException((uint)editionId);
            }
            var result = new ImagedObjectListDTO
            {
                result = new List<ImagedObjectDTO>(),
            };
            var images = await _imageRepo.GetImages(userId, editionId, null); //send imagedFragment from here 

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
                    result.result.Add(ImagedFragmentModelToDTO(i, imagedFragment, null));
            }

            return result; 
        }
        
        async public Task<ImagedObjectListDTO> GetImagedObjectsWithArtefacts(uint? userId, uint editionId)
        {
            var result = await GetImagedObjects(userId, editionId);

            var artefacts = (await _artefactRepository.GetEditionArtefactNameList(userId, editionId)).Select(x => new ArtefactDTO()
            {
                id = x.artefact_id,
                editionId = editionId,
                imageFragmentId = x.institution + "-" + x.catalog_number_1 + "-" + x.catalog_number_2,
                name = x.name,
                side = x.catalog_side == 0 ? ArtefactDTO.artSide.recto : ArtefactDTO.artSide.verso, 
                zOrder = 0,
                transformMatrix = "",
            });

            result.result = result.result.GroupJoin(
                artefacts, 
                arg => arg.id, 
                arg => arg.imageFragmentId,
                (imagedObject, artefactObjects) => new ImagedObjectDTO()
                {
                    id = imagedObject.id,
                    recto = imagedObject.recto,
                    verso = imagedObject.verso,
                    Artefacts = artefactObjects.ToList()
                }).ToList(); 

            return result; 
        }
        
        // TODO: Make this less wasteful by retrieving only the desired imaged object
        async public Task<ImagedObjectDTO> GetImagedObject(uint? userId, uint editionId, string imagedObjectId)
        {
            return (await GetImagedObjects(userId, editionId)).result.First(x => x.id == imagedObjectId);
        }
        
        private string getImagedObjectId(DataAccess.Models.Image image)
        {
            return image.Institution + "-" + image.Catlog1 + "-" + image.Catalog2;
        }
<<<<<<< HEAD:sqe-http-api/Services/ImagedFragmentsService.cs
        internal static ImagedFragmentDTO ImagedFragmentModelToDTO(DataAccess.Models.ImagedFragment model, List<ImageDTO> images, List<ArtefactDTO> artefact)
        {

            return new ImagedFragmentDTO
            {
                id = model.Id.ToString(),
                recto = getRecto(images),
                verso = getVerso(images),
                Artefacts = artefact
           
            };
        }
=======
        internal static ImagedObjectDTO ImagedFragmentModelToDTO(DataAccess.Models.ImagedObject model, List<ImageDTO> images)
        {
            var sides = getSides(images);
            return new ImagedObjectDTO
            {
                id = model.Id.ToString(),
                recto = sides.recto,
                verso = sides.verso
            };
        }

        /// <summary>
        /// Get a tuple of DTO's for the recto and verso image set from a flat list of ImageDTO's.
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        private static (ImageStackDTO recto, ImageStackDTO verso) getSides(List<ImageDTO> images)
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
                // Build the recto and verso Lists based on the "side" value
                switch (image.side)
                {
                    case "recto":
                        recto.Add(image);
                        rectoCatalogId = image.catalog_number;
                        if (image.master)
                            rectoMasterIndex = recto.Count() - 1;
                        break;
                    case "verso":
                        verso.Add(image);
                        versoCatalogId = image.catalog_number;
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
        
        // Daniella, it is probably better to do this all in one pass (see `getSides()`, rather than looping over the
        // images twice, once in getRecto, and again in getVerso.
>>>>>>> 6cc19a4187d1bfe5c70efc913e4adf5b324c1a4e:sqe-http-api/Services/ImagedObjectService.cs
        private static ImageStackDTO getRecto(List<ImageDTO> images)
        {
            var img = new List<ImageDTO>();
            foreach (var image in images)
            {
                if (image.side == "recto")
                {
                    img.Add(image);
                }
            }
            if (img.Count == 0)
            {
                return null;
            }
            var masterIndex = img.FindIndex(i => i.master);
            var catalog_id = img[0].catalog_number;
            return new ImageStackDTO
            {
                id = catalog_id,
                masterIndex = masterIndex,
                images = img
            };
        }

        private static ImageStackDTO getVerso(List<ImageDTO> images)
        {
            var img = new List<ImageDTO>();
            foreach (var image in images)
            {
                if (image.side == "verso")
                {
                    img.Add(image);
                }
            }
            if (img.Count == 0)
            {
                return null;
            }
            var masterIndex = img.FindIndex(i => i.master);
            var catalog_id = img[0].catalog_number;
            return new ImageStackDTO
            {
                id = catalog_id,
                masterIndex = masterIndex,
                images = img
            };
        }

        async public Task<ImagedObjectDTO> GetImagedFragment(uint? userId, uint scrollVersionId, string fragmentId)
        {
            var images = await _imageRepo.GetImages(userId, scrollVersionId, fragmentId); //send imagedFragment from here 
<<<<<<< HEAD:sqe-http-api/Services/ImagedFragmentsService.cs
            var imagedFragments = await _repo.GetImagedFragments(userId, scrollVersionId, fragmentId); //should be only one!
            if(imagedFragments.Count() == 0){
                throw new NotFoundException(scrollVersionId);
            }
            var artefacts = await _artefactService.GetAtrefactAsync(userId,null, scrollVersionId, fragmentId);
=======
            var imagedFragments = await _repo.GetImagedObjects(userId, scrollVersionId, fragmentId); //should be onky one!
>>>>>>> 6cc19a4187d1bfe5c70efc913e4adf5b324c1a4e:sqe-http-api/Services/ImagedObjectService.cs
            var img = new List<ImageDTO>();
            foreach (var image in images)
            {
                img.Add(_imageService.ImageToDTO(image));
                //var fragmentId = getImagedObjectId(image);
            }
            var result = ImagedFragmentModelToDTO(imagedFragments.First(), img, artefacts);

            return result;
        }
    }
}
