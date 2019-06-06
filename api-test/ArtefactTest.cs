using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using api_test.Helpers;
using Bogus;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Internal;
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
        private readonly Faker _faker = new Faker("en");
        private readonly DatabaseQuery _db;

        private const string version = "v1";
        private const string controller = "artefacts";

        public ArtefactTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();
        }
        
        #region Access artefacts

        /// <summary>
        /// Check that at least some edition has a valid artefact.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanAccessArtefacts()
        {
            // Act
            var artefacts = (await GetRandomEditionArtefacts()).artefacts;
            
            // Assert
            Assert.NotEmpty(artefacts);
            var artefact = artefacts[_faker.Random.Int(0, artefacts.Count - 1)];
            Assert.True(artefact.editionId > 0);
            Assert.True(artefact.id > 0);
            Assert.True(artefact.zOrder > -256 && artefact.zOrder < 256);
            Assert.NotNull(artefact.imagedObjectId);
            Assert.NotNull(artefact.side);
            Assert.NotNull(artefact.mask.mask);
            Assert.NotNull(artefact.mask.transformMatrix);
        }
        
        /// <summary>
        /// Ensure that a new artefact can be created.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanCreateArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetRandomEditionArtefacts()).artefacts; // Find edition with artefacts
            var newEdition = await HttpRequest.CreateNewEdition(_client, allArtefacts.First().editionId); // Clone it

            const string masterImageSQL = "SELECT sqe_image_id FROM SQE_image WHERE type = 0 ORDER BY RAND() LIMIT 1";
            var masterImageId = await _db.RunQuerySingleAsync<uint>(masterImageSQL, null);
            const string newArtefactShape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))";
            const string newTransform = null;
            var newName = _faker.Lorem.Sentence(5);
            var newArtefact = new CreateArtefactDTO()
            {
                mask = newArtefactShape,
                position = newTransform,
                name = newName,
                masterImageId = masterImageId
            };
            
            // Act
            var (response, writtenArtefact) = await HttpRequest.SendAsync<CreateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Post,
                $"/{version}/editions/{newEdition}/{controller}", newArtefact, await HttpRequest.GetJWTAsync(_client));
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(newEdition, writtenArtefact.editionId);
            Assert.Equal(newArtefact.mask, writtenArtefact.mask.mask);
            Assert.NotNull(writtenArtefact.mask.transformMatrix);
            Assert.Equal(newArtefact.name, writtenArtefact.name);
            
            // Cleanup
            await DeleteArtefact(newEdition, writtenArtefact.id);
        }
        
        /// <summary>
        /// Ensure that a existing artefact can be deleted.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanDeleteArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetRandomEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await HttpRequest.CreateNewEdition(_client, artefact.editionId); // Clone it
            
            // Act
            var (response, writtenArtefact) = await HttpRequest.SendAsync<string, string>(_client, HttpMethod.Delete,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}", null, 
                await HttpRequest.GetJWTAsync(_client));
            
            // Assert
            response.EnsureSuccessStatusCode();
                // Ensure successful nocontent status
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode); 
                // Double check that it is really gone
            var (delResponse, _) = await HttpRequest.SendAsync<string, string>(_client, HttpMethod.Get,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}", null, 
                await HttpRequest.GetJWTAsync(_client));
            Assert.Equal(HttpStatusCode.NotFound, delResponse.StatusCode);
        }
        #endregion Access artefacts
        
        
        #region Helpers

        /// <summary>
        /// Searches randomly for an edition with artefacts and returns the artefacts.
        /// </summary>
        /// <param name="userId">Id of the user whose editions should be randomly selected.</param>
        /// <param name="jwt">A JWT can be added the request to access private editions.</param>
        /// <returns></returns>
        private async Task<ArtefactListDTO> GetRandomEditionArtefacts(uint userId = 1, string jwt = null)
        {
            var artefactResponse = new ArtefactListDTO();
            const string sql = @"
SELECT edition_id 
FROM edition 
JOIN edition_editor USING(edition_id)
WHERE user_id = @UserId";
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            var allUserEditions = await _db.RunQueryAsync<uint>(sql, parameters);

            var r = new Random();
            var response = new HttpResponseMessage();
            foreach (var edition in allUserEditions.OrderBy(x => r.Next()))
            {
                var url = $"/{version}/editions/{edition}/{controller}?optional=masks";
                (response, artefactResponse) = await HttpRequest.SendAsync<string, ArtefactListDTO>(_client,
                    HttpMethod.Get, url, null, jwt);
                response.EnsureSuccessStatusCode();
                if (artefactResponse.artefacts.Any())
                    break;
            }

            return artefactResponse;
        }

        private async Task DeleteArtefact(uint editionId, uint ArtefactId)
        {
            var (response, _) = await HttpRequest.SendAsync<string, string>(_client, HttpMethod.Delete,
                $"/{version}/editions/{editionId}/{controller}/{ArtefactId}", null, 
                await HttpRequest.GetJWTAsync(_client));
            response.EnsureSuccessStatusCode();
        }
        #endregion Helpers
    }
}