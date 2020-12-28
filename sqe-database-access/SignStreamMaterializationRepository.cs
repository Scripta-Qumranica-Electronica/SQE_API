using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using MoreLinq.Extensions;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.DatabaseAccess
{
	public interface ISignStreamMaterializationRepository
	{
		Task<IEnumerable<SignStreamMaterializationSchedule>>
				GetAllScheduledSignStreamMaterializationsAsync();

		Task RequestMaterializationAsync(
				uint                                                 editionId
				, uint                                               signInterpretationId
				, SignStreamGraph                                    graph    = null
				, IReadOnlyDictionary<uint, BasicSignInterpretation> SignDict = null);

		Task RequestMaterializationAsync(uint editionId);
		Task MaterializeAllSignStreamsAsync();

		Task<bool> IsCycleAsync(
				uint   editionId
				, uint signInterpretationId
				, uint nextSignInterpretationId);
	}

	public class SignStreamMaterializationRepository : DbConnectionBase
													   , ISignStreamMaterializationRepository
	{
		public SignStreamMaterializationRepository(IConfiguration config) : base(config) { }
		public bool RunMaterialization { get; set; } = true;

		public async Task<IEnumerable<SignStreamMaterializationSchedule>>
				GetAllScheduledSignStreamMaterializationsAsync()
		{
			using (var connection = OpenConnection())
			{
				return await connection.QueryAsync<SignStreamMaterializationSchedule>(
						QueuedMaterializationsQuery.GetQuery);
			}
		}

		public async Task RequestMaterializationAsync(
				uint                                                 editionId
				, uint                                               signInterpretationId
				, SignStreamGraph                                    graph    = null
				, IReadOnlyDictionary<uint, BasicSignInterpretation> signDict = null)
		{
			if (!RunMaterialization)
				return;

			if ((graph == null)
				|| (signDict == null))
				(graph, signDict) = await _getEditionGraph(editionId);

			var firstSignInterpretationIdsInSignStream =
					graph.FindAllPaths(signInterpretationId, true).Select(x => x.Last());

			foreach (var initialSignInterpretationId in firstSignInterpretationIdsInSignStream)
			{
				await _beginMaterializationAsync(
						editionId
						, initialSignInterpretationId
						, graph
						, signDict);
			}
		}

		public async Task RequestMaterializationAsync(uint editionId)
		{
			if (!RunMaterialization)
				return;

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
			foreach (var materializationRequest in
					await GetAllScheduledSignStreamMaterializationsAsync())
			{
				// Give a padding of 5 minutes so that any in-process jobs can be
				// allowed to complete.  If they failed, we will get them on the next
				// scheduled materialization.
				if (materializationRequest.CreatedDate.AddMinutes(5)
					>= materializationRequest.CurrentTime)
					continue;

				var (graph, signDict) = await _getEditionGraph(materializationRequest.EditionId);

				await _materializeStreamForSignInterpretationAsync(
						materializationRequest.EditionId
						, materializationRequest.SignInterpretationId
						, graph
						, signDict);
			}
		}

		/// <summary>
		///  This method checks to see if a path exists from signInterpretationId
		///  to nextSignInterpretationId
		/// </summary>
		/// <param name="editionId">The edition in which the sign stream is searched</param>
		/// <param name="signInterpretationId">The starting node of the search</param>
		/// <param name="nextSignInterpretationId">The desired goal node of the search</param>
		/// <returns>
		///  True if a path exists from signInterpretationId to
		///  nextSignInterpretationId, otherwise false
		/// </returns>
		public async Task<bool> IsCycleAsync(
				uint   editionId
				, uint signInterpretationId
				, uint nextSignInterpretationId)
		{
			using (var conn = OpenConnection())
			{
				// First do a fast check with OQGraph, if it says there is no cycle,
				// then that can be trusted
				var oqGraphStreams = await conn.QueryAsync<uint>(
						QuickConfirmExistingPath.GetQuery
						, new
						{
								SignInterpretationId = signInterpretationId
								, NextSignInterpretationId = nextSignInterpretationId
								,
						});

				if (oqGraphStreams.First() == 0)
					return false;

				// If a cycle was found, it is not necessarily true that we have a cycle
				// is this edition, we need to use the recursive CTE to verify that a cycle
				// would indeed exist in this edition
				var preciseGraphStreams = await conn.QueryAsync<uint>(
						PreciseConfirmExistingPath.GetQuery
						, new
						{
								EditionId = editionId
								, SignInterpretationId = signInterpretationId
								, NextSignInterpretationId = nextSignInterpretationId
								,
						});

				if (preciseGraphStreams.First() == 0)
					return false;
			}

			return true;
		}

		private async Task _beginMaterializationAsync(
				uint                                                 editionId
				, uint                                               signInterpretationId
				, SignStreamGraph                                    graph
				, IReadOnlyDictionary<uint, BasicSignInterpretation> signDict)
		{
			// Don't use a transaction here, we want the materialization request to be written
			// and we don't really care at this point if it ever gets accomplished.  We will
			// have a scheduled task to read the queue table and perform any materializations
			// that failed for whatever reason.
			using (var connection = OpenConnection())
			{
				// Check if the request already exists
				var existingRequests =
						(await GetAllScheduledSignStreamMaterializationsAsync()).ToArray();

				var waitCount = 0;

				// Wait 2.5 seconds for the outstanding request to finish before
				// initiating this new one
				while ((waitCount <= 50)
					   && existingRequests.Any(
							   x => (x.EditionId == editionId)
									&& (x.SignInterpretationId == signInterpretationId)))
				{
					await Task.Delay(50);

					existingRequests =
							(await GetAllScheduledSignStreamMaterializationsAsync()).ToArray();

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
								, SignInterpretationId = signInterpretationId
								,
						});

				// Try to perform the materialization.  If it is successful, it
				// will delete the request from the queue
				await _materializeStreamForSignInterpretationAsync(
						editionId
						, signInterpretationId
						, graph
						, signDict);
			}
		}

		private async Task _materializeStreamForSignInterpretationAsync(
				uint                                                 editionId
				, uint                                               signInterpretationId
				, SignStreamGraph                                    graph
				, IReadOnlyDictionary<uint, BasicSignInterpretation> signDict)
		{
			// Wrap this in a transaction, we do not delete the materialization request
			// from the queue until the materialization has actually been performed.
			using (var transaction = new TransactionScope(
					TransactionScopeOption.Required
					, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
			using (var connection = OpenConnection())
			{
				var streams = _parseGraph(signInterpretationId, graph, signDict);

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
				foreach (var (text, index) in streams)
				{
					// Write the materialized stream
					await connection.ExecuteAsync(
							CreateMaterializationsQuery.GetQuery
							, new
							{
									EditionId = editionId
									, SignInterpretationId = signInterpretationId
									, MaterializedText = text
									,
							});

					// Write the indices in large batches (I did not see the possibility to load
					// from a DataTable with MysqlBulkLoader).
					var materializedId =
							await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);

					var sequences = index.Batch(900);

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
								, SignInterpretationId = signInterpretationId
								,
						});

				transaction.Complete();
			}
		}

		private static IEnumerable<(string Text, List<(uint Index, uint Id)> Index)> _parseGraph(
				uint                                                 startId
				, SignStreamGraph                                    graph
				, IReadOnlyDictionary<uint, BasicSignInterpretation> signDict)
		{
			var paths = graph.FindAllPaths(startId);

			return paths.Select(
								x =>
								{
									var index = new List<(uint Index, uint Id)>();
									var text = new StringBuilder("", x.Count);
									var currIdx = 0u;

									foreach (var signInterpretationId in x)
									{
										index.Add((currIdx, signInterpretationId));

										if (!signDict.TryGetValue(
												signInterpretationId
												, out var signInterpretation))
											continue;

										if (string.IsNullOrEmpty(signInterpretation.Character))
										{
											if (signInterpretation.Attributes.Any(
													x => x.AttributeValueId == 2))
												text.Append(" ");

											currIdx += 1;

											continue;
										}

										text.Append(signInterpretation.Character);
										currIdx += 1;
									}

									return (text.ToString(), index);
								})
						.ToList();
		}

		private async
				Task<(SignStreamGraph SignGraph, IReadOnlyDictionary<uint, BasicSignInterpretation>
						SignDict)> _getEditionGraph(uint editionId)
		{
			// Serialize the Database results into a formatted lookup dictionary
			var signDict = new Dictionary<uint, BasicSignInterpretation>();
			var signGraph = new SignStreamGraph(null);

			using (var connection = OpenConnection())
			{
				await connection
						.QueryAsync<BasicSingleSignInterpretation, BasicSignInterpretationAttribute,
								BasicSingleSignInterpretation>(
								AllSignStreamPossibilities.GetQuery
								, (sign, attribute) =>

								  {
									  signGraph.UnsafeAddLink(
											  sign.SignInterpretationId
											  , sign.NextSignInterpretationId);

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
																	  .NextSignInterpretationId
															  ,
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
								, new { EditionId = editionId }
								, splitOn: "AttributeId");
			}

			signGraph.FindLeaves();

			return (signGraph, signDict);
		}
	}
}
