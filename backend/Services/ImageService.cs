using SQE.Backend.DataAccess;
using SQE.Backend.Server.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace SQE.Backend.Server.Services
{
    public interface IImageService
    {
        Task<ImageGroupList> GetImageAsync(int? userId, List<int> scrollVersionId);
        Task<ImageInstitutionList> GetImageInstitutionsAsync();
    }

    public class ImageService : IImageService
    {
        private IImageRepository _repo;

        public ImageService(IImageRepository repo)
        {
            _repo = repo;
        }

        public async Task<ImageGroupList> GetImageAsync(int? userId, List<int> scrollVersionId)
        {
            var images = await _repo.ListImages(userId, scrollVersionId);

            return ImageToDTO(images);
        }

        internal static ImageGroupList ImageToDTO(IEnumerable<DataAccess.Models.ImageGroup> imageGroups)
        {
            return new ImageGroupList(imageGroups.Select(imageGroup =>
            {
                return new ImageGroup(imageGroup.Id, imageGroup.Institution, imageGroup.CatalogNumber1, imageGroup.CatalogNumber2, imageGroup.CatalogSide, new List<Image>());
            }).ToList());
        }

        public async Task<ImageInstitutionList> GetImageInstitutionsAsync()
        {
            var institutions = await _repo.ListImageInstitutions();

            return ImageInstitutionsToDTO(institutions);
        }

        internal static ImageInstitutionList ImageInstitutionsToDTO(IEnumerable<DataAccess.Models.ImageInstitution> imageInstitutions)
        {
            return new ImageInstitutionList(imageInstitutions.Select(imageInstitution =>
            {
                return new ImageInstitution(imageInstitution.Name);
            }).ToList());
        }
    }
}
