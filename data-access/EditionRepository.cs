using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Extensions.Configuration;
using System.Transactions;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess
{
    public interface IEditionRepository
    {
        Task<IEnumerable<Edition>> ListEditionsAsync(uint? userId, uint? editionId);
        Task ChangeEditionNameAsync(UserInfo user, string name);
        Task<uint> CopyEditionAsync(UserInfo user, string copyrightHolder = null, string collaborators = null);
        Task ChangeEditionCopyrightAsync(UserInfo user, string copyrightHolder = null, string collaborators = null);
        Task<string> DeleteAllEditionDataAsync(UserInfo user, string token);
        Task<string> GetDeleteToken(UserInfo user);
        Task<Permission> AddEditionEditor(UserInfo user, string editorEmail, bool? mayRead, bool? mayWrite, 
            bool? mayLock, bool? isAdmin);
        Task<Permission> ChangeEditionEditorRights(UserInfo user, string editorEmail, bool? mayRead, bool? mayWrite, 
            bool? mayLock, bool? isAdmin);
    }

    public class EditionRepository : DbConnectionBase, IEditionRepository
    {
        readonly IDatabaseWriter _databaseWriter;

        public EditionRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config) 
        {
            _databaseWriter = databaseWriter;
        }

        public async Task<IEnumerable<Edition>> ListEditionsAsync(uint? userId, uint? editionId) //
        {
            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<EditionGroupQuery.Result>(
                    EditionGroupQuery.GetQuery(userId.HasValue, editionId.HasValue), 
                    new {
                        UserId = userId ?? 0, // @UserId is not expanded if userId is null
                        EditionId = editionId ?? 0,
                    });

                var models = results.Select(result => CreateEdition(result, userId));
                return models;
            }
        }

        private static Edition CreateEdition(EditionGroupQuery.Result result, uint? currentUserId)
        {
            var model = new Edition
            {
                EditionId = result.EditionId,
                Name = result.Name,
                ScrollId = result.ScrollId,
                Thumbnail = result.Thumbnail,
                Locked = result.Locked,
                LastEdit = result.LastEdit,
                IsPublic = result.UserId == 1, // The default (public and uneditable) SQE data is associated with user_id 1.
                Owner = new User()
                {
                    UserId = result.UserId,
                    Email = result.Email,
                },
                Copyright = Licence.printLicence(result.CopyrightHolder, result.Collaborators),
                CopyrightHolder = result.CopyrightHolder,
                Collaborators = result.Collaborators,
            };

            if (currentUserId.HasValue)
            {
                model.Permission = new Permission
                {
                    IsAdmin = result.Admin,
                    MayWrite = result.MayWrite,
                    MayLock = result.MayLock,
                };
            }
            else
            {
                model.Permission = new Permission
                {
                    IsAdmin = false,
                    MayLock = false,
                    MayWrite = false,
                };
            }

            return model;
        }

        public async Task ChangeEditionNameAsync(UserInfo user, string name)
        {
            using (var connection = OpenConnection())
            {
                EditionNameQuery.Result result;
                
                try
                {
                    // Here we get the data from the original scroll_data field, we need the scroll_id,
                    // which no one in the front end will generally have or care about.
                    result = await connection.QuerySingleAsync<EditionNameQuery.Result>(EditionNameQuery.GetQuery(), 
                        new {
                        EditionId = user.editionId ?? 0
                    });
                }
                catch (InvalidOperationException)
                {
                    throw new StandardErrors.DataNotFound("edition", user.editionId ?? 0);
                }

                // Bronson - what happens if the scroll doesn't belong to the user? You should return some indication 
                // As the code stands now, you return "".  Itay - the function TrackMutation always checks this and
                // throws a NoPermissionException immediately.
                
                // Now we create the mutation object for the requested action
                // You will want to check the database to make sure you what you are doing.
                var nameChangeParams = new DynamicParameters();
                nameChangeParams.Add("@manuscript_id", result.ScrollId);
                nameChangeParams.Add("@Name", name);
                var nameChangeRequest = new MutationRequest(
                    MutateType.Update,
                    nameChangeParams,
                    "manuscript_data",
                    result.ScrollDataId
                    );
                
                // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
                // care of main_action and single_action.
                await _databaseWriter.WriteToDatabaseAsync(user, new List<MutationRequest>() { nameChangeRequest });
            }
        }

        /// <summary>
        /// This creates a new copy of the requested edition, which will be owned with full priveleges
        /// by the requesting user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="copyrightHolder"></param>
        /// <param name="collaborators"></param>
        /// <param name="user">User info object contains the editionId that the user wishes to copy and
        /// all user permissions related to it.</param>
        /// <returns>The editionId of the newly created edition.</returns>
        public async Task<uint> CopyEditionAsync(UserInfo user, string copyrightHolder = null,
            string collaborators = null)
        {
            uint toEditionId;
            
            // If we allowed copying of scrolls that are not locked, we would
            // have to block all transactions on all _owner tables in the DB
            // until the copy process was complete in order to guard against
            // creating an inconsistent copy.
            // What if someone unlocks the source scroll mid-copy?
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    // Check that edition is locked
                    var fromVersion =
                        await connection.QuerySingleAsync<EditionLockQuery.Result>(EditionLockQuery.GetQuery,
                            new {EditionId = user.editionId});
                    if (!fromVersion.Locked)
                        throw new StandardErrors.EditionCopyLockProtection(user);
                    
                    // Create a new edition
                    connection.Execute(CopyEditionQuery.GetQuery, 
                        new
                        {
                            EditionId = user.editionId,
                            CopyrightHolder = copyrightHolder,
                            Collaborators = collaborators,
                        });
                    
                    toEditionId = await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);
                    if (toEditionId == 0)
                        throw new StandardErrors.DataNotWritten("create edition");
                    
                    // Create new edition_editor
                    connection.Execute(Queries.CreateEditionEditorQuery.GetQuery, 
                        new
                        {
                            EditionId = toEditionId, 
                            UserId = user.userId,
                            MayLock = 1,
                            IsAdmin = 1
                        });

                    uint toEditionEditorId = await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);
                    if (toEditionEditorId == 0)
                        throw new StandardErrors.DataNotWritten("create edition_editor");

                    // Copy all owner table references from scroll_version_group of the requested
                    // scroll_version_id to the newly created scroll_version_id (this is automated
                    // and will work even if the database schema gets updated).
                    var ownerTables = await connection.QueryAsync<OwnerTables.Result>(OwnerTables.GetQuery);
                    foreach (var ownerTable in ownerTables)
                    {
                        var tableName = ownerTable.TableName;
                        var tableIdColumn = tableName.Substring(0, tableName.Length-5) + "id";
                        connection.Execute(
                            CopyEditionDataForTableQuery.GetQuery(tableName, tableIdColumn),
                            new
                            {
                                EditionId = user.editionId,
                                EditionEditorId = toEditionEditorId,
                                CopyToEditionId = toEditionId
                            });
                    }
                    //Cleanup
                    transactionScope.Complete();
                    connection.Close();
                }
            }
            return toEditionId;
        }

        /// <summary>
        /// Change copyright holder and/or collaborators of the users current edition.
        /// </summary>
        /// <param name="user">The user's current state.</param>
        /// <param name="copyrightHolder">The new copyright holder name to use</param>
        /// <param name="collaborators">The new collaborator list. Null is meaningful here 
        /// and will switch to an autogenerated collaborator listing.</param>
        /// <returns></returns>
        public async Task ChangeEditionCopyrightAsync(UserInfo user, string copyrightHolder = null, string collaborators = null)
        {
            // Let's only allow admins to change these legal details.
            if (!(await user.IsAdmin()))
                throw new StandardErrors.NoAdminPermissions(user);
            using (var connection = OpenConnection())
            {
                await connection.ExecuteAsync(UpdateEditionLegalDetailsQuery.GetQuery,
                    new
                    {
                        EditionId = user.editionId,
                        CopyrightHolder = copyrightHolder,
                        Collaborators = collaborators,
                    });
            }
        }

        /// <summary>
        /// Delete all data from the edition that the user is currently subscribed to.
        /// </summary>
        /// <param name="user">User object requesting the delete</param>
        /// <param name="token">Token required to verify delete. If this is null, one will be created and sent
        /// to the requester to use a confirmation of the delete.</param>
        /// <returns>Returns a null string if successful; a string with a confirmation token if no token was provided.</returns>
        public async Task<string> DeleteAllEditionDataAsync(UserInfo user, string token)
        {
            // We only allow admins to delete all data in an unlocked edition.
            if (!(await user.IsAdmin()))
                throw new StandardErrors.NoAdminPermissions(user);
            
            // A token is required to delete an edition (we make sure here that people don't accidentally do it)
            if (string.IsNullOrEmpty(token))
            {
                return await GetDeleteToken(user);
            }

            // Remove write permissions from all editors, so they cannot make any changes while the delete proceeds
            var editors = await _getEditionEditors(user.editionId.Value);
            await Task.WhenAll(
                editors.Select(
                    x => ChangeEditionEditorRights(user, x.Email, x.MayRead, false, x.MayLock, x.IsAdmin)
                    )
                );

            // This transaction may take a while, so we cannot lock all of these tables. Otherwise, we DO get deadlock.
            // ReadUncommitted is fine because we will not get any further writes to this edition (all editors have lost
            // write permission), and any new writes to the user_email_token table will be irrelevant (the token
            // is unique).
            using (var transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted })
            )
            using (var connection = OpenConnection())
            {
                // Verify that the token is still valid
                var deleteToken = await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetTokenQuery, new
                    { Tokens = new []{token}, Type = CreateUserEmailTokenQuery.DeleteEdition});
                if (deleteToken != 1)
                    throw new StandardErrors.DataNotWritten("verifying the delete request token");
                
                // Dynamically get all tables that can be part of an edition, that way we don't worry about
                // this breaking due to future updates.
                var dataTables = await connection.QueryAsync<OwnerTables.Result>(OwnerTables.GetQuery);
                
                // Loop over every table and remove every entry with the requested editionId
                // Each individual delete can be async and happen concurrently
                await Task.WhenAll(
                    dataTables.Select(
                        dataTable => connection.ExecuteAsync(DeleteEditionFromTable.GetQuery(dataTable.TableName), 
                            new {EditionId = user.editionId ?? 0, UserId = user.userId ?? 0,})
                        ).ToArray()
                    );
                
                // Commit the full transaction (all or nothing)
                transactionScope.Complete();
                return null;
            }
        }

        public async Task<string> GetDeleteToken(UserInfo user)
        {
            // Generate our secret token
            var token = Guid.NewGuid().ToString();

            using (var connection = OpenConnection())
            {
                // Add the secret token to the database
                var userEmailConfirmation = await connection.ExecuteAsync(
                    CreateUserEmailTokenQuery.GetQuery(),
                    new
                    {
                        UserId = user.userId,
                        Token = token,
                        Type = CreateUserEmailTokenQuery.DeleteEdition
                    });
                if (userEmailConfirmation != 1) // Something strange must have gone wrong
                    throw new StandardErrors.DataNotWritten("create edition delete token");
            }

            return token;
        }

        public async Task<Permission> AddEditionEditor(UserInfo user, string editorEmail, bool? mayRead, bool? mayWrite, 
            bool? mayLock, bool? isAdmin)
        {
            // Make sure requesting user is admin, only and edition admin may perform this action
            if (!(await user.IsAdmin()))
                throw new StandardErrors.NoAdminPermissions(user);
            
            // Check if the editor already exists, don't attempt to re-add
            if ((await _getEditionEditors(user.editionId ?? 0)).Any(x => x.Email == editorEmail))
                throw new StandardErrors.ConflictingData("editor email");
            
            // Set the permissions object by coalescing with the default values
            var permissions = new Permission()
            {
                MayRead = mayRead ?? true,
                MayWrite = mayWrite ?? false,
                MayLock = mayLock ?? false,
                IsAdmin = isAdmin ?? false
            };
            
            // Check for invalid settings
            if (permissions.IsAdmin && !permissions.MayRead)
                throw new StandardErrors.InputDataRuleViolation("an edition admin must have read rights");
            
            if (permissions.MayWrite && !permissions.MayRead)
                throw new StandardErrors.InputDataRuleViolation("an editor with write rights must have read rights");

            using (var connection = OpenConnection())
            {
                // Add the editor
                var editorUpdateExecution = await connection.ExecuteAsync(CreateDetailedEditionEditorQuery.GetQuery,
                    new
                    {
                        EditionId = user.editionId ?? 0,
                        Email = editorEmail,
                        permissions.MayRead,
                        permissions.MayWrite,
                        permissions.MayLock,
                        permissions.IsAdmin
                    });

                if (editorUpdateExecution != 1)
                    throw new StandardErrors.DataNotWritten($"update permissions for {editorEmail}");

                // Return the results
                return permissions;
            }

            // In the future should we email the editor to confirm adding?
        }

        public async Task<Permission> ChangeEditionEditorRights(UserInfo user, string editorEmail, bool? mayRead,
            bool? mayWrite, bool? mayLock, bool? isAdmin)
        {
            // Make sure requesting user is admin when raising access, only and edition admin may perform this action
            if (((mayRead ?? false) || (mayWrite ?? false) || (mayLock ?? false) || (isAdmin ?? false)) && 
                !(await user.IsAdmin()))
                throw new StandardErrors.NoAdminPermissions(user);
            
            // Check if the editor exists
            var editors = await _getEditionEditors(user.editionId ?? 0);
            var currentEditorSettingsList = editors.Where(x => x.Email == editorEmail).ToList();
            if (currentEditorSettingsList.Count != 1) // There should be only 1 record
                throw new StandardErrors.DataNotFound("editor email", user.editionId.ToString(), 
                    "edition_editors");
            
            // Set the new permissions object by coalescing the new settings with those already existing
            var currentEditorSettings = currentEditorSettingsList.First();
            var permissions = new Permission()
            {
                MayRead = mayRead ?? currentEditorSettings.MayRead,
                MayWrite = mayWrite ?? currentEditorSettings.MayWrite,
                MayLock = mayLock ?? currentEditorSettings.MayLock,
                IsAdmin = isAdmin ?? currentEditorSettings.IsAdmin
            };
            
            // Make sure we are not removing an admin's read access (that is not allowed)
            if (permissions.IsAdmin && !permissions.MayRead)
                throw new StandardErrors.InputDataRuleViolation("read rights may not be revoked for an edition admin");
            
            // Make sure that we are not revoking editor's read access when editor still has write access 
            if (permissions.MayWrite && !permissions.MayRead)
                throw new StandardErrors.InputDataRuleViolation("read rights may not be revoked for an editor with write rights");
            
            using (var connection = OpenConnection())
            {
                // If the last admin is giving up admin rights, return error message with token for complete delete
                if (!editors.Any(x => (x.Email == editorEmail && permissions.IsAdmin) || (x.Email != editorEmail && x.IsAdmin)))
                    throw new StandardErrors.InputDataRuleViolation($@"an edition must have at least one admin.  
Please give admin status to another editor before relinquishing admin status for the current user or deleting the edition.
An admin may delete the edition for all editors with the request DELETE /v1/editions/{user.editionId.ToString()}.");
            
                // Perform the update
                var editorUpdateExecution = await connection.ExecuteAsync(UpdateEditionEditorPermissionsQuery.GetQuery,
                    new
                    {
                        EditionId = user.editionId ?? 0,
                        Email = editorEmail,
                        permissions.MayRead,
                        permissions.MayWrite,
                        permissions.MayLock,
                        permissions.IsAdmin
                    });

                if (editorUpdateExecution != 1)
                    throw new StandardErrors.DataNotWritten($"update permissions for {editorEmail}");

                // Return the results
                return permissions;
            }
            

            // In the future should we email the editor about their change in status?
        }

        private async Task<List<DetailedPermissions>> _getEditionEditors(uint editionId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<DetailedPermissions>(GetEditionEditorsWithPermissionsQuery.GetQuery,
                    new {EditionId = editionId})).ToList();
            }
        }
    }
}




