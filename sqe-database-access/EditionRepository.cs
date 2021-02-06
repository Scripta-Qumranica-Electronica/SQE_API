using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

// ReSharper disable ArrangeRedundantParentheses
// ReSharper disable RemoveRedundantBraces
namespace SQE.DatabaseAccess
{
	public interface IEditionRepository
	{
		Task<IEnumerable<Edition>> ListEditionsAsync(uint? userId, uint? editionId);

		Task<Edition> GetEditionAsync(uint? userId, uint editionId);

		Task ChangeEditionNameAsync(UserInfo editionUser, string name);

		Task UpdateEditionMetricsAsync(
				UserInfo editionUser
				, uint   width
				, uint   height
				, int    xOrigin
				, int    yOrigin);

		Task<uint> CopyEditionAsync(
				UserInfo editionUser
				, string name            = null
				, string copyrightHolder = null
				, string collaborators   = null);

		Task ChangeEditionCopyrightAsync(
				UserInfo editionUser
				, string copyrightHolder = null
				, string collaborators   = null);

		Task<string> ArchiveEditionAsync(UserInfo editionUser, string token);

		Task<string> GetArchiveToken(UserInfo editionUser);

		Task<DetailedUserWithToken> RequestAddEditionEditorAsync(
				UserInfo editionUser
				, string editorEmail
				, bool?  mayRead
				, bool?  mayWrite
				, bool?  mayLock
				, bool?  isAdmin);

		Task<DetailedEditionPermission> AddEditionEditorAsync(string token, uint userId);

		Task<List<DetailedEditorRequestPermissions>> GetOutstandingEditionEditorRequestsAsync(
				uint userId);

		Task<List<DetailedEditorInvitationPermissions>> GetOutstandingEditionEditorInvitationsAsync(
				uint userId);

		Task<Permission> ChangeEditionEditorRightsAsync(
				UserInfo editionUser
				, string editorEmail
				, bool?  mayRead
				, bool?  mayWrite
				, bool?  mayLock
				, bool?  isAdmin);

		Task<List<uint>> GetEditionEditorUserIdsAsync(UserInfo editionUser);

		Task<IEnumerable<Edition>> GetManuscriptEditions(uint? userId, uint manuscriptId);

		Task<List<LetterShape>> GetEditionScriptCollectionAsync(UserInfo editonUser);

		Task<List<ScriptTextFragment>> GetEditionScriptLines(UserInfo editionUser);
	}

	public class EditionRepository : DbConnectionBase
									 , IEditionRepository
	{
		private readonly IDatabaseWriter _databaseWriter;

		public EditionRepository(IConfiguration config, IDatabaseWriter databaseWriter) :
				base(config) => _databaseWriter = databaseWriter;

		public async Task<IEnumerable<Edition>> ListEditionsAsync(uint? userId, uint? editionId) //
		{
			using (var connection = OpenConnection())
			{
				var editions = new List<Edition>();
				Edition lastEdition;

				await connection
						.QueryAsync<EditionListQuery.Result, EditorWithPermissions, Edition>(
								EditionListQuery.GetQuery(userId.HasValue, editionId.HasValue)
								, (editionGroup, editor) =>
								  {
									  // Set the copyrights for the previous, and now complete, edition before making the new one
									  if ((editions.LastOrDefault()?.EditionId != null)
										  && (editions.LastOrDefault()?.EditionId
											  != editionGroup.EditionId))
									  {
										  lastEdition = editions.Last();

										  lastEdition.Copyright = Licence.printLicence(
												  lastEdition.CopyrightHolder
												  , string.IsNullOrEmpty(lastEdition.Collaborators)
														  ? string.Join(
																  ", "
																  , lastEdition.Editors.Select(
																		  y =>
																		  {
																			  if ((y.Forename
																				   == null)
																				  && (y.Surname
																					  == null))
																			  {
																				  return y
																						  .EditorEmail;
																			  }

																			  return $@"{
																						  y.Forename
																					  } {
																						  y.Surname
																					  }".Trim();
																		  }))
														  : lastEdition.Collaborators);
									  }

									  if ((editions.LastOrDefault()?.EditionId == null)
										  || (editions.LastOrDefault()?.EditionId
											  != editionGroup.EditionId))
									  {
										  // Now start building the new edition
										  lastEdition = new Edition
										  {
												  Name = editionGroup.Name
												  , Width = editionGroup.Width
												  , Height = editionGroup.Height
												  , XOrigin = editionGroup.XOrigin
												  , YOrigin = editionGroup.YOrigin
												  , PPI = editionGroup.PPI
												  , ManuscriptMetricsEditor =
														  editionGroup.ManuscriptMetricsEditor
												  , Collaborators = editionGroup.Collaborators
												  , Copyright = null
												  , //Licence.printLicence(editionGroup.CopyrightHolder, editionGroup.Collaborators),
												  CopyrightHolder = editionGroup.CopyrightHolder
												  , EditionDataEditorId =
														  editionGroup.EditionDataEditorId
												  , EditionId = editionGroup.EditionId
												  , IsPublic = editionGroup.IsPublic
												  , LastEdit = editionGroup.LastEdit
												  , Locked = editionGroup.Locked
												  , Owner =
														  new User
														  {
																  Email = editionGroup
																		  .CurrentEmail
																  , UserId =
																		  editionGroup
																				  .CurrentUserId
																  ,
														  }
												  , Permission =
														  new Permission
														  {
																  IsAdmin =
																		  editionGroup
																				  .CurrentIsAdmin
																  , MayLock =
																		  editionGroup
																				  .CurrentMayLock
																  , MayWrite =
																		  editionGroup
																				  .CurrentMayWrite
																  , MayRead =
																		  editionGroup
																				  .CurrentMayRead
																  ,
														  }
												  , Thumbnail = editionGroup.Thumbnail
												  , ManuscriptId = editionGroup.ManuscriptId
												  , Editors = new List<EditorWithPermissions>()
												  ,
										  };

										  editions.Add(lastEdition);
									  }

									  // Add the new editor to this edition
									  editions.Last().Editors.Add(editor);

									  return editions.Last();
								  }
								, new
								{
										UserId = userId
										, EditionId = editionId
										,
								}
								, splitOn: "EditorId");

				if (editions.Count <= 0)
					return editions;

				{
					lastEdition = editions.Last();

					lastEdition.Copyright = Licence.printLicence(
							lastEdition.CopyrightHolder
							, string.IsNullOrEmpty(lastEdition.Collaborators)
									? string.Join(
											", "
											, lastEdition.Editors.Select(
													y =>
													{
														if ((y.Forename == null)
															&& (y.Surname == null))
														{
															return y.EditorEmail;
														}

														return $@"{y.Forename} {y.Surname}".Trim();
													}))
									: lastEdition.Collaborators);
				}

				return editions;
			}
		}

		public async Task<Edition> GetEditionAsync(uint? userId, uint editionId) //
		{
			using (var connection = OpenConnection())
			{
				var editionDictionary = new Dictionary<uint, Edition>();
				Edition lastEdition = null;

				await connection.QueryAsync<EditionQuery.Result, EditorWithPermissions, Edition>(
						EditionQuery.GetQuery(userId.HasValue, true)
						, (editionGroup, editor) =>
						  {
							  // Check if we have moved on to a new edition
							  if (!editionDictionary.TryGetValue(
									  editionGroup.EditionId
									  , out lastEdition))
							  {
								  // Set the copyrights for the previous, and now complete, edition before making the new one
								  if (lastEdition != null)
								  {
									  lastEdition.Copyright = Licence.printLicence(
											  lastEdition.CopyrightHolder
											  , string.IsNullOrEmpty(lastEdition.Collaborators)
													  ? string.Join(
															  ", "
															  , lastEdition.Editors.Select(
																	  y =>
																	  {
																		  if ((y.Forename == null)
																			  && (y.Surname == null)
																		  )
																			  return y.EditorEmail;

																		  return $@"{
																					  y.Forename
																				  } {
																					  y.Surname
																				  }".Trim();
																	  }))
													  : lastEdition.Collaborators);
								  }

								  // Now start building the new edition
								  lastEdition = new Edition
								  {
										  Name = editionGroup.Name
										  , Width = editionGroup.Width
										  , Height = editionGroup.Height
										  , XOrigin = editionGroup.XOrigin
										  , YOrigin = editionGroup.YOrigin
										  , PPI = editionGroup.PPI
										  , ManuscriptMetricsEditor =
												  editionGroup.ManuscriptMetricsEditor
										  , Collaborators = editionGroup.Collaborators
										  , Copyright = null
										  , //Licence.printLicence(editionGroup.CopyrightHolder, editionGroup.Collaborators),
										  CopyrightHolder = editionGroup.CopyrightHolder
										  , EditionDataEditorId = editionGroup.EditionDataEditorId
										  , EditionId = editionGroup.EditionId
										  , IsPublic = editionGroup.IsPublic
										  , LastEdit = editionGroup.LastEdit
										  , Locked = editionGroup.Locked
										  , Owner =
												  new User
												  {
														  Email = editionGroup.CurrentEmail
														  , UserId =
																  editionGroup.CurrentUserId
														  ,
												  }
										  , Permission =
												  new Permission
												  {
														  IsAdmin =
																  editionGroup
																		  .CurrentIsAdmin
														  , MayLock =
																  editionGroup
																		  .CurrentMayLock
														  , MayWrite =
																  editionGroup
																		  .CurrentMayWrite
														  , MayRead =
																  editionGroup
																		  .CurrentMayRead
														  ,
												  }
										  , Thumbnail = editionGroup.Thumbnail
										  , ManuscriptId = editionGroup.ManuscriptId
										  , Editors = new List<EditorWithPermissions>()
										  ,
								  };

								  editionDictionary.Add(lastEdition.EditionId, lastEdition);
							  }

							  // Add the new editor to this edition
							  lastEdition.Editors.Add(editor);

							  return lastEdition;
						  }
						, new
						{
								UserId = userId
								, EditionId = editionId
								,
						}
						, splitOn: "EditorId");

				return lastEdition ?? new Edition();
			}
		}

		public async Task ChangeEditionNameAsync(UserInfo editionUser, string name)
		{
			EditionNameQuery.Result result;

			using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
			using (var connection = OpenConnection())
			{
				try
				{
					// Here we get the data from the original scroll_data field, we need the scroll_id,
					// which no one in the front end will generally have or care about.
					result = await connection.QuerySingleAsync<EditionNameQuery.Result>(
							EditionNameQuery.GetQuery()
							, new { editionUser.EditionId });
				}
				catch (InvalidOperationException)
				{
					throw new StandardExceptions.DataNotFoundException(
							"edition"
							, editionUser.EditionId ?? 0);
				}

				// Now we create the mutation object for the requested action
				// You will want to check the database to make sure you what you are doing.
				var nameChangeParams = new DynamicParameters();
				nameChangeParams.Add("@manuscript_id", result.ManuscriptId);
				nameChangeParams.Add("@Name", name);

				var nameChangeRequest = new MutationRequest(
						MutateType.Update
						, nameChangeParams
						, "manuscript_data"
						, result.ManuscriptDataId);

				// Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
				// care of main_action and single_action.
				await _databaseWriter.WriteToDatabaseAsync(
						editionUser
						, new List<MutationRequest> { nameChangeRequest });

				transaction.Complete();
			}
		}

		/// <summary>
		///  Update the metric estimations of the manuscript for an edition
		/// </summary>
		/// <param name="editionUser">Details of the user requesting the changes</param>
		/// <param name="width">A non-negative estimation of the manuscript width in millimeters (may be zero)</param>
		/// <param name="height">A non-negative estimation of the manuscript height in millimeters (may be zero)</param>
		/// <param name="xOrigin">
		///  An estimation of the point at which the manuscript begins on the x axis in millimeters (may be
		///  zero)
		/// </param>
		/// <param name="yOrigin">
		///  An estimation of the point at which the manuscript begins on the x axis in millimeters (may be
		///  zero)(may be zero)
		/// </param>
		/// <returns></returns>
		public async Task UpdateEditionMetricsAsync(
				UserInfo editionUser
				, uint   width
				, uint   height
				, int    xOrigin
				, int    yOrigin)
		{
			using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
			using (var connection = OpenConnection())
			{
				var oldRecord =
						(await connection.QueryAsync<GetEditionManuscriptMetricsDetails.Result>(
								GetEditionManuscriptMetricsDetails.GetQuery
								, new { editionUser.EditionId })).ToList();

				if (oldRecord.Count != 1)
				{
					throw new StandardExceptions.DataNotFoundException(
							"manuscript metrics"
							, editionUser.EditionId ?? 0
							, "edition");
				}

				var parameters = new DynamicParameters();
				parameters.Add("width", width);
				parameters.Add("height", height);
				parameters.Add("x_origin", xOrigin);
				parameters.Add("y_origin", yOrigin);

				parameters.Add("manuscript_id", oldRecord.First().ManuscriptId);

				var mutation = new MutationRequest(
						MutateType.Update
						, parameters
						, "manuscript_metrics"
						, oldRecord.First().ManuscriptMetricsId);

				var results = await _databaseWriter.WriteToDatabaseAsync(editionUser, mutation);

				if (results.Count() != 1)
				{
					throw new StandardExceptions.DataNotWrittenException(
							"update manuscript metrics");
				}

				transaction.Complete();
			}
		}

		/// <summary>
		///  This creates a new copy of the requested edition, which will be owned with full privileges
		///  by the requesting user.
		/// </summary>
		/// <param name="editionUser">
		///  User info object contains the editionId that the user wishes to copy and
		///  all user permissions related to it.
		/// </param>
		/// <param name="name">
		///  New name for the edition.
		/// </param>
		/// <param name="copyrightHolder">
		///  Name of the person/institution that holds the copyright
		///  (automatically created from user when null)
		/// </param>
		/// <param name="collaborators">
		///  Names of all collaborators
		///  (automatically created from user and all editors when null)
		/// </param>
		/// <returns>The editionId of the newly created edition.</returns>
		public async Task<uint> CopyEditionAsync(
				UserInfo editionUser
				, string name            = null
				, string copyrightHolder = null
				, string collaborators   = null)
		{
			if (!editionUser.EditionId.HasValue)
				throw new StandardExceptions.ImproperInputDataException("edition id");

			// Note, we had tried to make this quicker by collecting all the edition info in a single
			// transaction, then performing the writes in a separate transaction. It turns out that
			// approach is about 4 times slower than the one here.
			List<OwnerTables.Result> ownerTables;

			using (var connection = OpenConnection())
			{
				ownerTables =
						(await connection.QueryAsync<OwnerTables.Result>(OwnerTables.GetQuery))
						.ToList();
			}

			return await DatabaseCommunicationRetryPolicy.ExecuteRetry(
					async () =>
					{
						// In an effort to speed this up further, I tried disabling foreign keys and unique checks.
						// It made no appreciable difference:
						// await connection.ExecuteAsync("SET @@session.foreign_key_checks=0;");
						// await connection.ExecuteAsync("SET @@session.unique_checks=0;");
						using (var transactionScope =
								new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
						using (var connection = OpenConnection())
						{
							// Create a new edition
							await connection.ExecuteAsync(
									CopyEditionQuery.GetQuery
									, new
									{
											editionUser.EditionId
											, CopyrightHolder = copyrightHolder
											, Collaborators = collaborators
											,
									});

							var toEditionId =
									await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);

							if (toEditionId == 0)
							{
								throw new StandardExceptions.DataNotWrittenException(
										"create edition");
							}

							// Create new edition_editor
							await connection.ExecuteAsync(
									CreateEditionEditorQuery.GetQuery
									, new
									{
											EditionId = toEditionId
											, UserId = editionUser.userId
											, MayLock = 1
											, IsAdmin = 1
											,
									});

							var toEditionEditorId =
									await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);

							if (toEditionEditorId == 0)
							{
								throw new StandardExceptions.DataNotWrittenException(
										"create edition_editor");
							}

							uint? manuscriptDataId = null;

							if (!string.IsNullOrEmpty(name))
							{
								await connection.ExecuteAsync(
										@"
                        INSERT INTO manuscript_data (manuscript_id, name, creator_id)
                        SELECT manuscript_id, @Name, @UserId
                        FROM edition
                        WHERE edition.edition_id = @EditionId
                        ON DUPLICATE KEY UPDATE manuscript_data_id=LAST_INSERT_ID(manuscript_data_id)"
										, new
										{
												Name = name
												, UserId = editionUser.userId
												, EditionId = toEditionId
												,
										});

								manuscriptDataId =
										await connection.QuerySingleAsync<uint>(
												LastInsertId.GetQuery);
							}

							foreach (var ownerTable in ownerTables)
							{
								var tableName = ownerTable.TableName;

								var tableIdColumn =
										tableName.Substring(0, tableName.Length - 5) + "id";

								if ((tableName == "manuscript_data_owner")
									&& manuscriptDataId.HasValue)
								{
									await connection.ExecuteAsync(
											@"
INSERT INTO manuscript_data_owner (manuscript_data_id, edition_id, edition_editor_id)
VALUES (@ManuscriptDataId, @EditionId, @EditionEditorId)"
											, new
											{
													EditionId = toEditionId
													, EditionEditorId = toEditionEditorId
													, ManuscriptDataId =
															manuscriptDataId.Value
													,
											});

									continue;
								}

								// Should I do any error checking here?
								await connection.ExecuteAsync(
										CopyTableQuery.GetQuery(
												tableName
												, tableIdColumn
												, toEditionId
												, toEditionEditorId
												, editionUser.EditionId.Value));
							}

							//Cleanup
							transactionScope.Complete();

							return toEditionId;
						}
					});
		}

		/// <summary>
		///  Change copyright holder and/or collaborators of the users current edition.
		/// </summary>
		/// <param name="editionUser">The user's current state.</param>
		/// <param name="copyrightHolder">The new copyright holder name to use</param>
		/// <param name="collaborators">
		///  The new collaborator list. Null is meaningful here
		///  and will switch to an autogenerated collaborator listing.
		/// </param>
		/// <returns></returns>
		public async Task ChangeEditionCopyrightAsync(
				UserInfo editionUser
				, string copyrightHolder = null
				, string collaborators   = null)
		{
			// Let's only allow admins to change these legal details.
			if (!editionUser.IsAdmin)
				throw new StandardExceptions.NoAdminPermissionsException(editionUser);

			using (var connection = OpenConnection())
			{
				await connection.ExecuteAsync(
						UpdateEditionLegalDetailsQuery.GetQuery
						, new
						{
								editionUser.EditionId
								, CopyrightHolder = copyrightHolder
								, Collaborators = collaborators
								,
						});
			}
		}

		/// <summary>
		///  Archive an edition that the user is currently subscribed to.
		/// </summary>
		/// <param name="editionUser">User object requesting the achival</param>
		/// <param name="token">
		///  Token required to verify archiving. If this is null, one will be created and sent
		///  to the requester to use a confirmation of the archival process.
		/// </param>
		/// <returns>Returns a null string if successful; a string with a confirmation token if no token was provided.</returns>
		public async Task<string> ArchiveEditionAsync(UserInfo editionUser, string token)
		{
			// We only allow admins to delete all data in an unlocked edition.
			if (!editionUser.IsAdmin)
				throw new StandardExceptions.NoAdminPermissionsException(editionUser);

			// A token is required to delete an edition (we make sure here that people don't accidentally do it)
			if (string.IsNullOrEmpty(token))
				return await GetArchiveToken(editionUser);

			using (var connection = OpenConnection())
			{
				// Verify that the token is still valid
				var archiveToken = await connection.ExecuteAsync(
						DeleteUserEmailTokenQuery.GetTokenQuery
						, new
						{
								Tokens = new[] { token }
								, Type = CreateUserEmailTokenQuery.DeleteEdition
								,
						});

				if (archiveToken != 1)
				{
					throw new StandardExceptions.DataNotWrittenException(
							"verifying the delete request token");
				}

				const string archiveSql =
						"UPDATE edition SET archived = 1 WHERE edition_id = @EditionId";

				var archive = await connection.ExecuteAsync(
						archiveSql
						, new { editionUser.EditionId });

				if (archive != 1)
				{
					throw new StandardExceptions.DataNotWrittenException(
							"archive edition"
							, "unknown reason");
				}

				return null;
			}
		}

		public async Task<string> GetArchiveToken(UserInfo editionUser)
		{
			// Generate our secret token
			var token = Guid.NewGuid().ToString();

			using (var connection = OpenConnection())
			{
				// Add the secret token to the database
				var userEmailConfirmation = await connection.ExecuteAsync(
						CreateUserEmailTokenQuery.GetQuery()
						, new
						{
								UserId = editionUser.userId
								, Token = token
								, Type = CreateUserEmailTokenQuery.DeleteEdition
								,
						});

				if (userEmailConfirmation != 1) // Something strange must have gone wrong
				{
					throw new StandardExceptions.DataNotWrittenException(
							"create edition delete token");
				}
			}

			return token;
		}

		/// <summary>
		///  Initiate a request for a user to be added as editor to an edition. This creates a token, which
		///  the requested editor can use to confirm the request.
		/// </summary>
		/// <param name="editionUser">User object requesting the new editor</param>
		/// <param name="editorEmail">New editor's email address</param>
		/// <param name="mayRead">Permission to read</param>
		/// <param name="mayWrite">Permission to write</param>
		/// <param name="mayLock">Permission to lock</param>
		/// <param name="isAdmin">Permission to admin</param>
		/// <returns></returns>
		public async Task<DetailedUserWithToken> RequestAddEditionEditorAsync(
				UserInfo editionUser
				, string editorEmail
				, bool?  mayRead
				, bool?  mayWrite
				, bool?  mayLock
				, bool?  isAdmin)
		{
			// Make sure requesting user is admin; only an edition admin may perform this action
			if (!editionUser.IsAdmin)
				throw new StandardExceptions.NoAdminPermissionsException(editionUser);

			// Instantiate the return object
			DetailedUserWithToken editorInfo;

			using (var transactionScope =
					new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
			{
				// Check if the editor already exists, don't attempt to re-add
				if ((await _getEditionEditors(editionUser.EditionId.Value)).Any(
						x => x.Email == editorEmail))
					throw new StandardExceptions.ConflictingDataException("editor email");

				// Set the permissions object by coalescing with the default values
				var permissions = new Permission
				{
						MayRead = mayRead ?? true
						, MayWrite = mayWrite ?? false
						, MayLock = mayLock ?? false
						, IsAdmin = isAdmin ?? false
						,
				};

				// Check for invalid settings
				if (permissions.IsAdmin
					&& !permissions.MayRead)
				{
					throw new StandardExceptions.InputDataRuleViolationException(
							"an edition admin must have read rights");
				}

				if (permissions.MayWrite
					&& !permissions.MayRead)
				{
					throw new StandardExceptions.InputDataRuleViolationException(
							"an editor with write rights must have read rights");
				}

				using (var connection = OpenConnection())
				{
					// Find the editor
					var editorInfoSearch = (await connection.QueryAsync<DetailedUserWithToken>(
							UserDetails.GetQuery(
									new List<string>
									{
											"user_id"
											, "forename"
											, "surname"
											, "organization"
											,
									}
									, new List<string> { "email" })
							, new { Email = editorEmail })).ToList();

					// Throw a meaningful error if the user's email was not found in the system.
					if (!editorInfoSearch.Any())
					{
						throw new StandardExceptions.DataNotFoundException(
								"editors"
								, editorEmail
								, "users");
					}

					editorInfo = editorInfoSearch.FirstOrDefault();

					// Check for existing request
					var existingRequestToken = (await connection.QueryAsync<Guid>(
							FindEditionEditorRequestByEditorEdition.GetQuery
							, new
							{
									editionUser.EditionId
									, AdminUserId = editionUser.userId
									, EditorUserId = editorInfo.UserId
									,
							})).ToList();

					// Add a GUID for this transaction (Reuse any pre-existing ones)
					if (existingRequestToken.Any())
						editorInfo.Token = existingRequestToken.FirstOrDefault();
					else
					{
						editorInfo.Token = existingRequestToken.Any()
								? existingRequestToken.FirstOrDefault()
								: Guid.NewGuid();

						// Write the GUID token to the database
						var writtenToken = await connection.ExecuteAsync(
								CreateUserEmailTokenQuery.GetQuery()
								, new
								{
										editorInfo.UserId
										, editorInfo.Token
										, Type = CreateUserEmailTokenQuery.EditorInvite
										,
								});

						if (writtenToken != 1)
						{
							throw new StandardExceptions.DataNotWrittenException(
									$"create editor invite token for {editorEmail}");
						}
					}

					// Record the editor request in database
					await connection.ExecuteAsync(
							RecordEditionEditorRequest.GetQuery
							, new
							{
									editorInfo.Token
									, AdminUserId = editionUser.userId
									, EditorUserId = editorInfo.UserId
									, editionUser.EditionId
									, permissions.IsAdmin
									, permissions.MayLock
									, permissions.MayWrite
									,
							});
				}

				// Complete the transaction
				transactionScope.Complete();
			}

			// Get datetime of request
			using (var connection = OpenConnection())
			{
				var date = (await connection.QueryAsync<DateTime>(
						GetEditionEditorRequestDate.GetQuery
						, new { editorInfo.Token })).AsList();

				if (date.Count != 1)
				{
					throw new StandardExceptions.DataNotWrittenException(
							"generate edition share request");
				}

				editorInfo.Date = date.FirstOrDefault();
			}

			// Return the results
			return editorInfo;
		}

		public async Task<DetailedEditionPermission> AddEditionEditorAsync(
				string token
				, uint userId)
		{
			DetailedEditionPermission editorEditionPermission;

			using (var transactionScope =
					new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
			using (var connection = OpenConnection())
			{
				var editorEditionPermissions =
						(await connection.QueryAsync<DetailedEditionPermission>(
								FindEditionEditorRequestByToken.GetQuery
								, new
								{
										Token = token
										, EditorUserId = userId
										,
								})).AsList();

				// Make sure the token exists
				if (!editorEditionPermissions.Any())
					throw new StandardExceptions.DataNotFoundException("token", token);

				editorEditionPermission = editorEditionPermissions.First();

				editorEditionPermission.MayRead = true; // Invited editors always have read access

				// Check if the editor already exists, don't attempt to re-add
				if ((await _getEditionEditors(editorEditionPermission.EditionId)).Any(
						x => x.Email == editorEditionPermission.Email))
					throw new StandardExceptions.ConflictingDataException("editor email");

				// Add the editor
				var editorUpdateExecution = await connection.ExecuteAsync(
						CreateDetailedEditionEditorQuery.GetQuery
						, new
						{
								editorEditionPermission.EditionId
								, editorEditionPermission.Email
								, editorEditionPermission.MayRead
								, editorEditionPermission.MayWrite
								, editorEditionPermission.MayLock
								, editorEditionPermission.IsAdmin
								,
						});

				if (editorUpdateExecution != 1)
				{
					throw new StandardExceptions.DataNotWrittenException(
							$"update permissions for {editorEditionPermission.Email}");
				}

				// Delete unneeded database entries
				await connection.ExecuteAsync(
						DeleteEditionEditorRequest.GetQuery
						, new
						{
								Token = new Guid(token)
								, EditorUserId = userId
								,
						});

				await connection.ExecuteAsync(
						DeleteUserEmailTokenQuery.GetTokenQuery
						, new
						{
								Tokens = new List<Guid> { new Guid(token) }
								, Type = CreateUserEmailTokenQuery.EditorInvite
								,
						});

				transactionScope.Complete();
			}

			// Return the results
			return editorEditionPermission;
		}

		/// <summary>
		///  Requests a list of editor requests made by the user, which have not yet been accepted
		/// </summary>
		/// <param name="userId">Id of the admin who has issued the request for a user to become an editor</param>
		/// <returns></returns>
		public async Task<List<DetailedEditorRequestPermissions>>
				GetOutstandingEditionEditorRequestsAsync(uint userId)
		{
			using var connection = OpenConnection();

			return (await connection.QueryAsync<DetailedEditorRequestPermissions>(
					FindEditionEditorRequestByAdminId.GetQuery
					, new { AdminUserId = userId })).ToList();
		}

		/// <summary>
		///  Requests a list of invitations to become an editor, which have been sent to the user
		/// </summary>
		/// <param name="userId">Id of the user who has been invited to become editor</param>
		/// <returns></returns>
		public async Task<List<DetailedEditorInvitationPermissions>>
				GetOutstandingEditionEditorInvitationsAsync(uint userId)
		{
			using (var connection = OpenConnection())
			{
				return (await connection.QueryAsync<DetailedEditorInvitationPermissions>(
						FindEditionEditorRequestByEditorId.GetQuery
						, new { EditorUserId = userId })).ToList();
			}
		}

		public async Task<Permission> ChangeEditionEditorRightsAsync(
				UserInfo editionUser
				, string editorEmail
				, bool?  mayRead
				, bool?  mayWrite
				, bool?  mayLock
				, bool?  isAdmin)
		{
			// Make sure requesting user is admin when raising access, only and edition admin may perform this action
			if (((mayRead ?? false)
				 || (mayWrite ?? false)
				 || (mayLock ?? false)
				 || (isAdmin ?? false))
				&& !editionUser.IsAdmin)
				throw new StandardExceptions.NoAdminPermissionsException(editionUser);

			// Check if the editor exists
			var editors = await _getEditionEditors(editionUser.EditionId.Value);

			var currentEditorSettingsList = editors.Where(x => x.Email == editorEmail).ToList();

			if (currentEditorSettingsList.Count != 1) // There should be only 1 record
			{
				throw new StandardExceptions.DataNotFoundException(
						"editor email"
						, editionUser.EditionId.ToString()
						, "edition_editors");
			}

			// Set the new permissions object by coalescing the new settings with those already existing
			var currentEditorSettings = currentEditorSettingsList.First();

			var permissions = new Permission
			{
					MayRead = mayRead ?? currentEditorSettings.MayRead
					, MayWrite = mayWrite ?? currentEditorSettings.MayWrite
					, MayLock = mayLock ?? currentEditorSettings.MayLock
					, IsAdmin = isAdmin ?? currentEditorSettings.IsAdmin
					,
			};

			// Make sure we are not removing an admin's read access (that is not allowed)
			if (permissions.IsAdmin
				&& !permissions.MayRead)
			{
				throw new StandardExceptions.InputDataRuleViolationException(
						"read rights may not be revoked for an edition admin");
			}

			// Make sure that we are not revoking editor's read access when editor still has write access
			if (permissions.MayWrite
				&& !permissions.MayRead)
			{
				throw new StandardExceptions.InputDataRuleViolationException(
						"read rights may not be revoked for an editor with write rights");
			}

			using (var connection = OpenConnection())
			{
				// If the last admin is giving up admin rights, return error message with token for complete delete
				if (!editors.Any(
						x => ((x.Email == editorEmail) && permissions.IsAdmin)
							 || ((x.Email != editorEmail) && x.IsAdmin)))
				{
					throw new StandardExceptions.InputDataRuleViolationException(
							$@"an edition must have at least one admin.
Please give admin status to another editor before relinquishing admin status for the current user or deleting the edition.
An admin may delete the edition for all editors with the request DELETE /v1/editions/{
										editionUser.EditionId.ToString()
									}.");
				}

				// Perform the update
				var editorUpdateExecution = await connection.ExecuteAsync(
						UpdateEditionEditorPermissionsQuery.GetQuery
						, new
						{
								editionUser.EditionId
								, Email = editorEmail
								, permissions.MayRead
								, permissions.MayWrite
								, permissions.MayLock
								, permissions.IsAdmin
								,
						});

				if (editorUpdateExecution != 1)
				{
					throw new StandardExceptions.DataNotWrittenException(
							$"update permissions for {editorEmail}");
				}

				// Return the results
				return permissions;
			}

			// In the future should we email the editor about their change in status?
		}

		/// <summary>
		///  Gets the user id's of each editor working on an edition.  This is useful for
		///  collecting the user id's to which a SignalR message must be broadcast.  This
		///  data is not intended to be made public to any clients.
		/// </summary>
		/// <param name="editionUser">User object requesting the delete</param>
		/// <returns></returns>
		public async Task<List<uint>> GetEditionEditorUserIdsAsync(UserInfo editionUser)
		{
			using (var connection = OpenConnection())
			{
				return (await connection.QueryAsync<uint>(
						EditionEditorUserIds.GetQuery
						, new
						{
								editionUser.EditionId
								, UserId = editionUser.userId
								,
						})).ToList();
			}
		}

		public async Task<IEnumerable<Edition>> GetManuscriptEditions(
				uint?  userId
				, uint manuscriptId)
		{
			using (var conn = OpenConnection())
			{
				var editions = new List<Edition>();
				Edition lastEdition;

				await conn.QueryAsync<EditionListQuery.Result, EditorWithPermissions, Edition>(
						EditionListQuery.GetQuery(userId.HasValue, false, true)
						, (editionGroup, editor) =>
						  {
							  // Set the copyrights for the previous, and now complete, edition before making the new one
							  if ((editions.LastOrDefault()?.EditionId != null)
								  && (editions.LastOrDefault()?.EditionId
									  != editionGroup.EditionId))
							  {
								  lastEdition = editions.Last();

								  lastEdition.Copyright = Licence.printLicence(
										  lastEdition.CopyrightHolder
										  , string.IsNullOrEmpty(lastEdition.Collaborators)
												  ? string.Join(
														  ", "
														  , lastEdition.Editors.Select(
																  y =>
																  {
																	  if ((y.Forename == null)
																		  && (y.Surname == null))
																	  {
																		  return y.EditorEmail;
																	  }

																	  return $@"{
																				  y.Forename
																			  } {
																				  y.Surname
																			  }".Trim();
																  }))
												  : lastEdition.Collaborators);
							  }

							  if ((editions.LastOrDefault()?.EditionId == null)
								  || (editions.LastOrDefault()?.EditionId
									  != editionGroup.EditionId))
							  {
								  // Now start building the new edition
								  lastEdition = new Edition
								  {
										  Name = editionGroup.Name
										  , Width = editionGroup.Width
										  , Height = editionGroup.Height
										  , XOrigin = editionGroup.XOrigin
										  , YOrigin = editionGroup.YOrigin
										  , PPI = editionGroup.PPI
										  , ManuscriptMetricsEditor =
												  editionGroup.ManuscriptMetricsEditor
										  , Collaborators = editionGroup.Collaborators
										  , Copyright = null
										  , //Licence.printLicence(editionGroup.CopyrightHolder, editionGroup.Collaborators),
										  CopyrightHolder = editionGroup.CopyrightHolder
										  , EditionDataEditorId = editionGroup.EditionDataEditorId
										  , EditionId = editionGroup.EditionId
										  , IsPublic = editionGroup.IsPublic
										  , LastEdit = editionGroup.LastEdit
										  , Locked = editionGroup.Locked
										  , Owner =
												  new User
												  {
														  Email = editionGroup.CurrentEmail
														  , UserId =
																  editionGroup.CurrentUserId
														  ,
												  }
										  , Permission =
												  new Permission
												  {
														  IsAdmin =
																  editionGroup
																		  .CurrentIsAdmin
														  , MayLock =
																  editionGroup
																		  .CurrentMayLock
														  , MayWrite =
																  editionGroup
																		  .CurrentMayWrite
														  , MayRead =
																  editionGroup
																		  .CurrentMayRead
														  ,
												  }
										  , Thumbnail = editionGroup.Thumbnail
										  , ManuscriptId = editionGroup.ManuscriptId
										  , Editors = new List<EditorWithPermissions>()
										  ,
								  };

								  editions.Add(lastEdition);
							  }

							  // Add the new editor to this edition
							  editions.Last().Editors.Add(editor);

							  return editions.Last();
						  }
						, new
						{
								UserId = userId
								, ManuscriptId = manuscriptId
								,
						}
						, splitOn: "EditorId");

				if (editions.Count <= 0)
					return editions;

				{
					lastEdition = editions.Last();

					lastEdition.Copyright = Licence.printLicence(
							lastEdition.CopyrightHolder
							, string.IsNullOrEmpty(lastEdition.Collaborators)
									? string.Join(
											", "
											, lastEdition.Editors.Select(
													y =>
													{
														if ((y.Forename == null)
															&& (y.Surname == null))
														{
															return y.EditorEmail;
														}

														return $@"{y.Forename} {y.Surname}".Trim();
													}))
									: lastEdition.Collaborators);
				}

				return editions;
			}
		}

		public async Task<List<LetterShape>> GetEditionScriptCollectionAsync(UserInfo editonUser)
		{
			using (var connection = OpenConnection())
			{
				return (await connection.QueryAsync<LetterShape>(
						EditionScriptQuery.GetQuery
						, new
						{
								editonUser.EditionId
								, UserId = editonUser.userId ?? 0
								,
						})).ToList();
			}
		}

		public async Task<List<ScriptTextFragment>> GetEditionScriptLines(UserInfo editionUser)
		{
			// Placeholders for query mapping
			ScriptTextFragment lastScriptTextFragment = null;
			ScriptLine lastScriptLine = null;
			ScriptArtefactCharacters lastScriptArtefactCharacters = null;
			Character lastCharacters = null;
			SpatialRoi lastSpatialRoi = null;
			CharacterAttribute lastCharacterAttribute = null;
			CharacterStreamPosition lastCharacterStreamPosition = null;

			using (var connection = OpenConnection())
			{
				var scriptLines = await connection.QueryAsync(
						EditionScriptLines.GetQuery
						, new[]
						{
								typeof(ScriptTextFragment)
								, typeof(ScriptLine)
								, typeof(ScriptArtefactCharacters)
								, typeof(Character)
								, typeof(SpatialRoi)
								, typeof(CharacterAttribute)
								, typeof(CharacterStreamPosition)
								,
						}
						, objects =>
						  {
							  // Collect the mapped objects
							  if (!(objects[0] is ScriptTextFragment scriptTextFragment))
								  return null;

							  if (!(objects[1] is ScriptLine scriptLine))
								  return null;

							  if (!(objects[2] is ScriptArtefactCharacters scriptArtefactCharacters)
							  )
								  return null;

							  if (!(objects[3] is Character character))
								  return null;

							  if (!(objects[4] is SpatialRoi spatialRoi))
								  return null;

							  if (!(objects[5] is CharacterAttribute characterAttribute))
								  return null;

							  if (!(objects[6] is CharacterStreamPosition characterStreamPosition))
								  return null;

							  // Construct the nestings
							  var newTextFragment = scriptTextFragment.TextFragmentId
													!= lastScriptTextFragment?.TextFragmentId;

							  if (newTextFragment)
							  {
								  lastScriptTextFragment = scriptTextFragment;

								  lastScriptTextFragment.Lines = new List<ScriptLine>();
							  }

							  if (scriptLine.LineId != lastScriptLine?.LineId)
							  {
								  lastScriptLine = scriptLine;

								  lastScriptLine.Artefacts = new List<ScriptArtefactCharacters>();

								  lastScriptTextFragment.Lines.Add(lastScriptLine);
							  }

							  if (scriptArtefactCharacters.ArtefactId
								  != lastScriptArtefactCharacters?.ArtefactId)
							  {
								  lastScriptArtefactCharacters = scriptArtefactCharacters;

								  lastScriptArtefactCharacters.Characters = new List<Character>();

								  lastScriptLine.Artefacts.Add(lastScriptArtefactCharacters);
							  }

							  if (character.SignInterpretationId
								  != lastCharacters?.SignInterpretationId)
							  {
								  lastCharacters = character;

								  lastCharacters.Attributes = new List<CharacterAttribute>();

								  lastCharacters.Rois = new List<SpatialRoi>();

								  lastCharacters.NextCharacters =
										  new List<CharacterStreamPosition>();

								  lastScriptArtefactCharacters.Characters.Add(lastCharacters);
							  }

							  if (spatialRoi.SignInterpretationRoiId
								  != lastSpatialRoi?.SignInterpretationRoiId)
							  {
								  lastSpatialRoi = spatialRoi;

								  lastCharacters.Rois.Add(lastSpatialRoi);
							  }

							  if (characterAttribute.SignInterpretationAttributeId
								  != lastCharacterAttribute?.SignInterpretationAttributeId)
							  {
								  lastCharacterAttribute = characterAttribute;

								  lastCharacters.Attributes.Add(lastCharacterAttribute);
							  }

							  if (characterStreamPosition.PositionInStreamId
								  == lastCharacterStreamPosition?.PositionInStreamId)
								  return scriptTextFragment;

							  lastCharacterStreamPosition = characterStreamPosition;

							  lastCharacters.NextCharacters.Add(lastCharacterStreamPosition);

							  return scriptTextFragment;
						  }
						, new
						{
								editionUser.EditionId
								, UserId = editionUser.userId
								,
						}
						, splitOn:
						"LineId,ArtefactId,SignInterpretationId,SignInterpretationRoiId,SignInterpretationAttributeId,PositionInStreamId");

				return scriptLines.Where(x => x != null).ToList();
			}
		}

		/// <summary>
		///  This method performs a full wipe of an edition's data. The information for the edition
		///  remains but the association with this edition is deleted. This method is intended
		///  for system admins to use.
		/// </summary>
		/// <param name="editionUser">User details for the edition to be deleted</param>
		/// <returns></returns>
		private async Task _fullEditionDelete(UserInfo editionUser)
		{
			// Remove write permissions from all editors, so they cannot make any changes while the delete proceeds
			var editors = await _getEditionEditors(editionUser.EditionId.Value);

			foreach (var editor in editors)
			{
				await ChangeEditionEditorRightsAsync(
						editionUser
						, editor.Email
						, editor.MayRead
						, false
						, editor.MayLock
						, editor.IsAdmin);
			}

			// Note: I had wrapped the following in a transaction, but this has the problem that it can lockup every
			// *_owner table in the entire database for a significant amount of time (sometimes 1000's of rows will be
			// deleted from a single table). So I am doing it now without any transaction and with some retry
			// logic.  What this means is that a delete might be partially carried out and return with an error,
			// in which case the user will need to try again. This is not too worrisome since an inconsistent state
			// for a deleted edition is not a cause for user concern (the users only care about the edition
			// becoming unusable, not whether any data was left behind). It is a concern for those maintaining the
			// database, and we should discuss what might be done for that.  We could check for this and other things
			// with some "health check" services.
			using (var connection = OpenConnection())
			{
				// Dynamically get all tables that can be part of an edition, that way we don't worry about
				// this breaking due to future updates.
				var dataTables =
						await connection.QueryAsync<OwnerTables.Result>(OwnerTables.GetQuery);

				// Loop over every table and remove every entry with the requested editionId
				// Each individual delete can be async and happen concurrently
				foreach (var dataTable in dataTables)
					await DeleteDataFromOwnerTable(connection, dataTable.TableName, editionUser);
			}
		}

		private static async Task DeleteDataFromOwnerTable(
				IDbConnection connection
				, string      tableName
				, UserInfo    editionUser)
		{
			await DatabaseCommunicationRetryPolicy.ExecuteRetry(
					async () => await connection.ExecuteAsync(
							DeleteEditionFromTable.GetQuery(tableName)
							, new
							{
									editionUser.EditionId
									, UserId = editionUser.userId
									,
							}));
		}

		private async Task<List<EditorPermissions>> _getEditionEditors(uint editionId)
		{
			using (var connection = OpenConnection())
			{
				return (await connection.QueryAsync<EditorPermissions>(
						GetEditionEditorsWithPermissionsQuery.GetQuery
						, new { EditionId = editionId })).ToList();
			}
		}
	}
}
