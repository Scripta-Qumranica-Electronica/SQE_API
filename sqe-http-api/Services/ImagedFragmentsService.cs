using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.SqeHttpApi.Server.Services
{
    public interface IImagedFragmentsService
    {
        Task<ImagedObjectListDTO> GetImagedObjects(uint? userId, uint scrollVersionId);
        Task<ImagedFragmentDTO> GetImagedFragment(uint? userId, uint scrollVersionId, string fragmentId);
    }
    public class ImagedFragmentsService : IImagedFragmentsService
    {
        IImagedObjectRepository _repo;
        IImageRepository _imageRepo;
        IImageService _imageService;

        public ImagedFragmentsService(IImagedObjectRepository repo, IImageRepository imageRepo, IImageService imageService)
        {
            _repo = repo;
            _imageRepo = imageRepo;
            _imageService = imageService;
        }

        async public Task<ImagedObjectListDTO> GetImagedObjects(uint? userId, uint scrollVersionId)
        {
            var imagedFragments = await _repo.GetImagedObjects(userId, scrollVersionId, null);

            if (imagedFragments == null)
            {
                throw new NotFoundException((uint)scrollVersionId);
            }
            var result = new ImagedObjectListDTO
            {
                result = new List<ImagedFragmentDTO>(),
            };
            var images = await _imageRepo.GetImages(userId, scrollVersionId, null); //send imagedFragment from here 

            var imageDict = new Dictionary<string, List<ImageDTO>>();
            foreach (var image in images)
            {
                var fragmentId = getFragmentId(image);
                if (!imageDict.ContainsKey(fragmentId))
                {
                    imageDict[fragmentId] = new List<ImageDTO>();
                }
                imageDict[fragmentId].Add(_imageService.ImageToDTO(image));
            }

            foreach (var i in imagedFragments)
            {
                if (imageDict.TryGetValue(i.Id, out var imagedFragment))
                    result.result.Add(ImagedFragmentModelToDTO(i, imagedFragment));
            }

            return result;
        }
        private string getFragmentId(DataAccess.Models.Image image)
        {
            return image.Institution + "-" + image.Catlog1 + "-" + image.Catalog2;
        }
        internal static ImagedFragmentDTO ImagedFragmentModelToDTO(DataAccess.Models.ImagedObject model, List<ImageDTO> images)
        {
            var sides = getSides(images);
            return new ImagedFragmentDTO
            {
                id = model.Id.ToString(),
                recto = sides.recto,
                verso = sides.verso
//                recto = getRecto(images),
//                verso = getVerso(images)
            };
        }

        /// <summary>
        /// Get a tuple of DTO's for the recto and verso image set from a flat list of ImageDTO's.
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        // Daniella, it is probably better to do this all in one pass, rather than looping over the
        // images twice, once in getRecto, and again in getVerso.
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

        async public Task<ImagedFragmentDTO> GetImagedFragment(uint? userId, uint scrollVersionId, string fragmentId)
        {
            var images = await _imageRepo.GetImages(userId, scrollVersionId, fragmentId); //send imagedFragment from here 
            var imagedFragments = await _repo.GetImagedObjects(userId, scrollVersionId, fragmentId); //should be onky one!
            var img = new List<ImageDTO>();
            foreach (var image in images)
            {
                img.Add(_imageService.ImageToDTO(image));
                //var fragmentId = getFragmentId(image);
            }
            var result = ImagedFragmentModelToDTO(imagedFragments.First(), img);

            return result;
        }
    }
}
