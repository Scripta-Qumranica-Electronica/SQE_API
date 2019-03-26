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
        Task <ImagedFragmentList> GetImagedFragments(int? userId, int scrollId);
        
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

        async public Task<ImagedFragmentList> GetImagedFragments(int? userId, int scrollVersionId)
        {
            var imagedFragments =  await _repo.GetImagedFragments(userId, scrollVersionId);

            if (imagedFragments == null)
            {
                throw new NotFoundException(scrollVersionId);
            }
            var result = new ImagedFragmentList
            {
                ImagedFragments = new List<ImagedFragment>(),
            };
            var images = await _imageRepo.GetImages(userId, scrollVersionId, null); //send imagedFragment from here 

            Dictionary<string, List<Image>> imageDict = new Dictionary<string, List<Image>>();
            foreach (var image in images)
            {
                var fragmentId = getFragmentId(image);
                if (!imageDict.ContainsKey(fragmentId))
                {
                    imageDict[fragmentId] = new List<Image>();
                }
                imageDict[fragmentId].Add(_imageService.ImageToDTO(image));
            }

            foreach (var i in imagedFragments)
            {
                if (imageDict.TryGetValue(i.Id, out var imagedFragment))
                    result.ImagedFragments.Add(ImagedFragmentModelToDTO(i, imagedFragment));
            }
            
            return result;
        }
        private string getFragmentId(DataAccess.Models.Image image)
        {
            return image.Institution + "-" + image.Catlog1 + "-" + image.Catalog2;
        }
        internal static ImagedFragment ImagedFragmentModelToDTO(DataAccess.Models.ImagedFragment model, List<Image> images)
        {

            return new ImagedFragment
            {
                id = model.Id.ToString(),
                recto = getRecto(images),
                verso = getVerso(images)
            };
        }
        private static ImageStack getRecto(List<Image> images)
        {
            List<Image> img = new List<Image>();
            foreach(var image in images)
            {
                if (image.side == "recto")
                {
                    img.Add(image);
                }
            }
            if(img.Count == 0)
            {
                return null;
            }
            int masterIndex = img.FindIndex(i => i.master == 1);
            int catalog_id = img[0].catalog_number;
            return new ImageStack
            {
                id = catalog_id,
                masterIndex = masterIndex,
                images = img
            };
        }

        private static ImageStack getVerso(List<Image> images)
        {
            List<Image> img = new List<Image>();
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
            return new ImageStack
            {
                id = catalog_id,
                masterIndex = masterIndex,
                images = img
            };
        }
    }
}
