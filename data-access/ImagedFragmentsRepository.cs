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
    public interface IImagedFragmentsRepository
    {
        Task<IEnumerable<ImagedFragment>> GetImagedFragments(uint? userId, uint scrollVersionId, string fragmentId);
    }

    public class ImagedFragmentsRepository : DBConnectionBase, IImagedFragmentsRepository
    {
        public ImagedFragmentsRepository(IConfiguration config) : base(config) { }

        public async Task<IEnumerable<ImagedFragment>> GetImagedFragments(uint? userId, uint scrollVersionId, string fragmentId)
        {
            var fragment = ImagedFragment.FromId(fragmentId);
            var sql = ImagedFragemntQueries.GetFragmentsQuery(fragmentId!=null);

            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ImagedFragemntQueries.Result>(sql, new
                {
                    UserId = userId ?? 1, // @UserId is not expanded if userId is null
                    ScrollVersionId = scrollVersionId,
                    Catalog1 = fragment?.Catalog1,
                    Catalog2 = fragment?.Catalog2,
                    Institution = fragment?.Institution

                });

                var models = results.Select(result => CreateImagedFragment(result));
                return models;

                /**var imagedFragment = results.FirstOrDefault();
                if (imagedFragment == null)
                    return null;

                return CreateImagedFragment(imagedFragment);**/
            }
      
        }

        private ImagedFragment CreateImagedFragment(ImagedFragemntQueries.Result imagedFragement) 
        {
            var model = new ImagedFragment
            {
                Institution = imagedFragement.institution,
                Catalog1 = imagedFragement.catalog_1,
                Catalog2 = imagedFragement.catalog_2,     
            };
            return model;
        }
    }
}
