using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using api_test.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.SqeHttpApi.Server;
using Bogus;
using Dapper;
using SQE.SqeHttpApi.Server.DTOs;
using Xunit;

namespace api_test
{
    public class ImagedObjectTest : WebControllerTest
    {
        private readonly Faker _faker = new Faker("en");
        private readonly DatabaseQuery _db;
        private const string version = "v1";
        private const string controller = "imaged-objects";

        private readonly string editionImagedObjects;
        private readonly string editionImagedObjectbyId;
        
        public ImagedObjectTest(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();
            
            editionImagedObjects = $"/{version}/editions/$EditionId/{controller}/";
            editionImagedObjectbyId = $"{editionImagedObjects}/$ImageObjectId";
        }

        /// <summary>
        /// This tests if an anonymous user can retrieve imaged objects
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectsOfEdition()
        {
            // Arrange
            var editionId = await GetEditionWithImages();
            var path = editionImagedObjects.Replace("$EditionId", editionId.ToString());
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, ImagedObjectListDTO>(_client, HttpMethod.Get,
                path, null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotEmpty(msg.imagedObjects);
            foreach (var io in msg.imagedObjects)
            {
                Assert.Null(io.artefacts);
            }
        }
        
        /// <summary>
        /// This tests if an anonymous user can retrieve imaged objects with artefacts
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectsOfEditionWithArtefacts()
        {
            // Arrange
            var editionId = await GetEditionWithImages();
            var path = editionImagedObjects.Replace("$EditionId", editionId.ToString()) + "?optional=artefacts";
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, ImagedObjectListDTO>(_client, HttpMethod.Get,
                path, null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            var foundArtefact = false;
            foreach (var io in msg.imagedObjects)
            {
                if (io.artefacts != null && string.IsNullOrEmpty(io.artefacts.First().mask.mask))
                {
                    foundArtefact = true;
                    break;
                }
            }
            Assert.True(foundArtefact);
        }
        
        /// <summary>
        /// This tests if an anonymous user can retrieve imaged objects with artefacts and masks
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectsOfEditionWithArtefactsAndMasks()
        {
            // Arrange
            var editionId = await GetEditionWithImages();
            var path = editionImagedObjects.Replace("$EditionId", editionId.ToString()) + "?optional=artefacts&optional=masks";
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, ImagedObjectListDTO>(_client, HttpMethod.Get,
                path, null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            var foundArtefactWithMask = false;
            foreach (var io in msg.imagedObjects)
            {
                if (io.artefacts != null)
                {
                    foreach (var art in io.artefacts)
                    {
                        if (!string.IsNullOrEmpty(art.mask.mask))
                        {
                            foundArtefactWithMask = true;
                            break;
                        }
                    }
                }
            }
            Assert.True(foundArtefactWithMask);
        }

        private async Task<uint> GetEditionWithImages(uint user = 1)
        {
            const string sql = @"
SELECT DISTINCT edition_editor.edition_id
FROM artefact_shape_owner
JOIN edition_editor USING(edition_editor_id)
JOIN artefact_shape USING(artefact_shape_id)
JOIN SQE_image USING(sqe_image_id)
WHERE user_id = @UserId";
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", user);
            var editionIds = (await _db.RunQueryAsync<uint>(sql, parameters)).ToList();
            return editionIds[_faker.Random.Number(0, editionIds.Count() -1)];
        }
    }
}