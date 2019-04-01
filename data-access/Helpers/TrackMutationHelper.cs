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
using System.Net;

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
        Task<HttpStatusCode> TrackMutation(ushort userId, uint? scrollVersionId, IEnumerable<SQENative.UserEditableTableTemplate> mutations);
    }

    public class TrackMutationHelper : DBConnectionBase, ITrackMutationHelper
    {
        private class Permission
        {
            public ushort may_write { get; set; }
            public ushort locked { get; set; }
        }
        
        public class SingleAction
        {
            private SingleAction(string value) { Value = value; }

            public string Value { get; private set; }

            public static SingleAction Add { get { return new SingleAction("add"); } }
            public static SingleAction Delete { get { return new SingleAction("delete"); } }
        }

        public TrackMutationHelper(IConfiguration config) : base(config) { }

        public async Task<HttpStatusCode> TrackMutation(ushort userId, uint? scrollVersionId, IEnumerable<SQENative.UserEditableTableTemplate> mutations)
        {
            // Assume nothing worked with the status code NotModified.
            HttpStatusCode response = HttpStatusCode.NotModified;
            
            // Grab a transaction scope, we roll back all changes if any transactions fail
            using (TransactionScope transactionScope = new TransactionScope())
            {
                using (MySqlConnection connection = OpenConnection() as MySqlConnection)
                {
                    await connection.OpenAsync();
                    
                    // Now we iterate over every requested mutation (there might be many).
                    // But they all must have the same scroll_version_id.  It doesn't make sense
                    // to group mutation on more than one scroll_version in a single transaction.
                    foreach (SQENative.UserEditableTableTemplate mutation in mutations)
                    {
                        // Check the class of the mutation. Owned tables have an owner table (e.g., artefact_shape and
                        // artefact_shape_owner).  In this case we must work with both those tables and
                        // record that a change happened via main_action and single_action.
                        if (mutation.GetType().IsSubclassOf(typeof(SQENative.OwnedTableTemplate)))
                        {
                            // Make sure we have a scroll_version and that the user can write to it.
                            // TODO: Might we move this outside the for loop and do one check for all mutation objects?
                            if (scrollVersionId.HasValue && await CheckAccess(scrollVersionId.Value, connection, userId))
                            {
                                // Cast the mutation to its proper subclass (we checked this a few lines above).
                                SQENative.OwnedTableTemplate ownedMutation = (SQENative.OwnedTableTemplate)mutation;
                                
                                // Register a main_action for this mutation
                                MySqlCommand cmd = StartMainAction(connection, scrollVersionId.Value);
                                _ = await cmd.ExecuteNonQueryAsync();
                                long mainActionId = cmd.LastInsertedId;
                                long insertId = 0;
                                
                                // Assume we will be doing an Adding action, we can change this below if we need to.
                                SingleAction singleAction = SingleAction.Add;

                                // Now check the action type (Create/Update/Delete)
                                switch (ownedMutation.action)
                                {
                                    case Action.Create:
                                        // Create the record in the desired table.
                                        // If the entry already exists, then "ON DUPLICATE KEY UPDATE"
                                        // passes the id of that record back to us.
                                        cmd = Create(mutation, connection);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        insertId = cmd.LastInsertedId;
                                        
                                        // Create the record in the "owner" table, which links the record
                                        // we just created to our scroll version.
                                        cmd = CreateOwner(ownedMutation, connection, insertId, scrollVersionId.Value);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        
                                        // Set the success message
                                        response = HttpStatusCode.Created;
                                        break;
                                    
                                    case Action.Update:
                                        // Verify that we have a real primary key value for the record
                                        // to be updated.  If we don't, then CheckMutate throws and exception.
                                        CheckMutate(mutation.PrimaryKeyValue());
                                        
                                        //TODO: Here we should run BeforeUpdate to coalesce the new row with the old one
                                        //That would be a nice convenience
                                        //BeforeUpdate(mutation, connection, tableId.Value);
                                        
                                        
                                        // Update the record by creating a new record in the desired table.
                                        // If a record with the same data already exists its id will be passed
                                        // back by "ON DUPLICATE KEY UPDATE".
                                        cmd = Create(mutation, connection);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        insertId = cmd.LastInsertedId;
                                        
                                        // Create the record in the "owner" table, which links the record
                                        // we just created to our scroll version.
                                        cmd = CreateOwner(ownedMutation, connection, insertId, scrollVersionId.Value);
                                        _ = await cmd.ExecuteNonQueryAsync();

                                        // Now we delete the old record from the owner table to accomplish this
                                        // "update".  Note: and update is an insert and a delete operation on the
                                        // owner table (thus unlinking the old data and linking the new).
                                        // The original data is not deleted.
                                        uint deleteID = mutation.PrimaryKeyValue();
                                        cmd = Delete(ownedMutation, connection, deleteID, scrollVersionId.Value);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        
                                        // Record our delete action first
                                        singleAction = SingleAction.Delete;
                                        cmd = CommitSingleAction(mutation, connection, singleAction, mainActionId, deleteID);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        
                                        // Set it to record our add action
                                        singleAction = SingleAction.Add;
                                        response = HttpStatusCode.OK;
                                        break;
                                    
                                    case Action.Delete:
                                        // Verify that we have a real primary key value for the record
                                        // to be updated.  If we don't, then CheckMutate throws and exception.
                                        CheckMutate(mutation.PrimaryKeyValue());
                                        
                                        // Now we delete the old record from the owner table to accomplish this.
                                        // Note: a delete operation simply unlinks the data from this scroll_version.
                                        // The original data is not deleted.
                                        cmd = Delete(ownedMutation, connection, mutation.PrimaryKeyValue(), scrollVersionId.Value);
                                        _ = await cmd.ExecuteNonQueryAsync();
                                        insertId = mutation.PrimaryKeyValue();
                                        
                                        // Set it to record our delete action.
                                        singleAction = SingleAction.Delete;
                                        response = HttpStatusCode.OK;
                                        break;
                                }

                                // Whatever we did, it worked of insertId is not 0
                                if (insertId != 0)
                                {
                                    // Commit the last single_action to the database
                                    cmd = CommitSingleAction(mutation, connection, singleAction, mainActionId, insertId);
                                    _ = await cmd.ExecuteNonQueryAsync();
                                }
                                else
                                {
                                    //we have a db mutation failure, throw an error.
                                    await connection.CloseAsync();
                                    //but maybe you will have to clean things up before throwing the error.
                                    throw new Exception($"Failed writing to {mutation.TableName}.");
                                }
                            } else
                            {
                                //break here, the user has no rights to alter this row
                                await connection.CloseAsync();
                                // Bronson - don't use Exception, only use derived classes (in this case you can use InvalidOperationException
                                // as this should clearly result in a 500 error to the user
                                throw new Exception($"User does not have rights to alter this {mutation.TableName} or the scroll_version does not exist.");
                            }
                        }
                        else if (mutation.GetType().IsSubclassOf(typeof(SQENative.AuthoredTableTemplate)))
                        {
                            // OK, so we don't have an "owned" table here, all of which are associated with a
                            // scroll_version.  Instead we have an "authored" table which are connected directly
                            // to a user id.  They do have a corresponding "authored" table, but do not get recorded
                            // in main_action and single_action.  E.g., we will probably add the ability for users
                            // to add images from other approved IIIF servers, thus there is the SQE_image table and
                            // SQE_image_authored, which would allow that.
                            
                            // Cast the mutation to the proper class (we just checked it a few lines back).
                            SQENative.AuthoredTableTemplate authoredMutation = (SQENative.AuthoredTableTemplate)mutation;
                            long insertId = 0;
                            
                            // Now check the action type (Create/Update/Delete)
                            switch (authoredMutation.action)
                            {
                                case Action.Create:
                                    // Create the record in the desired table
                                    MySqlCommand cmd = Create(mutation, connection);
                                    _ = await cmd.ExecuteNonQueryAsync();
                                    insertId = cmd.LastInsertedId;
                                    
                                    // Create the corresponding entry in the author table, so we know who made this
                                    // addition.
                                    cmd = CreateAuthor(authoredMutation, connection, insertId, userId);
                                    _ = await cmd.ExecuteNonQueryAsync();
                                    response = HttpStatusCode.Created;
                                    break;
                                case Action.Update:
                                    CheckMutate(mutation.PrimaryKeyValue());
                                    // TODO: add this functionality.
                                    response = HttpStatusCode.OK;
                                    break;
                                case Action.Delete:
                                    CheckMutate(mutation.PrimaryKeyValue());
                                    // TODO: add this functionality.
                                    response = HttpStatusCode.OK;
                                    break;
                            }
                            
                            // if the insertId is anything other than 0, everything worked.
                            if (insertId == 0)
                            {
                                //we have a db mutation failure, throw an error.
                                //but maybe you will have to clean things up before throwing the error.
                                await connection.CloseAsync();
                                // Bronson - don't use Exception, only use derived classes (in this case you can use InvalidOperationException
                                // as this should clearly result in a 500 error to the user
                                throw new InvalidOperationException($"Failed writing to {mutation.TableName}.");
                            }
                        }
                    }
                    // Everything worked, so lock in all the transactions and close the connection.
                    transactionScope.Complete();
                    await connection.CloseAsync();

                    // Bronson - return the status, raise an exception in case of an error
                    // Itay - I guess my issue was that there this would have otherwise been simply `void`.
                    // But I thought you weren't supposed to have void async functions.  I send now and HttpStatusCode.
                    return response; //How do I send a Status instead (e.g., Success/Error)
                }
            }
        }

        private static void CheckMutate(uint? tableId)
        {
            // See if we have an id here and that it isn't 0.
            // We can't do Update or Delete unless we know what the record id is.
            if (!tableId.HasValue || (tableId.HasValue && tableId.Value == 0))
            {
                throw new InvalidOperationException($"No primary key was provided for the transaction.");
                //throw an error, you can't do update or delete without passing a tableId
            }
            else
            {
                return;
            }
        }

        private async Task<bool> CheckAccess(uint scrollVersionId, MySqlConnection connection, ushort userId)
        {
            // Assume a fail.
            bool access = false;
            
            // Check if we can write to this scroll_version
            string query = $"SELECT may_write, locked " +
            	"FROM scroll_version_group " +
            	"JOIN scroll_version USING(scroll_version_group_id) " +
            	"WHERE scroll_version_id = @scrollVersionId AND user_id = @userId";

            try
            {
                Permission result = await connection.QuerySingleAsync<Permission>(query, new
                {
                    scrollVersionId = scrollVersionId,
                    userId = userId
                });
                // Examine the results and update access variable.
                access = result.may_write == 1 && result.locked == 0;
            }
            catch(InvalidOperationException)
            {
                // Throw on error, probably no rows were found.
                throw new NoPermissionException(userId, "change name", "scroll", (int)scrollVersionId);
            }
            //throw an error that the user does not have access to this scroll version
            return access;
        }

        private MySqlCommand StartMainAction( MySqlConnection connection, uint scrollVersionId)
        {
            // Code to log main_action in DB.
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
            // We insert a new record in the desired table.
            // "ON DUPLICATE KEY UPDATE" ensures that if a record with the same data
            // already exists, a duplicate is not created, instead the primary key
            // of that record is returned.
            string query = "INSERT INTO " + mutation.TableName + " (";
            List<string> fieldValueParams = new List<string>();
            List<string> fieldValueParamNames = new List<string>();
            List<object> fieldValues = new List<object>();
            IDictionary fields = mutation.ColumsAndValues();
            
            // We loop over every column, which we find in the mutation object.
            foreach (DictionaryEntry field in fields)
            {
                
                // We add every one to the query except the primary key.
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
            
            // Populate the parameters that we collected when looping through the columns above
            // TODO: I could probably just do this in the loop above.  Why loop again?
            for (int i = 0; i < fieldValueParams.Count && i < fieldValues.Count; i++)
            {
                cmd.Parameters.AddWithValue(fieldValueParamNames[i], fieldValues[i]);
            }
            return cmd;
        }

        private MySqlCommand CreateOwner(SQENative.OwnedTableTemplate mutation, MySqlConnection connection, long insertId, uint scrollVersionId)
        {
            // Here we find the name of the primary key column from the mutation object.
            // We use that to build the query: INSERT IGNORE INTO x_owner (x_id, scroll_version_id) VALUES(@x_id, @scrollVersionId)
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
            // Here we find the name of the primary key column from the mutation object.
            // We use that to build the query: INSERT IGNORE INTO x_author (x_id, user_id) VALUES(@x_id, @userId)
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
        // Itay: this is related to our most recent emails about coalescing records.
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
            // To delete something from a scroll version, we just remove the linking entry from the "owner" table.
            // The mutation object knows the name of its owner table, and the title of its primary key.
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
