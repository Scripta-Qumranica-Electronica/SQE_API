using System;
using System.Linq;
using System.Transactions;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using SQE.Backend.DataAccess.Models.Native;
using SQE.Backend.DataAccess.Queries;
using System.Collections;
using Dapper;
using System.Data.SqlClient;

namespace SQE.Backend.DataAccess.Helpers
{
    public enum Action
    {
        Create,
        Update,
        Delete
    }

    public interface ITrackMutationHelper
    {
        Task<bool> TrackMutation(ushort userId, uint? scrollVersionId, IEnumerable<SQENative.UserEditableTableTemplate> mutations, uint? tableId);
    }

    public class TrackMutationHelper : DBConnectionBase, ITrackMutationHelper
    {
        public class SingleAction
        {
            private SingleAction(string value) { Value = value; }

            public string Value { get; private set; }

            public static SingleAction Add { get { return new SingleAction("add"); } }
            public static SingleAction Delete { get { return new SingleAction("delete"); } }
        }

        public TrackMutationHelper(IConfiguration config) : base(config) { }

        public async Task<bool> TrackMutation(ushort userId, uint? scrollVersionId, IEnumerable<SQENative.UserEditableTableTemplate> mutations, uint? tableId)
        {
            var success = false;
            using (TransactionScope transactionScope = new TransactionScope())
            {
                using (MySqlConnection connection = OpenConnection() as MySqlConnection)
                {
                    await connection.OpenAsync();
                    foreach (SQENative.UserEditableTableTemplate mutation in mutations)
                    {
                        if (mutation.GetType().IsSubclassOf(typeof(SQENative.OwnedTableTemplate)))
                        {
                            if (scrollVersionId.HasValue && CheckAccess(scrollVersionId.Value, connection, userId))
                            {
                                SQENative.OwnedTableTemplate ownedMutation = (SQENative.OwnedTableTemplate)mutation;
                                MySqlCommand cmd = StartMainAction(connection, scrollVersionId.Value);
                                _ = await cmd.ExecuteNonQueryAsync();
                                long mainActionId = cmd.LastInsertedId;
                                long insertId = 0;
                                SingleAction singleAction = SingleAction.Add;

                                switch (ownedMutation.action)
                                {
                                    case Action.Create:
                                        cmd = Create(mutation, connection);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        insertId = cmd.LastInsertedId;
                                        cmd = CreateOwner(ownedMutation, connection, insertId, scrollVersionId.Value);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        break;
                                    
                                    case Action.Update:
                                        CheckMutate(tableId);
                                        //Here we should run BeforeUpdate to coalesce the new row with the old one
                                        //That would be a nice convenience
                                        //BeforeUpdate(mutation, connection, tableId.Value);
                                        cmd = Create(mutation, connection);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        insertId = cmd.LastInsertedId;
                                        
                                        cmd = CreateOwner(ownedMutation, connection, insertId, scrollVersionId.Value);
                                        _ = await cmd.ExecuteNonQueryAsync();


                                        uint deleteID = tableId ?? 0;
                                        cmd = Delete(ownedMutation, connection, deleteID, scrollVersionId.Value);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        
                                        singleAction = SingleAction.Delete;
                                        cmd = CommitSingleAction(mutation, connection, singleAction, mainActionId, deleteID);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        
                                        singleAction = SingleAction.Add;
                                        break;
                                    
                                    case Action.Delete:
                                        CheckMutate(tableId);
                                        cmd = Delete(ownedMutation, connection, tableId.Value, scrollVersionId.Value);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        insertId = tableId.Value;
                                        singleAction = SingleAction.Delete;
                                        break;
                                }

                                if (insertId != 0)
                                {
                                    cmd = CommitSingleAction(mutation, connection, singleAction, mainActionId, insertId);
                                    _ = await cmd.ExecuteNonQueryAsync();
                                }
                                else
                                {
                                    //we have a db mutation failure, throw an error.
                                    transactionScope.Complete();
                                    await connection.CloseAsync();
                                    //but maybe you will have to clean things up before throwing the error.
                                    throw new Exception($"Failed writing to {mutation.TableName}.");
                                }
                            } else
                            {
                                //break here, the user has no rights to alter this row
                                // Bronson - do not call Complete if you raise an exception unless you actually mean it! Isn't it better to rollback?
                                transactionScope.Complete();
                                await connection.CloseAsync();
                                // Bronson - don't use Exception, only use derived classes (in this case you can use InvalidOperationException
                                // as this should clearly result in a 500 error to the user
                                throw new Exception($"User does not have rights to alter this {mutation.TableName} or the scroll_version does not exist.");
                            }
                        }
                        else if (mutation.GetType().IsSubclassOf(typeof(SQENative.AuthoredTableTemplate)))
                        {
                            SQENative.AuthoredTableTemplate authoredMutation = (SQENative.AuthoredTableTemplate)mutation;
                            long insertId = 0;
                            switch (authoredMutation.action)
                            {
                                case Action.Create:
                                    MySqlCommand cmd = Create(mutation, connection);
                                    _ = await cmd.ExecuteNonQueryAsync();
                                    insertId = cmd.LastInsertedId;
                                    cmd = CreateAuthor(authoredMutation, connection, insertId, userId);
                                    _ = await cmd.ExecuteNonQueryAsync();
                                    break;
                                case Action.Update:
                                    CheckMutate(tableId);
                                    // TODO: add this functionality.
                                    break;
                                case Action.Delete:
                                    CheckMutate(tableId);
                                    // TODO: add this functionality.
                                    break;
                            }
                            if (insertId == 0)
                            {
                                //we have a db mutation failure, throw an error.
                                //but maybe you will have to clean things up before throwing the error.
                                // Bronson - do not call Complete if you raise an exception unless you actually mean it! Isn't it better to rollback?
                                transactionScope.Complete();
                                await connection.CloseAsync();
                                // Bronson - don't use Exception, only use derived classes (in this case you can use InvalidOperationException
                                // as this should clearly result in a 500 error to the user
                                throw new Exception($"Failed writing to {mutation.TableName}.");
                            }
                        }
                    }
                    success = true;
                    transactionScope.Complete();
                    await connection.CloseAsync();

                    // Bronson - return the status, raise an exception in case of an error
                    return success; //How do I send a Status instead (e.g., Success/Error)
                }
            }
        }

        private static void CheckMutate(uint? tableId)
        {
            if (!tableId.HasValue)
            {
                throw new System.Exception();
                //throw an error, you can't do update or delete without passing a tableId
            }
            else
            {
                return;
            }
        }

        private bool CheckAccess(uint scrollVersionId, MySqlConnection connection, ushort userId)
        {
            bool access = false;
            string query = $"SELECT may_write, locked " +
            	"FROM scroll_version_group " +
            	"JOIN scroll_version USING(scroll_version_group_id) " +
            	"WHERE scroll_version_id = @scrollVersionId AND user_id = @userId";
            try
            {
                dynamic result = connection.QuerySingle(query, new
                {
                    scrollVersionId = scrollVersionId,
                    userId = userId
                });
                access = result.may_write == 1 && result.locked == 0;
            } catch(SqlException) { }
            //throw an error that the user does not have access to this scroll version
            return access;
        }

        private MySqlCommand StartMainAction( MySqlConnection connection, uint scrollVersionId)
        {
            string query = "INSERT INTO main_action (scroll_version_id) VALUES(@ScrollVersionId)";
            MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ScrollVersionId", scrollVersionId);
            return cmd;
        }

        private MySqlCommand CommitSingleAction(SQENative.UserEditableTableTemplate mutation, MySqlConnection connection, SingleAction singleAction, long mainActionId, long insertId)
        {
            //Right now we use the logic of this API to make sure that the `table` field
            //is actually an appropriate value.  I with there were some way to lock
            //this down dynamically in the DB itself (I would prefer not to use an enum).
            
            const string query = "INSERT INTO single_action (`main_action_id`, `action`, `table`, `id_in_table`) VALUES(@MainActionId, @Action, @Table, @IdInTable)";
            MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@MainActionId", mainActionId);
            cmd.Parameters.AddWithValue("@Action", singleAction.Value);
            cmd.Parameters.AddWithValue("@Table", mutation.TableName);
            cmd.Parameters.AddWithValue("@IdInTable", insertId);
            return cmd;
        }

        private MySqlCommand Create(SQENative.UserEditableTableTemplate mutation, MySqlConnection connection)
        {
            string query = "INSERT INTO " + mutation.TableName + " (";
            List<string> fieldValueParams = new List<string>();
            List<string> fieldValueParamNames = new List<string>();
            List<object> fieldValues = new List<object>();
            IDictionary fields = mutation.ColumsAndValues();
            foreach (DictionaryEntry field in fields)
            {
                if (null != field.Value && field.Key.ToString() != mutation.PrimaryKey)
                {
                    fieldValueParams.Add(field.Key.ToString());
                    fieldValueParamNames.Add("@" + field.Key.ToString());
                    fieldValues.Add(field.Value);
                }
            }
            query += string.Join(",", fieldValueParams) + ") VALUES(" 
                + string.Join(",", fieldValueParamNames)
                + ") ON DUPLICATE KEY UPDATE "
                + mutation.PrimaryKey
                + " = LAST_INSERT_ID("
                + mutation.PrimaryKey
                + ")";
            MySqlCommand cmd = new MySqlCommand(query, connection);
            for (int i = 0; i < fieldValueParams.Count && i < fieldValues.Count; i++)
            {
                cmd.Parameters.AddWithValue(fieldValueParamNames[i], fieldValues[i]);
            }
            return cmd;
        }

        private MySqlCommand CreateOwner(SQENative.OwnedTableTemplate mutation, MySqlConnection connection, long insertId, uint scrollVersionId)
        {
            string query = "INSERT IGNORE INTO ";
            query += mutation.OwnerTable;
            string pkParam = "@" + mutation.PrimaryKey;
            const string lvParam = "@ScrollVersionId";
            query += " (" + mutation.PrimaryKey + ", scroll_version_id) VALUES(";
            query += pkParam + ", " + lvParam + ")";
            MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue(pkParam, insertId);
            cmd.Parameters.AddWithValue(lvParam, scrollVersionId);
            return cmd;
        }

        private MySqlCommand CreateAuthor(SQENative.AuthoredTableTemplate mutation, MySqlConnection connection, long insertId, ushort userId)
        {
            string query = "INSERT IGNORE INTO ";
            query += mutation.AuthorTable;
            string pkParam = "@" + mutation.PrimaryKey;
            const string lvParam = "@UserId";
            query += " (" + mutation.PrimaryKey + ", user_id) VALUES(";
            query += pkParam + ", " + lvParam + ")";
            MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue(pkParam, insertId);
            cmd.Parameters.AddWithValue(lvParam, userId);
            return cmd;
        }

        //This function does not work, I would like it to check the existing
        //field before updating, and coalesce it with the replacement field.
        // That would be very convenient.
        /**private void BeforeUpdate(SQENative.UserEditableTableTemplate mutation, MySqlConnection connection, uint tableId)
        {
            try
            {
                var query = "SELECT * FROM " + mutation.TableName + " WHERE " + mutation.PrimaryKey + " = @PK";

                var result = connection.QuerySingle(query, new
                {
                    PK = tableId,
                });
                var a = 1 + 1;
            } catch(SqlException) { }
        }*/

        private static MySqlCommand Delete(SQENative.OwnedTableTemplate mutation, MySqlConnection connection, uint tableId, uint scrollVersionId)
        {
            string query = "DELETE FROM " + mutation.OwnerTable + " WHERE " 
                + mutation.PrimaryKey + @" = @tableId 
                AND " + ScrollVersionGroupLimit.LimitToScrollVersionGroup;
            MySqlCommand cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@tableId", tableId);
            cmd.Parameters.AddWithValue("@scrollVersionId", scrollVersionId);
            return cmd;
        }
    }
}
