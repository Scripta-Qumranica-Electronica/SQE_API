using Dapper;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeApi.DataAccess.Models;
using SQE.SqeApi.DataAccess.Queries;

namespace SQE.SqeApi.DataAccess
{
    public interface IImageRepository
    {
        Task<IEnumerable<Image>> GetImagesAsync(uint? userId, uint editionId, string imagedObjectId);
        Task<IEnumerable<ImageInstitution>> ListImageInstitutionsAsync();
    }

    public class ImageRepository : DbConnectionBase, IImageRepository
    {
        public ImageRepository(IConfiguration config) : base(config) { }

        public async Task<IEnumerable<Image>> GetImagesAsync(uint? userId, uint editionId, string imagedObjectId)
        {
            var sql = ImageQueries.GetImageQuery(!string.IsNullOrEmpty(imagedObjectId));

            using (var connection = OpenConnection())
            {

                var results = await connection.QueryAsync<ImageQueries.Result>(sql, new
                {
                    UserId = userId ?? 0, // @UserId is not expanded if userId is null
                    EditionId = editionId,
                    ObjectId = imagedObjectId,
                });

                var models = results.Select(result => CreateImage(result));
                return models;
            }
        }

        private Image CreateImage(ImageQueries.Result image)
        {
            var model = new Image
            {
                URL = image.proxy + image.url + image.filename,
                Id = image.sqe_image_id,
                Side = image.side == 0 ? "recto" : "verso",
                Type = image.img_type,
                WaveLength = GetWave(image.wave_start, image.wave_end),
                Institution = image.institution,
                Catalog1 = image.catalog_1,
                Catalog2 = image.catalog_2,
                ImageCatalogId = image.image_catalog_id,
                ObjectId = image.object_id,
                Master = image.master,
                RegionInMaster = image.region_on_image1,
                RegionOfMaster = image.region_on_image2,
                TransformMatrix = image.transform_matrix
            };
            return model;
        }

        private string [] GetWave(ushort start, ushort end)
        {
            var str = new string[2];
            str[0] = start.ToString();
            str[1] = end.ToString();
            return str;
        }

        public async Task<IEnumerable<ImageInstitution>> ListImageInstitutionsAsync()
        {
            var sql = ImageInstitutionQuery.GetQuery();

            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ImageInstitutionQuery.Result>(sql);

                var models = results.Select(CreateInstitution);
                return models;
            }
        }

        private ImageInstitution CreateInstitution(ImageInstitutionQuery.Result result)
        {
            var model = new ImageInstitution
            {
                Name = result.Institution,
            };

            return model;
        }
    }
}
