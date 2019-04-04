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
        Task<ImageListDTO> GetImages(uint? userId, uint scrollVersionId, string fragmentId = null);
        ImageDTO ImageToDTO(DataAccess.Models.Image model);
	Task<ImageGroupListDTO> GetImageAsync(uint? userId, List<uint> scrollVersionId);
        Task<ImageInstitutionListDTO> GetImageInstitutionsAsync();
    
    }
    public class ImageService : IImageService
    {
        IImageRepository _repo;

        public ImageService(IImageRepository repo)
        {
            _repo = repo;
        }
        async public Task<ImageListDTO> GetImages(uint? userId, uint scrollVersionId, string fragmentId = null)
        {
            var images = await _repo.GetImages(userId, scrollVersionId, fragmentId);

            if (images == null)
            {
                throw new NotFoundException((uint)scrollVersionId);
            }
            var result = new ImageListDTO
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
        public ImageDTO.lighting GetLightingType(byte type)
        {
            if (type ==2 || type == 3)
            {
                return ImageDTO.lighting.raking;
            }
            return ImageDTO.lighting.direct; // need to check..
        }

        public ImageDTO.direction GetLigthingDirection(byte type)
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

        public async Task<ImageGroupListDTO> GetImageAsync(uint? userId, List<uint> scrollVersionId)
        {
            var images = await _repo.ListImages(userId, scrollVersionId);

            return ImageToDTO(images);
        }

        internal static ImageGroupListDTO ImageToDTO(IEnumerable<DataAccess.Models.ImageGroup> imageGroups)
        {
            return new ImageGroupListDTO(imageGroups.Select(imageGroup =>
            {
                return new ImageGroupDTO(imageGroup.Id, imageGroup.Institution, imageGroup.CatalogNumber1, imageGroup.CatalogNumber2, imageGroup.CatalogSide, new List<ImageDTO>());
            }).ToList());
        }

        public async Task<ImageInstitutionListDTO> GetImageInstitutionsAsync()
        {
            var institutions = await _repo.ListImageInstitutions();

            return ImageInstitutionsToDTO(institutions);
        }

        internal static ImageInstitutionListDTO ImageInstitutionsToDTO(IEnumerable<DataAccess.Models.ImageInstitution> imageInstitutions)
        {
            return new ImageInstitutionListDTO(imageInstitutions.Select(imageInstitution =>
            {
                return new ImageInstitutionDTO(imageInstitution.Name);
            }).ToList());
        }
    }
}
