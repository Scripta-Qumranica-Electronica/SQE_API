﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess
{
    public interface IEditionRepository
    {
        Task<IEnumerable<Edition>> ListEditionsAsync(uint? userId, uint? editionId);
        Task ChangeEditionNameAsync(EditionUserInfo editionUser, string name);

        Task<uint> CopyEditionAsync(EditionUserInfo editionUser,
            string copyrightHolder = null,
            string collaborators = null);

        Task ChangeEditionCopyrightAsync(EditionUserInfo editionUser,
            string copyrightHolder = null,
            string collaborators = null);

        Task<string> DeleteAllEditionDataAsync(EditionUserInfo editionUser, string token);
        Task<string> GetDeleteToken(EditionUserInfo editionUser);

        Task<Permission> AddEditionEditor(EditionUserInfo editionUser,
            string editorEmail,
            bool? mayRead,
            bool? mayWrite,
            bool? mayLock,
            bool? isAdmin);

        Task<Permission> ChangeEditionEditorRights(EditionUserInfo editionUser,
            string editorEmail,
            bool? mayRead,
            bool? mayWrite,
            bool? mayLock,
            bool? isAdmin);
    }

    public class EditionRepository : DbConnectionBase, IEditionRepository
    {
        private readonly IDatabaseWriter _databaseWriter;

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
                    new
                    {
                        UserId = userId,
                        EditionId = editionId
                    }
                );

                var models = results.Select(result => CreateEdition(result, userId));
                return models;
            }
        }

        public async Task ChangeEditionNameAsync(EditionUserInfo editionUser, string name)
        {
            using (var connection = OpenConnection())
            {
                EditionNameQuery.Result result;

                try
                {
                    // Here we get the data from the original scroll_data field, we need the scroll_id,
                    // which no one in the front end will generally have or care about.
                    result = await connection.QuerySingleAsync<EditionNameQuery.Result>(
                        EditionNameQuery.GetQuery(),
                        new
                        {
                            editionUser.EditionId
                        }
                    );
                }
                catch (InvalidOperationException)
                {
                    throw new StandardExceptions.DataNotFoundException("edition", editionUser.EditionId);
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
                await _databaseWriter.WriteToDatabaseAsync(editionUser, new List<MutationRequest> { nameChangeRequest });
            }
        }

        /// <summary>
        ///     This creates a new copy of the requested edition, which will be owned with full privileges
        ///     by the requesting user.
        /// </summary>
        /// <param name="editionUser">
        ///     User info object contains the editionId that the user wishes to copy and
        ///     all user permissions related to it.
        /// </param>
        /// <param name="copyrightHolder">
        ///     Name of the person/institution that holds the copyright
        ///     (automatically created from user when null)
        /// </param>
        /// <param name="collaborators">
        ///     Names of all collaborators
        ///     (automatically created from user and all editors when null)
        /// </param>
        /// <returns>The editionId of the newly created edition.</returns>
        public async Task<uint> CopyEditionAsync(EditionUserInfo editionUser,
            string copyrightHolder = null,
            string collaborators = null)
        {
            var originalEditionData = new List<List<uint>>();
            List<OwnerTables.Result> ownerTables;

            using (var connection = OpenConnection())
            {
                // Collect all the data for the copy in one transaction.
                // This way we don't care if the edition is locked, the DB
                // will release the share locks once this relatively quick
                // transaction is complete, and will not block the copy from
                // edition during the writing of the new edition.
                ownerTables = (await connection.QueryAsync<OwnerTables.Result>(OwnerTables.GetQuery)).ToList();
                foreach (var ownerTable in ownerTables)
                {
                    var tableName = ownerTable.TableName;
                    var tableIdColumn = tableName.Substring(0, tableName.Length - 5) + "id";
                    originalEditionData.Add(
                        (await connection.QueryAsync<uint>(
                            GetOwnerTableDataForQuery.GetQuery(tableName, tableIdColumn),
                            new
                            {
                                editionUser.EditionId
                            }
                        )).ToList()
                    );
                }
            }

            // Right now we create all the new rows for the new edition
            // in one transaction. The benefit of this is that if it fails
            // for some reason, nothing is committed. The downside is that
            // it is a long-running process, that touches many rows (thus
            // locking them up). If we really do run into performance problems,
            // consider writing each data table in its own transaction,
            // and be prepared to DELETE all INSERTS in the case an unrecoverable
            // failure is encountered. The danger of doing that is that you might
            // end up with "orphaned" INSERTS in the case that the API crashes
            // in the middle of such a procedure. Then you would need to periodically
            // check the owner tables for these failed writes, which could be difficult.
            return await DatabaseCommunicationRetryPolicy.ExecuteRetry(
                async () =>
                {
                    using (var transactionScope = new TransactionScope())
                    using (var connection = OpenConnection())
                    {
                        // Create a new edition
                        connection.Execute(
                            CopyEditionQuery.GetQuery,
                            new
                            {
                                editionUser.EditionId,
                                CopyrightHolder = copyrightHolder,
                                Collaborators = collaborators
                            }
                        );

                        var toEditionId = await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);
                        if (toEditionId == 0)
                            throw new StandardExceptions.DataNotWrittenException("create edition");

                        // Create new edition_editor
                        connection.Execute(
                            CreateEditionEditorQuery.GetQuery,
                            new
                            {
                                EditionId = toEditionId,
                                UserId = editionUser.userId,
                                MayLock = 1,
                                IsAdmin = 1
                            }
                        );

                        var toEditionEditorId = await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);
                        if (toEditionEditorId == 0)
                            throw new StandardExceptions.DataNotWrittenException("create edition_editor");

                        // Copy data collected in the previous transaction over to the new edition
                        var writeTasks = new List<Task<int>>();
                        foreach (var (ownerTable, index) in ownerTables.Select((v, i) => (v, i)))
                            if (originalEditionData[index].Count > 0)
                            {
                                var tableName = ownerTable.TableName;
                                var tableIdColumn = tableName.Substring(0, tableName.Length - 5) + "id";
                                writeTasks.Add(
                                    connection.ExecuteAsync(
                                        WriteOwnerTableData.GetQuery(
                                            tableName,
                                            tableIdColumn,
                                            toEditionId,
                                            toEditionEditorId,
                                            originalEditionData[index]
                                        )
                                    )
                                );
                            }

                        await Task.WhenAll(writeTasks);

                        //Cleanup
                        transactionScope.Complete();
                        return toEditionId;
                    }
                }
            );
        }

        /// <summary>
        ///     Change copyright holder and/or collaborators of the users current edition.
        /// </summary>
        /// <param name="editionUser">The user's current state.</param>
        /// <param name="copyrightHolder">The new copyright holder name to use</param>
        /// <param name="collaborators">
        ///     The new collaborator list. Null is meaningful here
        ///     and will switch to an autogenerated collaborator listing.
        /// </param>
        /// <returns></returns>
        public async Task ChangeEditionCopyrightAsync(EditionUserInfo editionUser,
            string copyrightHolder = null,
            string collaborators = null)
        {
            // Let's only allow admins to change these legal details.
            if (!editionUser.IsAdmin)
                throw new StandardExceptions.NoAdminPermissionsException(editionUser);
            using (var connection = OpenConnection())
            {
                await connection.ExecuteAsync(
                    UpdateEditionLegalDetailsQuery.GetQuery,
                    new
                    {
                        editionUser.EditionId,
                        CopyrightHolder = copyrightHolder,
                        Collaborators = collaborators
                    }
                );
            }
        }

        /// <summary>
        ///     Delete all data from the edition that the user is currently subscribed to.
        /// </summary>
        /// <param name="editionUser">User object requesting the delete</param>
        /// <param name="token">
        ///     Token required to verify delete. If this is null, one will be created and sent
        ///     to the requester to use a confirmation of the delete.
        /// </param>
        /// <returns>Returns a null string if successful; a string with a confirmation token if no token was provided.</returns>
        public async Task<string> DeleteAllEditionDataAsync(EditionUserInfo editionUser, string token)
        {
            // We only allow admins to delete all data in an unlocked edition.
            if (!editionUser.IsAdmin)
                throw new StandardExceptions.NoAdminPermissionsException(editionUser);

            // A token is required to delete an edition (we make sure here that people don't accidentally do it)
            if (string.IsNullOrEmpty(token)) return await GetDeleteToken(editionUser);

            // Remove write permissions from all editors, so they cannot make any changes while the delete proceeds
            var editors = await _getEditionEditors(editionUser.EditionId);
            await Task.WhenAll(
                editors.Select(
                    x => ChangeEditionEditorRights(editionUser, x.Email, x.MayRead, false, x.MayLock, x.IsAdmin)
                )
            );

            // Note: I had wrapped the following in a transaction, but this has the problem that it can lockup every
            // *_owner table in the entire database for a significant amount of time (sometimes 1000's of rows will be
            // deleted from a single table). So I am doing it now without any transaction and with some retry
            // logic.  What this means is that a delete might be partially carried out and return with an error,
            // in which case the user will need to try again. This is not too worrisome since an inconsistent state
            // for a deleted edition is not a cause for user concern (the users only care about the edition
            // becoming unusable, not whether any data was left behind). It is a concern for those maintaining the
            // database, and we should discuss what might be done for that.  We could check for this and other things
            // with some "health check" services.
            using (var connection = OpenConnection())
            {
                // Verify that the token is still valid
                var deleteToken = await connection.ExecuteAsync(
                    DeleteUserEmailTokenQuery.GetTokenQuery,
                    new
                    { Tokens = new[] { token }, Type = CreateUserEmailTokenQuery.DeleteEdition }
                );
                if (deleteToken != 1)
                    throw new StandardExceptions.DataNotWrittenException("verifying the delete request token");

                // Dynamically get all tables that can be part of an edition, that way we don't worry about
                // this breaking due to future updates.
                var dataTables = await connection.QueryAsync<OwnerTables.Result>(OwnerTables.GetQuery);

                // Loop over every table and remove every entry with the requested editionId
                // Each individual delete can be async and happen concurrently
                await Task.WhenAll(
                    dataTables.Select(
                            dataTable => DeleteDataFromOwnerTable(connection, dataTable.TableName, editionUser)
                        )
                        .ToArray()
                );

                return null;
            }
        }


        public async Task<string> GetDeleteToken(EditionUserInfo editionUser)
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
                        UserId = editionUser.userId,
                        Token = token,
                        Type = CreateUserEmailTokenQuery.DeleteEdition
                    }
                );
                if (userEmailConfirmation != 1) // Something strange must have gone wrong
                    throw new StandardExceptions.DataNotWrittenException("create edition delete token");
            }

            return token;
        }

        public async Task<Permission> AddEditionEditor(EditionUserInfo editionUser,
            string editorEmail,
            bool? mayRead,
            bool? mayWrite,
            bool? mayLock,
            bool? isAdmin)
        {
            // Make sure requesting user is admin, only and edition admin may perform this action
            if (!editionUser.IsAdmin)
                throw new StandardExceptions.NoAdminPermissionsException(editionUser);

            // Check if the editor already exists, don't attempt to re-add
            if ((await _getEditionEditors(editionUser.EditionId)).Any(x => x.Email == editorEmail))
                throw new StandardExceptions.ConflictingDataException("editor email");

            // Set the permissions object by coalescing with the default values
            var permissions = new Permission
            {
                MayRead = mayRead ?? true,
                MayWrite = mayWrite ?? false,
                MayLock = mayLock ?? false,
                IsAdmin = isAdmin ?? false
            };

            // Check for invalid settings
            if (permissions.IsAdmin
                && !permissions.MayRead)
                throw new StandardExceptions.InputDataRuleViolationException("an edition admin must have read rights");
            if (permissions.MayWrite
                && !permissions.MayRead)
                throw new StandardExceptions.InputDataRuleViolationException(
                    "an editor with write rights must have read rights"
                );
            using (var connection = OpenConnection())
            {
                // Add the editor
                var editorUpdateExecution = await connection.ExecuteAsync(
                    CreateDetailedEditionEditorQuery.GetQuery,
                    new
                    {
                        editionUser.EditionId,
                        Email = editorEmail,
                        permissions.MayRead,
                        permissions.MayWrite,
                        permissions.MayLock,
                        permissions.IsAdmin
                    }
                );

                if (editorUpdateExecution != 1)
                    throw new StandardExceptions.DataNotWrittenException($"update permissions for {editorEmail}");

                // Return the results
                return permissions;
            }

            // In the future should we email the editor to confirm adding?
        }

        public async Task<Permission> ChangeEditionEditorRights(EditionUserInfo editionUser,
            string editorEmail,
            bool? mayRead,
            bool? mayWrite,
            bool? mayLock,
            bool? isAdmin)
        {
            // Make sure requesting user is admin when raising access, only and edition admin may perform this action
            if (((mayRead ?? false) || (mayWrite ?? false) || (mayLock ?? false) || (isAdmin ?? false))
                && !editionUser.IsAdmin)
                throw new StandardExceptions.NoAdminPermissionsException(editionUser);

            // Check if the editor exists
            var editors = await _getEditionEditors(editionUser.EditionId);

            var currentEditorSettingsList = editors.Where(x => x.Email == editorEmail).ToList();
            if (currentEditorSettingsList.Count != 1) // There should be only 1 record
                throw new StandardExceptions.DataNotFoundException(
                    "editor email",
                    editionUser.EditionId.ToString(),
                    "edition_editors"
                );

            // Set the new permissions object by coalescing the new settings with those already existing
            var currentEditorSettings = currentEditorSettingsList.First();

            var permissions = new Permission
            {
                MayRead = mayRead ?? currentEditorSettings.MayRead,
                MayWrite = mayWrite ?? currentEditorSettings.MayWrite,
                MayLock = mayLock ?? currentEditorSettings.MayLock,
                IsAdmin = isAdmin ?? currentEditorSettings.IsAdmin
            };

            // Make sure we are not removing an admin's read access (that is not allowed)
            if (permissions.IsAdmin
                && !permissions.MayRead)
                throw new StandardExceptions.InputDataRuleViolationException(
                    "read rights may not be revoked for an edition admin"
                );

            // Make sure that we are not revoking editor's read access when editor still has write access 
            if (permissions.MayWrite
                && !permissions.MayRead)
                throw new StandardExceptions.InputDataRuleViolationException(
                    "read rights may not be revoked for an editor with write rights"
                );
            using (var connection = OpenConnection())
            {
                // If the last admin is giving up admin rights, return error message with token for complete delete
                if (!editors.Any(
                    x =>
                        x.Email == editorEmail && permissions.IsAdmin || x.Email != editorEmail && x.IsAdmin
                ))
                    throw new StandardExceptions.InputDataRuleViolationException(
                        $@"an edition must have at least one admin.  
Please give admin status to another editor before relinquishing admin status for the current user or deleting the edition.
An admin may delete the edition for all editors with the request DELETE /v1/editions/{editionUser.EditionId.ToString()}."
                    );

                // Perform the update
                var editorUpdateExecution = await connection.ExecuteAsync(
                    UpdateEditionEditorPermissionsQuery.GetQuery,
                    new
                    {
                        editionUser.EditionId,
                        Email = editorEmail,
                        permissions.MayRead,
                        permissions.MayWrite,
                        permissions.MayLock,
                        permissions.IsAdmin
                    }
                );

                if (editorUpdateExecution != 1)
                    throw new StandardExceptions.DataNotWrittenException($"update permissions for {editorEmail}");

                // Return the results
                return permissions;
            }

            // In the future should we email the editor about their change in status?
        }

        private static Edition CreateEdition(EditionGroupQuery.Result result, uint? currentUserId)
        {
            var model = new Edition
            {
                EditionId = result.EditionId,
                Name = result.Name,
                EditionDataEditorId = result.EditionDataEditorId,
                ScrollId = result.ScrollId,
                Thumbnail = result.Thumbnail,
                Locked = result.Locked,
                LastEdit = result.LastEdit,
                IsPublic = result.IsPublic,
                Owner = new User
                {
                    UserId = result.UserId,
                    Email = result.Email
                },
                Copyright = Licence.printLicence(result.CopyrightHolder, result.Collaborators),
                CopyrightHolder = result.CopyrightHolder,
                Collaborators = result.Collaborators
            };

            if (currentUserId.HasValue)
                model.Permission = new Permission
                {
                    IsAdmin = result.Admin,
                    MayWrite = result.MayWrite,
                    MayLock = result.MayLock
                };
            else
                model.Permission = new Permission
                {
                    IsAdmin = false,
                    MayLock = false,
                    MayWrite = false
                };

            return model;
        }

        private static async Task DeleteDataFromOwnerTable(IDbConnection connection,
            string tableName,
            EditionUserInfo editionUser)
        {
            await DatabaseCommunicationRetryPolicy.ExecuteRetry(
                async () =>
                    await connection.ExecuteAsync(
                        DeleteEditionFromTable.GetQuery(tableName),
                        new { editionUser.EditionId, UserId = editionUser.userId }
                    )
            );
        }

        private async Task<List<DetailedPermissions>> _getEditionEditors(uint editionId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<DetailedPermissions>(
                    GetEditionEditorsWithPermissionsQuery.GetQuery,
                    new { EditionId = editionId }
                )).ToList();
            }
        }
    }
}