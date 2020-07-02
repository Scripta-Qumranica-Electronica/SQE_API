using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using SQE.DatabaseAccess.Models;
using Xunit;

// TODO: all the tests for sharing have been commented out, since I updated the sharing system in
// accordance with the team wishes.  They simply should be updated to reflect the new API endpoints
// and the requirement for invited editor to accept invitation via JWT.
namespace SQE.ApiTest
{
    /// <summary>
    ///     This test suite tests all the current endpoints in the EditionController
    /// </summary>
    public class EditionTests : WebControllerTest
    {
        public EditionTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _addEditionEditor = $"/{version}/{controller}/$EditionId/editors";
        }

        private const string version = "v1";
        private const string controller = "editions";
        private readonly string _addEditionEditor;

        /// <summary>
        /// This verifies that an edition can be shared and that the
        /// submitted permission match the granted permissions
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanAdminShareEdition()
        {
            // Arrange
            // Grab a new edition
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);
            try
            {
                // Act
                // Send in the editor request 
                var (_, listenerResponse) = await shareEdition(
                    newEdition,
                    Request.DefaultUsers.User1,
                    Request.DefaultUsers.User2);

                // Act
                // User 2 will confirm the request using the token it received
                var (httpConfirmResponse, shareConfirmMsg, listenerConfirmResponse) =
                    await confirmEditor(
                        newEdition,
                        listenerResponse.token,
                        Request.DefaultUsers.User2,
                        Request.DefaultUsers.User1);

                // Assert
                Assert.Equal(shareConfirmMsg.isAdmin, listenerConfirmResponse.isAdmin);
                Assert.Equal(shareConfirmMsg.mayWrite, listenerConfirmResponse.mayWrite);
                Assert.Equal(shareConfirmMsg.mayLock, listenerConfirmResponse.mayLock);
                Assert.Equal(shareConfirmMsg.mayRead, listenerConfirmResponse.mayRead);
                Assert.Equal(Request.DefaultUsers.User2.email, listenerConfirmResponse.email);

                // Arrange
                // User 1 should get the basic info about the shared edition
                var get1 = new Get.V1_Editions_EditionId(newEdition);
                var (_, user1Msg, _, _) = await Request.Send(
                    get1,
                    _client,
                    null,
                    auth: true
                );

                // User 2 should get the basic info about the shared edition
                var (_, user2Msg, _, _) = await Request.Send(
                    get1,
                    _client,
                    null,
                    auth: true,
                    requestUser: Request.DefaultUsers.User2
                );

                // Assert
                Assert.Equal(user1Msg.primary.id, user2Msg.primary.id);
                Assert.Equal(user1Msg.primary.copyright, user2Msg.primary.copyright);
                Assert.Equal(user1Msg.primary.isPublic, user2Msg.primary.isPublic);
                Assert.Equal(user1Msg.primary.locked, user2Msg.primary.locked);
                Assert.Equal(user1Msg.primary.name, user2Msg.primary.name);
                Assert.Equal(user1Msg.primary.thumbnailUrl, user2Msg.primary.thumbnailUrl);
                Assert.Equal(user1Msg.primary.editionDataEditorId, user2Msg.primary.editionDataEditorId);
                user1Msg.primary.shares.ShouldDeepEqual(user2Msg.primary.shares);
                user1Msg.primary.lastEdit.ShouldDeepEqual(user2Msg.primary.lastEdit);
            }
            finally
            {
                // Cleanup
                await EditionHelpers.DeleteEdition(_client, newEdition);
            }
        }

        /// <summary>
        /// This checks to make sure that an admin can see invitations to edit
        /// that have not yet been accepted.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanViewOutstandingShareRequests()
        {
            // Arrange
            // Grab a new edition
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);
            try
            {
                // Act
                // Send in the editor request 
                var (httpResponse, listenerResponse) = await shareEdition(
                    newEdition,
                    Request.DefaultUsers.User1,
                    Request.DefaultUsers.User2);

                // Assert
                httpResponse.EnsureSuccessStatusCode();
                Assert.True(listenerResponse.mayRead);
                Assert.True(listenerResponse.mayWrite);
                Assert.True(listenerResponse.mayLock);
                Assert.True(listenerResponse.isAdmin);
                Assert.NotNull(listenerResponse.token);

                // Act
                // Check to see if outstanding request is accessible to admin
                var requestAvailable = new Get.V1_Editions_AdminShareRequests();
                var (httpShareRequestResponse, shareRequestMsg, _, _) = await Request.Send(
                    requestAvailable,
                    _client,
                    null,
                    auth: true,
                    requestUser: Request.DefaultUsers.User1,
                    requestRealtime: false
                );

                // Assert
                httpShareRequestResponse.EnsureSuccessStatusCode();
                Assert.NotEmpty(shareRequestMsg.editorRequests);
                var foundMatch = false;
                foreach (var share in shareRequestMsg.editorRequests)
                {
                    if (share.editionId == listenerResponse.editionId && share.editorEmail == Request.DefaultUsers.User2.email)
                    {
                        Assert.Equal(share.date, listenerResponse.date);
                        Assert.Equal(share.mayLock, listenerResponse.mayLock);
                        Assert.Equal(share.mayRead, listenerResponse.mayRead);
                        Assert.Equal(share.mayWrite, listenerResponse.mayWrite);
                        Assert.Equal(share.isAdmin, listenerResponse.isAdmin);
                        foundMatch = true;
                    }
                }
                Assert.True(foundMatch);
            }
            finally
            {
                // Cleanup
                await EditionHelpers.DeleteEdition(_client, newEdition);
            }
        }

        /// <summary>
        /// This checks whether a user can see invitations to become an editor
        /// that have not yet been accepted.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanViewOutstandingShareInvitations()
        {
            // Arrange
            // Grab a new edition
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);
            try
            {
                // Act
                // Send in the editor request 
                var (_, listenerResponse) = await shareEdition(
                    newEdition,
                    Request.DefaultUsers.User1,
                    Request.DefaultUsers.User2);

                // Act
                // Check to see if outstanding request is accessible to user who received the invitation
                var requestAvailable = new Get.V1_Editions_EditorInvitations();
                var (httpShareRequestResponse, shareRequestMsg, _, _) = await Request.Send(
                    requestAvailable,
                    _client,
                    null,
                    auth: true,
                    requestUser: Request.DefaultUsers.User2,
                    requestRealtime: false
                );

                // Assert
                httpShareRequestResponse.EnsureSuccessStatusCode();
                Assert.NotEmpty(shareRequestMsg.editorInvitations);
                var foundMatch = false;
                foreach (var share in shareRequestMsg.editorInvitations)
                {
                    if (share.editionId == listenerResponse.editionId && share.requestingAdminEmail == Request.DefaultUsers.User1.email)
                    {
                        Assert.Equal(share.date, listenerResponse.date);
                        Assert.Equal(share.mayLock, listenerResponse.mayLock);
                        Assert.Equal(share.mayRead, listenerResponse.mayRead);
                        Assert.Equal(share.mayWrite, listenerResponse.mayWrite);
                        Assert.Equal(share.isAdmin, listenerResponse.isAdmin);
                        Assert.Equal(share.token, listenerResponse.token);
                        foundMatch = true;
                    }
                }
                Assert.True(foundMatch);
            }
            finally
            {
                // Cleanup
                await EditionHelpers.DeleteEdition(_client, newEdition);
            }
        }

        /// <summary>
        /// This creates a new edition, shares it with minimal rights to another user,
        /// then grants that second user admin rights, then the second user
        /// performs an admin operation.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanChangeEditionSharePermissions()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);

            try
            {
                // Arrange
                var newPermissions = new InviteEditorDTO
                {
                    email = Request.DefaultUsers.User2.email,
                    mayLock = false,
                    mayWrite = false,
                    isAdmin = false
                };

                // Share the edition
                var (_, listenerResponse) = await shareEdition(
                    newEdition,
                    Request.DefaultUsers.User1,
                    Request.DefaultUsers.User2,
                    newPermissions);
                await confirmEditor(
                        newEdition,
                        listenerResponse.token,
                        Request.DefaultUsers.User2,
                        Request.DefaultUsers.User1);

                // Act 
                // Check permissions for edition info request
                var get1 = new Get.V1_Editions_EditionId(newEdition);
                var (_, user2Msg, _, _) = await Request.Send(
                    get1,
                    _client,
                    null,
                    auth: true,
                    requestUser: Request.DefaultUsers.User2
                );

                // Assert
                // Permissions are correct
                Assert.False(user2Msg.primary.permission.mayWrite);
                Assert.False(user2Msg.primary.permission.isAdmin);

                // Arrange (change permissions)
                var updatePermissions = new DetailedUpdateEditorRightsDTO()
                {
                    mayWrite = true,
                    isAdmin = true,
                    mayRead = true,
                    mayLock = false
                };

                // Act
                // Change permissions
                var add2 = new Put.V1_Editions_EditionId_Editors_EditorEmailId(
                    newEdition,
                    newPermissions.email,
                    updatePermissions
                );
                var (_, share2Msg, _, _) = await Request.Send(
                    add2,
                    _client,
                    null,
                    auth: true,
                    requestUser: Request.DefaultUsers.User1
                );

                // Assert
                Assert.True(share2Msg.mayRead);
                Assert.True(share2Msg.mayWrite);
                Assert.False(share2Msg.mayLock);
                Assert.True(share2Msg.isAdmin);

                // Act 
                // Check permissions for edition info request
                var (_, user2Msg2, _, _) = await Request.Send(
                    get1,
                    _client,
                    null,
                    auth: true,
                    requestUser: Request.DefaultUsers.User2
                );

                // Assert
                Assert.True(user2Msg2.primary.permission.mayWrite);
                Assert.True(user2Msg2.primary.permission.isAdmin);

                // Act
                // Check that user2 really can perform admin actions
                updatePermissions.mayLock = true;
                var add3 = new Put.V1_Editions_EditionId_Editors_EditorEmailId(
                    newEdition,
                    newPermissions.email,
                    updatePermissions
                );
                var (_, share3Msg, _, _) = await Request.Send(
                    add3,
                    _client,
                    null,
                    auth: true,
                    requestUser: Request.DefaultUsers.User2
                );

                // Assert
                Assert.True(share3Msg.mayRead);
                Assert.True(share3Msg.mayWrite);
                Assert.True(share3Msg.mayLock);
                Assert.True(share3Msg.isAdmin);
            }
            finally
            {
                // Cleanup
                await EditionHelpers.DeleteEdition(_client, newEdition);
            }
        }

        // TODO: write the rest of the tests.
        // TODO: finish updating tests to use new request objects.
        [Fact]
        public async Task CanDeleteEditionAsAdmin()
        {
            // Arrange
            var editionId = await EditionHelpers.CreateCopyOfEdition(_client);
            const string url = "/v1/editions";

            // Act
            var (response, msg) = await Request.SendHttpRequestAsync<string, string>(
                _client,
                HttpMethod.Delete,
                url + "/" + editionId,
                null,
                await Request.GetJwtViaHttpAsync(_client)
            );

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); // Should fail without confirmation

            // Delete the edition for real
            await EditionHelpers.DeleteEdition(_client, editionId);
            var (editionResponse, editionMsg) = await Request.SendHttpRequestAsync<string, EditionListDTO>(
                _client,
                HttpMethod.Get,
                url,
                null,
                await Request.GetJwtViaHttpAsync(_client)
            );
            editionResponse.EnsureSuccessStatusCode();
            var editionMatch = editionMsg.editions.SelectMany(x => x).Where(x => x.id == editionId);
            Assert.Empty(editionMatch);
        }

        // TODO: finish updating test to use new request objects.
        /// <summary>
        ///     Test copy of copy of edition
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanDuplicateCopiedEdition()
        {
            // ARRANGE
            var editionId = await EditionHelpers.CreateCopyOfEdition(_client, name: "first edition");
            await EditionHelpers.CreateCopyOfEdition(_client, editionId, "second edition");
        }

        /// <summary>
        /// Check if we can share an edition readonly with locking permission
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanLockableShareEdition()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);

            try
            {
                // Arrange
                var newPermissions = new InviteEditorDTO()
                {
                    email = Request.DefaultUsers.User2.email,
                    mayWrite = false,
                    isAdmin = false,
                    mayLock = true
                };

                // Act
                var (shareResponse, shareMsg) = await shareEdition(
                    newEdition,
                    Request.DefaultUsers.User1,
                    Request.DefaultUsers.User2,
                    newPermissions);
                await confirmEditor(
                    newEdition,
                    shareMsg.token,
                    Request.DefaultUsers.User2,
                    Request.DefaultUsers.User1);

                // Assert
                Assert.True(shareMsg.mayRead);
                Assert.False(shareMsg.mayWrite);
                Assert.True(shareMsg.mayLock);
                Assert.False(shareMsg.isAdmin);
            }
            finally
            {
                // Cleanup
                await EditionHelpers.DeleteEdition(_client, newEdition);
            }
        }

        [Fact]
        public async Task CanNotAdminWithoutReadShareEdition()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);

            try
            {
                // Arrange
                var newPermissions = new InviteEditorDTO
                {
                    email = Request.DefaultUsers.User2.email,
                    mayLock = false,
                    mayWrite = false,
                    isAdmin = false
                };

                // Share the edition
                var (_, listenerResponse) = await shareEdition(
                    newEdition,
                    Request.DefaultUsers.User1,
                    Request.DefaultUsers.User2,
                    newPermissions);
                await confirmEditor(
                        newEdition,
                        listenerResponse.token,
                        Request.DefaultUsers.User2,
                        Request.DefaultUsers.User1);

                // Act 
                // Check permissions for edition info request
                var get1 = new Get.V1_Editions_EditionId(newEdition);
                var (_, user2Msg, _, _) = await Request.Send(
                    get1,
                    _client,
                    null,
                    auth: true,
                    requestUser: Request.DefaultUsers.User2
                );

                // Assert
                // Permissions are correct
                Assert.False(user2Msg.primary.permission.mayWrite);
                Assert.False(user2Msg.primary.permission.isAdmin);

                // Arrange (change permissions)
                var updatePermissions = new DetailedUpdateEditorRightsDTO()
                {
                    mayWrite = false,
                    isAdmin = true,
                    mayRead = false,
                    mayLock = false
                };

                // Act
                // Change permissions
                var add2 = new Put.V1_Editions_EditionId_Editors_EditorEmailId(
                    newEdition,
                    newPermissions.email,
                    updatePermissions
                );
                var (share2Response, share2Msg, _, _) = await Request.Send(
                    add2,
                    _client,
                    null,
                    auth: true,
                    requestUser: Request.DefaultUsers.User1,
                    shouldSucceed: false
                );

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, share2Response.StatusCode);
            }
            finally
            {
                // Cleanup
                await EditionHelpers.DeleteEdition(_client, newEdition);
            }
        }

        // TODO: finish updating test to use new request objects.
        [Fact]
        public async Task CanNotDeleteEditionWhenAnonymous()
        {
            // Arrange
            var editionId = await EditionHelpers.CreateCopyOfEdition(_client);
            const string url = "/v1/editions";

            // Act
            var (response, msg) = await Request.SendHttpRequestAsync<string, string>(
                _client,
                HttpMethod.Delete,
                url + "/" + editionId,
                null
            );

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var (editionResponse, editionMsg) = await Request.SendHttpRequestAsync<string, EditionListDTO>(
                _client,
                HttpMethod.Get,
                url,
                null,
                await Request.GetJwtViaHttpAsync(_client)
            );
            editionResponse.EnsureSuccessStatusCode();
            var editionMatch = editionMsg.editions.SelectMany(x => x).Where(x => x.id == editionId);
            Assert.Single(editionMatch);

            await EditionHelpers.DeleteEdition(_client, editionId);
        }

        [Fact]
        public async Task CanNotWriteWithoutReadShareEdition()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);

            try
            {
                // Arrange
                var newPermissions = new InviteEditorDTO
                {
                    email = Request.DefaultUsers.User2.email,
                    mayLock = false,
                    mayWrite = false,
                    isAdmin = false
                };

                // Share the edition
                var (_, listenerResponse) = await shareEdition(
                    newEdition,
                    Request.DefaultUsers.User1,
                    Request.DefaultUsers.User2,
                    newPermissions);
                await confirmEditor(
                        newEdition,
                        listenerResponse.token,
                        Request.DefaultUsers.User2,
                        Request.DefaultUsers.User1);

                // Act 
                // Check permissions for edition info request
                var get1 = new Get.V1_Editions_EditionId(newEdition);
                var (_, user2Msg, _, _) = await Request.Send(
                    get1,
                    _client,
                    null,
                    auth: true,
                    requestUser: Request.DefaultUsers.User2
                );

                // Assert
                // Permissions are correct
                Assert.False(user2Msg.primary.permission.mayWrite);
                Assert.False(user2Msg.primary.permission.isAdmin);

                // Arrange (change permissions)
                var updatePermissions = new DetailedUpdateEditorRightsDTO()
                {
                    mayWrite = true,
                    isAdmin = false,
                    mayRead = false,
                    mayLock = false
                };

                // Act
                // Change permissions
                var add2 = new Put.V1_Editions_EditionId_Editors_EditorEmailId(
                    newEdition,
                    newPermissions.email,
                    updatePermissions
                );
                var (share2Response, share2Msg, _, _) = await Request.Send(
                    add2,
                    _client,
                    null,
                    auth: true,
                    requestUser: Request.DefaultUsers.User1,
                    shouldSucceed: false
                );

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, share2Response.StatusCode);
            }
            finally
            {
                // Cleanup
                await EditionHelpers.DeleteEdition(_client, newEdition);
            }
        }

        [Fact]
        public async Task CanProperlyDeleteSharedEdition()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);
            var notDeleted = true;

            try
            {
                // Arrange
                var newPermissions = new InviteEditorDTO
                {
                    email = Request.DefaultUsers.User2.email,
                    mayLock = true,
                    mayWrite = true,
                    isAdmin = false
                };

                // Share the edition
                var (_, listenerResponse) = await shareEdition(
                    newEdition,
                    Request.DefaultUsers.User1,
                    Request.DefaultUsers.User2,
                    newPermissions);
                await confirmEditor(
                    newEdition,
                    listenerResponse.token,
                    Request.DefaultUsers.User2,
                    Request.DefaultUsers.User1);

                var (deleteResponse, deleteMsg) = await Request.SendHttpRequestAsync<string, string>(
                    _client,
                    HttpMethod.Delete,
                    "/v1/editions/" + newEdition,
                    null,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User2.email, password = Request.DefaultUsers.User2.password }
                    )
                );

                // Assert
                deleteResponse.EnsureSuccessStatusCode();

                var (user1Resp, user1Msg) = await Request.SendHttpRequestAsync<string, EditionGroupDTO>(
                    _client,
                    HttpMethod.Get,
                    $"/v1/editions/{newEdition}",
                    null,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User1.email, password = Request.DefaultUsers.User1.password }
                    )
                );
                user1Resp.EnsureSuccessStatusCode();

                var (user2Resp, user2Msg) = await Request.SendHttpRequestAsync<string, EditionGroupDTO>(
                    _client,
                    HttpMethod.Get,
                    $"/v1/editions/{newEdition}",
                    null,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User2.email, password = Request.DefaultUsers.User2.password }
                    )
                );
                Assert.Equal(HttpStatusCode.Forbidden, user2Resp.StatusCode);
                Assert.NotNull(user1Msg);

                // Act (final delete)
                var (delete2Response, delete2Msg) = await Request.SendHttpRequestAsync<string, string>(
                    _client,
                    HttpMethod.Delete,
                    "/v1/editions/" + newEdition,
                    null,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User1.email, password = Request.DefaultUsers.User1.password }
                    )
                );

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, delete2Response.StatusCode); // Should fail for last admin
                                                                                     // Kill the edition for real
                await EditionHelpers.DeleteEdition(
                    _client,
                    newEdition,
                    true,
                    true,
                    new Request.UserAuthDetails
                    { email = Request.DefaultUsers.User1.email, password = Request.DefaultUsers.User1.password }
                );
                var (user1Resp2, user1Msg2) = await Request.SendHttpRequestAsync<string, EditionGroupDTO>(
                    _client,
                    HttpMethod.Get,
                    $"/v1/editions/{newEdition}",
                    null,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User1.email, password = Request.DefaultUsers.User1.password }
                    )
                );
                if (user1Resp2.StatusCode == HttpStatusCode.NoContent)
                    notDeleted = false;
                user1Resp2.EnsureSuccessStatusCode();
                Assert.Null(user1Msg2);
            }
            finally
            {
                // Cleanup
                if (notDeleted)
                    await EditionHelpers.DeleteEdition(_client, newEdition);
            }


            //Todo: maybe run a database check here to ensure that all references to newEdition have been removed from all *_owner tables
        }

        [Fact]
        public async Task CanWriteShareEdition()
        {
            // Arrange
            var newPermissions = new InviteEditorDTO
            {
                email = Request.DefaultUsers.User2.email,
                mayLock = false,
                mayWrite = true,
                isAdmin = false
            };
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);
            try
            {
                // Arrange
                var (_, listenerResponse) = await shareEdition(
                    newEdition,
                    Request.DefaultUsers.User1,
                    Request.DefaultUsers.User2,
                    newPermissions);
                await confirmEditor(
                    newEdition,
                    listenerResponse.token,
                    Request.DefaultUsers.User2,
                    Request.DefaultUsers.User1);
                const string newName = "My cool new name";
                var update = new EditionUpdateRequestDTO()
                {
                    name = newName,
                    collaborators = null,
                    copyrightHolder = null
                };

                // Act
                var changeNameReq = new Put.V1_Editions_EditionId(newEdition, update);
                var (updateResp, updateMsg, _, listenerMsg) = await Request.Send(
                    changeNameReq,
                    _client,
                    StartConnectionAsync,
                    auth: true,
                    requestRealtime: false,
                    requestUser: Request.DefaultUsers.User2,
                    listenerUser: Request.DefaultUsers.User1);

                // Assert
                Assert.Equal(update.name, updateMsg.name);
                Assert.Equal(update.name, listenerMsg.name);

                var (user1Resp, user1Msg) = await Request.SendHttpRequestAsync<string, EditionGroupDTO>(
                    _client,
                    HttpMethod.Get,
                    $"/v1/editions/{newEdition}",
                    null,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User1.email, password = Request.DefaultUsers.User1.password }
                    )
                );
                user1Resp.EnsureSuccessStatusCode();

                var (user2Resp, user2Msg) = await Request.SendHttpRequestAsync<string, EditionGroupDTO>(
                    _client,
                    HttpMethod.Get,
                    $"/v1/editions/{newEdition}",
                    null,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User2.email, password = Request.DefaultUsers.User2.password }
                    )
                );
                user2Resp.EnsureSuccessStatusCode();
                Assert.Equal(user1Msg.primary.id, user2Msg.primary.id);
                Assert.Equal(user1Msg.primary.copyright, user2Msg.primary.copyright);
                Assert.Equal(user1Msg.primary.isPublic, user2Msg.primary.isPublic);
                Assert.Equal(user1Msg.primary.locked, user2Msg.primary.locked);
                Assert.Equal(user1Msg.primary.name, user2Msg.primary.name);
                Assert.Equal(user1Msg.primary.thumbnailUrl, user2Msg.primary.thumbnailUrl);
                user1Msg.primary.lastEdit.ShouldDeepEqual(user2Msg.primary.lastEdit);
            }
            finally
            {
                // Cleanup
                await EditionHelpers.DeleteEdition(_client, newEdition);
            }
        }

        // TODO: finish updating test to use new request objects.
        /// <summary>
        ///     Test copying an edition
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateEdition()
        {
            // ARRANGE (with name)
            const string url = "/v1/editions/1";
            const string name = "interesting-long-test name @3#ח";
            var newScrollRequest = new EditionCopyDTO(name, null, null);

            //Act
            var newEd = new Post.V1_Editions_EditionId(1, newScrollRequest);
            var (response, msg, rt, lt) = await Request.Send(
                newEd,
                _client,
                StartConnectionAsync,
                auth: true,
                deterministic: false
            );
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(
                "application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString()
            );
            Assert.Equal(name, msg.name);
            Assert.Equal((uint)1215, msg.metrics.ppi);
            Assert.True(msg.id != 1);

            // ARRANGE (without name)
            newScrollRequest = new EditionCopyDTO(null, null, null);
            newEd = new Post.V1_Editions_EditionId(1, newScrollRequest);

            //Act
            (response, msg, rt, lt) = await Request.Send(
                newEd,
                _client,
                StartConnectionAsync,
                auth: true,
                deterministic: false
            );
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(
                "application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString()
            );
            Assert.True(msg.name != "");
            Assert.True(msg.id != 1);
        }

        // TODO: finish updating test to use new request objects.
        /// <summary>
        ///     Check if we can get editions when unauthenticated
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetAllEditionsUnauthenticated()
        {
            // ARRANGE
            const string url = "/v1/editions";

            // Act
            var (response, msg) =
                await Request.SendHttpRequestAsync<string, EditionListDTO>(_client, HttpMethod.Get, url, null);
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
            Assert.True(msg.editions.Count > 0);
        }

        // TODO: finish updating test to use new request objects.
        /// <summary>
        ///     Check if we can get editions when unauthenticated
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetOneEditionUnauthenticated()
        {
            // ARRANGE
            const string url = "/v1/editions/1";

            // Act
            var (response, msg) = await Request.SendHttpRequestAsync<string, EditionGroupDTO>(
                _client,
                HttpMethod.Get,
                url,
                null
            );
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
            Assert.NotNull(msg.primary.name);
        }

        // TODO: finish updating test to use new request objects.
        /// <summary>
        ///     Check that we get private editions when authenticated, and don't get them when unauthenticated.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetPrivateEditions()
        {
            // ARRANGE
            var bearerToken = await Request.GetJwtViaHttpAsync(_client);
            var editionId = await EditionHelpers.CreateCopyOfEdition(_client);
            const string url = "/v1/editions";

            // Act (get listings with authentication)
            var (response, msg) = await Request.SendHttpRequestAsync<string, EditionListDTO>(
                _client,
                HttpMethod.Get,
                url,
                null,
                bearerToken
            );
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString()
            );
            Assert.True(msg.editions.Count > 0);
            Assert.Contains(msg.editions.SelectMany(x => x), x => x.id == editionId);

            // Act (get listings without authentication)
            (response, msg) = await Request.SendHttpRequestAsync<string, EditionListDTO>(
                _client,
                HttpMethod.Get,
                url,
                null
            );
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString()
            );
            Assert.True(msg.editions.Count > 0);
            Assert.DoesNotContain(msg.editions.SelectMany(x => x), x => x.id == editionId);
        }

        // TODO: finish updating test to use new request objects.
        /// <summary>
        ///     Check if we protect against disallowed unauthenticated requests
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RefuseUnauthenticatedEditionWrite()
        {
            // ARRANGE
            const string url = "/v1/editions/1";
            var payload = new EditionUpdateRequestDTO("none", null, null);

            // Act (Create new scroll)
            var (response, _) = await Request.SendHttpRequestAsync<EditionUpdateRequestDTO, EditionListDTO>(
                _client,
                HttpMethod.Post,
                url,
                payload
            );

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // Act (change scroll name)
            (response, _) = await Request.SendHttpRequestAsync<EditionUpdateRequestDTO, EditionListDTO>(
                _client,
                HttpMethod.Put,
                url,
                payload
            );

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // TODO: finish updating test to use new request objects.
        /// <summary>
        ///     Test updating an edition
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateEdition()
        {
            // ARRANGE
            var bearerToken = await Request.GetJwtViaHttpAsync(_client);
            var editionId = await EditionHelpers.CreateCopyOfEdition(_client);
            var url = "/v1/editions/" + editionId;
            var (response, msg) = await Request.SendHttpRequestAsync<string, EditionGroupDTO>(
                _client,
                HttpMethod.Get,
                url,
                null,
                bearerToken
            );
            response.EnsureSuccessStatusCode();
            var oldName = msg.primary.name;
            const string name = "מגלה א";
            var metrics = new UpdateEditionManuscriptMetricsDTO()
            {
                width = 150,
                height = 50,
                xOrigin = -5,
                yOrigin = 0
            };
            var newScrollRequest = new EditionUpdateRequestDTO(name, null, null)
            {
                metrics = metrics
            };

            //Act
            var (response2, msg2) = await Request.SendHttpRequestAsync<EditionUpdateRequestDTO, EditionDTO>(
                _client,
                HttpMethod.Put,
                url,
                newScrollRequest,
                bearerToken
            );
            response2.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(
                "application/json; charset=utf-8",
                response2.Content.Headers.ContentType.ToString()
            );
            Assert.True(msg2.name != oldName);
            Assert.True(msg2.name == name);
            Assert.True(msg2.id == editionId);
            Assert.Equal(metrics.width, msg2.metrics.width);
            Assert.Equal(metrics.height, msg2.metrics.height);
            Assert.Equal(metrics.xOrigin, msg2.metrics.xOrigin);
            Assert.Equal(metrics.yOrigin, msg2.metrics.yOrigin);
        }

        /// <summary>
        /// This creates a share edition request. It uses a realtime listener to check that
        /// the user who was requested as an editor receives realtime notification of the request.
        /// </summary>
        /// <param name="editionId">The edition being shared</param>
        /// <param name="user1">The user initiating the share request</param>
        /// <param name="user2">The user being requested as an editor</param>
        /// <param name="permissionRequest">The permissions being requested for user2</param>
        /// <param name="shouldSucceed">Whether or not the request should succeed</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<(HttpResponseMessage requesterResponse, EditorInvitationDTO listenerResponse)> shareEdition(uint editionId,
            Request.UserAuthDetails user1, Request.UserAuthDetails user2, InviteEditorDTO permissionRequest = null, bool shouldSucceed = true)
        {
            if (permissionRequest == null)
                permissionRequest = new InviteEditorDTO
                {
                    email = user2.email,
                    mayLock = true,
                    mayWrite = true,
                    isAdmin = true
                };

            if (user2.email != permissionRequest.email)
                throw new Exception("user2 must be the same user as in the permissionRequest");

            var add1 = new Post.V1_Editions_EditionId_AddEditorRequest(editionId, permissionRequest);
            var (httpResponse, _, _, listenerResponse) = await Request.Send(
                add1,
                _client,
                StartConnectionAsync,
                auth: true,
                requestUser: user1,
                listenerUser: user2, // User 2 will listen on SignalR for the request
                requestRealtime: false,
                listenToEdition: false
            );

            if (shouldSucceed)
            {
                httpResponse.EnsureSuccessStatusCode();
                Assert.True(listenerResponse.mayRead);
                Assert.Equal(permissionRequest.mayWrite, listenerResponse.mayWrite);
                Assert.Equal(permissionRequest.mayLock, listenerResponse.mayLock);
                Assert.Equal(permissionRequest.isAdmin, listenerResponse.isAdmin);
                Assert.NotNull(listenerResponse.token);
            }

            return (httpResponse, listenerResponse);
        }

        /// <summary>
        /// A convenience method to confirm an editor invitation, this uses a realtime listener
        /// to confirm that the admin who made the request is notified of the acceptance.
        /// </summary>
        /// <param name="editionId"><The edition being shared/param>
        /// <param name="token">The token for the share invitation</param>
        /// <param name="editor">The user object of the editor who accepts the invitation</param>
        /// <param name="admin">The user object of the admin who made the share request</param>
        /// <param name="shouldSucceed">Whether or not the operation is expected to succeed</param>
        /// <returns></returns>
        private async Task<(
            HttpResponseMessage httpResponse,
            DetailedEditorRightsDTO httpMessage,
            DetailedEditorRightsDTO listenerMessage
            )> confirmEditor(uint editionId, Guid token, Request.UserAuthDetails editor, Request.UserAuthDetails admin,
                bool shouldSucceed = true)
        {
            var confirmRequest = new Post.V1_Editions_ConfirmEditorship_Token(token, editionId);
            var (httpConfirmResponse, shareConfirmMsg, _, listenerConfirmResponse) = await Request.Send(
                confirmRequest,
                _client,
                StartConnectionAsync,
                auth: true,
                requestUser: editor,
                listenerUser: admin, // User 1 will listen on the SignalR edition room for news of confirmation
                requestRealtime: false
            );

            if (shouldSucceed)
            {
                httpConfirmResponse.EnsureSuccessStatusCode();
                shareConfirmMsg.ShouldDeepEqual(listenerConfirmResponse);
            }

            return (httpConfirmResponse, shareConfirmMsg, listenerConfirmResponse);
        }
    }
}