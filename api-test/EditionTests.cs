using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using api_test.Helpers;
using SQE.SqeHttpApi.Server.DTOs;
using Xunit;

namespace api_test
{
    /// <summary>
    /// This test suite tests all the current endpoints in the EditionController
    /// </summary>
    public class EditionTests
    {
        private readonly WebAppFactory _factory;
        public EditionTests()
        {
            _factory = new WebAppFactory();
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
            var (response, msg) = await HttpRequest.SendAsync<string, EditionListDTO>(_factory.Client, HttpMethod.Get, url, null);
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
                _factory.Client, 
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
            var payload = new EditionUpdateRequestDTO(){name = "none"};
            
            // Act (Create new scroll)
            var (response, _) = await HttpRequest.SendAsync<EditionUpdateRequestDTO, EditionListDTO>(
                _factory.Client, 
                HttpMethod.Post, 
                url, 
                payload);
            
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            
            // Act (change scroll name)
            (response, _) = await HttpRequest.SendAsync<EditionUpdateRequestDTO, EditionListDTO>(
                _factory.Client, 
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
            var newScrollRequest = new EditionUpdateRequestDTO() {name = name};
            
            //Act
            var (response, msg) = await HttpRequest.SendAsync<EditionUpdateRequestDTO, EditionDTO>(
                _factory.Client, 
                HttpMethod.Post, 
                url, 
                newScrollRequest,
                await HttpRequest.GetJWTAsync(_factory.Client));
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
            Assert.True(msg.name == name);
            Assert.True(msg.id != 1);
            
            // ARRANGE (without name)
            newScrollRequest = new EditionUpdateRequestDTO() {name = ""};
            
            //Act
            (response, msg) = await HttpRequest.SendAsync<EditionUpdateRequestDTO, EditionDTO>(
                _factory.Client, 
                HttpMethod.Post, 
                url, 
                newScrollRequest,
                await HttpRequest.GetJWTAsync(_factory.Client));
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
            var bearerToken = await HttpRequest.GetJWTAsync(_factory.Client);
            var editionId = await HttpRequest.CreateNewEdition(_factory.Client);
            var url = "/v1/editions/" + editionId;
            var (response, msg) = await HttpRequest.SendAsync<string, EditionGroupDTO>(
                _factory.Client, 
                HttpMethod.Get, 
                url, 
                null,
                bearerToken);
            response.EnsureSuccessStatusCode();
            var oldName = msg.primary.name;
            const string name = "מגלה א";
            var newScrollRequest = new EditionUpdateRequestDTO {name = name};
            
            //Act
            var (response2, msg2) = await HttpRequest.SendAsync<EditionUpdateRequestDTO, EditionDTO>(
                _factory.Client, 
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
            var bearerToken = await HttpRequest.GetJWTAsync(_factory.Client);
            var editionId = await HttpRequest.CreateNewEdition(_factory.Client);
            const string url = "/v1/editions";
            
            // Act (get listings with authentication)
            var (response, msg) = await HttpRequest.SendAsync<string, EditionListDTO>(
                _factory.Client, 
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
                _factory.Client, 
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
    }
}