﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using System.Data.SqlClient;
using SQE.Backend.DataAccess.Queries;
using System.Linq;
using SQE.Backend.DataAccess.Models;
using SQE.Backend.DataAccess.Helpers;
using static SQE.Backend.DataAccess.Helpers.TrackMutationHelper;
using SQE.Backend.DataAccess.Models.Native;
using Microsoft.Extensions.Configuration;
using System.Transactions;
using static SQE.Backend.DataAccess.Queries.ScrollVersionGroupQuery;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;

namespace SQE.Backend.DataAccess
{
    public interface IScrollRepository
    {
        Task<IEnumerable<ScrollVersion>> ListScrollVersions(int? userId, List<int> scrollIds);
        Task<Dictionary<int, List<int>>> GetScrollVersionGroups(int? scrollVersionId);
        Task<string> ChangeScrollVersionName(uint scrollVersionId, string name, int userId);
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

        public async Task<string> ChangeScrollVersionName(uint scrollVersionId, string name, int userId)
        {
            var sql = ScrollNameQuery.GetQuery();
            var finishedName = "";
            using (var connection = OpenConnection())
            {
                try
                {
                    var result = connection.QuerySingle(sql, new
                    {
                        scrollVersionId = scrollVersionId,
                        userId
                    });

                    SQENative.ScrollData scrollData = new SQENative.ScrollData((uint)result.scroll_data_id, (uint)result.scroll_id, name)
                    {
                        action = Helpers.Action.Update
                    };
                    var response = await _mutation.TrackMutation((ushort)userId, (uint)scrollVersionId, new List<SQENative.UserEditableTableTemplate>() { scrollData }, (uint)result.scroll_data_id);
                    if (response)
                    {
                        finishedName = name;
                    }
                } catch(SqlException) { }
            }
            return finishedName;
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
            // Check if scroll_version is locked, if not, return error.
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
                            new{scrollVersionId});
                        // Check that results == 1, else throw an error
                        
                        var scrollVersionGroupId = await connection.QuerySingleAsync<int>(LastInsertId.GetQuery);
                        
                        // Set scroll_version_group_admin to userId
                        results = connection.Execute(CreateScrollVersionGroupAdmin.GetQuery, 
                            new{scrollVersionGroupId, userId});
                        // Check that results == 1, else throw an error
                        
                        // Create new scroll_version
                        results = connection.Execute(Queries.CreateScrollVersion.GetQuery, 
                            new
                            {
                                scrollVersionGroupId, 
                                userId,
                                mayLock = 1
                            });
                        // Check that results == 1, else throw an error
                        
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
                                    scrollVersionId,
                                    copyToScrollVersionId
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




