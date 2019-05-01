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

    public interface IArtefactRepository
    {
<<<<<<< HEAD
        Task<IEnumerable<Artefact>> GetArtefact(uint? userId, int? artefactId, uint? scrollVersionId, string fragmentId);

=======
        Task<Artefact> GetArtefact(uint? userId, uint artefactId);
        Task<IEnumerable<ArtefactNamesOfEditionQuery.Result>> GetEditionArtefactNameList(uint? userId, uint editionId);
>>>>>>> 6cc19a4187d1bfe5c70efc913e4adf5b324c1a4e
    }

    public class ArtefactRepository : DbConnectionBase, IArtefactRepository
    {
        public ArtefactRepository(IConfiguration config) : base(config) { }

        public async Task<IEnumerable<Artefact>> GetArtefact(uint? userId, int? artefactId, uint? scrollVersionId, string fragmentId)
        {
            var fragment = ImagedFragment.FromId(fragmentId);

            var sql = ArtefactQueries.GetArtefactQuery(scrollVersionId, artefactId, fragment);

            using (var connection = OpenConnection() as MySqlConnection)
            {
                var results = await connection.QueryAsync<ArtefactQueries.Result>(sql, new
                {
                    UserId = userId ?? 1, // @UserId is not expanded if userId is null
                    ScrollVersionId = scrollVersionId?? null,
                    Id = artefactId?? null,
                    Catalog1 = fragment?.Catalog1,
                    Catalog2 = fragment?.Catalog2,
                    Institution = fragment?.Institution
                });

                var models = results.Select(result => CreateArtefact(result));
                return models;
            }
        }

<<<<<<< HEAD
        private Artefact CreateArtefact(ArtefactQueries.Result artefact)
        {
            var model = new Artefact
            {
                Id = artefact.Id,
                TransformMatrix = artefact.transformMatrix,
                ScrollVersionId = artefact.scrollVersionId,
                Name = artefact.Name,
                Zorder = artefact.zOrder,
                ImagedFragmentId = artefact.institution + "-" + artefact.catalog_number_1 + "-" + artefact.catalog_number_2,
                side = "recto",
                Mask = new Polygon()

            };
            return model;
=======
        public async Task<IEnumerable<ArtefactNamesOfEditionQuery.Result>> GetEditionArtefactNameList(uint? userId, uint editionId)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<ArtefactNamesOfEditionQuery.Result>(ArtefactNamesOfEditionQuery.GetQuery(userId),
                    new
                    {
                        EditionId = editionId,
                        UserId = userId ?? 0
                    });
            }
>>>>>>> 6cc19a4187d1bfe5c70efc913e4adf5b324c1a4e
        }

    }
}
