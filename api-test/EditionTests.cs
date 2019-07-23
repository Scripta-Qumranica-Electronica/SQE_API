using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bogus;
using Dapper;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.CodeAnalysis.Operations;
using SQE.ApiTest.Helpers;
using SQE.SqeHttpApi.Server;
using SQE.SqeHttpApi.Server.DTOs;
using Xunit;
using HttpRequest = SQE.ApiTest.Helpers.HttpRequest;

namespace SQE.ApiTest
{
    /// <summary>
    /// This test suite tests all the current endpoints in the EditionController
    /// </summary>
    public class EditionTests : WebControllerTest
    {
        private readonly Faker _faker = new Faker("en");
        private readonly DatabaseQuery _db;
        private const string version = "v1";
        private const string controller = "editions";
        private readonly string _addEditionEditor;
        
        public EditionTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();
            _addEditionEditor = $"/{version}/{controller}/$EditionId/editors";
        }

        /// <summary>
        /// Check if we can get editions when unauthenticated
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetAllEditionsUnauthenticated()
        {
            // ARRANGE
            const string url = "/v1/editions";
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, EditionListDTO>(_client, HttpMethod.Get, url, null);
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json; charset=utf-8",response.Content.Headers.ContentType.ToString());
            Assert.True(msg.editions.Count > 0);
        }
        
        /// <summary>
        /// Check if we can get editions when unauthenticated
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetOneEditionUnauthenticated()
        {
            // ARRANGE
            const string url = "/v1/editions/1";
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(
                _client, 
                HttpMethod.Get, 
                url, 
                null);
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json; charset=utf-8",response.Content.Headers.ContentType.ToString());
            Assert.NotNull(msg.primary.name);
        }
        
        /// <summary>
        /// Check if we protect against disallowed unauthenticated requests
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RefuseUnauthenticatedEditionWrite()
        {
            // ARRANGE
            const string url = "/v1/editions/1";
            var payload = new EditionUpdateRequestDTO("none", null, null);
            
            // Act (Create new scroll)
            var (response, _) = await HttpRequest.SendAsync<EditionUpdateRequestDTO, EditionListDTO>(
                _client, 
                HttpMethod.Post, 
                url, 
                payload);
            
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            // Act (change scroll name)
            (response, _) = await HttpRequest.SendAsync<EditionUpdateRequestDTO, EditionListDTO>(
                _client, 
                HttpMethod.Put, 
                url, 
                payload);
            
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Test copying an edition
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateEdition()
        {
            // ARRANGE (with name)
            const string url = "/v1/editions/1";
            const string name = "interesting-long-test name @3#ח";
            var newScrollRequest = new EditionUpdateRequestDTO(name, null, null);
            
            //Act
            var (response, msg) = await HttpRequest.SendAsync<EditionUpdateRequestDTO, EditionDTO>(
                _client, 
                HttpMethod.Post, 
                url, 
                newScrollRequest,
                await HttpRequest.GetJWTAsync(_client));
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
            Assert.True(msg.name == name);
            Assert.True(msg.id != 1);
            
            // ARRANGE (without name)
            newScrollRequest = new EditionUpdateRequestDTO("", null, null);
            
            //Act
            (response, msg) = await HttpRequest.SendAsync<EditionUpdateRequestDTO, EditionDTO>(
                _client, 
                HttpMethod.Post, 
                url, 
                newScrollRequest,
                await HttpRequest.GetJWTAsync(_client));
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
            Assert.True(msg.name != "");
            Assert.True(msg.id != 1);
        }
        
        /// <summary>
        /// Test updating an edition
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateEdition()
        {
            // ARRANGE
            var bearerToken = await HttpRequest.GetJWTAsync(_client);
            var editionId = await HttpRequest.CreateNewEdition(_client);
            var url = "/v1/editions/" + editionId;
            var (response, msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(
                _client, 
                HttpMethod.Get, 
                url, 
                null,
                bearerToken);
            response.EnsureSuccessStatusCode();
            var oldName = msg.primary.name;
            const string name = "מגלה א";
            var newScrollRequest = new EditionUpdateRequestDTO(name, null, null);
            
            //Act
            var (response2, msg2) = await HttpRequest.SendAsync<EditionUpdateRequestDTO, EditionDTO>(
                _client, 
                HttpMethod.Put, 
                url, 
                newScrollRequest,
                bearerToken);
            response2.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal("application/json; charset=utf-8",
                response2.Content.Headers.ContentType.ToString());
            Assert.True(msg2.name != oldName);
            Assert.True(msg2.name == name);
            Assert.True(msg2.id == editionId);
        }
        
        /// <summary>
        /// Check that we get private editions when authenticated, and don't get them when unauthenticated.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetPrivateEditions()
        {
            // ARRANGE
            var bearerToken = await HttpRequest.GetJWTAsync(_client);
            var editionId = await HttpRequest.CreateNewEdition(_client);
            const string url = "/v1/editions";
            
            // Act (get listings with authentication)
            var (response, msg) = await HttpRequest.SendAsync<string, EditionListDTO>(
                _client, 
                HttpMethod.Get, 
                url, 
                null, 
                bearerToken);
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
            Assert.True(msg.editions.Count > 0);
            Assert.Contains(msg.editions.SelectMany(x => x), x => x.id == editionId);
            
            // Act (get listings without authentication)
            (response, msg) = await HttpRequest.SendAsync<string, EditionListDTO>(
                _client, 
                HttpMethod.Get, 
                url, 
                null);
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
            Assert.True(msg.editions.Count > 0);
            Assert.DoesNotContain(msg.editions.SelectMany(x => x), x => x.id == editionId);
        }

        // TODO: write the rest of the tests.
        [Fact]
        public async Task CanDeleteEditionAsAdmin()
        {
            // Arrange
            var editionId = await HttpRequest.CreateNewEdition(_client);
            const string url = "/v1/editions";
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, string>(
                _client, 
                HttpMethod.Delete, 
                url + "/" + editionId.ToString(), 
                null, 
                await HttpRequest.GetJWTAsync(_client));
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var (editionResponse, editionMsg) = await HttpRequest.SendAsync<string, EditionListDTO>(
                _client, 
                HttpMethod.Get, 
                url, 
                null, 
                await HttpRequest.GetJWTAsync(_client));
            editionResponse.EnsureSuccessStatusCode();
            var editionMatch = editionMsg.editions.SelectMany(x => x).Where(x => x.id == editionId);
            Assert.Empty(editionMatch);

            await HttpRequest.DeleteEdition(_client, editionId, true);
        }
        
        // TODO: need sharing capability before this can be tested properly
        [Fact]
        public async Task CanNotDeleteEditionWhenAnonymous()
        {
            // Arrange
            var editionId = await HttpRequest.CreateNewEdition(_client);
            const string url = "/v1/editions";
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, string>(
                _client, 
                HttpMethod.Delete, 
                url + "/" + editionId.ToString(), 
                null, 
                null);
            
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var (editionResponse, editionMsg) = await HttpRequest.SendAsync<string, EditionListDTO>(
                _client, 
                HttpMethod.Get, 
                url, 
                null, 
                await HttpRequest.GetJWTAsync(_client));
            editionResponse.EnsureSuccessStatusCode();
            var editionMatch = editionMsg.editions.SelectMany(x => x).Where(x => x.id == editionId);
            Assert.Single(editionMatch);

            await HttpRequest.DeleteEdition(_client, editionId, true);
        }

        #region Edition Editor Permissions
        
        #region Should Succeed
        
        [Fact]
        public async Task CanDefaultShareEdition()
        {
        
            // Arrange
            const string user1Pwd = "pwd1";
            var user1 = await HttpRequest.CreateRandomUser(_client, user1Pwd);
            const string user2Pwd = "pwd2";
            var user2 = await HttpRequest.CreateRandomUser(_client, user2Pwd);
            var newEdition = await HttpRequest.CreateNewEdition(_client, username: user1.email, pwd: user1Pwd);
            var newPermissions = new EditorRightsDTO()
            {
                email = user2.email,
            };
            
            // Act
            var (shareResponse, shareMsg) = await HttpRequest.SendAsync<EditorRightsDTO, EditorRightsDTO>(_client,
                HttpMethod.Post,
                _addEditionEditor.Replace("$EditionId", newEdition.ToString()), newPermissions,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));

            // Assert
            shareResponse.EnsureSuccessStatusCode();
            Assert.True(shareMsg.mayRead);
            Assert.False(shareMsg.mayWrite);
            Assert.False(shareMsg.mayLock);
            Assert.False(shareMsg.isAdmin);
            
            var (user1Resp, user1Msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));
            user1Resp.EnsureSuccessStatusCode();
            
            var (user2Resp, user2Msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user2.email, user2Pwd));
            user2Resp.EnsureSuccessStatusCode();
            Assert.Equal(user1Msg.primary.id, user2Msg.primary.id);
            Assert.Equal(user1Msg.primary.copyright, user2Msg.primary.copyright);
            Assert.Equal(user1Msg.primary.isPublic, user2Msg.primary.isPublic);
            Assert.Equal(user1Msg.primary.locked, user2Msg.primary.locked);
            Assert.Equal(user1Msg.primary.name, user2Msg.primary.name);
            Assert.Equal(user1Msg.primary.thumbnailUrl, user2Msg.primary.thumbnailUrl);
            user1Msg.primary.lastEdit.ShouldDeepEqual(user2Msg.primary.lastEdit);
        }
        
        [Fact]
        public async Task CanWriteShareEdition()
        {
        
            // Arrange
            const string user1Pwd = "pwd1";
            var user1 = await HttpRequest.CreateRandomUser(_client, user1Pwd);
            const string user2Pwd = "pwd2";
            var user2 = await HttpRequest.CreateRandomUser(_client, user2Pwd);
            var newEdition = await HttpRequest.CreateNewEdition(_client, username: user1.email, pwd: user1Pwd);
            var newPermissions = new EditorRightsDTO()
            {
                email = user2.email,
                mayWrite = true
            };
            
            // Act
            var (shareResponse, shareMsg) = await HttpRequest.SendAsync<EditorRightsDTO, EditorRightsDTO>(_client,
                HttpMethod.Post,
                _addEditionEditor.Replace("$EditionId", newEdition.ToString()), newPermissions,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));

            // Assert
            shareResponse.EnsureSuccessStatusCode();
            Assert.True(shareMsg.mayRead);
            Assert.True(shareMsg.mayWrite);
            Assert.False(shareMsg.mayLock);
            Assert.False(shareMsg.isAdmin);
            
            var (user1Resp, user1Msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));
            user1Resp.EnsureSuccessStatusCode();
            
            var (user2Resp, user2Msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user2.email, user2Pwd));
            user2Resp.EnsureSuccessStatusCode();
            Assert.Equal(user1Msg.primary.id, user2Msg.primary.id);
            Assert.Equal(user1Msg.primary.copyright, user2Msg.primary.copyright);
            Assert.Equal(user1Msg.primary.isPublic, user2Msg.primary.isPublic);
            Assert.Equal(user1Msg.primary.locked, user2Msg.primary.locked);
            Assert.Equal(user1Msg.primary.name, user2Msg.primary.name);
            Assert.Equal(user1Msg.primary.thumbnailUrl, user2Msg.primary.thumbnailUrl);
            user1Msg.primary.lastEdit.ShouldDeepEqual(user2Msg.primary.lastEdit);
        }
        
        [Fact]
        public async Task CanLockableShareEdition()
        {
        
            // Arrange
            const string user1Pwd = "pwd1";
            var user1 = await HttpRequest.CreateRandomUser(_client, user1Pwd);
            const string user2Pwd = "pwd2";
            var user2 = await HttpRequest.CreateRandomUser(_client, user2Pwd);
            var newEdition = await HttpRequest.CreateNewEdition(_client, username: user1.email, pwd: user1Pwd);
            var newPermissions = new EditorRightsDTO()
            {
                email = user2.email,
                mayLock = true
            };
            
            // Act
            var (shareResponse, shareMsg) = await HttpRequest.SendAsync<EditorRightsDTO, EditorRightsDTO>(_client,
                HttpMethod.Post,
                _addEditionEditor.Replace("$EditionId", newEdition.ToString()), newPermissions,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));

            // Assert
            shareResponse.EnsureSuccessStatusCode();
            Assert.True(shareMsg.mayRead);
            Assert.False(shareMsg.mayWrite);
            Assert.True(shareMsg.mayLock);
            Assert.False(shareMsg.isAdmin);
            
            var (user1Resp, user1Msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));
            user1Resp.EnsureSuccessStatusCode();
            
            var (user2Resp, user2Msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user2.email, user2Pwd));
            user2Resp.EnsureSuccessStatusCode();
            Assert.Equal(user1Msg.primary.id, user2Msg.primary.id);
            Assert.Equal(user1Msg.primary.copyright, user2Msg.primary.copyright);
            Assert.Equal(user1Msg.primary.isPublic, user2Msg.primary.isPublic);
            Assert.Equal(user1Msg.primary.locked, user2Msg.primary.locked);
            Assert.Equal(user1Msg.primary.name, user2Msg.primary.name);
            Assert.Equal(user1Msg.primary.thumbnailUrl, user2Msg.primary.thumbnailUrl);
            user1Msg.primary.lastEdit.ShouldDeepEqual(user2Msg.primary.lastEdit);
        }
        
        [Fact]
        public async Task CanAdminShareEdition()
        {
            // Arrange
            const string user1Pwd = "pwd1";
            var user1 = await HttpRequest.CreateRandomUser(_client, user1Pwd);
            const string user2Pwd = "pwd2";
            var user2 = await HttpRequest.CreateRandomUser(_client, user2Pwd);
            var newEdition = await HttpRequest.CreateNewEdition(_client, username: user1.email, pwd: user1Pwd);
            var newPermissions = new EditorRightsDTO()
            {
                email = user2.email,
                mayLock = true,
                mayWrite = true,
                isAdmin = true
            };
            
            // Act
            var (shareResponse, shareMsg) = await HttpRequest.SendAsync<EditorRightsDTO, EditorRightsDTO>(_client,
                HttpMethod.Post,
                _addEditionEditor.Replace("$EditionId", newEdition.ToString()), newPermissions,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));

            // Assert
            shareResponse.EnsureSuccessStatusCode();
            Assert.True(shareMsg.mayRead);
            Assert.True(shareMsg.mayWrite);
            Assert.True(shareMsg.mayLock);
            Assert.True(shareMsg.isAdmin);
            
            var (user1Resp, user1Msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));
            user1Resp.EnsureSuccessStatusCode();
            
            var (user2Resp, user2Msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user2.email, user2Pwd));
            user2Resp.EnsureSuccessStatusCode();
            Assert.Equal(user1Msg.primary.id, user2Msg.primary.id);
            Assert.Equal(user1Msg.primary.copyright, user2Msg.primary.copyright);
            Assert.Equal(user1Msg.primary.isPublic, user2Msg.primary.isPublic);
            Assert.Equal(user1Msg.primary.locked, user2Msg.primary.locked);
            Assert.Equal(user1Msg.primary.name, user2Msg.primary.name);
            Assert.Equal(user1Msg.primary.thumbnailUrl, user2Msg.primary.thumbnailUrl);
            user1Msg.primary.lastEdit.ShouldDeepEqual(user2Msg.primary.lastEdit);
        }
        
        [Fact]
        public async Task CanChangeEditionSharePermissions()
        {
            // Arrange
            const string user1Pwd = "pwd1";
            var user1 = await HttpRequest.CreateRandomUser(_client, user1Pwd);
            const string user2Pwd = "pwd2";
            var user2 = await HttpRequest.CreateRandomUser(_client, user2Pwd);
            var newEdition = await HttpRequest.CreateNewEdition(_client, username: user1.email, pwd: user1Pwd);
            var newPermissions = new EditorRightsDTO()
            {
                email = user2.email,
                mayLock = false,
                mayWrite = false,
                isAdmin = false
            };
            
            // Act
            var (shareResponse, shareMsg) = await HttpRequest.SendAsync<EditorRightsDTO, EditorRightsDTO>(_client,
                HttpMethod.Post,
                _addEditionEditor.Replace("$EditionId", newEdition.ToString()), newPermissions,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));

            // Assert
            shareResponse.EnsureSuccessStatusCode();
            Assert.True(shareMsg.mayRead);
            Assert.False(shareMsg.mayWrite);
            Assert.False(shareMsg.mayLock);
            Assert.False(shareMsg.isAdmin);
            
            var (user2Resp, user2Msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user2.email, user2Pwd));
            user2Resp.EnsureSuccessStatusCode();
            Assert.False(user2Msg.primary.permission.mayWrite);
            Assert.False(user2Msg.primary.permission.isAdmin);
            
            // Act
            newPermissions.mayWrite = true;
            newPermissions.isAdmin = true;
            var (share2Response, share2Msg) = await HttpRequest.SendAsync<EditorRightsDTO, EditorRightsDTO>(_client,
                HttpMethod.Put,
                _addEditionEditor.Replace("$EditionId", newEdition.ToString()), newPermissions,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));
            
            // Assert
            share2Response.EnsureSuccessStatusCode();
            Assert.True(share2Msg.mayRead);
            Assert.True(share2Msg.mayWrite);
            Assert.False(share2Msg.mayLock);
            Assert.True(share2Msg.isAdmin);
            
            var (user2Resp2, user2Msg2) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user2.email, user2Pwd));
            user2Resp2.EnsureSuccessStatusCode();
            Assert.True(user2Msg2.primary.permission.mayWrite);
            Assert.True(user2Msg2.primary.permission.isAdmin);
            
            // Act
            newPermissions.mayLock = true;
            var (share3Response, share3Msg) = await HttpRequest.SendAsync<EditorRightsDTO, EditorRightsDTO>(_client,
                HttpMethod.Put,
                _addEditionEditor.Replace("$EditionId", newEdition.ToString()), newPermissions,
                await HttpRequest.GetJWTAsync(_client, user2.email, user2Pwd));
            
            // Assert
            share3Response.EnsureSuccessStatusCode();
            Assert.True(share3Msg.mayRead);
            Assert.True(share3Msg.mayWrite);
            Assert.True(share3Msg.mayLock);
            Assert.True(share3Msg.isAdmin);
        }
        
        [Fact]
        public async Task CanProperlyDeleteSharedEdition()
        {
            // Arrange
            const string user1Pwd = "pwd1";
            var user1 = await HttpRequest.CreateRandomUser(_client, user1Pwd);
            const string user2Pwd = "pwd2";
            var user2 = await HttpRequest.CreateRandomUser(_client, user2Pwd);
            var newEdition = await HttpRequest.CreateNewEdition(_client, username: user1.email, pwd: user1Pwd);
            var newPermissions = new EditorRightsDTO()
            {
                email = user2.email,
                mayLock = true,
                mayWrite = true,
                isAdmin = false
            };
            
            // Act
            var (shareResponse, shareMsg) = await HttpRequest.SendAsync<EditorRightsDTO, EditorRightsDTO>(_client,
                HttpMethod.Post,
                _addEditionEditor.Replace("$EditionId", newEdition.ToString()), newPermissions,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));
            
            var (deleteResponse, deleteMsg) = await HttpRequest.SendAsync<string, string>(
                _client, 
                HttpMethod.Delete, 
                "/v1/editions/" + newEdition.ToString(), 
                null, 
                await HttpRequest.GetJWTAsync(_client, user2.email, user2Pwd));

            // Assert
            shareResponse.EnsureSuccessStatusCode();
            Assert.True(shareMsg.mayRead);
            Assert.True(shareMsg.mayWrite);
            Assert.True(shareMsg.mayLock);
            Assert.False(shareMsg.isAdmin);
            deleteResponse.EnsureSuccessStatusCode();
            
            var (user1Resp, user1Msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));
            user1Resp.EnsureSuccessStatusCode();

            var (user2Resp, user2Msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user2.email, user2Pwd));
            user2Resp.EnsureSuccessStatusCode();
            Assert.NotNull(user1Msg);
            Assert.Null(user2Msg);
            
            // Act (final delete)
            var (delete2Response, delete2Msg) = await HttpRequest.SendAsync<string, string>(
                _client, 
                HttpMethod.Delete, 
                "/v1/editions/" + newEdition.ToString(), 
                null, 
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));
            
            // Assert
            delete2Response.EnsureSuccessStatusCode();
            var (user1Resp2, user1Msg2) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get,
                $"/v1/editions/{newEdition}", null,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));
            user1Resp2.EnsureSuccessStatusCode();
            Assert.Null(user1Msg2);
            
            //Todo: maybe run a database check here to ensure that all references to newEdition have been removed from all *_owner tables
        }
        
        #endregion Should Succeed
        
        #region Should Fail
        
        [Fact]
        public async Task CanNotWriteWithoutReadShareEdition()
        {
        
            // Arrange
            const string user1Pwd = "pwd1";
            var user1 = await HttpRequest.CreateRandomUser(_client, user1Pwd);
            const string user2Pwd = "pwd2";
            var user2 = await HttpRequest.CreateRandomUser(_client, user2Pwd);
            var newEdition = await HttpRequest.CreateNewEdition(_client, username: user1.email, pwd: user1Pwd);
            var newPermissions = new EditorRightsDTO()
            {
                email = user2.email,
                mayRead = false,
                mayLock = false,
                mayWrite = true,
                isAdmin = false
            };
            
            // Act
            var (shareResponse, shareMsg) = await HttpRequest.SendAsync<EditorRightsDTO, EditorRightsDTO>(_client,
                HttpMethod.Post,
                _addEditionEditor.Replace("$EditionId", newEdition.ToString()), newPermissions,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, shareResponse.StatusCode);
        }
        
        [Fact]
        public async Task CanNotAdminWithoutReadShareEdition()
        {
        
            // Arrange
            const string user1Pwd = "pwd1";
            var user1 = await HttpRequest.CreateRandomUser(_client, user1Pwd);
            const string user2Pwd = "pwd2";
            var user2 = await HttpRequest.CreateRandomUser(_client, user2Pwd);
            var newEdition = await HttpRequest.CreateNewEdition(_client, username: user1.email, pwd: user1Pwd);
            var newPermissions = new EditorRightsDTO()
            {
                email = user2.email,
                mayRead = false,
                mayLock = false,
                mayWrite = false,
                isAdmin = true
            };
            
            // Act
            var (shareResponse, shareMsg) = await HttpRequest.SendAsync<EditorRightsDTO, EditorRightsDTO>(_client,
                HttpMethod.Post,
                _addEditionEditor.Replace("$EditionId", newEdition.ToString()), newPermissions,
                await HttpRequest.GetJWTAsync(_client, user1.email, user1Pwd));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, shareResponse.StatusCode);
        }
        
        #endregion Should Fail
        
        #endregion Edition Editor Permissions
        
        #region Helpers
        
        
        /// <summary>
        /// Searches randomly for an edition and returns it.
        /// </summary>
        /// <param name="userId">Id of the user whose editions should be randomly selected.</param>
        /// <param name="jwt">A JWT can be added the request to access private editions.</param>
        /// <returns></returns>
        public async Task<EditionGroupDTO> GetRandomEdition(uint userId = 1, string jwt = null)
        {
            const string sql = @"
SELECT edition_id 
FROM edition 
JOIN edition_editor USING(edition_id)
WHERE user_id = @UserId";
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            var allUserEditions = (await _db.RunQueryAsync<uint>(sql, parameters)).ToList();
            
            var randomEdition = allUserEditions[_faker.Random.Int(0, allUserEditions.Count - 1)];
            var url = $"/{version}/editions/{randomEdition}";
            var (response, editionResponse) = await HttpRequest.SendAsync<string, EditionGroupDTO>(_client,
                HttpMethod.Get, url, null, jwt);
            response.EnsureSuccessStatusCode();

            return editionResponse;
        }
        #endregion Helpers
    }
}