using System;
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
    public interface IAttributeRepository
    {
        Task<List<SignInterpretationAttributeData>> CreateAttributesAsync(UserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationAttributeData> newAttributes);

        Task<List<SignInterpretationAttributeData>> UpdateAttributesAsync(UserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationAttributeData> updateAttributes);

        Task<List<uint>> DeleteAttributesAsync(UserInfo editionUser, List<uint> deleteAttributeIds);

        Task<List<uint>> DeleteAllAttributesForSignInterpretationAsync(UserInfo editionUser,
            uint signInterpretationId);

        Task<SignInterpretationAttributeData> GetSignInterpretationAttributeByIdAsync(UserInfo editionUser,
            uint signInterpretationAttributeId);

        Task<List<SignInterpretationAttributeData>> GetSignInterpretationAttributesByDataAsync(
            UserInfo editionUser,
            SignInterpretationAttributeDataSearchData dataSearchData);

        Task<List<SignInterpretationAttributeData>> GetSignInterpretationAttributesByInterpretationId(
            UserInfo editionUser,
            uint signInterpretationId);

        Task<uint> GetSignInterpretationAttributeIdByIdAsync(UserInfo editionUser,
            uint signInterpretationAttributeId);

        Task<List<uint>> GetSignInterpretationAttributeIdsByDataAsync(
            UserInfo editionUser,
            SignInterpretationAttributeDataSearchData dataSearchData);

        Task<List<uint>> GetSignInterpretationAttributeIdsByInterpretationId(
            UserInfo editionUser,
            uint signInterpretationId);


        Task<List<SignInterpretationAttributeData>> ReplaceSignInterpretationAttributesAsync(UserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationAttributeData> newAttributes);
    }

    public class AttributeRepository : DbConnectionBase, IAttributeRepository
    {
        private readonly IDatabaseWriter _databaseWriter;

        public AttributeRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config)
        {
            _databaseWriter = databaseWriter;
        }

        /// <summary>
        ///     Creates new attributes für a sign interpretation
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <param name="newAttributes">List of new attributes</param>
        /// <returns>List of new attributes</returns>
        public async Task<List<SignInterpretationAttributeData>> CreateAttributesAsync(UserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationAttributeData> newAttributes)
        {
            return await _createOrUpdateAttributesAsync(editionUser,
                signInterpretationId,
                newAttributes,
                MutateType.Create);
        }

        /// <summary>
        ///     Update the given attributes
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretaionId">Id of sign interpretation</param>
        /// <param name="updateAttributes">List of Attributes with the new values</param>
        /// <returns>Returns the list of Attributes which contain the new ids</returns>
        public async Task<List<SignInterpretationAttributeData>> UpdateAttributesAsync(
            UserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationAttributeData> updateAttributes)
        {
            return await _createOrUpdateAttributesAsync(editionUser,
                signInterpretationId,
                updateAttributes,
                MutateType.Update);
        }


        /// <summary>
        ///     Deletes the attributes with the given ids.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="deleteAttributeIds">List of ids of the attributes to be deleted</param>
        /// <returns>The list of the ids of deleted Attributes or empty list if the given list was null.</returns>
        /// <exception cref="DataNotWrittenException"></exception>
        public async Task<List<uint>> DeleteAttributesAsync(UserInfo editionUser,
            List<uint> deleteAttributeIds)
        {
            if (deleteAttributeIds == null) return new List<uint>();
            var requests = deleteAttributeIds.Select(
                id => new MutationRequest(
                    MutateType.Delete,
                    new DynamicParameters(),
                    "sign_interpretation_attribute")).ToList();

            var writeResults = await _databaseWriter.WriteToDatabaseAsync(editionUser, requests);

            // Check whether for each attribute a request was processed.
            if (writeResults.Count != deleteAttributeIds.Count)
                throw new StandardExceptions.DataNotWrittenException("delete sign interpretation attribute");
            return deleteAttributeIds;
        }

        /// <summary>
        ///     Gets the attribute with the given id
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationAttributeId">Id of the attribute to be retrieved</param>
        /// <returns>Sign interpretation attribute with the given id</returns>
        /// <exception cref="DataNotFoundException"></exception>
        public async Task<SignInterpretationAttributeData> GetSignInterpretationAttributeByIdAsync(
            UserInfo editionUser,
            uint signInterpretationAttributeId)
        {
            var searchData = new SignInterpretationAttributeDataSearchData
            {
                SignInterpretationAttributeId = signInterpretationAttributeId
            };

            var result = await GetSignInterpretationAttributesByDataAsync(
                editionUser,
                searchData);

            if (result.Count != 1)
                throw new StandardExceptions.DataNotFoundException(
                    "sign interpretation attribute",
                    signInterpretationAttributeId
                );
            return result.First();
        }

        /// <summary>
        ///     Retrieves all sign interpretation attributes which match the data provided by searchData
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="dataSearchData">Sign interpretation attribute search data object</param>
        /// <returns>List of sign interpretation attribute data - if nothing had been found the list is empty.</returns>
        public async Task<List<SignInterpretationAttributeData>> GetSignInterpretationAttributesByDataAsync(
            UserInfo editionUser,
            SignInterpretationAttributeDataSearchData dataSearchData)
        {
            var query = GetSignInterpretationAttributesByDataQuery.GetQuery.Replace(
                "@WhereData",
                dataSearchData.getSearchParameterString());
            using (var connection = OpenConnection())
            {
                var result = await connection.QueryAsync<SignInterpretationAttributeData>(
                    query,
                    new { editionUser.EditionId });
                return result == null ? new List<SignInterpretationAttributeData>() : result.ToList();
            }
        }

        /// <summary>
        ///     Gets all attributes of a the sign interpretation referred by its id
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <returns>List of sign interpretation attributes</returns>
        public async Task<List<SignInterpretationAttributeData>> GetSignInterpretationAttributesByInterpretationId(
            UserInfo editionUser, uint signInterpretationId)
        {
            var searchData = new SignInterpretationAttributeDataSearchData
            {
                SignInterpretationId = signInterpretationId
            };

            return await GetSignInterpretationAttributesByDataAsync(
                editionUser,
                searchData);
        }

        /// <summary>
        ///     Gets the attribute with the given id
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationAttributeId">Id of the attribute to be retrieved</param>
        /// <returns>Sign interpretation attribute with the given id</returns>
        /// <exception cref="DataNotFoundException"></exception>
        public async Task<uint> GetSignInterpretationAttributeIdByIdAsync(
            UserInfo editionUser,
            uint signInterpretationAttributeId)
        {
            var searchData = new SignInterpretationAttributeDataSearchData
            {
                SignInterpretationAttributeId = signInterpretationAttributeId
            };

            var result = await GetSignInterpretationAttributeIdsByDataAsync(
                editionUser,
                searchData);

            if (result.Count != 1)
                throw new StandardExceptions.DataNotFoundException(
                    "sign interpretation attribute",
                    signInterpretationAttributeId
                );
            return result.First();
        }

        /// <summary>
        ///     Retrieves all sign interpretation attribute ids which match the data provided by searchData
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="dataSearchData">Sign interpretation attribute search data object</param>
        /// <returns>List of sign interpretation attribute ids - if nothing had been found the list is empty.</returns>
        public async Task<List<uint>> GetSignInterpretationAttributeIdsByDataAsync(UserInfo editionUser,
            SignInterpretationAttributeDataSearchData dataSearchData)
        {
            var query = GetSignInterpretationAttributeIdsByDataQuery.GetQuery.Replace(
                "@WhereData",
                dataSearchData.getSearchParameterString());
            using (var connection = OpenConnection())
            {
                var result = await connection.QueryAsync<uint>(
                    query,
                    new { editionUser.EditionId });
                return result == null ? new List<uint>() : result.ToList();
            }
        }

        /// <summary>
        ///     Gets all attribute ids of a the sign interpretation referred by its id
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <returns>List of sign interpretation attribute idss</returns>
        public async Task<List<uint>> GetSignInterpretationAttributeIdsByInterpretationId(UserInfo editionUser,
            uint signInterpretationId)
        {
            var searchData = new SignInterpretationAttributeDataSearchData
            {
                SignInterpretationId = signInterpretationId
            };

            return await GetSignInterpretationAttributeIdsByDataAsync(
                editionUser,
                searchData);
        }


        /// <summary>
        ///     Deletes all attributes for the sign interpretation referred by its id
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <returns>List of ids of delete attributes</returns>
        public async Task<List<uint>> DeleteAllAttributesForSignInterpretationAsync(UserInfo editionUser,
            uint signInterpretationId)
        {
            var attributes = await GetSignInterpretationAttributeIdsByInterpretationId(
                editionUser,
                signInterpretationId);
            return await DeleteAttributesAsync(
                editionUser,
                attributes);
        }

        /// <summary>
        ///     Deletes all existing attributes of a sign interpretation and add the new attributes to it
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sogn itnerpretation</param>
        /// <param name="newAttributes">List of new sign interpretation attributes</param>
        /// <returns>List of the new sign interpretation attributes with the new ids</returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<List<SignInterpretationAttributeData>> ReplaceSignInterpretationAttributesAsync(
            UserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationAttributeData> newAttributes)
        {
            if (newAttributes == null || newAttributes.Count <= 0) return new List<SignInterpretationAttributeData>();
            await DeleteAllAttributesForSignInterpretationAsync(
                editionUser,
                signInterpretationId);
            return await CreateAttributesAsync(editionUser, signInterpretationId, newAttributes);
        }


        #region Private functions

        /// <summary>
        ///     Creates and executes create or update mutation requests for the given attributes
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <param name="attributes">List of attributes</param>
        /// <param name="action">Mutate type create or update</param>
        /// <returns>
        ///     List of set attributes with the new sign interpretation attribute id set or empty list
        ///     if the list of attributes had been null.
        /// </returns>
        /// <exception cref="DataNotWrittenException"></exception>
        private async Task<List<SignInterpretationAttributeData>> _createOrUpdateAttributesAsync(
            UserInfo editionUser,
            uint signInterpretationId,
            List<SignInterpretationAttributeData> attributes,
            MutateType action
        )
        {
            // Let's test whether a list of new attributes is provided and contains attributes
            if (!(attributes?.Count > 0)) return new List<SignInterpretationAttributeData>();
            // Create requests for the attributes
            var requests = new List<MutationRequest>();
            foreach (var attribute in attributes)
            {
                var signInterpretationAttributeParameters = new DynamicParameters();
                signInterpretationAttributeParameters.Add("@sign_interpretation_id", signInterpretationId);
                signInterpretationAttributeParameters.Add("@attribute_value_id", attribute.AttributeValueId);
                signInterpretationAttributeParameters.Add("@sequence", attribute.Sequence);
                signInterpretationAttributeParameters.Add("@numeric_value", attribute.NumericValue);
                var signInterpretationAttributeRequest = new MutationRequest(
                    action,
                    signInterpretationAttributeParameters,
                    "sign_interpretation_attribute",
                    action == MutateType.Update ? attribute.SignInterpretationAttributeId : null
                );
                requests.Add(signInterpretationAttributeRequest);
            }

            var writeResults = await _databaseWriter.WriteToDatabaseAsync(editionUser, requests);

            // Check whether for each attribute a request was processed.
            if (writeResults.Count != attributes.Count)
            {
                var actionName = action == MutateType.Create ? "create" : "update";
                throw new StandardExceptions.DataNotWrittenException($"{actionName} sign interpretation attribute");
            }

            // Now set the new Ids
            for (var i = 0; i < attributes.Count; i++)
                attributes[i].SignInterpretationAttributeId = (uint)writeResults[i].NewId;

            // Now return the list of new attributes which now also contains the the new ids.
            return attributes;
            //If no list of new attributes had been provided return an empty list.
        }

        #endregion
    }
}