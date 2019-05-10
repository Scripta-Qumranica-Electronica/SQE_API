using System;
using System.Transactions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using System.Linq;
using Dapper;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess.Helpers
{
    /// <summary>
    /// This enum sets a mutation request to one of the three types.
    /// </summary>
    public enum MutateType
    {
        Create,
        Update,
        Delete
    }
    
    /// <summary>
    /// This is an object containing all the necessary data for a single mutation in the database.
    /// </summary>
    public class MutationRequest
    {
        public MutateType Action { get; }
        public List<string> ColumnNames { get; } // Itay, we still need this, since we add more to the SQL parameters than
                                             // just the column names after this mutation request is created (e.g.
                                             // @EditionId and maybe @OwnedTableId).  But now this is computed
                                             // automatically from the Parameters in the constructor. So we no longer
                                             // need any safety checks in the constructor.
        public DynamicParameters Parameters { get; }
        public string TableName { get; }
        public uint? TablePkId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SQE.SqeHttpApi.DataAccess.Helpers.MutationRequest"/> class.
        /// </summary>
        /// <param name="action">Set the mutate to Create, Update, or Delete.
        /// For Update or Delete, the id for the record being updated or deleted must be passed in tablePkId.</param>
        /// <param name="parameters">These are the parameters for the columns that will be inserted/updated in the SQL query.
        /// The parameter names must start with @ and use the column name exactly as it is written in the database (e.g., `@scroll_id`).
        /// For Delete actions this should be empty.</param>
        /// <param name="tableName">Name of the table you are altering.</param>
        /// <param name="tablePkId">Id of the record being updated or deleted.  This will be null with an Insert action.</param>
        public MutationRequest(MutateType action, DynamicParameters parameters, string tableName, uint? tablePkId = null)
        {
            Action = action;
            Parameters = parameters;
            TableName = tableName;
            TablePkId = tablePkId;
            // The columns we are writing must all have values in the Parameters.  
            // We pull these out of the parameters.  ColumnNames is used to build 
            // the insert statements: INSERT INTO table_x (...ColumnNames).
            ColumnNames = Parameters.ParameterNames.ToList();
            
            // Fail creating the object if missing record id for update/delete.
            if((action == MutateType.Update || action == MutateType.Delete) && !tablePkId.HasValue)
            {
                throw new ArgumentException(
                    "The primary key of the record is necessary for Update and Delete actions",
                    nameof(tablePkId)
                    );
            }
            else if(tablePkId.HasValue) // Add the record id to the parameters.
            {
                Parameters.Add("@OwnedTableId", tablePkId.Value);
            }
        }
    }
    
    // This is used to know if we need to wrap the insert parameter in ST_GeomFromText()
    // I don't love this.  Perhaps we can directly insert the binary blob, or maybe we can find
    // a less computationally expensive way to do this.
    public static class GeometryColumns
    {
        public static readonly List<string> columns = new List<string>()
        {
            "region_in_sqe_imageartefact_shape", 
            "artefact_B_offsetartefact_stack",
            "pathartefact_stack",
            "region_on_image1image_to_image_map",
            "point_on_image1point_to_point_map",
            "point_on_image2point_to_point_map",
            "pathroi_shape"
        };
    }

    /// <summary>
    /// This is a return type giving necessary information for each mutation.
    /// </summary>
    public class AlteredRecord
    {
        public string TableName { get; set; }
        public uint? OldId { get; set; } 
        public uint? NewId { get; set; }
         
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SQE.SqeHttpApi.DataAccess.Helpers.AlteredRecord"/> class.
        /// </summary>
        /// <param name="tableName">Name of the table that was altered.</param>
        /// <param name="oldId">Id of the record that was altered. Only present with update/delete.</param>
        /// <param name="newId">Id of the new record that was created. Only present with update/create.</param>
        public AlteredRecord(string tableName, uint? oldId, uint? newId)
        {
            TableName = tableName;
            OldId = oldId;
            NewId = newId;
        }
    }

    public interface IDatabaseWriter
    {
        Task<List<AlteredRecord>> WriteToDatabaseAsync(UserInfo user,
            List<MutationRequest> mutationRequests);
    }

    public class DatabaseWriter : DbConnectionBase, IDatabaseWriter
    {
        /// <summary>
        /// Enum for allowed actions in the single_action database table.
        /// </summary>
        public enum SingleAction
        {
            Add,
            Delete
        }

        public DatabaseWriter(IConfiguration config) : base(config) {}

        /// <summary>
        /// Performs a list mutation requests for a single scroll version and user.
        /// </summary>
        /// <returns>A list of AlteredRecord objects containing the details of each mutation.
        /// The order of the returned list or results matches the order of the list of mutation requests</returns>
        /// <param name="user"></param>
        /// <param name="mutationRequests">List of mutation requests.</param>
        public async Task<List<AlteredRecord>> WriteToDatabaseAsync(UserInfo user,
            List<MutationRequest> mutationRequests)
        {
            var alteredRecords = new List<AlteredRecord>();
            // Grab a transaction scope, we roll back all changes if any transactions fail
            // I could limit the transaction scope to each individual mutation request,
            // but I fear the multiple requests may be dependent upon each other (i.e., all or nothing).
            using (var transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted }
                )
            )
            using (var connection = OpenConnection())
            {
                // Check the permissions and throw if user has no rights to alter this scrollVersion
                if (await user.MayWrite() && (await user.EditionEditorId()).HasValue)
                {
                    foreach (var mutationRequest in mutationRequests)
                    {
                        // Set the editionId for the mutation.
                        // Though we accept a List of mutations, we have the restriction that
                        // they all belong to the same editionId and userID.
                        // This way, we only do one permission check for the whole batch.
                        mutationRequest.Parameters.Add("@EditionId", user.editionId);
                        mutationRequest.Parameters.Add("@EditionEditorId", (await user.EditionEditorId()).Value);
                        await AddMainActionAsync(connection, mutationRequest);
                        switch (mutationRequest.Action)
                        {
                            case (MutateType.Create):
                                // Insert the record and add its response to the alteredRecords response.
                                alteredRecords.Add(await InsertAsync(connection, mutationRequest));
                                break;
                                
                            case (MutateType.Update): // Update in our system is really Delete + Insert, the old record remains.
                                // Delete the old record
                                var deletedRecord = await DeleteAsync(connection, mutationRequest);
                                
                                // Insert the new record
                                var insertedRecord = await InsertAsync(connection, mutationRequest);
                                
                                // Merge the request responses by copying the deleted Id to the insertRecord object
                                insertedRecord.OldId = deletedRecord.OldId;
                                
                                // Add info to the return object
                                alteredRecords.Add(insertedRecord);
                                break;
                                
                            case (MutateType.Delete):
                                // Delete the record and add its response to the alteredRecords response.
                                alteredRecords.Add(await DeleteAsync(connection, mutationRequest));
                                break;
                        }
                    }
                }
                transactionScope.Complete();
            }

            return alteredRecords;
        }
        
        /*
        /// <summary>
        /// This function verifies that the current user is allowed to edit the requested editionId.
        /// TODO: we should move this into the logic of the User object.
        /// </summary>
        /// <param name="connection">An IDbConnection belonging to the current transaction.</param>
        /// <param name="editionId">The editionId to be altered</param>
        /// <param name="userId">The userId of the person requesting the alterations</param>
        /// <returns></returns>
        /// <exception cref="NoPermissionException">If the user does not have access an invalid operation error
        /// is thrown.</exception>
        private static async Task CheckAccessAsync(IDbConnection connection, uint editionId, ushort userId)
        {
            // Check if we can write to this scroll_version
            // TODO: It would be cool if we could check this immediately when got
            // the HTTP request, instead of waiting till now.
            try
            {
                var result = await connection.QuerySingleAsync<PermissionCheckQuery.Result>(
                    PermissionCheckQuery.GetQuery, 
                    new
                    {
                        EditionId = editionId,
                        UserId = userId
                    });
                //throw an error that the user not allowed to write to this scroll version
                if (result.may_write != 1)
                    throw new NoPermissionException(userId, "alter", "scroll", editionId);
                //throw an error that the scroll version is currently locked for this user
                if (result.locked != 0)
                    throw new NoPermissionException(userId, "alter locked", "scroll", editionId);
            }
            catch(InvalidOperationException)
            {
                // Throw on error, probably no rows were found.
                throw new NoPermissionException(userId, "access", "scroll", editionId);
            }
        }
        */
        
        /// <summary>
        /// Insert record into table.  This takes care of writing the new record (if necessary) and makes the necessary
        /// changes to the owner tables.  It also records the action in the database.
        /// </summary>
        /// <param name="connection">An IDbConnection belonging to the current transaction.</param>
        /// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
        /// <returns>The alteredRecord object to be added to the request response.</returns>
        private static async Task<AlteredRecord> InsertAsync(IDbConnection connection, MutationRequest mutationRequest)
        {
            // Insert the record (or return the id of a preexisting record matching the unique constraints.
            var createInsertId = await InsertOwnedTableAsync(connection, mutationRequest);
                                
            // Insert the link to the editionId in the owner table
            await InsertOwnerTableAsync(connection, mutationRequest, createInsertId);

            // Record the insert
            await AddSingleActionAsync(connection, mutationRequest, SingleAction.Add);
                                
            // Create info for the request's return object
            return new AlteredRecord(mutationRequest.TableName, null, createInsertId);
        }
        
        /// <summary>
        /// Delete record.  This takes care of deleting the record and by making the necessary
        /// changes to the owner table.  It also records the action in the database.
        /// </summary>
        /// <param name="connection">An IDbConnection belonging to the current transaction</param>
        /// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
        /// <returns>The alteredRecord object to be added to the request response.</returns>
        private static async Task<AlteredRecord> DeleteAsync(IDbConnection connection, MutationRequest mutationRequest)
        {
            // Delete the link between this scrollVersionID and the record from the owner table
            await DeleteOwnerTableAsync(connection, mutationRequest);
                                
            // Record the delete
            await AddSingleActionAsync(connection, mutationRequest, SingleAction.Delete);
                                
            // Create info for the request's return object
            return new AlteredRecord(mutationRequest.TableName, mutationRequest.TablePkId, null);
        }

        /// <summary>
        /// This inserts the requested data into its table. If a record with the same data already exists, then the
        /// Id of that record is used in place of creating a duplicate record.
        /// </summary>
        /// <param name="connection">An IDbConnection belonging to the current transaction</param>
        /// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
        /// <returns>Returns the Id of the newly inserted record. If a record with the same data already existed,
        /// then the Id of that record is returned.</returns>
        private static async Task<uint> InsertOwnedTableAsync(IDbConnection connection, MutationRequest mutationRequest)
        {
            // Format query
            var query = OwnedTableInsertQuery.GetQuery;
            query = query.Replace("$TableName", mutationRequest.TableName);
            query = query.Replace("$Columns", string.Join(",", mutationRequest.ColumnNames));
            query = query.Replace(
                "$Values", 
                string.Join(",", mutationRequest.ColumnNames.Select(
                    x => GeometryColumns.columns.IndexOf(x + mutationRequest.TableName) > -1 
                        ? $"ST_GeomFromText(@{x})" 
                        :  "@" + x)
                )
            );
            
            // Execute query
            var alteredRecords = await connection.ExecuteAsync(query, mutationRequest.Parameters);

            uint insertId;
            if (alteredRecords == 0) // Nothing was inserted because the exact record already existed.
            {
                // Get id of new record (or the record matching the unique constraints of this request).
                query = OwnedTableIdQuery.GetQuery;
                query = query.Replace("$TableName", mutationRequest.TableName);
                query = query.Replace("$Columns", string.Join(",", mutationRequest.ColumnNames));
                query = query.Replace(
                    "$Values", 
                    string.Join(",", mutationRequest.ColumnNames.Select(
                        x => GeometryColumns.columns.IndexOf(x + mutationRequest.TableName) > -1 
                            ? $"ST_GeomFromText(@{x})" 
                            :  "@" + x)
                    )
                );
                query = query.Replace("$PrimaryKeyName", mutationRequest.TableName + "_id");
                insertId = await connection.QuerySingleAsync<uint>(query, mutationRequest.Parameters);
            }
            else // A new record was inserted.
            {
                // Get the id of the newly inserted record.
                insertId = await LastInsertIdAsync(connection);
            }
            
            return insertId;
        }
        
        /// <summary>
        /// Creates an entry in the owner table linking the editionId to the record with the inserted data.
        /// </summary>
        /// <param name="connection">An IDbConnection belonging to the current transaction</param>
        /// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
        /// <param name="insertId">The primary key Id of the record that was just inserted.</param>
        /// <returns></returns>
        private static async Task InsertOwnerTableAsync(IDbConnection connection, MutationRequest mutationRequest,  uint insertId)
        {
            // Format query
            var query = OwnerTableInsertQuery.GetQuery;
            query = query.Replace("$OwnerTableName", mutationRequest.TableName + "_owner");
            query = query.Replace("$OwnedTablePkName", mutationRequest.TableName + "_id");
            
            // Insert the @OwnedTableId parameter
            mutationRequest.Parameters.Add("@OwnedTableId", insertId);

            // Execute query
            await connection.ExecuteAsync(query, mutationRequest.Parameters);
        }
        
        /// <summary>
        /// Delete the entry in the owner table that links a particular record to a specific editionId
        /// </summary>
        /// <param name="connection">An IDbConnection belonging to the current transaction</param>
        /// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
        /// <returns></returns>
        private static async Task DeleteOwnerTableAsync(IDbConnection connection, MutationRequest mutationRequest)
        {
            // Format query
            var query = OwnerTableDeleteQuery.GetQuery;
            query = query.Replace("$OwnerTableName", mutationRequest.TableName + "_owner");
            query = query.Replace("$OwnedTablePkName", mutationRequest.TableName + "_id");
            
            // Execute query
            await connection.ExecuteAsync(query, mutationRequest.Parameters);
        }

        /// <summary>
        /// Convenience function to get the last insert Id. Throws on error.
        /// </summary>
        /// <param name="connection">An IDbConnection belonging to the current transaction</param>
        /// <returns>Returns the Id of the last inserted record.</returns>
        private static async Task<uint> LastInsertIdAsync(IDbConnection connection)
        {
            const string sql = "SELECT LAST_INSERT_ID()";
            return await connection.QuerySingleAsync<uint>(sql);
        }
        
        /// <summary>
        /// Creates an entry in the main_action table for the current mutation request.
        /// </summary>
        /// <param name="connection">An IDbConnection belonging to the current transaction</param>
        /// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
        /// <returns></returns>
        private static async Task AddMainActionAsync(IDbConnection connection, MutationRequest mutationRequest)
        {
            // Format and execute the query
            var query = MainActionInsertQuery.GetQuery;
            await connection.ExecuteAsync(query, mutationRequest.Parameters);
            
            // Get id of new record.
            var insertId = await LastInsertIdAsync(connection);
            
            // Insert the @MainActionId into the mutation object's query parameters.
            mutationRequest.Parameters.Add("@MainActionId", insertId);
        }
        
        /// <summary>
        /// Creates and entry in the single_action table to record the mutation.
        /// </summary>
        /// <param name="connection">An IDbConnection belonging to the current transaction</param>
        /// <param name="mutationRequest">A mutation request object with all the necessary data.</param>
        /// <param name="action"></param>
        /// <returns></returns>
        private static async Task AddSingleActionAsync(IDbConnection connection, MutationRequest mutationRequest, SingleAction action)
        {
            // Format query
            var query = SingleActionInsertQuery.GetQuery;
            
            // Add parameters
            mutationRequest.Parameters.Add("@TableName", mutationRequest.TableName);
            mutationRequest.Parameters.Add("@Action", action.ToString().ToLower());
            
            // Execute query
            await connection.ExecuteAsync(query, mutationRequest.Parameters);
        }
    }
}
