using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using SQE.Backend.DataAccess.Queries;
using System.Linq;
using SQE.Backend.DataAccess.Models;
using Microsoft.Extensions.Configuration;
using System.Transactions;
using System.Data.SqlClient;
using static SQE.Backend.DataAccess.Queries.ScrollVersionGroupQuery;
using MySql.Data.MySqlClient;

namespace SQE.Backend.DataAccess
{
    public interface IScrollRepository
    {
        Task<IEnumerable<ScrollVersion>> ListScrollVersions(int? userId, List<int> scrollIds);
        Task<Dictionary<int, List<int>>> GetScrollVersionGroups(int? scrollVersionId);
        Task<ScrollVersion> ChangeScrollVersionName(ScrollVersion sv, string name);
        Task<bool> CanRead(int sv, int? userId);
        Task<ScrollVersion> CopyScrollVersion(ScrollVersion sv, string name, int? userId);
        //Task<List<string>> GetOwnerTables();
        Task<bool> UpdateOwnerTables(int olvd, int svid);
        //Task<int> UpdateAction(int scrollDataId, int oldScrollDataId, int scrollVersionId);
        // Task<IEnumerable<Dictionary<int, List<Share>>>> GetScrollVersionShares(List<int> scrollIds);
    }

    public class ScrollRepository : DBConnectionBase, IScrollRepository
    {
        public ScrollRepository(IConfiguration config) : base(config) { }

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

        public async Task<ScrollVersion> ChangeScrollVersionName(ScrollVersion sv, string name)
        {
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection() as MySqlConnection)
                {
                    var scrollDataId = await GetScrollDataId(name, sv.Id);

                    await connection.OpenAsync();
                    //update scroll data owner
                    var cmd = new MySqlCommand(UpdateScrollNameQueries.UpdateScrollDataOwner(), connection);
                    cmd.Parameters.AddWithValue("@ScrollVersionId", sv.Id);
                    cmd.Parameters.AddWithValue("@ScrollDataId", scrollDataId);
                    await cmd.ExecuteNonQueryAsync();

                    //update main_action table
                    cmd = new MySqlCommand(UpdateScrollNameQueries.AddMainAction(), connection);
                    cmd.Parameters.AddWithValue("@ScrollVersionId", sv.Id);
                    await cmd.ExecuteNonQueryAsync();
                    var mainActionId = Convert.ToInt32(cmd.LastInsertedId);

                    //update single_action table

                    cmd = new MySqlCommand(UpdateScrollNameQueries.AddSingleAction(), connection);
                    cmd.Parameters.AddWithValue("@MainActionId", mainActionId);
                    cmd.Parameters.AddWithValue("@IdInTable", scrollDataId);
                    cmd.Parameters.AddWithValue("@Action", "add");



                    await cmd.ExecuteNonQueryAsync();

                    transactionScope.Complete();
                    await connection.CloseAsync();
                }

            }
            return sv;
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

        private async Task<int> GetScrollDataId(string name, int scrollVersionId)
        {

            using (var connection = OpenConnection() as MySqlConnection)
            {
                await connection.OpenAsync();

                var cmd = new MySqlCommand(UpdateScrollNameQueries.CheckIfNameExists(), connection);
                cmd.Parameters.AddWithValue("@ScrollVersionId", scrollVersionId);
                cmd.Parameters.AddWithValue("@Name", name);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null)
                {
                    //no need in new recored, return reference to scrollDataID
                    return Convert.ToInt32(result);
                }

                //create new recored 
                cmd = new MySqlCommand(UpdateScrollNameQueries.AddScrollName(), connection);
                cmd.Parameters.AddWithValue("@ScrollVersionId", scrollVersionId);
                cmd.Parameters.AddWithValue("@Name", name);
                await cmd.ExecuteNonQueryAsync();

                await connection.CloseAsync();
                return Convert.ToInt32(cmd.LastInsertedId);
            }
        }

        public async Task<bool> CanRead(int scrollVersionId, int? userId)
        {
            if (userId == null)
            {
                return false;
            }
            using (var connection = OpenConnection() as MySqlConnection)
            {
                await connection.OpenAsync();

                var cmd = new MySqlCommand(CopyScrollVersionQueries.CheckPermission(), connection);
                cmd.Parameters.AddWithValue("@ScrollVersionId", scrollVersionId);
                var result = await cmd.ExecuteScalarAsync();
                int userID = Convert.ToInt32(result);
                if (userID == 1 || userID == userId)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<ScrollVersion> CopyScrollVersion(ScrollVersion sv, string name, int? userId)
        {

            //the user have permissions to copy scrollVersion

            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection() as MySqlConnection)
                {
                    var scrollDataId = await GetScrollDataId(name, sv.Id);

                    await connection.OpenAsync();

                    //update scroll_version_group --NEED TO CHECK THE SCROLL ID!!!!
                    /**var cmd = new MySqlCommand(CopyScrollVersionQueries.InsertIntoScrollVersionGroup(), connection);
                    cmd.Parameters.AddWithValue("@ScrollID", sv.Id);
                    await cmd.ExecuteNonQueryAsync();
                    var scrollVersionGroupId = cmd.LastInsertedId;**/

                    //getScrollVersionGroupId
                    var cmd = new MySqlCommand(CopyScrollVersionQueries.GetScrollVersionGroupId(), connection);
                    cmd.Parameters.AddWithValue("@ScrollVersionId", sv.Id);
                    var scrollVersionGroupId = await cmd.ExecuteScalarAsync();


                    //update scroll_version
                    cmd = new MySqlCommand(CopyScrollVersionQueries.InsertIntoScrollVersion(), connection);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@ScrollVersionGroupId", Convert.ToInt32(scrollVersionGroupId));
                    await cmd.ExecuteNonQueryAsync();
                    var svid = cmd.LastInsertedId; //new scroll version id


                    //insert into scroll_data
                    cmd = new MySqlCommand(CopyScrollVersionQueries.InsertIntoScrollData(), connection);
                    cmd.Parameters.AddWithValue("@ScrollVersionId", sv.Id);
                    cmd.Parameters.AddWithValue("@Name", name);
                    await cmd.ExecuteNonQueryAsync();
                    var scrollData = cmd.LastInsertedId;

                    await connection.CloseAsync();
                    //update all owner tables
                    await UpdateOwnerTables(sv.Id, Convert.ToInt32(svid));

                    await connection.OpenAsync();

                    //if the name was change, update main_action and single action

                    //update main_action table
                    cmd = new MySqlCommand(UpdateScrollNameQueries.AddMainAction(), connection);
                    cmd.Parameters.AddWithValue("@ScrollVersionId", sv.Id);
                    await cmd.ExecuteNonQueryAsync();
                    var mainActionId = Convert.ToInt32(cmd.LastInsertedId);

                    //update single_action table

                    cmd = new MySqlCommand(UpdateScrollNameQueries.AddSingleAction(), connection);
                    cmd.Parameters.AddWithValue("@MainActionId", mainActionId);
                    cmd.Parameters.AddWithValue("@IdInTable", scrollDataId);
                    cmd.Parameters.AddWithValue("@Action", "add");

                    transactionScope.Complete();
                }

            }

            return sv;
        }

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


       

    }
}




