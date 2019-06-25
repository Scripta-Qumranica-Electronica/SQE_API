using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bogus;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.ApiTest.Helpers;
using SQE.SqeHttpApi.Server;
using SQE.SqeHttpApi.Server.DTOs;
using Xunit;

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
        
        public EditionTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();
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
        
        
        
        #region Helpers
        
        
        /// <summary>
        /// Searches randomly for an edition and returns it.
        /// </summary>
        /// <param name="userId">Id of the user whose editions should be randomly selected.</param>
        /// <param name="jwt">A JWT can be added the request to access private editions.</param>
        /// <returns></returns>
        private async Task<EditionListDTO> GetRandomEdition(uint userId = 1, string jwt = null)
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
            var (response, editionResponse) = await HttpRequest.SendAsync<string, EditionListDTO>(_client,
                HttpMethod.Get, url, null, jwt);
            response.EnsureSuccessStatusCode();

            return editionResponse;
        }
        #endregion Helpers
    }
}