using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using MoreLinq.Extensions;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess
{
	public interface ISignStreamMaterializationRepository
	{
		Task<IEnumerable<(uint EditionId, uint SignInterpretationId)>>
				GetAllScheduledSignStreamMaterializationsAsync();

		Task RequestMaterializationAsync(uint editionId, uint signInterpretationId);
		Task RequestMaterializationAsync(uint editionId);
		Task MaterializeAllSignStreamsAsync();
	}

	public class SignStreamMaterializationRepository : DbConnectionBase
													   , ISignStreamMaterializationRepository
	{
		private readonly IDatabaseWriter _databaseWriter;

		public SignStreamMaterializationRepository(
				IConfiguration    config
				, IDatabaseWriter databaseWriter) : base(config)
			=> _databaseWriter = databaseWriter;

		public async Task<IEnumerable<(uint EditionId, uint SignInterpretationId)>>
				GetAllScheduledSignStreamMaterializationsAsync()
		{
			using (var connection = OpenConnection())
				return await connection.QueryAsync<(uint EditionId, uint SignInterpretationId)>(
						QueuedMaterializationsQuery.GetQuery);
		}

		public async Task RequestMaterializationAsync(uint editionId, uint signInterpretationId)
		{
			var firstSignInterpretationIdsInSignStream =
					await getBeginningsOfSignStreamForInterpretationId(
							editionId
							, signInterpretationId);

			foreach (var initialSignInterpretationId in firstSignInterpretationIdsInSignStream)
				await beginMaterializationAsync(editionId, initialSignInterpretationId);
		}

		public async Task RequestMaterializationAsync(uint editionId)
		{
			using (var connection = OpenConnection())
			{
				var startIds = await connection.QueryAsync<uint>(
						InitialStreamSignInterpretationForEdition.GetQuery
						, new { EditionId = editionId });

				foreach (var startId in startIds)
					await RequestMaterializationAsync(editionId, startId);
			}
		}

		public async Task MaterializeAllSignStreamsAsync()
		{
			// Collect all materialization requests that were not successfully completed.
			foreach (var (editionId, signInterpretationId) in
					await GetAllScheduledSignStreamMaterializationsAsync())
				await materializeStreamForSignInterpretationAsync(editionId, signInterpretationId);
		}

		private async Task<IEnumerable<uint>> getBeginningsOfSignStreamForInterpretationId(
				uint   editionId
				, uint signInterpretationId)
		{
			using (var connection = OpenConnection())
			{
				return await connection.QueryAsync<uint>(
						BeginningsOfStreamForSignInterpretation.GetQuery
						, new
						{
								EditionId = editionId
								, SignInterpretationId = signInterpretationId
								,
						});
			}
		}

		private async Task beginMaterializationAsync(uint editionId, uint signInterpretationId)
		{
			// Don't use a transaction here, we want the materialization request to be written
			// and we don't really care at this point if it ever gets accomplished.  We will
			// have a scheduled task to read the queue table and perform any materializations
			// that failed for whatever reason.
			using (var connection = OpenConnection())
			{
				// Check if the request already exists
				var existingRequests = await GetAllScheduledSignStreamMaterializationsAsync();
				var waitCount = 0;

				// Wait 2.5 seconds for the outstanding request to finish before
				// initiating this new one
				while ((waitCount <= 50)
					   && existingRequests.Any(
							   x => (x.EditionId == editionId)
									&& (x.SignInterpretationId == signInterpretationId)))
				{
					await Task.Delay(50);
					existingRequests = await GetAllScheduledSignStreamMaterializationsAsync();
					waitCount += 1;
				}

				// Give up if that stream is still being processed,
				// a scheduled task will pick it up later.
				if (existingRequests.Any(
						x => (x.EditionId == editionId)
							 && (x.SignInterpretationId == signInterpretationId)))
					return;

				// Record the request for a materialization
				await connection.ExecuteAsync(
						CreateQueuedMaterializationsQuery.GetQuery
						, new
						{
								EditionId = editionId
								, SignInterpretationId = signInterpretationId,
						});

				// Try to perform the materialization.  If it is successful, it
				// will delete the request from the queue
				await materializeStreamForSignInterpretationAsync(editionId, signInterpretationId);
			}
		}

		private async Task materializeStreamForSignInterpretationAsync(
				uint   editionId
				, uint signInterpretationId)
		{
			// Wrap this in a transaction, we do not delete the materialization request
			// from the queue until the materialization has actually been performed.
			using (var transaction = new TransactionScope(
					TransactionScopeOption.Required
					, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
			using (var connection = OpenConnection())
			{
				await connection.ExecuteAsync(
						DeleteQueuedMaterializationQuery.GetQuery
						, new
						{
								EditionId = editionId
								, SignInterpretationId = signInterpretationId,
						});

				// Collect the materialized stream possibilities (maintain the indices for individual characters)

				// Serialize the Database results into a formatted lookup dictionary
				var signDict = new Dictionary<uint, BasicSignInterpretation>();

				await connection
						.QueryAsync<BasicSingleSignInterpretation, BasicSignInterpretationAttribute,
								BasicSingleSignInterpretation>(
								AllSignStreamPossibilities.GetQuery
								, (sign, attribute) =>

								  {
									  if (signDict.TryGetValue(
											  sign.SignInterpretationId
											  , out var existingSign))
									  {
										  existingSign.Attributes.Add(attribute);

										  existingSign.NextSignInterpretationIds.Add(
												  sign.NextSignInterpretationId);

										  return sign;
									  }

									  var expandedInterpretation = new BasicSignInterpretation
									  {
											  Attributes =
													  new HashSet<
																	  BasicSignInterpretationAttribute
															  > { attribute }
											  , Character = sign.Character
											  , IsVariant = sign.IsVariant
											  , NextSignInterpretationId =
													  sign.NextSignInterpretationId
											  , NextSignInterpretationIds =
													  new HashSet<uint>
													  {
															  sign
																	  .NextSignInterpretationId,
													  }
											  , SignInterpretationId =
													  sign.SignInterpretationId
											  ,
									  };

									  signDict.Add(
											  sign.SignInterpretationId
											  , expandedInterpretation);

									  return sign;
								  }
								, new
								{
										EditionId = editionId
										, SignInterpretationId = signInterpretationId
										,
								}
								, splitOn: "AttributeId");

				// Use the dictionary to recursively build all possible stream combinations
				var paths = _walkSignStream(0, signInterpretationId, signDict);

				// Delete preexisting materialized streams
				await connection.ExecuteAsync(
						DeleteMaterializationsQuery.GetQuery
						, new
						{
								EditionId = editionId
								, SignInterpretationId = signInterpretationId
								,
						});

				// Write the paths
				foreach (var path in paths)
				{
					// Write the materialized stream
					await connection.ExecuteAsync(
							CreateMaterializationsQuery.GetQuery
							, new
							{
									EditionId = editionId
									, SignInterpretationId = signInterpretationId
									, MaterializedText = string.Join("", path.Text)
									,
							});

					// Write the indices in large batches (I did not see the possibility to load
					// from a DataTable with MysqlBulkLoader).
					var materializedId =
							await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);

					var sequences = path.Indices.Batch(900);

					foreach (var sequence in sequences)
					{
						await connection.ExecuteAsync(
								$@"
INSERT IGNORE INTO materialized_sign_stream_indices (materialized_sign_stream_id, `index`, sign_interpretation_id)
VALUES {
											string.Join(
													",\n"
													, sequence.Select(
															x => $"({materializedId},{x.Index},{x.Id})"))
										}
");
					}
				}

				// Delete the request from the queue table
				await connection.ExecuteAsync(
						DeleteQueuedMaterializationQuery.GetQuery
						, new
						{
								EditionId = editionId
								, SignInterpretationId = signInterpretationId,
						});

				transaction.Complete();
			}
		}

		private static List<(List<string> Text, List<(uint Index, uint Id)> Indices)>
				_walkSignStream(
						uint index
						, uint currentSignInterpretationId
						, IReadOnlyDictionary<uint, BasicSignInterpretation> signDict)
		{
			var response = new List<(List<string> Text, List<(uint Index, uint Id)> Indices)>();

			var text = new List<string>();
			var indices = new List<(uint Index, uint Id)>();

			// Stop recursion when there are no new signs available
			if (!signDict.TryGetValue(currentSignInterpretationId, out var currentSign))
				return response;

			// Add the index info
			indices.Add((index, currentSignInterpretationId));

			// Add a character to the character stream list (if available)
			if (currentSign.Attributes.Any(x => x.AttributeId == 1))
			{
				text.Add(
						string.IsNullOrEmpty(currentSign.Character)
								? " "
								: currentSign.Character);

				index += 1;
			}

			// Recurse on every next interpretation Id
			foreach (var sign in currentSign.NextSignInterpretationIds)
			{
				var nextResponse = _walkSignStream(index, sign, signDict);

				foreach (var (nextText, nextIndices) in nextResponse)
					response.Add(
							(text.Concat(nextText).ToList(), indices.Concat(nextIndices).ToList()));
			}

			if (response.Count == 0)
				response.Add((text, indices));

			return response;
		}
	}
}
