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
        
        public List<string> Columns { get; } // Bronson - we don't need this, we can get the columns from the parameter keys
        
        public DynamicParameters Parameters { get; }
        public string TableName { get; }
        public uint? TablePkId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SQE.Backend.DataAccess.Helpers.MutationRequest"/> class.
        /// </summary>
        /// <param name="action">Set the mutate to Create, Update, or Delete.  For Update or Delete, the id for the record being updated or deleted must be passed in tablePkId.</param>
        /// <param name="columns">The names of each column you want to alter.  These must have a corresponding @column_name entry in the parameters variable. For Delete actions this should be empty.</param>
        /// <param name="parameters">These are the parameters that will be used in the SQL query. For Delete actions this should be empty.</param>
        /// <param name="tableName">Name of the table you are altering.</param>
        /// <param name="tablePkId">Id of the record being updated or deleted.  This will be null with an Insert action.</param>
        public MutationRequest(MutateType action, List<string> columns, DynamicParameters parameters, string tableName, uint? tablePkId)
        {
            Action = action;
            Columns = columns;
            Parameters = parameters;
            TableName = tableName;
            TablePkId = tablePkId;
            
            // Fail creating the object if Parameters is missing a value for any Columns.
            // Bronson - no need to test this if we get the columns from parameters - fewer bugs, fewer client code
            if (Parameters.ParameterNames.Intersect(Columns.Select(x => "@" + x)).Count() ==
                Columns.Count())
            {
                throw new System.ArgumentException(
                    "Not all members of Columns have a value in Parameters", 
                    nameof(parameters)
                    );
            }

            if((action == MutateType.Update || action == MutateType.Delete) && !tablePkId.HasValue)
            {
                throw new System.ArgumentException(
                    "The primary key of the record is necessary for Update and Delete actions",
                    nameof(tablePkId)
                    );
            }
            else
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

    public interface ITrackMutationHelper
    {
        // Bronson - please name async methods with Async at the end. This is the Microsoft convention and
        // I find it quite useful. https://docs.microsoft.com/en-us/dotnet/csharp/async
        // We can decide not to do that, but that will most likely result in people getting confused.
        //
        // Note that you don't place the async modifier on interface methods, just on their implementation
        Task<List<AlteredRecord>> TrackMutation(uint scrollVersionId, ushort userId, List<MutationRequest> mutationRequests);
    }

    public class TrackMutationHelper : DBConnectionBase, ITrackMutationHelper
    {
        // Bronson - move this to PermissionCheckQuery.Result, as we do with all queries.
        private class Permission
        {
            public ushort may_write { get; set; }
            public ushort locked { get; set; }
        }

        /// <summary>
        /// This is basically an Enum for strings, it protects us from typos when entering the action type in AddSingleAction.
        /// </summary>
        // Bronson: This is one way to do it. For only two values, it's fine. Do consider three alternatives, though, all 
        // involving an actual enum:
        //
        // public enum SingleAction 
        // {
        //  Add,
        //  Delete
        // }
        //
        // 1. Use a method that converts the enum values to strings:
        //    private string SingleActionToString(SingleAction action)
        //    {
        //         if(action==SingleAction.Add)
        //           return "add";
        //         ...and so on...
        //    }
        //
        //    I think it's better than the singleton-only SingleAction class.
        //
        // 2. Use reflection and attributes. This makes sense for enums with more values.
        //    See how it's done *very nicely* here:  https://stackoverflow.com/questions/1799370/getting-attributes-of-enums-value .
        //    This is definitely overkill for our simple enum, and worse than your solution in our case, but still - nice to know.
        //
        // 3. Notice that you convert Add to add and Update to update, and simply use
        //    var action = SingleAction.Add;
        //    var stringAction = action.ToString().Lower();
        //
        //    I would definitely use this in our very simple case, no need to do anything more complicated
        public class SingleAction
        {
            private SingleAction(string value)
            {
                Value = value;
            }

            public string Value { get; private set; }

            public static SingleAction Add
            {
                get { return new SingleAction("add"); }
            }

            public static SingleAction Delete
            {
                get { return new SingleAction("delete"); }
            }
        }

        public TrackMutationHelper(IConfiguration config) : base(config) {}

        /// <summary>
        /// Performs a list mutation requests for a single scroll version and user.
        /// </summary>
        /// <returns>A list of AlteredRecord objects containing the details of each mutation.
        /// The order of the returned list or results matches the order of the list of mutation requests</returns>
        /// <param name="scrollVersionId">Id of the scroll version where these mutations will be performed.</param>
        /// <param name="userId">Id of the user performing the mutations.</param>
        /// <param name="mutationRequests">List of mutation requests.</param>
        public async Task<List<AlteredRecord>> TrackMutation(uint scrollVersionId, ushort userId, List<MutationRequest> mutationRequests)
        {
            List<AlteredRecord> AlteredRecords = new List<AlteredRecord>();
            // Grab a transaction scope, we roll back all changes if any transactions fail
            // I could limit the transaction scope to each individual mutation request,
            // but I fear the multiple requests may be dependent upon each other (i.e., all or nothing).
            using (TransactionScope transactionScope = new TransactionScope())
            using (var connection = OpenConnection())
            {
                // Check the permissions and throw if user has no rights to alter this scrollVersion
                await CheckAccess(connection, scrollVersionId, userId);

                foreach (var mutationRequest in mutationRequests)
                {
                    // Set the scrollVersionId for the mutation.
                    // Though we accept a List of mutations, we have the restriction that
                    // they all belong to the same scrollVersionId and userID.
                    // This way, we only do one permission check for the whole batch.
                    mutationRequest.Parameters.Add("@ScrollVersionId", scrollVersionId);
                    try
                    {
                        await AddMainAction(connection, mutationRequest);
                        switch (mutationRequest.Action)
                        {
                            case (MutateType.Create):
                                // Insert the record (or return the id of a preexisting record matching the unique constraints.
                                var createInsertId = await InsertOwnedTable(connection, mutationRequest);
                                
                                // Insert the link to the scrollVersionId in the owner table
                                await InsertOwnerTable(connection, mutationRequest, createInsertId);

                                // Record the insert
                                await AddSingleAction(connection, mutationRequest, SingleAction.Add);
                                
                                // Add info to the return object
                                AlteredRecords.Add(new AlteredRecord(mutationRequest.TableName, null, createInsertId));
                                break;
                            
                            case (MutateType.Update):
                                // Delete the link between this scrollVersionID and the record from the owner table
                                await DeleteOwnerTable(connection, mutationRequest);
                                
                                // Record the delete
                                await AddSingleAction(connection, mutationRequest, SingleAction.Delete);
                                
                                // Insert the record (or return the id of a preexisting record matching the unique constraints.
                                var updateInsertId = await InsertOwnedTable(connection, mutationRequest);
                                
                                // Insert the link to the scrollVersionId in the owner table
                                await InsertOwnerTable(connection, mutationRequest, updateInsertId);
                                
                                // Record the insert
                                await AddSingleAction(connection, mutationRequest, SingleAction.Add);
                                
                                // Add info to the return object
                                AlteredRecords.Add(new AlteredRecord(mutationRequest.TableName, mutationRequest.TablePkId, updateInsertId));
                                break;
                            
                            case (MutateType.Delete):
                                // Delete the link between this scrollVersionID and the record from the owner table
                                await DeleteOwnerTable(connection, mutationRequest);
                                
                                // Record the delete
                                await AddSingleAction(connection, mutationRequest, SingleAction.Delete);
                                
                                // Add info to the return object
                                AlteredRecords.Add(new AlteredRecord(mutationRequest.TableName, mutationRequest.TablePkId, null));
                                break;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        throw;
                    }
                    
                }
                transactionScope.Complete();
            }

            return AlteredRecords;
        }
        
        private async Task CheckAccess(IDbConnection connection, uint scrollVersionId, ushort userId)
        {
            // Check if we can write to this scroll_version
            try
            {
                Permission result = await connection.QuerySingleAsync<Permission>(
                    PermissionCheck.GetQuery, 
                    new
                    {
                        ScrollVersionId = scrollVersionId,
                        UserId = userId
                    });
                // Bronson - please, one liners like this make me dizzy
                //throw an error that the user not allowed to write to this scroll version
                // if (result.may_write != 1) {throw new NoPermissionException(userId, "alter", "scroll", (int)scrollVersionId);}
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

        private static async Task<uint> InsertOwnedTable(IDbConnection connection, MutationRequest mutationRequest)
        {
            // Format query
            var query = OwnedTableInsert.GetQuery;
            query = query.Replace("@TableName", mutationRequest.TableName);
            query = query.Replace("@Columns", String.Join(",", mutationRequest.Columns));
            query = query.Replace(
                "@Values", 
                String.Join(",", mutationRequest.Columns.Select(x => "@" + x))
                );
            query = query.Replace("@PrimaryKeyName", mutationRequest.TableName + "_id");
            
            // Execute query
            await connection.ExecuteAsync(query, mutationRequest.Parameters);
            
            // Get id of new record (or the record matching the unique constraints of this request).
            var insertId = await LastInsertId(connection);
            return insertId;
        }
        
        private static async Task InsertOwnerTable(IDbConnection connection, MutationRequest mutationRequest,  uint insertId)
        {
            // Format query
            var query = OwnerTableInsert.GetQuery;
            query = query.Replace("@OwnerTableName", mutationRequest.TableName + "_owner");
            query = query.Replace("@OwnedTablePkName", mutationRequest.TableName + "_id");
            
            // Insert the @OwnedTableId parameter
            mutationRequest.Parameters.Add("@OwnedTableId", insertId);

            // Execute query
            await connection.ExecuteAsync(query, mutationRequest.Parameters);
        }
        
        private static async Task DeleteOwnerTable(IDbConnection connection, MutationRequest mutationRequest)
        {
            // Format query
            var query = OwnerTableDelete.GetQuery;
            query = query.Replace("@OwnerTableName", mutationRequest.TableName + "_owner");
            query = query.Replace("@OwnedTablePkName", mutationRequest.TableName + "_id");
            
            // Execute query
            await connection.ExecuteAsync(query, mutationRequest.Parameters);
        }

        private static async Task<uint> LastInsertId(IDbConnection connection)
        {
            return await connection.QuerySingleAsync<uint>("SELECT LAST_INSERT_ID()");
        }
        
        private static async Task AddMainAction(IDbConnection connection, MutationRequest mutationRequest)
        {
            // Format and execute the query
            var query = MainActionInsert.GetQuery;
            await connection.ExecuteAsync(query, mutationRequest.Parameters);
            
            // Get id of new record.
            var insertId = await LastInsertId(connection);
            
            // Insert the @MainActionId into the mutation object's query parameters.
            mutationRequest.Parameters.Add("@MainActionId", insertId);
        }
        
        private static async Task AddSingleAction(IDbConnection connection, MutationRequest mutationRequest, SingleAction action)
        {
            // Format query
            var query = SingleActionInsert.GetQuery;
            
            // Add parameters
            mutationRequest.Parameters.Add("@TableName", mutationRequest.TableName);
            mutationRequest.Parameters.Add("@Action", action.Value);
            
            // Execute query
            await connection.ExecuteAsync(query, mutationRequest.Parameters);
        }
    }
}
