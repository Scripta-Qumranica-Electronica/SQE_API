using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using SQE.Backend.DataAccess.Models;
using SQE.Backend.DataAccess.Queries;

namespace SQE.Backend.DataAccess
{
    public interface IImagedFragmentsRepository
    {
        Task<IEnumerable<ImagedFragment>> GetImagedFragments(int? userId, int scrollVersionId);
    }

    public class ImagedFragmentsRepository : DBConnectionBase, IImagedFragmentsRepository
    {
        public ImagedFragmentsRepository(IConfiguration config) : base(config) { }

        public async Task<IEnumerable<ImagedFragment>> GetImagedFragments(int? userId, int scrollVersionId)
        {
            var sql = ImagedFragemntQueries.GetFragmentsQuery();

            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ImagedFragemntQueries.Result>(sql, new
                {
                    UserId = userId ?? 1, // @UserId is not expanded if userId is null
                    ScrollVersionId = scrollVersionId

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
