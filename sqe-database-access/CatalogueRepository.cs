using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
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

        Task CreateNewImagedObjectTextFragmentMatchAsync(uint userId, string imagedObjectId,
            byte imageSide, uint textFragmentId, uint editionId,
            string canonicalEditionName, string canonicalEditionVolume, string canonicalEditionLoc1,
            string canonicalEditionLoc2, byte canonicalEditionSide, string comment);

        Task ConfirmImagedObjectTextFragmentMatchAsync(uint userId, uint editionCatalogToTextFragmentId,
            bool confirm);
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

        public async Task<IEnumerable<CatalogueMatch>> GetImagedObjectAndTextFragmentMatchesForManuscriptAsync(
            uint manuscriptId)
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

        public async Task<IEnumerable<CatalogueMatch>> GetImagedObjectAndTextFragmentMatchesForEditionAsync(
            uint editionId)
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

        public async Task CreateNewImagedObjectTextFragmentMatchAsync(uint userId, string imagedObjectId,
            byte imageSide, uint textFragmentId, uint editionId,
            string canonicalEditionName, string canonicalEditionVolume, string canonicalEditionLoc1,
            string canonicalEditionLoc2, byte canonicalEditionSide, string comment)
        {
            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            using (var connection = OpenConnection())
            {
                var existingEditionCats = (await connection.QueryAsync<EditionCatalogueEntry>(
                    EditionCatalogueQuery.GetQuery(false, false, string.IsNullOrEmpty(canonicalEditionName),
                        string.IsNullOrEmpty(canonicalEditionVolume), string.IsNullOrEmpty(canonicalEditionLoc1),
                        string.IsNullOrEmpty(canonicalEditionLoc2), true,
                        string.IsNullOrEmpty(comment), false, true),
                    new
                    {
                        EditionName = canonicalEditionName,
                        EditionVolume = canonicalEditionVolume,
                        EditionLocation1 = canonicalEditionLoc1,
                        EditionLocation2 = canonicalEditionLoc2,
                        EditionSide = canonicalEditionSide,
                        Comment = comment,
                        EditionId = editionId,
                        UserId = userId
                    })).AsList();
                var editionCatalogueId = existingEditionCats.Any()
                    ? existingEditionCats.First().IaaEditionCatalogId
                    : (uint?)null;
                if (!editionCatalogueId.HasValue)
                {
                    var writeEc = await connection.ExecuteAsync(EditionCatalogueInsertQuery.GetQuery, new
                    {
                        EditionName = canonicalEditionName,
                        EditionVolume = canonicalEditionVolume,
                        EditionLocation1 = canonicalEditionLoc1,
                        EditionLocation2 = canonicalEditionLoc2,
                        EditionSide = canonicalEditionSide,
                        Comment = comment,
                        EditionId = editionId,
                        UserId = userId
                    });
                    if (writeEc != 1)
                        throw new StandardExceptions.DataNotWrittenException("Create Edition Catalogue Entry");
                    editionCatalogueId = await connection.QuerySingleAsync<uint>("SELECT LAST_INSERT_ID()");

                    writeEc = await connection.ExecuteAsync(EditionCatalogueAuthorInsertQuery.GetQuery, new
                    {
                        IaaEditionCatalogId = editionCatalogueId,
                        UserId = userId
                    });
                    if (writeEc != 1)
                        throw new StandardExceptions.DataNotWrittenException("Create Edition Catalogue Author Entry");
                }

                await connection.ExecuteAsync(EditionCatalogTextFragmentMatchInsertQuery.GetQuery, new
                {
                    IaaEditionCatalogId = editionCatalogueId,
                    TextFragmentId = textFragmentId,
                    UserId = userId
                });
                var textFragmentImagedObjectMatchId =
                    await connection.QuerySingleAsync<uint>("SELECT LAST_INSERT_ID()");

                // If no record was inserted, then it already exists.  So collect it from the DB
                if (textFragmentImagedObjectMatchId == 0)
                    textFragmentImagedObjectMatchId = await connection.QuerySingleAsync<uint>(
                        @"SELECT iaa_edition_catalog_to_text_fragment_id
FROM iaa_edition_catalog_to_text_fragment
WHERE text_fragment_id = @TextFragmentId
    AND iaa_edition_catalog_id = @IaaEditionCatalogId", new
                        {
                            IaaEditionCatalogId = editionCatalogueId,
                            TextFragmentId = textFragmentId
                        });


                await connection.ExecuteAsync(EditionCatalogImageCatalogMatchInsertQuery.GetQuery, new
                {
                    IaaEditionCatalogId = editionCatalogueId,
                    ImagedObjectId = imagedObjectId,
                    Side = imageSide,
                    UserId = userId
                });

                await connection.ExecuteAsync(EditionCatalogTextFragmentMatchConfirmationInsertQuery.GetQuery, new
                {
                    IaaEditionCatalogToTextFragmentId = textFragmentImagedObjectMatchId,
                    Confirmed = (bool?)null,
                    UserId = userId
                });

                transactionScope.Complete();
            }
        }

        /// <summary>
        ///     Confirm or reject a edition catalog to text fragment match.
        /// </summary>
        /// <param name="userId">User's unique id</param>
        /// <param name="editionCatalogToTextFragmentId">Unique id of the record to confirm or reject</param>
        /// <param name="confirm">Boolean whether the match is confirmed (true) or rejected (false)</param>
        /// <returns></returns>
        public async Task ConfirmImagedObjectTextFragmentMatchAsync(uint userId, uint editionCatalogToTextFragmentId,
            bool confirm)
        {
            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            using (var connection = OpenConnection())
            {
                // Check if match exists
                var existingMatches =
                    (await GetImagedObjectTextFragmentMatchById(editionCatalogToTextFragmentId)).ToList();
                if (!existingMatches.Any())
                    throw new StandardExceptions.DataNotFoundException("match id", editionCatalogToTextFragmentId,
                        "imaged object text fragment matches");

                if (existingMatches.OrderBy(x => x.MatchConfirmationDate).Last().Confirmed != confirm)
                    await connection.ExecuteAsync(EditionCatalogTextFragmentMatchConfirmationInsertQuery.GetQuery, new
                    {
                        IaaEditionCatalogToTextFragmentId = editionCatalogToTextFragmentId,
                        UserId = userId,
                        Confirmed = confirm
                    });

                transactionScope.Complete();
            }
        }

        // /// <summary>
        // ///     Confirm or reject a edition catalog to text fragment match.
        // /// </summary>
        // /// <param name="userId">User's unique id</param>
        // /// <param name="editionCatalogToTextFragmentId">Unique id of the record to confirm or reject</param>
        // /// <param name="confirm">Boolean whether the match is confirmed (true) or rejected (false)</param>
        // /// <returns></returns>
        // private async Task ChangeImagedObjectTextFragmentMatchConfirmationAsync(uint userId, uint editionCatalogToTextFragmentId,
        //     bool? confirm)
        // {
        //     using (var connection = OpenConnection())
        //     {
        //         await connection.ExecuteAsync(EditionCatalogTextFragmentMatchConfirmationUpdateQuery.GetQuery, new
        //         {
        //             IaaEditionCatalogToTextFragmentId = editionCatalogToTextFragmentId,
        //             UserId = userId,
        //             Confirmed = confirm
        //         });
        //     }
        // }

        private async Task<IEnumerable<CatalogueMatch>> GetImagedObjectTextFragmentMatchById(
            uint imagedObjectTextFragmentMatchId)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<CatalogueMatch>(
                    CatalogueQuery.GetQuery(CatalogueQueryFilterType.Match), new
                    {
                        MatchId = imagedObjectTextFragmentMatchId
                    });
            }
        }
    }
}