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
        Task ChangeEditionNameAsync(uint editionId, string name, UserInfo user);
        Task<uint> CopyEditionAsync(uint editionId, UserInfo user);
        //Task<List<string>> GetOwnerTables();
        // Task<bool> UpdateOwnerTables(uint olvd, uint svid);
        //Task<int> UpdateAction(uint scrollDataId, uint oldScrollDataId, uint editionId);
        // Task<IEnumerable<Dictionary<uint, List<Share>>>> GetScrollVersionShares(List<uint> scrollIds);
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
                EditionId = result.edition_id,
                Name = result.name,
                Thumbnail = result.thumbnail,
                Locked = result.locked,
                LastEdit = result.last_edit,
                IsPublic = result.user_id == 1, // The default (public and uneditable) SQE data is associated with user_id 1.
                Owner = new User
                {
                    UserId = result.user_id,
                    UserName = result.user_name,
                }
            };

            if (currentUserId.HasValue)
            {
                model.Permission = new Permission
                {
                    CanAdmin = model.Owner.UserId == currentUserId.Value && result.admin,
                    CanWrite = model.Owner.UserId == currentUserId.Value && result.may_write,
                    CanLock = model.Owner.UserId == currentUserId.Value && result.may_lock,
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
                    if (!dictionary.ContainsKey(result.scroll_id))
                        dictionary[result.scroll_id] = new List<uint>();
                    dictionary[result.scroll_id].Add(result.edition_id);
                }

                return dictionary;
            }
        }

        public async Task ChangeEditionNameAsync(uint editionId, string name, UserInfo user)
        {
            using (var connection = OpenConnection())
            {
                try
                {
                    // Here we get the data from the original scroll_data field, we need the scroll_id,
                    // which no one in the front end will generally have or care about.
                    var result = await connection.QuerySingleAsync<EditionNameQuery.Result>(EditionNameQuery.GetQuery(), 
                        new {
                        EditionId = editionId
                    });

                    // Bronson - what happens if the scroll doesn't belong to the user? You should return some indication 
                    // As the code stands now, you return "".  Itay - the function TrackMutation always checks this and
                    // throws a NoPermissionException immediately.
                    
                    // Now we create the mutation object for the requested action
                    // You will want to check the database to make sure you what you are doing.
                    var nameChangeParams = new DynamicParameters();
                    nameChangeParams.Add("@scroll_id", result.scroll_id);
                    nameChangeParams.Add("@name", name);
                    var nameChangeRequest = new MutationRequest(
                        MutateType.Update,
                        nameChangeParams,
                        "scroll_data",
                        result.scroll_data_id
                        );
                    
                    // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
                    // care of main_action and single_action.
                    var response = await _databaseWriter.WriteToDatabaseAsync(user, new List<MutationRequest>() { nameChangeRequest });
                    // Bronson: Where isn't there a response?
                    // Itay: You get no response when you try to change to the name that is already being used.
                    // This can happen when, after the database write begins, someone else changed it to the same name first.
                    if (response.Count == 0 || !response[0].NewId.HasValue)
                    {
                        throw new ImproperRequestException("change scroll name",
                            "no change in name or failed to write to DB");
                    }
                }
                catch (InvalidOperationException)
                {
                    throw new NoPermissionException(user.userId, "change name", "scroll", editionId);
                }
            }
        }
        /**
        public async Task<uint> UpdateAction(int scrollDataId, uint oldScrollDataId, uint editionId)
        {
            using (var connection = OpenConnection() as MySqlConnection)
            {
                await connection.OpenAsync();

                //update main_action table
                var cmd = new MySqlCommand(UpdateScrollNameQueries.AddMainAction(), connection);
                cmd.Parameters.AddWithValue("@ScrollVersionId", editionId);
                await cmd.ExecuteNonQueryAsync();
                var mainActionId = Convert.ToInt32(cmd.LastInsertedId);

                //update single_action table - add with new scroll data id

                cmd = new MySqlCommand(UpdateScrollNameQueries.AddSingleAction(), connection);
                cmd.Parameters.AddWithValue("@MainActionId", mainActionId);
                cmd.Parameters.AddWithValue("@IdInTable", scrollDataId);
                cmd.Parameters.AddWithValue("@Action", "add");
                await cmd.ExecuteNonQueryAsync();

                //delete previous scroll data id
                cmd = new MySqlCommand(UpdateScrollNameQueries.AddSingleAction(), connection);
                cmd.Parameters.AddWithValue("@MainActionId", mainActionId);
                cmd.Parameters.AddWithValue("@IdInTable", oldScrollDataId);
                cmd.Parameters.AddWithValue("@Action", "delete");
                await cmd.ExecuteNonQueryAsync();

                await connection.CloseAsync();
            }
            return scrollDataId;
        }**/

//        private async Task<uint> GetScrollDataId(string name, uint editionId)
//        {
//
//            using (var connection = OpenConnection() as MySqlConnection)
//            {
//                await connection.OpenAsync();
//
//                var cmd = new MySqlCommand(UpdateScrollNameQueries.CheckIfNameExists(), connection);
//                cmd.Parameters.AddWithValue("@ScrollVersionId", editionId);
//                cmd.Parameters.AddWithValue("@Name", name);
//                var result = await cmd.ExecuteScalarAsync();
//                if (result != null)
//                {
//                    //no need in new recored, return reference to scrollDataID
//                    return Convert.ToInt32(result);
//                }
//
//                //create new recored 
//                cmd = new MySqlCommand(UpdateScrollNameQueries.AddScrollName(), connection);
//                cmd.Parameters.AddWithValue("@ScrollVersionId", editionId);
//                cmd.Parameters.AddWithValue("@Name", name);
//                await cmd.ExecuteNonQueryAsync();
//
//                await connection.CloseAsync();
//                return Convert.ToInt32(cmd.LastInsertedId);
//            }
//        }

        public async Task<uint> CopyEditionAsync(uint editionId, UserInfo user)
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
                                new {EditionId = editionId});
                        if (!fromVersion.locked)
                            throw new ImproperRequestException("copy scroll", "scroll to be copied is not locked");
                        
                        // Create a new edition
                        var results = connection.Execute(CreateEditionQuery.GetQuery, 
                            new{EditionId = editionId});
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
                                    EditionId = editionId,
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

        /*
        public async Task<bool> UpdateOwnerTables(uint olvId, uint svid)
        {
            using (var connection = OpenConnection() as MySqlConnection)
            {
                await connection.OpenAsync();
                List<string> ownerList = CopyScrollVersionQueries.GetOwnerList();
                foreach (var ownerTable in ownerList)
                {
                    var cmd = new MySqlCommand(ownerTable, connection);
                    cmd.Parameters.AddWithValue("@SVID", svid);
                    cmd.Parameters.AddWithValue("@OLDSVID", olvId);
                    await cmd.ExecuteNonQueryAsync();
                }
                await connection.CloseAsync();
            }
            return true;
        }
        */

    }
}




