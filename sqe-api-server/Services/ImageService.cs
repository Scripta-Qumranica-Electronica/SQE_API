using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
    public interface IImageService
    {
        ImageDTO ImageToDTO(Image model);
        Task<ImageInstitutionListDTO> GetImageInstitutionsAsync();
        Task<List<ImagedObjectTextFragmentMatchDTO>> GetImageTextFragmentsAsync(string imagedObjectId);
    }

    public class ImageService : IImageService
    {
        private readonly IHubContext<MainHub, ISQEClient> _hubContext;
        private readonly IImageRepository _imageRepo;

        public ImageService(IImageRepository imageRepo, IHubContext<MainHub, ISQEClient> hubContext)
        {
            _imageRepo = imageRepo;
            _hubContext = hubContext;
        }

        public ImageDTO ImageToDTO(Image model)
        {
            return new ImageDTO
            {
                id = model.Id,
                url = model.URL,
                imageToImageMapEditorId = model.ImageToImageMapEditorId,
                waveLength = model.WaveLength,
                type = GetType(model.Type),
                regionInMasterImage = model.RegionInMaster,
                regionInImage = model.RegionOfMaster,
                lightingDirection = GetLightingDirection(model.Type),
                lightingType = GetLightingType(model.Type),
                side = model.Side,
                transformToMaster = model.TransformMatrix,
                catalogNumber = model.ImageCatalogId,
                master = model.Master
            };
        }

        public async Task<ImageInstitutionListDTO> GetImageInstitutionsAsync()
        {
            var institutions = await _imageRepo.ListImageInstitutionsAsync();

            return ImageInstitutionsToDTO(institutions);
        }

        public async Task<List<ImagedObjectTextFragmentMatchDTO>> GetImageTextFragmentsAsync(string imagedObjectId)
        {
            imagedObjectId = System.Web.HttpUtility.UrlDecode(imagedObjectId);
            var textFragments = await _imageRepo.GetImageTextFragmentsAsync(imagedObjectId);
            return textFragments.Select(x => new ImagedObjectTextFragmentMatchDTO(
                x.EditionId, x.ManuscriptName, x.TextFragmentId, x.TextFragmentName, x.Side == 0 ? "recto" : "verso")
            ).ToList();
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
            if (type == 2
                || type == 3) return ImageDTO.Lighting.raking;
            return ImageDTO.Lighting.direct; // need to check..
        }

        private static ImageDTO.Direction GetLightingDirection(byte type)
        {
            if (type == 2) return ImageDTO.Direction.left;
            if (type == 3) return ImageDTO.Direction.right;
            return ImageDTO.Direction.top; // need to check..
        }

        private static ImageInstitutionListDTO ImageInstitutionsToDTO(IEnumerable<ImageInstitution> imageInstitutions)
        {
            return new ImageInstitutionListDTO(
                imageInstitutions.Select(
                        imageInstitution => new ImageInstitutionDTO(imageInstitution.Name)
                    )
                    .ToList()
            );
        }
    }
}