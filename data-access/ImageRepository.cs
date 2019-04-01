using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.Backend.DataAccess.Models;
using SQE.Backend.DataAccess.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.DataAccess
{
    public interface IImageRepository
    {
        Task<IEnumerable<Image>> GetImages(int? userId, int scrollVersionId, string fragmentId);
        Task<IEnumerable<ImageGroup>> ListImages(int? userId, List<int> scrollIds);
        Task<IEnumerable<ImageInstitution>> ListImageInstitutions();
    }

    public class ImageRepository : DBConnectionBase, IImageRepository
    {
        public ImageRepository(IConfiguration config) : base(config) { }

        public async Task<IEnumerable<Image>> GetImages(int? userId, int scrollVersionId, string fragmentId)
        {
            var fragment = ImagedFragment.FromId(fragmentId);

            var sql = ImageQueries.GetImageQuery(fragment != null);

            using (var connection = OpenConnection())
            {

                var results = await connection.QueryAsync<ImageQueries.Result>(sql, new
                {
                    UserId = userId ?? -1, // @UserId is not expanded if userId is null
                    ScrollVersionId = scrollVersionId,
                    fragment?.Catalog1,
                    fragment?.Catalog2,
                    fragment?.Institution,
                });

                var models = results.Select(result => CreateImage(result));
                return models;
            }
        }

        private Image CreateImage(ImageQueries.Result image)
        {
            var model = new Image
            {
                URL = image.proxy + image.url,
                RegionInMaster = null,
                RegionOfMaster = null,
                Side = image.side == 0 ? "recto" : "verso",
                Type = image.img_type,
                WaveLength = GetWave(image.wave_start, image.wave_end),
                TransformMatrix = null,
                Institution = image.institution,
                Catlog1 = image.catalog_1,
                Catalog2 = image.catalog_2,
                ImageCatalogId = image.image_catalog_id,
                Master = image.master
            };
            return model;
        }

        /**private string GetType(int type)
        {
            if (type == 0)
                return "color";
            if (type == 1)
                return "infrared";
            return type;
        }**/

        private string [] GetWave(int start, int end)
        {
            string[] str = new string[2];
            str[0] = start.ToString();
            str[1] = end.ToString();
            return str;
        }

        public async Task<IEnumerable<ImageGroup>> ListImages(int? userId, List<int> scrollIds) //
        {
            string sql;
            if (userId.HasValue)
            {
                sql = UserImageGroupQuery.GetQuery(scrollIds.Count > 0);
            }
            else
            {
                sql = ImageGroupQuery.GetQuery(scrollIds.Count > 0);
            }

            if (scrollIds != null)
            {
                sql = sql.Replace("@ScrollVersionIds", $"({string.Join(",", scrollIds)})");
            }

            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ImageGroupQuery.Result>(sql, new
                {
                    UserId = userId ?? -1, // @UserId is not expanded if userId is null
                });

                var models = results.Select(result => CreateImage(result));
                return models;
            }
        }

        private ImageGroup CreateImage(ImageGroupQuery.Result result)
        {
            var model = new ImageGroup
                {
                    Id = result.image_catalog_id,
                    Institution = result.institution,
                    CatalogNumber1 = result.catalog_number_1,
                    CatalogNumber2 = result.catalog_number_2,
                    CatalogSide = result.catalog_side,
                    Images = new List<Image>(),
                };

            return model;
        }

        public async Task<IEnumerable<ImageInstitution>> ListImageInstitutions()
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
