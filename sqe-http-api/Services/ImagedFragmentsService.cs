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
        Task<ImagedFragmentListDTO> GetImagedFragments(uint? userId, uint scrollVersionId);
        Task<ImagedFragmentDTO> GetImagedFragment(uint? userId, uint scrollVersionId, string fragmentId);
    }
    public class ImagedFragmentsService : IImagedFragmentsService
    {
        IImagedFragmentsRepository _repo;
        IImageRepository _imageRepo;
        IImageService _imageService;

        public ImagedFragmentsService(IImagedFragmentsRepository repo, IImageRepository imageRepo, IImageService imageService)
        {
            _repo = repo;
            _imageRepo = imageRepo;
            _imageService = imageService;
        }

        async public Task<ImagedFragmentListDTO> GetImagedFragments(uint? userId, uint scrollVersionId)
        {
            var imagedFragments = await _repo.GetImagedFragments(userId, scrollVersionId, null);

            if (imagedFragments == null)
            {
                throw new NotFoundException((uint)scrollVersionId);
            }
            var result = new ImagedFragmentListDTO
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
        internal static ImagedFragmentDTO ImagedFragmentModelToDTO(DataAccess.Models.ImagedFragment model, List<ImageDTO> images)
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

        private static (ImageStackDTO recto, ImageStackDTO verso) getSides(List<ImageDTO> images)
        {
            var recto = new List<ImageDTO>();
            var verso = new List<ImageDTO>();
            foreach (var image in images)
            {
                switch (image.side)
                {
                    case "recto":
                        recto.Add(image);
                        break;
                    case "verso":
                        verso.Add(image);
                        break;
                }
            }
            if (recto.Count == 0)
            {
                recto = null;
            }
            if (verso.Count == 0)
            {
                verso = null;
            }
            var rectoMasterIndex = recto.FindIndex(i => i.master);
            var rectoCatalog_id = recto[0].catalog_number;
            var versoMasterIndex = verso.FindIndex(i => i.master);
            var versoCatalog_id = verso[0].catalog_number;
            return (new ImageStackDTO
                    {
                        id = rectoCatalog_id,
                        masterIndex = rectoMasterIndex,
                        images = recto
                    },
                    new ImageStackDTO
                    {
                        id = versoCatalog_id,
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
            var imagedFragments = await _repo.GetImagedFragments(userId, scrollVersionId, fragmentId); //should be onky one!
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
