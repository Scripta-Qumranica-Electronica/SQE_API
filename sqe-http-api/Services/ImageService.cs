using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.Server.DTOs;


namespace SQE.SqeHttpApi.Server.Helpers
{
    public interface IImageService
    {
        Task<ImageListDTO> GetImagesAsync(uint? userId, uint scrollVersionId, string fragmentId = null);
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
        public async Task<ImageListDTO> GetImagesAsync(uint? userId, uint scrollVersionId, string fragmentId = null)
        {
            var images = await _repo.GetImagesAsync(userId, scrollVersionId, fragmentId);

            if (images == null)
            {
                throw new NotFoundException((uint)scrollVersionId);
            }
            var result = new ImageListDTO
            {
                images = new List<ImageDTO>(),
            };


            foreach (var i in images)
            {
                result.images.Add(ImageToDTO(i));
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
                lightingDirection = GetLightingDirection(model.Type),
                lightingType = GetLightingType(model.Type),
                side = model.Side,
                transformToMaster = model.TransformMatrix,
                catalogNumber = model.ImageCatalogId,
                master = model.Master
            };
        }
        private static string GetType(byte type)
        {
            if (type == 0)
                return "color";
            if (type == 1)
                return "infrared";
            if (type == 2)
                return "rakingLeft";
            if (type == 3)
                return "rakingRight";
            return null;

        }

        private static ImageDTO.Lighting GetLightingType(byte type)
        {
            if (type ==2 || type == 3)
            {
                return ImageDTO.Lighting.raking;
            }
            return ImageDTO.Lighting.direct; // need to check..
        }

        private static ImageDTO.Direction GetLightingDirection(byte type)
        {
            if (type == 2)
            {
                return ImageDTO.Direction.left;
            }
            if (type == 3)
            {
                return ImageDTO.Direction.right;
            }
            return ImageDTO.Direction.top; // need to check..
        }

        public async Task<ImageGroupListDTO> GetImageAsync(uint? userId, List<uint> scrollVersionId)
        {
            var images = await _repo.ListImagesAsync(userId, scrollVersionId);

            return ImageToDTO(images);
        }

        private static ImageGroupListDTO ImageToDTO(IEnumerable<DataAccess.Models.ImageGroup> imageGroups)
        {
            return new ImageGroupListDTO(imageGroups.Select(
                imageGroup => new ImageGroupDTO(
                    imageGroup.Id, 
                    imageGroup.Institution, 
                    imageGroup.CatalogNumber1, 
                    imageGroup.CatalogNumber2, 
                    imageGroup.CatalogSide, 
                    new List<ImageDTO>()
                    )
                ).ToList()
            );
        }

        public async Task<ImageInstitutionListDTO> GetImageInstitutionsAsync()
        {
            var institutions = await _repo.ListImageInstitutionsAsync();

            return ImageInstitutionsToDTO(institutions);
        }

        private static ImageInstitutionListDTO ImageInstitutionsToDTO(IEnumerable<DataAccess.Models.ImageInstitution> imageInstitutions)
        {
            return new ImageInstitutionListDTO(imageInstitutions.Select(
                imageInstitution => new ImageInstitutionDTO(imageInstitution.Name)
                ).ToList()
            );
        }
    }
}
