using SQE.Backend.Server.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.Backend.DataAccess;


namespace SQE.Backend.Server.Services
{
    public interface IImageService
    {
        Task<ImageList> GetImages(int? userId, int scrollVersionId, string fragmentId = null);
        Image ImageToDTO(DataAccess.Models.Image model);

    }
    public class ImageService : IImageService
    {
        IImageRepository _repo;

        public ImageService(IImageRepository repo)
        {
            _repo = repo;
        }
        async public Task<ImageList> GetImages(int? userId, int scrollVersionId, string fragmentId = null)
        {
            var images = await _repo.GetImages(userId, scrollVersionId, fragmentId);

            if (images == null)
            {
                throw new NotFoundException(scrollVersionId);
            }
            var result = new ImageList
            {
                ImagesList = new List<Image>(),
            };


            foreach (var i in images)
            {
                result.ImagesList.Add(ImageToDTO(i));
            }

            return result;
        }

        public Image ImageToDTO(DataAccess.Models.Image model)
        {

            return new Image
            {
                url = model.URL,
                waveLength = model.WaveLength,
                type = GetType(model.Type),
                regionInMaster = null,
                regionOfMaster = null,
                ligthingDirection = GetLigthingDirection(model.Type),
                ligthingType = GetLigthingType(model.Type),
                side = model.Side,
                transformToMaster = model.TransformMatrix,
                catalog_number = model.ImageCatalogId,
                master = model.Master
            };
        }
        private string GetType(int type)
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
        public Image.ligthing GetLigthingType(int type)
        {
            if (type ==2 || type == 3)
            {
                return Image.ligthing.raking;
            }
            return Image.ligthing.direct; // need to check..
        }

        public Image.direction GetLigthingDirection(int type)
        {
            if (type == 2)
            {
                return Image.direction.left;
            }
            if (type == 3)
            {
                return Image.direction.right;
            }
            return Image.direction.top; // need to check..
        }
    }
}
