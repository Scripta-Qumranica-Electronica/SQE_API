using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess
{
    public interface IImagedObjectRepository
    {
        Task<IEnumerable<ImagedObject>> GetImagedObjects(uint? userId, uint editionId, string imagedObjectId);
    }

    public class ImagedObjectRepository : DbConnectionBase, IImagedObjectRepository
    {
        public ImagedObjectRepository(IConfiguration config) : base(config) { }

        public async Task<IEnumerable<ImagedObject>> GetImagedObjects(uint? userId, uint editionId, string imagedObjectId)
        {
            var fragment = ImagedObject.FromId(imagedObjectId);
            var sql = ImagedFragemntQueries.GetFragmentsQuery(imagedObjectId!=null);

            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ImagedFragemntQueries.Result>(sql, new
                {
                    UserId = userId ?? 1, // @UserId is not expanded if userId is null
                    EditionId = editionId,
                    Catalog1 = fragment?.Catalog1,
                    Catalog2 = fragment?.Catalog2,
                    Institution = fragment?.Institution

                });

                var models = results.Select(result => CreateImagedObject(result));
                return models;

                /**var imagedFragment = results.FirstOrDefault();
                if (imagedFragment == null)
                    return null;

                return CreateImagedObject(imagedFragment);**/
            }
      
        }

        private ImagedObject CreateImagedObject(ImagedFragemntQueries.Result imagedFragement) 
        {
            var model = new ImagedObject
            {
                Institution = imagedFragement.institution,
                Catalog1 = imagedFragement.catalog_1,
                Catalog2 = imagedFragement.catalog_2,     
            };
            return model;
        }
    }
}
