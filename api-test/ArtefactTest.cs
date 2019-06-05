using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using api_test.Helpers;
using Bogus;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.SqeHttpApi.Server;
using SQE.SqeHttpApi.Server.DTOs;
using Xunit;

namespace api_test
{
    /// <summary>
    /// This test suite tests all the current endpoints in the ArtefactController
    /// </summary>
    public class ArtefactTests : WebControllerTest
    {
        private readonly DatabaseQuery _db;
        private readonly Faker _faker = new Faker("en");

        private const string version = "v1";
        private const string controller = "artefacts";
        
        public ArtefactTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();
        }
        

        /// <summary>
        /// Check if we can get artefacts when unauthenticated
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
        
        #region Helpers

        private async Task<ArtefactListDTO> GetRandomEditionWithArtefacts(string jwt = null)
        {
            ArtefactListDTO artefactResponse = new ArtefactListDTO();
            var count = 0;
            while (!artefactResponse.artefacts.Any() || count > 2000) // We have a cutoff, just in case
            {
                var url = $"/{version}/edition/{_faker.Random.Int(1, 1000)}/{controller}";
                (_, artefactResponse) = await HttpRequest.SendAsync<string, ArtefactListDTO>(_client,
                    HttpMethod.Get, url, null, jwt);
                count++;
            }

            return artefactResponse;
        }
        #endregion Helpers
    }
}