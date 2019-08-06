using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bogus;
using Dapper;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.ApiTest.Helpers;
using SQE.SqeApi.Server;
using SQE.SqeApi.Server.DTOs;
using Xunit;

// TODO: It would be nice to be able to generate random polygons for these testing purposes.
namespace SQE.ApiTest
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
        
        #region Access artefacts (should succeed)

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
        }
        
        /// <summary>
        /// Ensure that a new artefact can be created (and then deleted).
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanCreateArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetRandomEditionArtefacts()).artefacts; // Find edition with artefacts
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, allArtefacts.First().editionId); // Clone it

            const string masterImageSQL = "SELECT sqe_image_id FROM SQE_image WHERE type = 0 ORDER BY RAND() LIMIT 1";
            var masterImageId = await _db.RunQuerySingleAsync<uint>(masterImageSQL, null);
            const string newArtefactShape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))";
            var newTransform = RandomPosition();
            var newName = _faker.Lorem.Sentence(5);
            var newArtefact = new CreateArtefactDTO()
            {
                mask = newArtefactShape,
                position = null,
                name = newName,
                masterImageId = masterImageId
            };
            var jwt = await HttpRequest.GetJWTAsync(_client);
            
            // Get a SignalR connection
            var signalr = await StartConnectionAsync(jwt);
            signalr.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0,5) * 1000);
                await signalr.StartAsync();
            };
            
            // Act
            // Subscribe to the edition
            await signalr.InvokeAsync("SubscribeToEdition", newEdition);
            // Listen for calls to "createArtefact"
            signalr.On<ArtefactDTO>("createArtefact", (receivedArtefact) =>
            {
                // These tests work now, but since this is run over three mutation requests, there is no 
                // guarantee that it will always be checking the returned artefact against the proper
                // instance of `newArtefact`. Rewrite tests with signalr to check only one mutation,
                // or use a different method to verify that the expected notification has indeed been
                // received.
                Assert.NotNull(receivedArtefact);
                Assert.Equal(newArtefact.name, receivedArtefact.name);
                Assert.Equal(newArtefact.position, receivedArtefact.mask.transformMatrix);
                Assert.Equal(newArtefact.mask, receivedArtefact.mask.mask);
            });
            var (response, writtenArtefact) = await HttpRequest.SendAsync<CreateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Post,
                $"/{version}/editions/{newEdition}/{controller}", newArtefact, jwt);
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(newEdition, writtenArtefact.editionId);
            Assert.Equal(newArtefact.mask, writtenArtefact.mask.mask);
            Assert.Null(writtenArtefact.mask.transformMatrix);
            Assert.Equal(newArtefact.name, writtenArtefact.name);
            
            // Cleanup
            await DeleteArtefact(newEdition, writtenArtefact.id);
            
            // Arrange
            newName = null;
            
            newArtefact = new CreateArtefactDTO()
            {
                mask = newArtefactShape,
                position = newTransform,
                name = newName,
                masterImageId = masterImageId
            };
            
            // Act
            (response, writtenArtefact) = await HttpRequest.SendAsync<CreateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Post,
                $"/{version}/editions/{newEdition}/{controller}", newArtefact, jwt);
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(newEdition, writtenArtefact.editionId);
            Assert.Equal(newArtefact.mask, writtenArtefact.mask.mask);
            Assert.Equal(newTransform, writtenArtefact.mask.transformMatrix);
            Assert.Equal("", writtenArtefact.name);
            
            // Cleanup
            await DeleteArtefact(newEdition, writtenArtefact.id);
            
            // Arrange
            newName = _faker.Lorem.Sentence(5);
            
            newArtefact = new CreateArtefactDTO()
            {
                mask = null,
                position = newTransform,
                name = newName,
                masterImageId = masterImageId
            };
            
            // Act
            (response, writtenArtefact) = await HttpRequest.SendAsync<CreateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Post,
                $"/{version}/editions/{newEdition}/{controller}", newArtefact, jwt);
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(newEdition, writtenArtefact.editionId);
            Assert.Equal("", writtenArtefact.mask.mask);
            Assert.Equal(newTransform, writtenArtefact.mask.transformMatrix);
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
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it
            
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

            await EditionHelpers.DeleteEdition(_client, newEdition, true);
        }
        
        /// <summary>
        /// Ensure that a existing artefact can be updated.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUpdateArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetRandomEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it
            var newArtefactName = _faker.Random.Words(5);
            var newArtefactPosition = RandomPosition();
            const string newArtefactShape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))";
            
            // Act (update name)
            var (nameResponse, updatedNameArtefact) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Put,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}", new UpdateArtefactDTO
                {
                    mask = null,
                    position = null,
                    name = newArtefactName
                }, 
                await HttpRequest.GetJWTAsync(_client));
            
            // Assert (update name)
            nameResponse.EnsureSuccessStatusCode();
            Assert.Equal(artefact.mask.transformMatrix, updatedNameArtefact.mask.transformMatrix);
            Assert.NotEqual(artefact.name, updatedNameArtefact.name);
            Assert.Equal(newArtefactName, updatedNameArtefact.name);
            
            // Act (update position)
            var (positionResponse, updatedPositionArtefact) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Put,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}", new UpdateArtefactDTO
                {
                    mask = null,
                    position = newArtefactPosition,
                    name = null
                }, 
                await HttpRequest.GetJWTAsync(_client));
            
            // Assert (update position)
            positionResponse.EnsureSuccessStatusCode();
            Assert.NotEqual(artefact.mask.transformMatrix, updatedPositionArtefact.mask.transformMatrix);
            Assert.Equal(newArtefactPosition, updatedPositionArtefact.mask.transformMatrix);
            Assert.Equal(newArtefactName, updatedPositionArtefact.name);
            
            // Act (update shape)
            var (shapeResponse, updatedShapeArtefact) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Put,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}", new UpdateArtefactDTO
                {
                    mask = newArtefactShape,
                    position = null,
                    name = null
                }, 
                await HttpRequest.GetJWTAsync(_client));
            
            // Assert (update shape)
            shapeResponse.EnsureSuccessStatusCode();
            Assert.NotEqual(artefact.mask.mask, updatedShapeArtefact.mask.mask);
            Assert.Equal(newArtefactShape, updatedShapeArtefact.mask.mask);
            Assert.Equal(newArtefactPosition, updatedShapeArtefact.mask.transformMatrix);
            Assert.Equal(newArtefactName, updatedShapeArtefact.name);
            
            // Arrange (update all)
            var otherTransform = RandomPosition();
            // Act (update all)
            var (allResponse, updatedAllArtefact) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Put,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}", new UpdateArtefactDTO
                {
                    mask = artefact.mask.mask,
                    position = otherTransform,
                    name = artefact.name
                }, 
                await HttpRequest.GetJWTAsync(_client));
            
            // Assert (update all)
            allResponse.EnsureSuccessStatusCode();
            Assert.Equal(artefact.mask.mask, updatedAllArtefact.mask.mask);
            Assert.Equal(otherTransform, updatedAllArtefact.mask.transformMatrix);
            Assert.Equal(artefact.name, updatedAllArtefact.name);

            await EditionHelpers.DeleteEdition(_client, newEdition, true);
        }

        #endregion Access artefacts (should succeed)
        
        #region Access artefacts (should fail)
        /// <summary>
        /// Ensure that a new artefact cannot be created in an edition not owned by the current user.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotCreateArtefactsOnUnownedEdition()
        {
            // Arrange
            var allArtefacts = (await GetRandomEditionArtefacts()).artefacts; // Find edition with artefacts
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, allArtefacts.First().editionId); // Clone it

            const string masterImageSQL = "SELECT sqe_image_id FROM SQE_image WHERE type = 0 ORDER BY RAND() LIMIT 1";
            var masterImageId = await _db.RunQuerySingleAsync<uint>(masterImageSQL, null);
            const string newArtefactShape = "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))";
            var newTransform = RandomPosition();
            var newName = _faker.Lorem.Sentence(5);
            var newArtefact = new CreateArtefactDTO()
            {
                mask = newArtefactShape,
                position = newTransform,
                name = newName,
                masterImageId = masterImageId
            };
            
            // Act
            var (response, _) = await HttpRequest.SendAsync<CreateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Post,
                $"/{version}/editions/{newEdition}/{controller}", newArtefact);
            
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            await EditionHelpers.DeleteEdition(_client, newEdition, true);
        }
        
        /// <summary>
        /// Ensure that a existing artefact cannot be deleted by a use who does not have access to it.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotDeleteUnownedArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetRandomEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it
            
            // Act
            var (response, _) = await HttpRequest.SendAsync<string, string>(_client, HttpMethod.Delete,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}", null);
            
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            await EditionHelpers.DeleteEdition(_client, newEdition, true);
        }
        
        /// <summary>
        /// Ensure that a existing artefact cannot be updated by a user who does not have access.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotUpdateUnownedArtefacts()
        {
            // Arrange
            var allArtefacts = (await GetRandomEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it
            var newArtefactName = _faker.Random.Words(5);
            
            // Act (update name)
            var (nameResponse, _) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Put,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}", new UpdateArtefactDTO
                {
                    mask = null,
                    position = null,
                    name = newArtefactName
                });
            
            // Assert (update name)
            Assert.Equal(HttpStatusCode.Unauthorized, nameResponse.StatusCode);

            await EditionHelpers.DeleteEdition(_client, newEdition, true);
        }
        
        /// <summary>
        /// Ensure that improperly formatted artefact WKT masks are rejected.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RejectsUpdateToImproperArtefactShape()
        {
            // Arrange
            var allArtefacts = (await GetRandomEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it
            const string newArtefactShape = "POLYGON(0 0,0 200,200 200,0 200,0 0),5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))";
            
            // Act (update name)
            var (nameResponse, _) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Put,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}", new UpdateArtefactDTO
                {
                    mask = newArtefactShape,
                    position = null,
                    name = null
                },
            await HttpRequest.GetJWTAsync(_client));
            
            // Assert (update name)
            Assert.Equal(HttpStatusCode.BadRequest, nameResponse.StatusCode);

            await EditionHelpers.DeleteEdition(_client, newEdition, true);
        }
        
        /// <summary>
        /// Ensure that improperly formatted artefact position transform matrices are rejected.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RejectsUpdateToImproperArtefactPosition()
        {
            // Arrange
            var allArtefacts = (await GetRandomEditionArtefacts()).artefacts; // Find edition with artefacts
            var artefact = allArtefacts.First();
            var newEdition = await EditionHelpers.CreateCopyOfEdition(_client, artefact.editionId); // Clone it
            var newArtefactMatrix = RandomPosition(properlyFormatted: false);
            
            // Act (update name)
            var (nameResponse, _) = await HttpRequest.SendAsync<UpdateArtefactDTO, ArtefactDTO>(_client, HttpMethod.Put,
                $"/{version}/editions/{newEdition}/{controller}/{artefact.id}", new UpdateArtefactDTO
                {
                    mask = null,
                    position = newArtefactMatrix,
                    name = null
                },
                await HttpRequest.GetJWTAsync(_client));
            
            // Assert (update name)
            Assert.Equal(HttpStatusCode.BadRequest, nameResponse.StatusCode);

            await EditionHelpers.DeleteEdition(_client, newEdition, true);
        }
        #endregion Access artefacts (should fail)
        
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
        
        
        private string RandomPosition(bool properlyFormatted = true)
        {
            return properlyFormatted 
                ? $"{{\"matrix\":[[{_faker.Random.Double(-1, 1)},{_faker.Random.Double(-1, 1)},{_faker.Random.Int()}],[{_faker.Random.Double(-1, 1)},{_faker.Random.Double(-1, 1)},{_faker.Random.Int()}]]}}"
                : "{\"matrix\":[[1,0,0],[0,1,0,0]]}";
        }
        #endregion Helpers
    }
}