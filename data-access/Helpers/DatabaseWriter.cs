using System;
using System.Transactions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQE.Backend.DataAccess.Queries;
using System.Data;
using System.Linq;
using Dapper;

namespace SQE.Backend.DataAccess.Helpers
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
                                             // @ScrollVersionId and maybe @OwnedTableId).  But now this is computed
                                             // automatically from the Parameters in the constructor. So we no longer
                                             // need any safety checks in the constructor.
        public DynamicParameters Parameters { get; }
        public string TableName { get; }
        public uint? TablePkId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SQE.Backend.DataAccess.Helpers.MutationRequest"/> class.
        /// </summary>
        /// <param name="action">Set the mutate to Create, Update, or Delete.
        /// For Update or Delete, the id for the record being updated or deleted must be passed in tablePkId.</param>
        /// <param name="parameters">These are the parameters for the columns that will be inserted/updated in the SQL query.
        /// The parameter names must start with @ and use the column name exactly as it is written in the database (e.g., `@scroll_id`).
        /// For Delete actions this should be empty.</param>
        /// <param name="tableName">Name of the table you are altering.</param>
        /// <param name="tablePkId">Id of the record being updated or deleted.  This will be null with an Insert action.</param>
        public MutationRequest(MutateType action, DynamicParameters parameters, string tableName, uint? tablePkId)
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

    /// <summary>
    /// This is a return type giving necessary information for each mutation.
    /// </summary>
    public class AlteredRecord
    {
        public string TableName { get; set; }
        public uint? OldId { get; set; } 
        public uint? NewId { get; set; }
         
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SQE.Backend.DataAccess.Helpers.AlteredRecord"/> class.
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
        Task<List<AlteredRecord>> WriteToDatabaseAsync(uint scrollVersionId, ushort userId, List<MutationRequest> mutationRequests);
    }

    public class DatabaseWriter : DBConnectionBase, IDatabaseWriter
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
        /// <param name="scrollVersionId">Id of the scroll version where these mutations will be performed.</param>
        /// <param name="userId">Id of the user performing the mutations.</param>
        /// <param name="mutationRequests">List of mutation requests.</param>
        public async Task<List<AlteredRecord>> WriteToDatabaseAsync(uint scrollVersionId, ushort userId, List<MutationRequest> mutationRequests)
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
                await CheckAccessAsync(connection, scrollVersionId, userId);

                foreach (var mutationRequest in mutationRequests)
                {
                    // Set the scrollVersionId for the mutation.
                    // Though we accept a List of mutations, we have the restriction that
                    // they all belong to the same scrollVersionId and userID.
                    // This way, we only do one permission check for the whole batch.
                    mutationRequest.Parameters.Add("@ScrollVersionId", scrollVersionId);
                    await AddMainActionAsync(connection, mutationRequest);
                    switch (mutationRequest.Action)
                    {
                        case (MutateType.Create):
                            // Insert the record (or return the id of a preexisting record matching the unique constraints.
                            var createInsertId = await InsertOwnedTableAsync(connection, mutationRequest);
                                
                            // Insert the link to the scrollVersionId in the owner table
                            await InsertOwnerTableAsync(connection, mutationRequest, createInsertId);

                            // Record the insert
                            await AddSingleActionAsync(connection, mutationRequest, SingleAction.Add);
                                
                            // Add info to the return object
                            alteredRecords.Add(new AlteredRecord(mutationRequest.TableName, null, createInsertId));
                            break;
                            
                        case (MutateType.Update):
                            // Delete the link between this scrollVersionID and the record from the owner table
                            await DeleteOwnerTableAsync(connection, mutationRequest);
                                
                            // Record the delete
                            await AddSingleActionAsync(connection, mutationRequest, SingleAction.Delete);
                                
                            // Insert the record (or return the id of a preexisting record matching the unique constraints.
                            var updateInsertId = await InsertOwnedTableAsync(connection, mutationRequest);
                                
                            // Insert the link to the scrollVersionId in the owner table
                            await InsertOwnerTableAsync(connection, mutationRequest, updateInsertId);
                                
                            // Record the insert
                            await AddSingleActionAsync(connection, mutationRequest, SingleAction.Add);
                                
                            // Add info to the return object
                            alteredRecords.Add(new AlteredRecord(mutationRequest.TableName, mutationRequest.TablePkId, updateInsertId));
                            break;
                            
                        case (MutateType.Delete):
                            // Delete the link between this scrollVersionID and the record from the owner table
                            await DeleteOwnerTableAsync(connection, mutationRequest);
                                
                            // Record the delete
                            await AddSingleActionAsync(connection, mutationRequest, SingleAction.Delete);
                                
                            // Add info to the return object
                            alteredRecords.Add(new AlteredRecord(mutationRequest.TableName, mutationRequest.TablePkId, null));
                            break;
                    }
                }
                transactionScope.Complete();
            }

            return alteredRecords;
        }
        
        private static async Task CheckAccessAsync(IDbConnection connection, uint scrollVersionId, ushort userId)
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
                        ScrollVersionId = scrollVersionId,
                        UserId = userId
                    });
                //throw an error that the user not allowed to write to this scroll version
                if (result.may_write != 1)
                    throw new NoPermissionException(userId, "alter", "scroll", scrollVersionId);
                //throw an error that the scroll version is currently locked for this user
                if (result.locked != 0)
                    throw new NoPermissionException(userId, "alter locked", "scroll", scrollVersionId);
            }
            catch(InvalidOperationException)
            {
                // Throw on error, probably no rows were found.
                throw new NoPermissionException(userId, "access", "scroll", scrollVersionId);
            }
        }

        private static async Task<uint> InsertOwnedTableAsync(IDbConnection connection, MutationRequest mutationRequest)
        {
            // Format query
            var query = OwnedTableInsertQuery.GetQuery;
            query = query.Replace("@TableName", mutationRequest.TableName);
            query = query.Replace("@Columns", string.Join(",", mutationRequest.ColumnNames));
            query = query.Replace(
                "@Values", 
                string.Join(",", mutationRequest.ColumnNames.Select(x => "@" + x))
                );
            
            // Execute query
            await connection.ExecuteAsync(query, mutationRequest.Parameters);

            // Get id of new record (or the record matching the unique constraints of this request).
            query = OwnedTableIdQuery.GetQuery;
            query = query.Replace("@TableName", mutationRequest.TableName);
            query = query.Replace("@Columns", string.Join(",", mutationRequest.ColumnNames));
            query = query.Replace(
                "@Values",
                string.Join(",", mutationRequest.ColumnNames.Select(x => "@" + x))
                );
            query = query.Replace("@PrimaryKeyName", mutationRequest.TableName + "_id");
            return await connection.QuerySingleAsync<uint>(query, mutationRequest.Parameters);
        }
        
        private static async Task InsertOwnerTableAsync(IDbConnection connection, MutationRequest mutationRequest,  uint insertId)
        {
            // Format query
            var query = OwnerTableInsertQuery.GetQuery;
            query = query.Replace("@OwnerTableName", mutationRequest.TableName + "_owner");
            query = query.Replace("@OwnedTablePkName", mutationRequest.TableName + "_id");
            
            // Insert the @OwnedTableId parameter
            mutationRequest.Parameters.Add("@OwnedTableId", insertId);

            // Execute query
            await connection.ExecuteAsync(query, mutationRequest.Parameters);
        }
        
        private static async Task DeleteOwnerTableAsync(IDbConnection connection, MutationRequest mutationRequest)
        {
            // Format query
            var query = OwnerTableDeleteQuery.GetQuery;
            query = query.Replace("@OwnerTableName", mutationRequest.TableName + "_owner");
            query = query.Replace("@OwnedTablePkName", mutationRequest.TableName + "_id");
            
            // Execute query
            await connection.ExecuteAsync(query, mutationRequest.Parameters);
        }

        private static async Task<uint> LastInsertIdAsync(IDbConnection connection)
        {
            return await connection.QuerySingleAsync<uint>("SELECT LAST_INSERT_ID()");
        }
        
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
