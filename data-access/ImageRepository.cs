using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using SQE.Backend.DataAccess.Queries;
using System.Linq;
using SQE.Backend.DataAccess.Models;
using Microsoft.Extensions.Configuration;
using System.Transactions;
using System.Data.SqlClient;
using static SQE.Backend.DataAccess.Queries.ImageGroupQuery;
using static SQE.Backend.DataAccess.Queries.UserImageGroupQuery;
using MySql.Data.MySqlClient;

namespace SQE.Backend.DataAccess
{
    public interface IImageRepository
    {
        Task<IEnumerable<ImageGroup>> ListImages(int? userId, List<int> scrollIds);
        Task<IEnumerable<ImageInstitution>> ListImageInstitutions();
    }

    public class ImageRepository : DBConnectionBase, IImageRepository
    {
        public ImageRepository(IConfiguration config) : base(config) { }

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


             //We can't expand the ScrollIds parameters with MySql, as it is a list. We need to expand ourselves.
             //Since ids is a list of integers, SQL injection is quite impossible.
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
                Name = result.institution,
            };

            return model;
        }
    }
}




