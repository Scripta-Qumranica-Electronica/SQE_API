using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using SQE.Backend.DataAccess.Queries;
using System.Linq;
using SQE.Backend.DataAccess.Models;
using SQE.Backend.DataAccess.Helpers;
using Microsoft.Extensions.Configuration;
using System.Transactions;

namespace SQE.Backend.DataAccess
{
    public interface IScrollRepository
    {
        Task<IEnumerable<ScrollVersion>> ListScrollVersions(int? userId, List<int> scrollIds);
        Task<Dictionary<int, List<int>>> GetScrollVersionGroups(int? scrollVersionId);
        Task ChangeScrollVersionName(uint scrollVersionId, string name, int userId);
        Task<uint> CopyScrollVersion(uint scrollVersionId, ushort userId);
        //Task<List<string>> GetOwnerTables();
        // Task<bool> UpdateOwnerTables(int olvd, int svid);
        //Task<int> UpdateAction(int scrollDataId, int oldScrollDataId, int scrollVersionId);
        // Task<IEnumerable<Dictionary<int, List<Share>>>> GetScrollVersionShares(List<int> scrollIds);
    }

    public class ScrollRepository : DBConnectionBase, IScrollRepository
    {
        ITrackMutationHelper _mutation;

        public ScrollRepository(IConfiguration config, ITrackMutationHelper mutationHelper) : base(config) 
        {
            _mutation = mutationHelper;
        }

        public async Task<IEnumerable<ScrollVersion>> ListScrollVersions(int? userId, List<int> ids) //
        {
            var sql = ScrollVersionQuery.GetQuery(userId.HasValue, ids != null);

            // We can't expand the ScrollIds parameters with MySql, as it is a list. We need to expand ourselves.
            // Since ids is a list of integers, SQL injection is quite impossible.
            if (ids != null)
            {
                var idList = "(" + string.Join(", ", ids.Select(id => id.ToString())) + ")";
                sql = sql.Replace("@ScrollVersionIds", idList);
            }

            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ScrollVersionQuery.Result>(sql, new
                {
                    UserId = userId ?? -1, // @UserId is not expanded if userId is null
                });

                var models = results.Select(result => CreateScrollVersion(result, userId));
                return models;
            }
        }

        private ScrollVersion CreateScrollVersion(ScrollVersionQuery.Result result, int? currentUserId)
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

        public async Task<Dictionary<int, List<int>>> GetScrollVersionGroups(int? scrollVersionId)
        {
            var sql = ScrollVersionGroupQuery.GetQuery(scrollVersionId.HasValue);

            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<ScrollVersionGroupQuery.Result>(sql, new
                {
                    ScrollVersionId = scrollVersionId ?? -1,
                });

                var dictionary = new Dictionary<int, List<int>>();
                foreach (var result in results)
                {
                    if (!dictionary.ContainsKey(result.group_id))
                        dictionary[result.group_id] = new List<int>();
                    dictionary[result.group_id].Add(result.scroll_version_id);
                }

                return dictionary;
            }
        }

        public async Task ChangeScrollVersionName(uint scrollVersionId, string name, int userId)
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
                    DynamicParameters nameChangeParams = new DynamicParameters();
                    nameChangeParams.Add("@scroll_id", result.scroll_id);
                    nameChangeParams.Add("@name", name);
                    MutationRequest nameChangeRequest = new MutationRequest(
                        MutateType.Update,
                        new List<string>(new string[] {"scroll_id", "name"}),
                        nameChangeParams,
                        "scroll_data",
                        result.scroll_data_id
                        );
                    
                    // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
                    // care of main_action and single_action.
                    var response = await _mutation.TrackMutation(scrollVersionId, (ushort) userId, new List<MutationRequest>() { nameChangeRequest });
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
        public async Task<int> UpdateAction(int scrollDataId, int oldScrollDataId, int scrollVersionId)
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

//        private async Task<int> GetScrollDataId(string name, int scrollVersionId)
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

        public async Task<uint> CopyScrollVersion(uint scrollVersionId, ushort userId) //not working well, Help please!
        {
            var copyToScrollVersionId = scrollVersionId;
            // TODO Check if scroll_version is locked, if not, return error.
            // If we allowed copying of scrolls that are not locked, we would
            // have to block all transactions on all _owner tables in the DB
            // until the copy process was complete in order to guard against
            // creating an inconsistent copy.
            // What if someone unlocks the source scroll mid-copy?
            using (TransactionScope transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    try
                    {
                        // Create a new scroll_version_group
                        var results = connection.Execute(CreateScrollVersionGroup.GetQuery, 
                            new{ScrollVersionId = scrollVersionId});
                        // TODO: Check that results == 1, else throw an error
                        
                        var scrollVersionGroupId = await connection.QuerySingleAsync<int>(LastInsertId.GetQuery);
                        
                        // Set scroll_version_group_admin to userId
                        results = connection.Execute(CreateScrollVersionGroupAdmin.GetQuery, 
                            new
                            {
                                ScrollVersionGroupId = scrollVersionGroupId, 
                                UserId = userId
                            });
                        // TODO: Check that results == 1, else throw an error
                        
                        // Create new scroll_version
                        results = connection.Execute(Queries.CreateScrollVersion.GetQuery, 
                            new
                            {
                                ScrollVersionGroupId = scrollVersionGroupId, 
                                UserId = userId,
                                MayLock = 1
                            });
                        // TODO: Check that results == 1, else throw an error
                        
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
                                CopyScrollVersionDataForTable.GetQuery(tableName, tableIdColumn),
                                new
                                {
                                    ScrollVersionId = scrollVersionId,
                                    CopyToScrollVersionId = copyToScrollVersionId
                                });
                        }
                    }
                    catch
                    {
                        //Maybe we do something special with the errors?
                    }
                    //Cleanup
                    transactionScope.Complete();
                    connection.Close();
                    return copyToScrollVersionId;
                }
            }
        }

        /*
        public async Task<bool> UpdateOwnerTables(int olvId, int svid)
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




