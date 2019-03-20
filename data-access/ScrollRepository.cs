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

                    // First, see if there's a ScrollData with this name
                    // Create a function that returns the scroll_data_id of a name - either an existing one or a new one
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
                    await cmd.ExecuteNonQueryAsync();

                    transactionScope.Complete();
                    await connection.CloseAsync();
                }

            }
            return sv;
        }

        private async Task<int> GetScrollDataId(string name, int scrollVersionId)
    {

        using (var connection = OpenConnection() as MySqlConnection)
        {
            await connection.OpenAsync();

            var cmd = new MySqlCommand(UpdateScrollNameQueries.CheckIfNameExists(), connection);
            cmd.Parameters.AddWithValue("@ScrollVersionId", scrollVersionId);
            cmd.Parameters.AddWithValue("@Name", name);
            var result = (int?)await cmd.ExecuteScalarAsync();
            if (result != null)
            {
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
}
}




