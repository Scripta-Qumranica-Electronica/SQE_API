using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.Backend.Server.DTOs;
using SQE.Backend.DataAccess;

namespace SQE.Backend.Server.Services
{
    public interface IImagedFragmentsService
    {
        Task<ImagedFragmentListDTO> GetImagedFragments(int? userId, int scrollId);
        Task<ImagedFragmentDTO> GetImagedFragment(int? userId, int scrollVersionId, string fragmentId);
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

        async public Task<ImagedFragmentListDTO> GetImagedFragments(int? userId, int scrollVersionId)
        {
            var imagedFragments = await _repo.GetImagedFragments(userId, scrollVersionId, null);

            if (imagedFragments == null)
            {
                throw new NotFoundException(scrollVersionId);
            }
            var result = new ImagedFragmentListDTO
            {
                result = new List<ImagedFragmentDTO>(),
            };
            var images = await _imageRepo.GetImages(userId, scrollVersionId, null); //send imagedFragment from here 

            Dictionary<string, List<ImageDTO>> imageDict = new Dictionary<string, List<ImageDTO>>();
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

            return new ImagedFragmentDTO
            {
                id = model.Id.ToString(),
                recto = getRecto(images),
                verso = getVerso(images)
            };
        }
        private static ImageStackDTO getRecto(List<ImageDTO> images)
        {
            List<ImageDTO> img = new List<ImageDTO>();
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
            int masterIndex = img.FindIndex(i => i.master == 1);
            int catalog_id = img[0].catalog_number;
            return new ImageStackDTO
            {
                id = catalog_id,
                masterIndex = masterIndex,
                images = img
            };
        }

        private static ImageStackDTO getVerso(List<ImageDTO> images)
        {
            List<ImageDTO> img = new List<ImageDTO>();
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
            int masterIndex = img.FindIndex(i => i.master == 1);
            int catalog_id = img[0].catalog_number;
            return new ImageStackDTO
            {
                id = catalog_id,
                masterIndex = masterIndex,
                images = img
            };
        }

        async public Task<ImagedFragmentDTO> GetImagedFragment(int? userId, int scrollVersionId, string fragmentId)
        {
            var images = await _imageRepo.GetImages(userId, scrollVersionId, fragmentId); //send imagedFragment from here 
            var imagedFragments = await _repo.GetImagedFragments(userId, scrollVersionId, fragmentId); //should be onky one!
            List<ImageDTO> img = new List<ImageDTO>();
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
