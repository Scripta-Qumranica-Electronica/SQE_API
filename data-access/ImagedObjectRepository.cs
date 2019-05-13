using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess
{
    public interface IImagedObjectRepository
    {
        Task<IEnumerable<ImagedObject>> GetImagedObjectsAsync(uint? userId, uint editionId, string imagedObjectId);
    }

    public class ImagedObjectRepository : DbConnectionBase, IImagedObjectRepository
    {
        public ImagedObjectRepository(IConfiguration config) : base(config) { }

        public async Task<IEnumerable<ImagedObject>> GetImagedObjectsAsync(uint? userId, uint editionId, string imagedObjectId)
        {
            var sql = ImagedFragmentQueries.GetQuery(imagedObjectId!=null);

            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ImagedFragmentQueries.Result>(sql, new
                {
                    UserId = userId ?? 1, // @UserId is not expanded if userId is null
                    EditionId = editionId,
                    ObjectId = imagedObjectId,
                });

                var models = results.Select(result => CreateImagedObject(result));
                return models;
            }
      
        }

        private ImagedObject CreateImagedObject(ImagedFragmentQueries.Result imagedFragment) 
        {
            var model = new ImagedObject
            {
                Id = imagedFragment.object_id,
                Institution = imagedFragment.institution,
                Catalog1 = imagedFragment.catalog_1,
                Catalog2 = imagedFragment.catalog_2,     
            };
            return model;
        }
    }
}
