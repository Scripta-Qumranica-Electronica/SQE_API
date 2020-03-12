using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess
{
    public interface ISignInterpretationCommentaryRepository
    {
        Task<SignInterpretationCommentaryData> CreateCommentaryAsync(EditionUserInfo editionUser,
            uint signInterpretationId,
            SignInterpretationCommentaryData newCommentaryData);

        Task<List<SignInterpretationCommentaryData>> CreateCommentariesAsync(EditionUserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationCommentaryData> newCommentaries);

        Task<SignInterpretationCommentaryData> UpdateCommentaryAsync(EditionUserInfo editionUser,
            uint signInterpretationId,
            SignInterpretationCommentaryData updateCommentaryData);

        Task<List<uint>> DeleteCommentariesAsync(EditionUserInfo editionUser, List<uint> deleteCommentaryIds);

        Task<List<uint>> DeleteAllCommentariesForSignInterpretationAsync(EditionUserInfo editionUser,
            uint signInterpretationId);

        Task<SignInterpretationCommentaryDataSearchData> GetSignInterpretationCommentaryByIdAsync(EditionUserInfo editionUser,
            uint signInterpretationCommentaryId);

        Task<List<SignInterpretationCommentaryDataSearchData>> GetSignInterpretationCommentariesByDataAsync(
            EditionUserInfo editionUser,
            SignInterpretationCommentaryDataSearchData dataSearchData);

        Task<List<SignInterpretationCommentaryDataSearchData>> GetSignInterpretationCommentariesByInterpretationId(
            EditionUserInfo editionUser,
            uint signInterpretationId);

        Task<SignInterpretationCommentaryData> ReplaceSignInterpretationCommentary(EditionUserInfo editionUser,
            uint signInterpretationId,
            SignInterpretationCommentaryData newCommentaryData);

        Task<List<SignInterpretationCommentaryData>> ReplaceSignInterpretationCommentaries(EditionUserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationCommentaryData> newCommentaries);
    }

    public class SignInterpretationCommentaryRepository : DbConnectionBase, ISignInterpretationCommentaryRepository
    {

        private readonly IDatabaseWriter _databaseWriter;
        public SignInterpretationCommentaryRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config)
        {
            _databaseWriter = databaseWriter;
        }
        /// <summary>
        /// Creates new commentary für a sign intepretation
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <param name="newCommentaryData">Sign interpretation commentary object</param>
        /// <returns>Sign interpretation commentary object with new commentary id set</returns>
        public async Task<SignInterpretationCommentaryData> CreateCommentaryAsync(EditionUserInfo editionUser,
            uint signInterpretationId,
            SignInterpretationCommentaryData newCommentaryData)
        {
            var result = await _createOrUpdateCommentariesAsync(editionUser,
                signInterpretationId,
                new List<SignInterpretationCommentaryData>() { newCommentaryData },
                MutateType.Create);
            return result.Count > 0 ? result.First() : new SignInterpretationCommentaryData();
        }

        public async Task<List<SignInterpretationCommentaryData>> CreateCommentariesAsync(EditionUserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationCommentaryData> newCommentaries)
        {
            return await _createOrUpdateCommentariesAsync(editionUser,
                signInterpretationId,
                newCommentaries,
                MutateType.Create);
        }


        /// <summary>
        /// Update the given commentary
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <param name="updateCommentaryData">Sign interpretation commentary object</param>
        /// <returns>Sign interpretation commentary object with new commentary id set</returns>
        public async Task<SignInterpretationCommentaryData> UpdateCommentaryAsync(EditionUserInfo editionUser,
            uint signInterpretationId,
            SignInterpretationCommentaryData updateCommentaryData)
        {
            var result = await _createOrUpdateCommentariesAsync(editionUser,
                signInterpretationId,
                new List<SignInterpretationCommentaryData>() { updateCommentaryData },
                MutateType.Update);
            return result.Count > 0 ? result.First() : new SignInterpretationCommentaryData();
        }

        /// <summary>
        /// Deletes the Commentaries with the given ids.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="deleteCommentaryIds">List of ids of the attributes to be deleted</param>
        /// <returns>The list of the ids of deleted commentaries or empty list if the given list was null.</returns>
        /// <exception cref="StandardExceptions.DataNotWrittenException"></exception>
        public async Task<List<uint>> DeleteCommentariesAsync(EditionUserInfo editionUser,
            List<uint> deleteCommentaryIds)
        {
            if (deleteCommentaryIds == null) return new List<uint>();
            var requests = deleteCommentaryIds.Select(
                id => new MutationRequest(
                    MutateType.Delete,
                    new DynamicParameters(),
                    "sign_interpretation_commentary")).ToList();

            var writeResults = await _databaseWriter.WriteToDatabaseAsync(editionUser, requests);

            // Check whether for each attribute a request was processed.
            if (writeResults.Count != deleteCommentaryIds.Count)
            {
                throw new StandardExceptions.DataNotWrittenException("delete sign interpretation commentary");
            }
            return deleteCommentaryIds;

        }

        /// <summary>
        /// Deletes all commentaries for the sign interpretation referred by its id
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <returns>List of ids of delete commentaries</returns>
        public async Task<List<uint>> DeleteAllCommentariesForSignInterpretationAsync(EditionUserInfo editionUser,
            uint signInterpretationId)
        {
            var commentaries = await GetSignInterpretationCommentariesByInterpretationId(
                editionUser,
                signInterpretationId);
            return await DeleteCommentariesAsync(
                editionUser,
                (commentaries.Select(commentary => (uint)commentary.SignInterpretationCommentaryId)).ToList());
        }

        /// <summary>
        /// Gets the commentary with the given id
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationCommentaryId">Id of the commentary to be retrieved</param>
        /// <returns>Sign interpretation commentary with the given id</returns>
        /// <exception cref="DataNotFoundException"></exception>
        public async Task<SignInterpretationCommentaryDataSearchData> GetSignInterpretationCommentaryByIdAsync(
            EditionUserInfo editionUser,
            uint signInterpretationCommentaryId)
        {
            var searchData = new SignInterpretationCommentaryDataSearchData()
            {
                SignInterpretationCommentaryId = signInterpretationCommentaryId
            };

            var result = await GetSignInterpretationCommentariesByDataAsync(
                editionUser,
                searchData);

            if (result.Count != 1)
                throw new StandardExceptions.DataNotFoundException(
                    "sign interpretation commentary",
                    signInterpretationCommentaryId
                );
            return result.First();
        }

        /// <summary>
        /// Retrieves all sign interpretation commentaries which match the data provided by searchData
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="dataSearchData">Sign interpretation commentary search data object</param>
        /// <returns>List of sign interpretation commentary data - if nothing had been found the list is empty.</returns>
        public async Task<List<SignInterpretationCommentaryDataSearchData>> GetSignInterpretationCommentariesByDataAsync(
            EditionUserInfo editionUser,
            SignInterpretationCommentaryDataSearchData dataSearchData)
        {
            var query = GetSignInterpretationCommentaryByData.GetQuery.Replace(
                "@WhereData",
                dataSearchData.getSearchParameterString());
            using (var connection = OpenConnection())
            {
                var result = await connection.QueryAsync<SignInterpretationCommentaryDataSearchData>(
                    query,
                    new { editionUser.EditionId });
                return result == null ? new List<SignInterpretationCommentaryDataSearchData>() : result.ToList();
            }
        }

        /// <summary>
        /// Gets all commentaries of a the sign interpretation referred by its id
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <returns>List of sign interpretation commentaries</returns>
        public async Task<List<SignInterpretationCommentaryDataSearchData>> GetSignInterpretationCommentariesByInterpretationId(
            EditionUserInfo editionUser,
            uint signInterpretationId)
        {
            var searchData = new SignInterpretationCommentaryDataSearchData()
            {
                SignInterpretationId = signInterpretationId
            };

            return await GetSignInterpretationCommentariesByDataAsync(
                editionUser,
                searchData);
        }

        /// <summary>
        /// Deletes all existing commentaries of a sign interpretation referred by the field signInterpretationId
        /// of the new commentary and add the new commentary to it
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <param name="newCommentaryData">New sign interpretation commentaries</param>
        /// <returns>New sign interpretation commentary with the new id</returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<SignInterpretationCommentaryData> ReplaceSignInterpretationCommentary(
            EditionUserInfo editionUser,
            uint signInterpretationId,
            SignInterpretationCommentaryData newCommentaryData)
        {
            if (newCommentaryData == null) return new SignInterpretationCommentaryData();
            var result = await ReplaceSignInterpretationCommentaries(editionUser,
                signInterpretationId,
                new List<SignInterpretationCommentaryData>() { newCommentaryData });
            return result.Count == 0 ? new SignInterpretationCommentaryData() : result.First();
        }

        /// <summary>
        /// Deletes all existing commentaries of a sign interpretation referred by the field signInterpretationId
        /// of the new commentary and add the new commentaries to it
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <param name="newCommentary">List of new sign interpretation commentaries</param>
        /// <returns>List of the new sign interpretation commentaries with the new ids</returns>
        public async Task<List<SignInterpretationCommentaryData>> ReplaceSignInterpretationCommentaries(
            EditionUserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationCommentaryData> newCommentaries)
        {
            if (!(newCommentaries?.Count > 0)) return new List<SignInterpretationCommentaryData>();
            await DeleteAllCommentariesForSignInterpretationAsync(
                editionUser,
                signInterpretationId);
            return await CreateCommentariesAsync(editionUser, signInterpretationId, newCommentaries);
        }

        #region Private functions

        /// <summary>
        /// Creates and executes create or update mutation requests for the given commentary which must have set the field
        /// signInterpretationId properly.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="commentary">Sign interpretation commentary object</param>
        /// <param name="action">Mutate type create or update</param>
        /// <returns>Commentary with the new sign interpretation commentary id set. If the commentary had been null
        /// the id is set to zero</returns>
        /// <exception cref="StandardExceptions.DataNotWrittenException"></exception>
        private async Task<List<SignInterpretationCommentaryData>> _createOrUpdateCommentariesAsync(
            EditionUserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationCommentaryData> commentaries,
            MutateType action
            )
        {

            // Let's test whether a list of new attributes is provided.
            // It doesn't matter if this list is empty
            if (!(commentaries?.Count > 0)) return new List<SignInterpretationCommentaryData>();
            // Create requests for the commentary
            var requests = new List<MutationRequest>();
            foreach (var commentary in commentaries)
            {
                var signInterpretationCommentaryParameters = new DynamicParameters();
                signInterpretationCommentaryParameters.Add("@sign_interpretation_id", signInterpretationId);
                signInterpretationCommentaryParameters.Add("@attribute_id", commentary.AttributeId);
                signInterpretationCommentaryParameters.Add("@commentary", commentary.Commentary);
                var signInterpretationCommentaryRequest = new MutationRequest(
                    action,
                    signInterpretationCommentaryParameters,
                    "sign_interpretation_commentary",
                    action == MutateType.Update ? (uint?)commentary.SignInterpretationCommentaryId : null
                );
                requests.Add(signInterpretationCommentaryRequest);
            }

            var writeResults = await _databaseWriter.WriteToDatabaseAsync(editionUser,
                requests);

            // Check whether for each commentary a request was processed.
            if (writeResults.Count != commentaries.Count)
            {
                var actionName = action == MutateType.Create ? "create" : "update";
                throw new StandardExceptions.DataNotWrittenException($"{actionName} sign interpretation commentary");
            }

            // Now set the new Ids

            for (var i = 0; i < commentaries.Count; i++)
            {
                commentaries[i].SignInterpretationCommentaryId = (uint)writeResults[i].NewId;
            }



            // Now return the list of new attributes which now also contains the the new ids.
            return commentaries;
            //If no list of new attributes had been provided return an empty list.
        }

        #endregion
    }
}