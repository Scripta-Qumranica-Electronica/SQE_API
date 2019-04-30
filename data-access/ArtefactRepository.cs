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
        Task<IEnumerable<Artefact>> GetArtefact(uint? userId, int? artefactId, uint? scrollVersionId, string fragmentId);

    }

    public class ArtefactRepository : DBConnectionBase, IArtefactRepository
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
        }

    }
}
