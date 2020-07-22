using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess
{
    public interface ICatalogueRepository
    {
        Task<IEnumerable<CatalogueMatch>> GetTextFragmentMatchesForImagedObjectAsync(string imagedObjectId);
        Task<IEnumerable<CatalogueMatch>> GetImagedObjectMatchesForTextFragmentAsync(uint textFragmentId);
        Task<IEnumerable<CatalogueMatch>> GetImagedObjectAndTextFragmentMatchesForManuscriptAsync(uint manuscriptId);
        Task<IEnumerable<CatalogueMatch>> GetImagedObjectAndTextFragmentMatchesForEditionAsync(uint editionId);
    }

    public class CatalogueRepository : DbConnectionBase, ICatalogueRepository
    {
        public CatalogueRepository(IConfiguration config) : base(config)
        {
        }

        public async Task<IEnumerable<CatalogueMatch>> GetTextFragmentMatchesForImagedObjectAsync(string imagedObjectId)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<CatalogueMatch>(
                    CatalogueQuery.GetQuery(CatalogueQueryFilterType.ImagedObject), new
                    {
                        ImagedObjectId = imagedObjectId
                    });
            }
        }

        public async Task<IEnumerable<CatalogueMatch>> GetImagedObjectMatchesForTextFragmentAsync(uint textFragmentId)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<CatalogueMatch>(
                    CatalogueQuery.GetQuery(CatalogueQueryFilterType.TextFragment), new
                    {
                        TextFragmentId = textFragmentId
                    });
            }
        }

        public async Task<IEnumerable<CatalogueMatch>> GetImagedObjectAndTextFragmentMatchesForManuscriptAsync(uint manuscriptId)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<CatalogueMatch>(
                    CatalogueQuery.GetQuery(CatalogueQueryFilterType.Manuscript), new
                    {
                        ManuscriptId = manuscriptId
                    });
            }
        }

        public async Task<IEnumerable<CatalogueMatch>> GetImagedObjectAndTextFragmentMatchesForEditionAsync(uint editionId)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<CatalogueMatch>(
                    CatalogueQuery.GetQuery(CatalogueQueryFilterType.Edition), new
                    {
                        EditionId = editionId
                    });
            }
        }
    }
}