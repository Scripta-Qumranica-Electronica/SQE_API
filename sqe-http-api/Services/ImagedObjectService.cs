using System;
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
            bool withMask = false);
        Task<ImagedObjectDTO> GetImagedObjectAsync(uint? userId, uint editionId, string imagedObjectId);
        Task<ImagedObjectDTO> GetImagedFragmentAsync(uint? userId, uint scrollVersionId, string fragmentId);
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
                result = new List<ImagedObjectDTO>(),
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
                    result.result.Add(ImagedObjectModelToDTO(i, imagedFragment));
            }

            return result; 
        }
        
        public async Task<ImagedObjectListDTO> GetImagedObjectsWithArtefactsAsync(uint? userId, uint editionId,
            bool withMask = false)
        {
            var result = await GetImagedObjectsAsync(userId, editionId);

            var artefacts = (await _artefactRepository.GetEditionArtefactListAsync(userId, editionId, withMask)).Select(x => new ArtefactDTO()
            {
                Id = x.artefact_id,
                EditionId = editionId,
                Mask = new PolygonDTO()
                {
                    mask = x.mask
                },
                ImagedObjectId = x.institution + "-" 
                                                + x.catalog_number_1 
                                                + (string.IsNullOrEmpty(x.catalog_number_2) ? "" : "-" + x.catalog_number_2),
                Name = x.name,
                Side = x.catalog_side == 0 ? ArtefactDTO.artSide.recto : ArtefactDTO.artSide.verso, 
                zOrder = 0,
                TransformMatrix = "",
            });

            result.result = result.result.GroupJoin(
                artefacts, 
                arg => arg.id, 
                arg => arg.ImagedObjectId,
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
        public async Task<ImagedObjectDTO> GetImagedObjectAsync(uint? userId, uint editionId, string imagedObjectId)
        {
            return (await GetImagedObjectsAsync(userId, editionId)).result.First(x => x.id == imagedObjectId);
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

        // TODO: check if this is used anywhere and perhaps remove.
        public async Task<ImagedObjectDTO> GetImagedFragmentAsync(uint? userId, uint scrollVersionId, string fragmentId)
        {
            var images = await _imageRepo.GetImagesAsync(userId, scrollVersionId, fragmentId); //send imagedFragment from here 
            var imagedFragments = await _repo.GetImagedObjectsAsync(userId, scrollVersionId, fragmentId); //should be only one!
            var img = images.Select(image => _imageService.ImageToDTO(image)).ToList();
            var result = ImagedObjectModelToDTO(imagedFragments.First(), img);

            return result;
        }
    }
}
