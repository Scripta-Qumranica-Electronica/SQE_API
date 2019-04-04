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
        Task<ImageList> GetImages(uint? userId, uint scrollVersionId, string fragmentId = null);
        Image ImageToDTO(DataAccess.Models.Image model);
	Task<ImageGroupList> GetImageAsync(uint? userId, List<uint> scrollVersionId);
        Task<ImageInstitutionList> GetImageInstitutionsAsync();
    
    }
    public class ImageService : IImageService
    {
        IImageRepository _repo;

        public ImageService(IImageRepository repo)
        {
            _repo = repo;
        }
        async public Task<ImageList> GetImages(uint? userId, uint scrollVersionId, string fragmentId = null)
        {
            var images = await _repo.GetImages(userId, scrollVersionId, fragmentId);

            if (images == null)
            {
                throw new NotFoundException((uint)scrollVersionId);
            }
            var result = new ImageList
            {
                ImagesList = new List<ImageDTO>(),
            };


            foreach (var i in images)
            {
                result.ImagesList.Add(ImageToDTO(i));
            }

            return result;
        }

        public ImageDTO ImageToDTO(DataAccess.Models.Image model)
        {

            return new ImageDTO
            {
                url = model.URL,
                waveLength = model.WaveLength,
                type = GetType(model.Type),
                regionInMaster = null,
                regionOfMaster = null,
                lightingDirection = GetLigthingDirection(model.Type),
                lightingType = GetLightingType(model.Type),
                side = model.Side,
                transformToMaster = model.TransformMatrix,
                catalog_number = model.ImageCatalogId,
                master = model.Master
            };
        }
        private string GetType(byte type)
        {
            if (type == 0)
                return "color";
            if (type == 1)
                return "infrared";
            if (type == 2)
                return "raking-left";
            if (type == 3)
                return "raking-right";
            return null;

        }
        public Image.lighting GetLightingType(byte type)
        {
            if (type ==2 || type == 3)
            {
                return Image.lighting.raking;
            }
            return Image.lighting.direct; // need to check..
        }

        public Image.direction GetLigthingDirection(byte type)
        {
            if (type == 2)
            {
                return ImageDTO.direction.left;
            }
            if (type == 3)
            {
                return ImageDTO.direction.right;
            }
            return ImageDTO.direction.top; // need to check..
        }

        public async Task<ImageGroupList> GetImageAsync(uint? userId, List<uint> scrollVersionId)
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
