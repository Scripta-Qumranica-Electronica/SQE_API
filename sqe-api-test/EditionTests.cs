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
using Xunit;

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

        [Fact]
        public async Task CanAdminShareEdition()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);
            try
            {
                var newPermissions = new CreateEditorRightsDTO()
                {
                    email = Request.DefaultUsers.User2.email,
                    mayLock = true,
                    mayWrite = true,
                    isAdmin = true
                };

                // Act
                var add1 = new Post.V1_Editions_EditionId_Editors(newEdition, newPermissions);
                var (_, shareMsg, _, _) = await Request.Send(
                    add1,
                    _client,
                    null,
                    auth: true
                );

                // Assert
                Assert.True(shareMsg.mayRead);
                Assert.True(shareMsg.mayWrite);
                Assert.True(shareMsg.mayLock);
                Assert.True(shareMsg.isAdmin);

                var get1 = new Get.V1_Editions_EditionId(newEdition);
                var (_, user1Msg, _, _) = await Request.Send(
                    get1,
                    _client,
                    null,
                    auth: true
                );

                var (_, user2Msg, _, _) = await Request.Send(
                    get1,
                    _client,
                    null,
                    auth: true,
                    user1: Request.DefaultUsers.User2
                );
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

        [Fact]
        public async Task CanChangeEditionSharePermissions()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);

            try
            {
                var newPermissions = new CreateEditorRightsDTO
                {
                    email = Request.DefaultUsers.User2.email,
                    mayLock = false,
                    mayWrite = false,
                    isAdmin = false
                };

                // Act
                var add1 = new Post.V1_Editions_EditionId_Editors(newEdition, newPermissions);
                var (_, shareMsg, _, _) = await Request.Send(
                    add1,
                    _client,
                    null,
                    auth: true
                );

                // Assert
                Assert.True(shareMsg.mayRead);
                Assert.False(shareMsg.mayWrite);
                Assert.False(shareMsg.mayLock);
                Assert.False(shareMsg.isAdmin);

                var get1 = new Get.V1_Editions_EditionId(newEdition);
                var (_, user2Msg, _, _) = await Request.Send(
                    get1,
                    _client,
                    null,
                    auth: true,
                    user1: Request.DefaultUsers.User2
                );
                Assert.False(user2Msg.primary.permission.mayWrite);
                Assert.False(user2Msg.primary.permission.isAdmin);

                // Act
                newPermissions.mayWrite = true;
                newPermissions.isAdmin = true;
                var add2 = new Put.V1_Editions_EditionId_Editors_EditorEmailId(newEdition, newPermissions.email, newPermissions);
                var (_, share2Msg, _, _) = await Request.Send(
                    add2,
                    _client,
                    null,
                    auth: true
                );

                // Assert
                Assert.True(share2Msg.mayRead);
                Assert.True(share2Msg.mayWrite);
                Assert.False(share2Msg.mayLock);
                Assert.True(share2Msg.isAdmin);

                var (_, user2Msg2, _, _) = await Request.Send(
                    get1,
                    _client,
                    null,
                    auth: true,
                    user1: Request.DefaultUsers.User2
                );
                Assert.True(user2Msg2.primary.permission.mayWrite);
                Assert.True(user2Msg2.primary.permission.isAdmin);

                // Act
                newPermissions.mayLock = true;
                var add3 = new Put.V1_Editions_EditionId_Editors_EditorEmailId(newEdition, newPermissions.email, newPermissions);
                var (_, share3Msg, _, _) = await Request.Send(
                    add3,
                    _client,
                    null,
                    auth: true,
                    user1: Request.DefaultUsers.User2
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

        // TODO: finish updating tests to use new request objects.
        [Fact]
        public async Task CanDefaultShareEdition()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);

            try
            {
                var newPermissions = new CreateEditorRightsDTO
                {
                    email = Request.DefaultUsers.User2.email
                };

                // Act
                var (shareResponse, shareMsg) = await Request.SendHttpRequestAsync<CreateEditorRightsDTO, CreateEditorRightsDTO>(
                    _client,
                    HttpMethod.Post,
                    _addEditionEditor.Replace("$EditionId", newEdition.ToString()),
                    newPermissions,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User1.email, password = Request.DefaultUsers.User1.password }
                    )
                );

                // Assert
                shareResponse.EnsureSuccessStatusCode();
                Assert.True(shareMsg.mayRead);
                Assert.False(shareMsg.mayWrite);
                Assert.False(shareMsg.mayLock);
                Assert.False(shareMsg.isAdmin);

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
            var bearerToken = await Request.GetJwtViaHttpAsync(_client);
            var editionId = await EditionHelpers.CreateCopyOfEdition(_client, name: "first edition");
            var secondEditionId = await EditionHelpers.CreateCopyOfEdition(_client, editionId, "second edition");

            Assert.True(secondEditionId != null);
        }

        // TODO: finish updating test to use new request objects.
        [Fact]
        public async Task CanLockableShareEdition()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);

            try
            {
                var newPermissions = new CreateEditorRightsDTO
                {
                    email = Request.DefaultUsers.User2.email,
                    mayLock = true
                };

                // Act
                var (shareResponse, shareMsg) = await Request.SendHttpRequestAsync<CreateEditorRightsDTO, CreateEditorRightsDTO>(
                    _client,
                    HttpMethod.Post,
                    _addEditionEditor.Replace("$EditionId", newEdition.ToString()),
                    newPermissions,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User1.email, password = Request.DefaultUsers.User1.password }
                    )
                );

                // Assert
                shareResponse.EnsureSuccessStatusCode();
                Assert.True(shareMsg.mayRead);
                Assert.False(shareMsg.mayWrite);
                Assert.True(shareMsg.mayLock);
                Assert.False(shareMsg.isAdmin);

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
        [Fact]
        public async Task CanNotAdminWithoutReadShareEdition()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);

            try
            {
                var newPermissions = new CreateEditorRightsDTO
                {
                    email = Request.DefaultUsers.User2.email,
                    mayRead = false,
                    mayLock = false,
                    mayWrite = false,
                    isAdmin = true
                };

                // Act
                var (shareResponse, shareMsg) = await Request.SendHttpRequestAsync<CreateEditorRightsDTO, CreateEditorRightsDTO>(
                    _client,
                    HttpMethod.Post,
                    _addEditionEditor.Replace("$EditionId", newEdition.ToString()),
                    newPermissions,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User1.email, password = Request.DefaultUsers.User1.password }
                    )
                );

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, shareResponse.StatusCode);
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

        // TODO: finish updating test to use new request objects.
        [Fact]
        public async Task CanNotWriteWithoutReadShareEdition()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);

            try
            {
                var newPermissions = new CreateEditorRightsDTO
                {
                    email = Request.DefaultUsers.User2.email,
                    mayRead = false,
                    mayLock = false,
                    mayWrite = true,
                    isAdmin = false
                };

                // Act
                var (shareResponse, shareMsg) = await Request.SendHttpRequestAsync<CreateEditorRightsDTO, CreateEditorRightsDTO>(
                    _client,
                    HttpMethod.Post,
                    _addEditionEditor.Replace("$EditionId", newEdition.ToString()),
                    newPermissions,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User1.email, password = Request.DefaultUsers.User1.password }
                    )
                );

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, shareResponse.StatusCode);
            }
            finally
            {
                // Cleanup
                await EditionHelpers.DeleteEdition(_client, newEdition);
            }
        }

        // TODO: finish updating test to use new request objects.
        [Fact]
        public async Task CanProperlyDeleteSharedEdition()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);
            var notDeleted = true;

            try
            {
                var newPermissions = new CreateEditorRightsDTO
                {
                    email = Request.DefaultUsers.User2.email,
                    mayLock = true,
                    mayWrite = true,
                    isAdmin = false
                };

                // Act
                var (shareResponse, shareMsg) = await Request.SendHttpRequestAsync<CreateEditorRightsDTO, CreateEditorRightsDTO>(
                    _client,
                    HttpMethod.Post,
                    _addEditionEditor.Replace("$EditionId", newEdition.ToString()),
                    newPermissions,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User1.email, password = Request.DefaultUsers.User1.password }
                    )
                );

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
                shareResponse.EnsureSuccessStatusCode();
                Assert.True(shareMsg.mayRead);
                Assert.True(shareMsg.mayWrite);
                Assert.True(shareMsg.mayLock);
                Assert.False(shareMsg.isAdmin);
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

        // TODO: finish updating test to use new request objects.
        [Fact]
        public async Task CanWriteShareEdition()
        {
            // Arrange
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client);
            try
            {
                var newPermissions = new CreateEditorRightsDTO
                {
                    email = Request.DefaultUsers.User2.email,
                    mayWrite = true
                };

                // Act
                var (shareResponse, shareMsg) = await Request.SendHttpRequestAsync<CreateEditorRightsDTO, CreateEditorRightsDTO>(
                    _client,
                    HttpMethod.Post,
                    _addEditionEditor.Replace("$EditionId", newEdition.ToString()),
                    newPermissions,
                    await Request.GetJwtViaHttpAsync(
                        _client,
                        new Request.UserAuthDetails
                        { email = Request.DefaultUsers.User1.email, password = Request.DefaultUsers.User1.password }
                    )
                );

                // Assert
                shareResponse.EnsureSuccessStatusCode();
                Assert.True(shareMsg.mayRead);
                Assert.True(shareMsg.mayWrite);
                Assert.False(shareMsg.mayLock);
                Assert.False(shareMsg.isAdmin);

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
            Assert.True(msg.name == name);
            Assert.True(msg.id != 1);

            // ARRANGE (without name)
            newScrollRequest = new EditionCopyDTO(null, null, null);

            //Act
            (response, msg) = await Request.SendHttpRequestAsync<EditionUpdateRequestDTO, EditionDTO>(
                _client,
                HttpMethod.Post,
                url,
                newScrollRequest,
                await Request.GetJwtViaHttpAsync(_client)
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
            var newScrollRequest = new EditionUpdateRequestDTO(name, null, null);

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
        }
    }
}