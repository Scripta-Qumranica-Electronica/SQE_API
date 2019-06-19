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

        private readonly string imagedObjectBarePath;
        private readonly string singleImagedObject;
        private readonly string imagedObjectInstitutions;
        private readonly string editionImagedObjects;
        private readonly string editionImagedObjectbyId;
        
        public ImagedObjectTest(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();

            imagedObjectBarePath = $"/{version}/{controller}";
            singleImagedObject = $"{imagedObjectBarePath}/$id";
            imagedObjectInstitutions = $"{imagedObjectBarePath}/institutions";
            editionImagedObjects = $"/{version}/editions/$EditionId/{controller}";
            editionImagedObjectbyId = $"{editionImagedObjects}/$ImageObjectId";
        }

        #region Anonymous Requests
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
        
        /// <summary>
        /// This tests if an anonymous user can retrieve an imaged object belonging to an edition
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectOfEdition()
        {
            // Arrange
            var (editionId, objectId) = await GetEditionImagesWithArtefact();
            var path = editionImagedObjectbyId.Replace("$EditionId", editionId.ToString())
                .Replace("$ImageObjectId", objectId);
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, ImagedObjectDTO>(_client, HttpMethod.Get,
                path, null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Null(msg.artefacts);
        }
        
        /// <summary>
        /// This tests if an anonymous user can retrieve an imaged object of an edition with artefacts
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectOfEditionWithArtefacts()
        {
            // Arrange
            var (editionId, objectId) = await GetEditionImagesWithArtefact();
            var path = editionImagedObjectbyId.Replace("$EditionId", editionId.ToString())
                           .Replace("$ImageObjectId", objectId) + "?optional=artefacts";
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, ImagedObjectDTO>(_client, HttpMethod.Get,
                path, null);
            
            // Assert
            response.EnsureSuccessStatusCode();;
            Assert.NotNull(msg.artefacts);
        }
        
        /// <summary>
        /// This tests if an anonymous user can retrieve an imaged object of an edition with artefacts and masks
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectOfEditionWithArtefactsAndMasks()
        {
            // Arrange
            var (editionId, objectId) = await GetEditionImagesWithArtefact();
            var path = editionImagedObjectbyId.Replace("$EditionId", editionId.ToString())
                           .Replace("$ImageObjectId", objectId) + "?optional=artefacts&optional=masks";
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, ImagedObjectDTO>(_client, HttpMethod.Get,
                path, null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            var foundArtefactWithMask = false;
            foreach (var art in msg.artefacts)
            {
                if (!string.IsNullOrEmpty(art.mask.mask))
                {
                    foundArtefactWithMask = true;
                    break;
                }
            }
            Assert.True(foundArtefactWithMask);
        }
        
        /// <summary>
        /// This tests if an anonymous user can retrieve imaged object institutions
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectInstitutions()
        {
            // Act
            var (response, msg) = await HttpRequest.SendAsync<string, ImageInstitutionListDTO>(_client, HttpMethod.Get,
                imagedObjectInstitutions, null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotEmpty(msg.institutions);
        }
        
//        /// <summary>
//        /// This tests if an anonymous user can retrieve a full list of imaged objects
//        /// </summary>
//        /// <returns></returns>
//        [Fact]
//        public async Task CanGetAllImagedObjects()
//        {
//            // Act
//            var (response, msg) = await HttpRequest.SendAsync<string, ImageGroupListDTO>(_client, HttpMethod.Get,
//                imagedObjectBarePath, null);
//            
//            // Assert
//            response.EnsureSuccessStatusCode();
//            Assert.NotEmpty(msg.imageGroups);
//        }
//        
//        /// <summary>
//        /// This tests if an anonymous user can retrieve a specific imaged object
//        /// </summary>
//        /// <returns></returns>
//        [Fact]
//        public async Task CanGetImagedObject()
//        {
//            // Arrange
//            var (response, msg) = await HttpRequest.SendAsync<string, ImageGroupListDTO>(_client, HttpMethod.Get,
//                imagedObjectBarePath, null);
//            response.EnsureSuccessStatusCode();
//            var id = msg.imageGroups[_faker.Random.Number(0, msg.imageGroups.Count() - 1)].id;
//            var path = singleImagedObject.Replace("$id", id.ToString());
//            
//            
//            // Act
//            var (singleResponse, singleMsg) = await HttpRequest.SendAsync<string, ImageGroupListDTO>(_client, HttpMethod.Get,
//                path, null);
//            
//            // Assert
//            singleResponse.EnsureSuccessStatusCode();
//            Assert.NotEmpty(msg.imageGroups);
//        }
        #endregion Anonymous Requests

        #region Helpers
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
        
        private async Task<(uint editionId, string objectId)> GetEditionImagesWithArtefact(uint user = 1)
        {
            const string sql = @"
SELECT DISTINCT edition_editor.edition_id, image_catalog.object_id
FROM artefact_shape_owner
JOIN edition_editor USING(edition_editor_id)
JOIN artefact_shape USING(artefact_shape_id)
JOIN SQE_image USING(sqe_image_id)
JOIN image_catalog USING(image_catalog_id)
WHERE user_id = @UserId AND artefact_shape.region_in_sqe_image IS NOT NULL 
LIMIT 50";
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", user);
            var editionIds = (await _db.RunQueryAsync<(uint editionId, string objectId)>(sql, parameters)).ToList();
            return editionIds[_faker.Random.Number(0, editionIds.Count() -1)];
        }
        #endregion Helpers

    }   
}