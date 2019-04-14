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
    public interface IScrollRepository
    {
        Task<IEnumerable<ScrollVersion>> ListScrollVersions(uint? userId, List<uint> scrollVersionIds);
        Task<Dictionary<uint, List<uint>>> GetScrollVersionGroups(uint? scrollVersionId);
        Task ChangeScrollVersionName(uint scrollVersionId, string name, uint userId);
        Task<uint> CopyScrollVersion(uint scrollVersionId, ushort userId);
        //Task<List<string>> GetOwnerTables();
        // Task<bool> UpdateOwnerTables(uint olvd, uint svid);
        //Task<int> UpdateAction(uint scrollDataId, uint oldScrollDataId, uint scrollVersionId);
        // Task<IEnumerable<Dictionary<uint, List<Share>>>> GetScrollVersionShares(List<uint> scrollIds);
    }

    public class ScrollRepository : DBConnectionBase, IScrollRepository
    {
        readonly IDatabaseWriter _databaseWriter;

        public ScrollRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config) 
        {
            _databaseWriter = databaseWriter;
        }

        public async Task<IEnumerable<ScrollVersion>> ListScrollVersions(uint? userId, List<uint> scrollVersionIds) //
        {
            var sql = ScrollVersionQuery.GetQuery(userId.HasValue, scrollVersionIds != null);

            // We can't expand the ScrollIds parameters with MySql, as it is a list. We need to expand ourselves.
            // Since ids is a list of integers, SQL injection is quite impossible.
            if (scrollVersionIds != null)
            {
                var idList = "(" + string.Join(", ", scrollVersionIds.Select(id => id.ToString())) + ")";
                sql = sql.Replace("@ScrollVersionIds", idList);
            }

            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ScrollVersionQuery.Result>(sql, new
                {
                    UserId = userId ?? 0, // @UserId is not expanded if userId is null
                });

                var models = results.Select(result => CreateScrollVersion(result, userId));
                return models;
            }
        }

        private ScrollVersion CreateScrollVersion(ScrollVersionQuery.Result result, uint? currentUserId)
        {
            var model = new ScrollVersion
            {
                Id = result.id,
                Name = result.name,
                Thumbnail = result.thumbnail,
                Locked = result.locked,
                LastEdit = result.last_edit,
                IsPublic = result.user_name.ToUpper() == "SQE_API",
                Owner = new User
                {
                    UserId = result.user_id,
                    UserName = result.user_name,
                }
            };

            if (currentUserId.HasValue && model.Owner.UserId == currentUserId.Value)
            {
                model.Permission = new Permission
                {
                    CanAdmin = true,
                    CanWrite = true,
                };
            }
            else
            {
                model.Permission = new Permission
                {
                    CanAdmin = false,
                    CanWrite = false,
                };
            }

            return model;
        }

        public async Task<Dictionary<uint, List<uint>>> GetScrollVersionGroups(uint? scrollVersionId)
        {
            var sql = ScrollVersionGroupQuery.GetQuery(scrollVersionId.HasValue);

            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ScrollVersionGroupQuery.Result>(sql, new
                {
                    ScrollVersionId = scrollVersionId ?? 0,
                });

                var dictionary = new Dictionary<uint, List<uint>>();
                foreach (var result in results)
                {
                    if (!dictionary.ContainsKey(result.group_id))
                        dictionary[result.group_id] = new List<uint>();
                    dictionary[result.group_id].Add(result.scroll_version_id);
                }

                return dictionary;
            }
        }

        public async Task ChangeScrollVersionName(uint scrollVersionId, string name, uint userId)
        {
            var sql = ScrollNameQuery.GetQuery();

            using (var connection = OpenConnection())
            {
                try
                {
                    // Here we get the data from the original scroll_data field, we need the scroll_id,
                    // which no one in the front end will generally have or care about.
                    var result = await connection.QuerySingleAsync<ScrollNameQuery.Result>(sql, new
                    {
                        ScrollVersionId = scrollVersionId,
                        UserId = userId
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
                    var response = await _databaseWriter.WriteToDatabaseAsync(scrollVersionId, (ushort) userId, new List<MutationRequest>() { nameChangeRequest });
                    // Bronson: Where isn't there a response?
                    if (response.Count == 0 || response[0].NewId == result.scroll_id || response[0].NewId == 0)
                    {
                        throw new ImproperRequestException("change scroll name",
                            "no change in name or failed to write to DB");
                    }
                }
                catch (InvalidOperationException) // Bronson: The error QuerySingle throws if there's no result.  Itay: thanks!
                {
                    throw new NoPermissionException(userId, "change name", "scroll", scrollVersionId);
                }
            }
        }
        /**
        public async Task<uint> UpdateAction(int scrollDataId, uint oldScrollDataId, uint scrollVersionId)
        {
            using (var connection = OpenConnection() as MySqlConnection)
            {
                await connection.OpenAsync();

                //update main_action table
                var cmd = new MySqlCommand(UpdateScrollNameQueries.AddMainAction(), connection);
                cmd.Parameters.AddWithValue("@ScrollVersionId", scrollVersionId);
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

//        private async Task<uint> GetScrollDataId(string name, uint scrollVersionId)
//        {
//
//            using (var connection = OpenConnection() as MySqlConnection)
//            {
//                await connection.OpenAsync();
//
//                var cmd = new MySqlCommand(UpdateScrollNameQueries.CheckIfNameExists(), connection);
//                cmd.Parameters.AddWithValue("@ScrollVersionId", scrollVersionId);
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
//                cmd.Parameters.AddWithValue("@ScrollVersionId", scrollVersionId);
//                cmd.Parameters.AddWithValue("@Name", name);
//                await cmd.ExecuteNonQueryAsync();
//
//                await connection.CloseAsync();
//                return Convert.ToInt32(cmd.LastInsertedId);
//            }
//        }

        public async Task<uint> CopyScrollVersion(uint scrollVersionId, ushort userId)
        {
            uint copyToScrollVersionId;
            
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
                        var fromVersion =
                            await connection.QuerySingleAsync<ScrollLockQuery.Result>(ScrollLockQuery.GetQuery,
                                new {ScrollVersionId = scrollVersionId});
                        if (!fromVersion.locked)
                            throw new ImproperRequestException("copy scroll", "scroll to be copied is not locked");
                        // Create a new scroll_version_group
                        var results = connection.Execute(CreateScrollVersionGroupQuery.GetQuery, 
                            new{ScrollVersionId = scrollVersionId});
                        if (results != 1)
                            throw new DbFailedWrite();
                        
                        var scrollVersionGroupId = await connection.QuerySingleAsync<int>(LastInsertId.GetQuery);
                        
                        // Set scroll_version_group_admin to userId
                        results = connection.Execute(CreateScrollVersionGroupAdminQuery.GetQuery, 
                            new
                            {
                                ScrollVersionGroupId = scrollVersionGroupId, 
                                UserId = userId
                            });
                        if (results != 1)
                            throw new DbFailedWrite();
                        
                        // Create new scroll_version
                        results = connection.Execute(Queries.CreateScrollVersionQuery.GetQuery, 
                            new
                            {
                                ScrollVersionGroupId = scrollVersionGroupId, 
                                UserId = userId,
                                MayLock = 1
                            });
                        if (results != 1)
                            throw new DbFailedWrite();
                        
                        copyToScrollVersionId = await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);

                        // Copy all owner table references from scroll_version_group of the requested
                        // scroll_version_id to the newly created scroll_version_id (this is automated
                        // and will work even if the database schema gets updated).
                        const string otb = OwnerTables.GetQuery;
                        var ownerTables = await connection.QueryAsync<OwnerTables.Result>(otb);
                        foreach (var ownerTable in ownerTables)
                        {
                            var tableName = ownerTable.TableName;
                            var tableIdColumn = tableName.Substring(0, tableName.Length-5) + "id";
                            results = connection.Execute(
                                CopyScrollVersionDataForTableQuery.GetQuery(tableName, tableIdColumn),
                                new
                                {
                                    ScrollVersionId = scrollVersionId,
                                    CopyToScrollVersionId = copyToScrollVersionId
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
            return copyToScrollVersionId;
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




