using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;
// ReSharper disable ArrangeRedundantParentheses

namespace SQE.DatabaseAccess
{
	public interface ISignInterpretationCommentaryRepository
	{
		Task<SignInterpretationCommentaryData> CreateCommentaryAsync(
				UserInfo                           editionUser
				, uint                             signInterpretationId
				, SignInterpretationCommentaryData newCommentaryData);

		Task<List<SignInterpretationCommentaryData>> CreateCommentariesAsync(
				UserInfo                                 editionUser
				, uint                                   signInterpretationId
				, List<SignInterpretationCommentaryData> newCommentaries);

		Task<SignInterpretationCommentaryData> CreateOrUpdateCommentaryAsync(
				UserInfo editionUser
				, uint   signInterpretationId
				, uint?  attributeValueId
				, string commentary);

		Task<List<uint>> DeleteCommentariesAsync(
				UserInfo     editionUser
				, List<uint> deleteCommentaryIds);

		Task<List<uint>> DeleteAllCommentariesForSignInterpretationAsync(
				UserInfo editionUser
				, uint   signInterpretationId);

		Task<SignInterpretationCommentaryData> GetSignInterpretationCommentaryByIdAsync(
				UserInfo editionUser
				, uint   signInterpretationCommentaryId);

		Task<IEnumerable<SignInterpretationCommentaryData>>
				GetSignInterpretationCommentariesByDataAsync(
						UserInfo                                     editionUser
						, SignInterpretationCommentaryDataSearchData dataSearchData);

		Task<IEnumerable<SignInterpretationCommentaryData>>
				GetSignInterpretationCommentariesByInterpretationId(
						UserInfo editionUser
						, uint   signInterpretationId);

		Task<SignInterpretationCommentaryData> ReplaceSignInterpretationCommentary(
				UserInfo                           editionUser
				, uint                             signInterpretationId
				, SignInterpretationCommentaryData newCommentaryData);

		Task<List<SignInterpretationCommentaryData>> ReplaceSignInterpretationCommentaries(
				UserInfo                                 editionUser
				, uint                                   signInterpretationId
				, List<SignInterpretationCommentaryData> newCommentaries);
	}

	public class SignInterpretationCommentaryRepository : DbConnectionBase
														  , ISignInterpretationCommentaryRepository
	{
		private readonly IAttributeRepository _attributeRepository;
		private readonly IDatabaseWriter      _databaseWriter;

		public SignInterpretationCommentaryRepository(
				IConfiguration         config
				, IDatabaseWriter      databaseWriter
				, IAttributeRepository attributeRepository) : base(config)
		{
			_databaseWriter = databaseWriter;
			_attributeRepository = attributeRepository;
		}

		/// <summary>
		///  Creates new commentary for a sign interpretation
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <param name="newCommentaryData">Sign interpretation commentary object</param>
		/// <returns>Sign interpretation commentary object with new commentary id set</returns>
		public async Task<SignInterpretationCommentaryData> CreateCommentaryAsync(
				UserInfo                           editionUser
				, uint                             signInterpretationId
				, SignInterpretationCommentaryData newCommentaryData)
		{
			var result = await _createOrUpdateCommentariesAsync(
					editionUser
					, signInterpretationId
					, new List<SignInterpretationCommentaryData> { newCommentaryData }
					, MutateType.Create);

			return result.Count > 0
					? result.First()
					: new SignInterpretationCommentaryData();
		}

		public async Task<List<SignInterpretationCommentaryData>> CreateCommentariesAsync(
				UserInfo                                 editionUser
				, uint                                   signInterpretationId
				, List<SignInterpretationCommentaryData> newCommentaries)
			=> await _createOrUpdateCommentariesAsync(
					editionUser
					, signInterpretationId
					, newCommentaries
					, MutateType.Create);

		/// <summary>
		///  Update the given commentary
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <param name="attributeValueId"></param>
		/// <param name="commentary"></param>
		/// <returns>Sign interpretation commentary object with new commentary id set</returns>
		public async Task<SignInterpretationCommentaryData> CreateOrUpdateCommentaryAsync(
				UserInfo editionUser
				, uint   signInterpretationId
				, uint?  attributeValueId
				, string commentary)
		{
			uint? attributeId = null;

			if (attributeValueId.HasValue)
			{
				var searchData = new SignInterpretationAttributeDataSearchData
				{
						SignInterpretationId = signInterpretationId
						, AttributeValueId = attributeValueId
						,
				};

				var signInterpretationAttributes =
						await _attributeRepository.GetSignInterpretationAttributesByDataAsync(
								editionUser
								, searchData);

				if (signInterpretationAttributes.Count != 1)
				{
					throw new StandardExceptions.DataNotFoundException(
							"sign interpretation attribute"
							, attributeValueId.Value
							, "attribute value id");
				}

				attributeId = signInterpretationAttributes.First().AttributeId;
			}

			var existingCommentaries =
					(await GetSignInterpretationCommentariesByInterpretationId(
							editionUser
							, signInterpretationId)).AsList();

			// Check if this is actually a request to set the commentary to null (i.e., delete)
			if (string.IsNullOrEmpty(commentary))
			{
				var signInterpretationCommentaryId = existingCommentaries
													 .Where(
															 x
																	 => (x.AttributeId
																		 == attributeId)
																		&& x
																		   .SignInterpretationCommentaryId
																		   .HasValue)
													 .Select(
															 x => x.SignInterpretationCommentaryId
																   .Value);

				await DeleteCommentariesAsync(editionUser, signInterpretationCommentaryId.AsList());

				return null; // Early return, nothing more to do
			}

			var preparedCommentary = new SignInterpretationCommentaryData
			{
					AttributeId = attributeId
					, Commentary = commentary
					, SignInterpretationId = signInterpretationId
					,
			};

			var action = MutateType.Create;

			// Check if we are replacing an existing comment
			var replacedCommentary =
					existingCommentaries.FirstOrDefault(x => x.AttributeId == attributeId);

			if (replacedCommentary != null)
			{
				action = MutateType.Update;

				preparedCommentary.SignInterpretationCommentaryId =
						replacedCommentary.SignInterpretationCommentaryId;
			}

			var result = await _createOrUpdateCommentariesAsync(
					editionUser
					, signInterpretationId
					, new List<SignInterpretationCommentaryData> { preparedCommentary }
					, action);

			return result.Count > 0
					? result.First()
					: new SignInterpretationCommentaryData();
		}

		/// <summary>
		///  Deletes the Commentaries with the given ids.
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="deleteCommentaryIds">List of ids of the attributes to be deleted</param>
		/// <returns>The list of the ids of deleted commentaries or empty list if the given list was null.</returns>
		/// <exception cref="StandardExceptions.DataNotWrittenException"></exception>
		public async Task<List<uint>> DeleteCommentariesAsync(
				UserInfo     editionUser
				, List<uint> deleteCommentaryIds)
		{
			if (deleteCommentaryIds == null)
				return new List<uint>();

			var requests = deleteCommentaryIds.Select(
													  id => new MutationRequest(
															  MutateType.Delete
															  , new DynamicParameters()
															  , "sign_interpretation_commentary"
															  , id))
											  .ToList();

			var writeResults = await _databaseWriter.WriteToDatabaseAsync(editionUser, requests);

			// Check whether for each attribute a request was processed.
			if (writeResults.Count != deleteCommentaryIds.Count)
			{
				throw new StandardExceptions.DataNotWrittenException(
						"delete sign interpretation commentary");
			}

			return deleteCommentaryIds;
		}

		/// <summary>
		///  Deletes all commentaries for the sign interpretation referred by its id
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <returns>List of ids of delete commentaries</returns>
		public async Task<List<uint>> DeleteAllCommentariesForSignInterpretationAsync(
				UserInfo editionUser
				, uint   signInterpretationId)
		{
			var commentaries =
					await GetSignInterpretationCommentariesByInterpretationId(
							editionUser
							, signInterpretationId);

			return await DeleteCommentariesAsync(
					editionUser
					, commentaries
					  .Where(commentary => commentary.SignInterpretationCommentaryId.HasValue)
					  .Select(commentary => commentary.SignInterpretationCommentaryId.Value)
					  .ToList());
		}

		/// <summary>
		///  Gets the commentary with the given id
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationCommentaryId">Id of the commentary to be retrieved</param>
		/// <returns>Sign interpretation commentary with the given id</returns>
		/// <exception cref="DataNotFoundException"></exception>
		public async Task<SignInterpretationCommentaryData>
				GetSignInterpretationCommentaryByIdAsync(
						UserInfo editionUser
						, uint   signInterpretationCommentaryId)
		{
			var searchData = new SignInterpretationCommentaryDataSearchData
			{
					SignInterpretationCommentaryId = signInterpretationCommentaryId,
			};

			var result =
					(await GetSignInterpretationCommentariesByDataAsync(editionUser, searchData))
					.ToList();

			if (result.Count != 1)
			{
				throw new StandardExceptions.DataNotFoundException(
						"sign interpretation commentary"
						, signInterpretationCommentaryId);
			}

			return result.First();
		}

		/// <summary>
		///  Retrieves all sign interpretation commentaries which match the data provided by searchData
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="dataSearchData">Sign interpretation commentary search data object</param>
		/// <returns>List of sign interpretation commentary data - if nothing had been found the list is empty.</returns>
		public async Task<IEnumerable<SignInterpretationCommentaryData>>
				GetSignInterpretationCommentariesByDataAsync(
						UserInfo                                     editionUser
						, SignInterpretationCommentaryDataSearchData dataSearchData)
		{
			var query = GetSignInterpretationCommentaryByData.GetQuery.Replace(
					"@WhereData"
					, dataSearchData.getSearchParameterString());

			using (var connection = OpenConnection())
			{
				return await connection.QueryAsync<SignInterpretationCommentaryData>(
						query
						, new { editionUser.EditionId });
			}
		}

		/// <summary>
		///  Gets all commentaries of a the sign interpretation referred by its id
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <returns>List of sign interpretation commentaries</returns>
		public async Task<IEnumerable<SignInterpretationCommentaryData>>
				GetSignInterpretationCommentariesByInterpretationId(
						UserInfo editionUser
						, uint   signInterpretationId)
		{
			var searchData = new SignInterpretationCommentaryDataSearchData
			{
					SignInterpretationId = signInterpretationId,
			};

			return await GetSignInterpretationCommentariesByDataAsync(editionUser, searchData);
		}

		/// <summary>
		///  Deletes all existing commentaries of a sign interpretation referred by the field signInterpretationId
		///  of the new commentary and add the new commentary to it
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <param name="newCommentaryData">New sign interpretation commentaries</param>
		/// <returns>New sign interpretation commentary with the new id</returns>
		public async Task<SignInterpretationCommentaryData> ReplaceSignInterpretationCommentary(
				UserInfo                           editionUser
				, uint                             signInterpretationId
				, SignInterpretationCommentaryData newCommentaryData)
		{
			if (newCommentaryData == null)
				return new SignInterpretationCommentaryData();

			var result = await ReplaceSignInterpretationCommentaries(
					editionUser
					, signInterpretationId
					, new List<SignInterpretationCommentaryData> { newCommentaryData });

			return result.Count == 0
					? new SignInterpretationCommentaryData()
					: result.First();
		}

		/// <summary>
		///  Deletes all existing commentaries of a sign interpretation referred by the field signInterpretationId
		///  of the new commentary and add the new commentaries to it
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of sign interpretation</param>
		/// <param name="newCommentaries">List of new sign interpretation commentaries</param>
		/// <returns>List of the new sign interpretation commentaries with the new ids</returns>
		public async Task<List<SignInterpretationCommentaryData>>
				ReplaceSignInterpretationCommentaries(
						UserInfo                                 editionUser
						, uint                                   signInterpretationId
						, List<SignInterpretationCommentaryData> newCommentaries)
		{
			if (!(newCommentaries?.Count > 0))
				return new List<SignInterpretationCommentaryData>();

			await DeleteAllCommentariesForSignInterpretationAsync(
					editionUser
					, signInterpretationId);

			return await CreateCommentariesAsync(
					editionUser
					, signInterpretationId
					, newCommentaries);
		}

		#region Private functions

		/// <summary>
		///  Creates and executes create or update mutation requests for the given commentary which must have set the field
		///  signInterpretationId properly.
		/// </summary>
		/// <param name="editionUser">Edition user object</param>
		/// <param name="signInterpretationId">Id of the sign interpretation</param>
		/// <param name="commentaries">List of sign interpretation commentary object</param>
		/// <param name="action">Mutate type create or update</param>
		/// <returns>
		///  Commentary with the new sign interpretation commentary id set. If the commentary had been null
		///  the id is set to zero
		/// </returns>
		/// <exception cref="StandardExceptions.DataNotWrittenException"></exception>
		private async Task<List<SignInterpretationCommentaryData>> _createOrUpdateCommentariesAsync(
				UserInfo                                 editionUser
				, uint                                   signInterpretationId
				, List<SignInterpretationCommentaryData> commentaries
				, MutateType                             action)
		{
			var response = commentaries;

			// Create requests for the commentary
			var requests = new List<MutationRequest>();

			foreach (var commentary in commentaries)
			{
				var signInterpretationCommentaryParameters = new DynamicParameters();

				signInterpretationCommentaryParameters.Add(
						"@sign_interpretation_id"
						, signInterpretationId);

				signInterpretationCommentaryParameters.Add("@attribute_id", commentary.AttributeId);

				signInterpretationCommentaryParameters.Add("@commentary", commentary.Commentary);

				var signInterpretationCommentaryRequest = new MutationRequest(
						action
						, signInterpretationCommentaryParameters
						, "sign_interpretation_commentary"
						, action == MutateType.Update
								? commentary.SignInterpretationCommentaryId
								: null);

				requests.Add(signInterpretationCommentaryRequest);
			}

			var writeResults = await _databaseWriter.WriteToDatabaseAsync(editionUser, requests);

			// Check whether for each commentary a request was processed.
			if (writeResults.Count != commentaries.Count)
			{
				var actionName = action == MutateType.Create
						? "create"
						: "update";

				throw new StandardExceptions.DataNotWrittenException(
						$"{actionName} sign interpretation commentary");
			}

			// Now set the new Ids in the response
			for (var i = 0; i < commentaries.Count; i++)
			{
				var newId = writeResults[i].NewId;

				if (newId.HasValue)
					response[i].SignInterpretationCommentaryId = newId.Value;
			}

			// Now return the list of new attributes which now also contains the the new ids.
			return response;
		}

		#endregion
	}
}
