using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeApi.DataAccess;
using SQE.SqeApi.Server.DTOs;


namespace SQE.SqeApi.Server.Services
{
    public interface IImageService
    {
        ImageDTO ImageToDTO(DataAccess.Models.Image model);
        Task<ImageInstitutionListDTO> GetImageInstitutionsAsync();
    
    }
    public class ImageService : IImageService
    {
        IImageRepository _repo;

        public ImageService(IImageRepository repo)
        {
            _repo = repo;
        }

        public ImageDTO ImageToDTO(DataAccess.Models.Image model)
        {

            return new ImageDTO
            {
                id = model.Id,
                url = model.URL,
                waveLength = model.WaveLength,
                type = GetType(model.Type),
                regionInMaster = new PolygonDTO()
                {
                    mask = model.RegionInMaster,
                    transformMatrix = null
                },
                regionOfMaster = new PolygonDTO()
                {
                    mask = model.RegionOfMaster,
                    transformMatrix = null
                },
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
