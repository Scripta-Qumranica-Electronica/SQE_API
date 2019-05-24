using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Transactions;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess
{
    public interface IEditionRepository
    {
        Task<IEnumerable<Edition>> ListEditionsAsync(uint? userId, uint? editionId);
        Task<Dictionary<uint, List<uint>>> GetEditionsAsync(uint? editionId, uint? userId);
        Task ChangeEditionNameAsync(UserInfo user, string name);
        Task<uint> CopyEditionAsync(UserInfo user, string copyrightHolder = null, string collaborators = null);
        Task ChangeEditionCopyrightAsync(UserInfo user, string copyrightHolder = null, string collaborators = null);
    }

    public class EditionRepository : DbConnectionBase, IEditionRepository
    {
        readonly IDatabaseWriter _databaseWriter;

        public EditionRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config) 
        {
            _databaseWriter = databaseWriter;
        }

        public async Task<IEnumerable<Edition>> ListEditionsAsync(uint? userId, uint? editionId) //
        {
            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<EditionGroupQuery.Result>(
                    EditionGroupQuery.GetQuery(userId.HasValue, editionId.HasValue), 
                    new {
                        UserId = userId ?? 0, // @UserId is not expanded if userId is null
                        EditionId = editionId ?? 0,
                    });

                var models = results.Select(result => CreateEdition(result, userId));
                return models;
            }
        }

        private static Edition CreateEdition(EditionGroupQuery.Result result, uint? currentUserId)
        {
            var model = new Edition
            {
                EditionId = result.EditionId,
                Name = result.Name,
                ScrollId = result.ScrollId,
                Thumbnail = result.Thumbnail,
                Locked = result.Locked,
                LastEdit = result.LastEdit,
                IsPublic = result.UserId == 1, // The default (public and uneditable) SQE data is associated with user_id 1.
                Owner = new User()
                {
                    UserId = result.UserId,
                    Email = result.Email,
                },
                Copyright = Licence.printLicence(result.CopyrightHolder, result.Collaborators),
                CopyrightHolder = result.CopyrightHolder,
                Collaborators = result.Collaborators,
            };

            if (currentUserId.HasValue)
            {
                model.Permission = new Permission
                {
                    CanAdmin = model.Owner.UserId == currentUserId.Value && result.Admin,
                    CanWrite = model.Owner.UserId == currentUserId.Value && result.MayWrite,
                    CanLock = model.Owner.UserId == currentUserId.Value && result.MayLock,
                };
            }
            else
            {
                model.Permission = new Permission
                {
                    CanAdmin = false,
                    CanLock = false,
                    CanWrite = false,
                };
            }

            return model;
        }

        // TODO do we even need this?
        public async Task<Dictionary<uint, List<uint>>> GetEditionsAsync(uint? editionId, uint? userId)
        {
            var sql = ScrollVersionGroupQuery.GetQuery(editionId.HasValue, userId.HasValue);

            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ScrollVersionGroupQuery.Result>(sql, new
                {
                    EditionId = editionId ?? 0,
                    UserId = userId ?? 0
                });

                var dictionary = new Dictionary<uint, List<uint>>();
                foreach (var result in results)
                {
                    if (!dictionary.ContainsKey(result.ScrollId))
                        dictionary[result.ScrollId] = new List<uint>();
                    dictionary[result.ScrollId].Add(result.EditionId);
                }

                return dictionary;
            }
        }

        public async Task ChangeEditionNameAsync(UserInfo user, string name)
        {
            using (var connection = OpenConnection())
            {
                try
                {
                    // Here we get the data from the original scroll_data field, we need the scroll_id,
                    // which no one in the front end will generally have or care about.
                    var result = await connection.QuerySingleAsync<EditionNameQuery.Result>(EditionNameQuery.GetQuery(), 
                        new {
                        EditionId = user.editionId ?? 0
                    });

                    // Bronson - what happens if the scroll doesn't belong to the user? You should return some indication 
                    // As the code stands now, you return "".  Itay - the function TrackMutation always checks this and
                    // throws a NoPermissionException immediately.
                    
                    // Now we create the mutation object for the requested action
                    // You will want to check the database to make sure you what you are doing.
                    var nameChangeParams = new DynamicParameters();
                    nameChangeParams.Add("@scroll_id", result.ScrollId);
                    nameChangeParams.Add("@Name", name);
                    var nameChangeRequest = new MutationRequest(
                        MutateType.Update,
                        nameChangeParams,
                        "scroll_data",
                        result.ScrollDataId
                        );
                    
                    // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
                    // care of main_action and single_action.
                    await _databaseWriter.WriteToDatabaseAsync(user, new List<MutationRequest>() { nameChangeRequest });
                }
                catch (InvalidOperationException)
                {
                    throw new NoPermissionException(user.userId, "change Name", "scroll", user.editionId);
                }
            }
        }

        /// <summary>
        /// This creates a new copy of the requested edition, which will be owned with full priveleges
        /// by the requesting user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="copyrightHolder"></param>
        /// <param name="collaborators"></param>
        /// <param Name="user">User info object contains the editionId that the user wishes to copy and
        /// all user permissions related to it.</param>
        /// <returns>The editionId of the newly created edition.</returns>
        /// <exception cref="ImproperRequestException"></exception>
        /// <exception cref="DbFailedWrite"></exception>
        public async Task<uint> CopyEditionAsync(UserInfo user, string copyrightHolder = null,
            string collaborators = null)
        {
            uint toEditionId;
            
            // If we allowed copying of scrolls that are not locked, we would
            // have to block all transactions on all _owner tables in the DB
            // until the copy process was complete in order to guard against
            // creating an inconsistent copy.
            // What if someone unlocks the source scroll mid-copy?
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    try
                    {
                        // Check that edition is locked
                        var fromVersion =
                            await connection.QuerySingleAsync<EditionLockQuery.Result>(EditionLockQuery.GetQuery,
                                new {EditionId = user.editionId});
                        if (!fromVersion.Locked)
                            throw new ImproperRequestException("copy scroll", "scroll to be copied is not locked");
                        
                        // Create a new edition
                        var results = connection.Execute(CopyEditionQuery.GetQuery, 
                            new
                            {
                                EditionId = user.editionId,
                                CopyrightHolder = copyrightHolder,
                                Collaborators = collaborators,
                            });
                        if (results != 1)
                            throw new DbFailedWrite();
                        
                        toEditionId = await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);
                        
                        // Create new edition_editor
                        results = connection.Execute(Queries.CreateEditionEditorQuery.GetQuery, 
                            new
                            {
                                EditionId = toEditionId, 
                                UserId = user.userId,
                                MayLock = 1,
                                IsAdmin = 1
                            });
                        if (results != 1)
                            throw new DbFailedWrite();
                        
                        var toEditionEditorId = await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);

                        // Copy all owner table references from scroll_version_group of the requested
                        // scroll_version_id to the newly created scroll_version_id (this is automated
                        // and will work even if the database schema gets updated).
                        var ownerTables = await connection.QueryAsync<OwnerTables.Result>(OwnerTables.GetQuery);
                        foreach (var ownerTable in ownerTables)
                        {
                            var tableName = ownerTable.TableName;
                            var tableIdColumn = tableName.Substring(0, tableName.Length-5) + "id";
                            results = connection.Execute(
                                CopyEditionDataForTableQuery.GetQuery(tableName, tableIdColumn),
                                new
                                {
                                    EditionId = user.editionId,
                                    EditionEditorId = toEditionEditorId,
                                    CopyToEditionId = toEditionId
                                });
                        }
                    }
                    catch
                    {
                        connection.Close();
                        //Maybe we do something special with the errors?
                        throw new DbFailedWrite();
                    }
                    //Cleanup
                    transactionScope.Complete();
                    connection.Close();
                }
            }
            return toEditionId;
        }

        /// <summary>
        /// Change copyright holder and/or collaborators of the users current edition.
        /// </summary>
        /// <param name="user">The user's current state.</param>
        /// <param name="copyrightHolder">The new copyright holder name to use</param>
        /// <param name="collaborators">The new collaborator list. Null is meaningful here 
        /// and will switch to an autogenerated collaborator listing.</param>
        /// <returns></returns>
        public async Task ChangeEditionCopyrightAsync(UserInfo user, string copyrightHolder = null, string collaborators = null)
        {
            // Let's only allow admins to change these legal details.
            if (!(await user.IsAdmin()))
                throw new NoPermissionException(user.userId, "change legal details", "edition", user.editionId);
            using (var connection = OpenConnection())
            {
                await connection.ExecuteAsync(UpdateEditionLegalDetailsQuery.GetQuery,
                    new
                    {
                        EditionId = user.editionId,
                        CopyrightHolder = copyrightHolder,
                        Collaborators = collaborators,
                    });
            }
        }
    }
}




