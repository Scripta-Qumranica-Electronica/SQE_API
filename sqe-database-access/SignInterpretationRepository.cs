using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;
// ReSharper disable ArrangeRedundantParentheses

namespace SQE.DatabaseAccess
{
	public interface ISignInterpretationRepository
	{
		Task<SignInterpretationData> GetSignInterpretationById(
				UserInfo user
				, uint   signInterpretationId);

		Task UpdateSignInterpretationCharacterById(
				UserInfo user
				, uint   signInterpretationId
				, string character
				, byte   priority
				, uint   attributeValueId);
	}

	public class SignInterpretationRepository : DbConnectionBase
												, ISignInterpretationRepository
	{
		private readonly IAttributeRepository _attributeRepository;
		private readonly IDatabaseWriter      _databaseWriter;

		private readonly ISignInterpretationCommentaryRepository
				_interpretationCommentaryRepository;

		private readonly IRoiRepository _roiRepository;

		public SignInterpretationRepository(
				IConfiguration                            config
				, IAttributeRepository                    attributeRepository
				, ISignInterpretationCommentaryRepository interpretationCommentaryRepository
				, IRoiRepository                          roiRepository
				, IDatabaseWriter                         databaseWriter) : base(config)
		{
			_attributeRepository = attributeRepository;

			_interpretationCommentaryRepository = interpretationCommentaryRepository;

			_roiRepository = roiRepository;
			_databaseWriter = databaseWriter;
		}

		public async Task<SignInterpretationData> GetSignInterpretationById(
				UserInfo user
				, uint   signInterpretationId)
		{
			// We use several existing quick functions to get the specifics of a sign interpretation,
			// so wrap it in a transaction to make sure the result is consistent.
			using (var transactionScope = AsyncFlowTransaction.GetScope())
			using (var conn = OpenConnection())
			{
				var attributes =
						_attributeRepository.GetSignInterpretationAttributesByInterpretationId(
								user
								, signInterpretationId);

				var commentaries =
						_interpretationCommentaryRepository
								.GetSignInterpretationCommentariesByInterpretationId(
										user
										, signInterpretationId);

				var roiIds =
						await _roiRepository.GetSignInterpretationRoisIdsByInterpretationId(
								user
								, signInterpretationId);

				// TODO: perhaps create method that does can get all the ROIs with one query
				var rois = new SignInterpretationRoiData[roiIds.Count];

				foreach (var (roiId, index) in roiIds.Select((x, idx) => (x, idx)))
				{
					rois[index] =
							await _roiRepository.GetSignInterpretationRoiByIdAsync(user, roiId);
				}

				SignInterpretationData returnSignInterpretation = null;

				var _ = await conn.QueryAsync(
						SignInterpretationQuery.GetQuery
						, new[]
						{
								typeof(SignInterpretationData)
								, typeof(NextSignInterpretation)
								, typeof(uint?)
								,
						}
						, objects =>
						  {
							  var signInterpretationData = objects[0] as SignInterpretationData;

							  var nextSignInterpretation = objects[1] as NextSignInterpretation;

							  var signStreamSelectionId = objects[2] as uint?;

							  // Since the Query searches for a single sign interpretation id, we only ever create a single object
							  returnSignInterpretation ??= signInterpretationData;

							  if ((returnSignInterpretation != null)
								  && !returnSignInterpretation.NextSignInterpretations.Contains(
										  nextSignInterpretation))
							  {
								  returnSignInterpretation.NextSignInterpretations.Add(
										  nextSignInterpretation);
							  }

							  if ((returnSignInterpretation != null)
								  && signStreamSelectionId.HasValue
								  && !returnSignInterpretation.SignStreamSectionIds.Contains(
										  signStreamSelectionId.Value))
							  {
								  returnSignInterpretation.SignStreamSectionIds.Add(
										  signStreamSelectionId.Value);
							  }

							  return returnSignInterpretation;
						  }
						, new
						{
								user.EditionId
								, SignInterpretationId = signInterpretationId
								,
						}
						, splitOn: "NextSignInterpretationId, SignStreamSectionId");

				returnSignInterpretation.Attributes = await attributes;

				returnSignInterpretation.Commentaries = (await commentaries).AsList();

				returnSignInterpretation.SignInterpretationRois = rois.AsList();

				transactionScope.Complete();

				return returnSignInterpretation;
			}
		}

		public async Task UpdateSignInterpretationCharacterById(
				UserInfo user
				, uint   signInterpretationId
				, string character
				, byte   priority
				, uint   attributeValueId)
		{
			using (var transactionScope =
					new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
			using (var conn = OpenConnection())
			{
				var signInterpretationCharacterIds = await conn.QueryAsync<uint>(
						FindSignInterpretationCharacterId.GetQuery
						, new
						{
								user.EditionId
								, SignInterpretationId = signInterpretationId
								,
						});

				if (!signInterpretationCharacterIds.Any())
				{
					throw new StandardExceptions.DataNotFoundException(
							"character"
							, signInterpretationId.ToString()
							, "sign_interpretation_character");
				}

				var signInterpretationCharacterId = signInterpretationCharacterIds.First();
				var signInterpretationCharacterParameters = new DynamicParameters();

				signInterpretationCharacterParameters.Add(
						"@sign_interpretation_id"
						, signInterpretationId);

				signInterpretationCharacterParameters.Add("@character", character);

				//signInterpretationCharacterParameters.Add("@priority", priority);
				// TODO: Add support to write the "priority" to the owner table

				var signInterpretationCharacterRequest = new MutationRequest(
						MutateType.Update
						, signInterpretationCharacterParameters
						, "sign_interpretation_character"
						, signInterpretationCharacterId);

				var writeResults = await _databaseWriter.WriteToDatabaseAsync(
						user
						, signInterpretationCharacterRequest);

				// Check whether the request was processed.
				// If so return the new signInterpretationCharacterId.
				if ((writeResults.Count < 1)
					|| !writeResults.First().NewId.HasValue)
				{
					throw new StandardExceptions.DataNotWrittenException(
							"update sign interpretation character");
				}

				var signInterpretationAttribute =
						await _attributeRepository
								.GetSignInterpretationAttributesByInterpretationId(
										user
										, signInterpretationId);

				// Check if the correct attribute value is already set
				if (signInterpretationAttribute.Any(x => x.AttributeValueId == attributeValueId))
				{
					transactionScope.Complete();

					return;
				}

				// if not the old one must be deleted and the new one set
				var deleteAttribute = signInterpretationAttribute.First(x => x.AttributeId == 1);

				// delete the old attribute
				await _attributeRepository.DeleteAttributeFromSignInterpretationAsync(
						user
						, signInterpretationId
						, deleteAttribute.AttributeValueId.Value);

				// update to the current attribute
				deleteAttribute.AttributeValueId = attributeValueId;

				await _attributeRepository.CreateSignInterpretationAttributesAsync(
						user
						, signInterpretationId
						, deleteAttribute);

				transactionScope.Complete();
			}
		}
	}
}
