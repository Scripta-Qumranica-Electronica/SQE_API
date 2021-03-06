using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
    public class ImagedObjectTest : WebControllerTest
    {
        public ImagedObjectTest(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();

            imagedObjectBarePath = $"/{version}/{controller}";
            singleImagedObject = $"{imagedObjectBarePath}/$id";
            imagedObjectInstitutions = $"{imagedObjectBarePath}/institutions";
            editionImagedObjects = $"/{version}/editions/$EditionId/{controller}";
            editionImagedObjectbyId = $"{editionImagedObjects}/$ImageObjectId";
        }

        private readonly DatabaseQuery _db;
        private const string version = "v1";
        private const string controller = "imaged-objects";

        private readonly string imagedObjectBarePath;
        private readonly string singleImagedObject;
        private readonly string imagedObjectInstitutions;
        private readonly string editionImagedObjects;
        private readonly string editionImagedObjectbyId;

        private async Task<uint> GetEditionWithImages(uint user = 1)
        {
            const string sql = @"
SELECT DISTINCT artefact_shape_owner.edition_id
FROM artefact_shape
JOIN artefact_shape_owner ON artefact_shape.artefact_shape_id = artefact_shape_owner.artefact_shape_id
JOIN edition_editor ON artefact_shape_owner.edition_editor_id = edition_editor.edition_editor_id
  AND edition_editor.user_id = @UserId
JOIN SQE_image ON artefact_shape.sqe_image_id = SQE_image.sqe_image_id";
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", user);
            var editionIds = (await _db.RunQueryAsync<uint>(sql, parameters)).ToList();
            return editionIds[3];
        }

        private async Task<(uint editionId, string objectId)> GetEditionImagesWithArtefact(uint user = 1)
        {
            const string sql = @"
SELECT DISTINCT artefact_shape_owner.edition_id, image_catalog.object_id
FROM artefact_shape
JOIN artefact_shape_owner ON artefact_shape.artefact_shape_id = artefact_shape_owner.artefact_shape_id
JOIN SQE_image ON artefact_shape.sqe_image_id = SQE_image.sqe_image_id
JOIN image_catalog ON SQE_image.image_catalog_id = image_catalog.image_catalog_id
JOIN edition_editor ON artefact_shape_owner.edition_editor_id = edition_editor.edition_editor_id
  AND edition_editor.user_id = @UserId
WHERE artefact_shape.region_in_sqe_image IS NOT NULL 
LIMIT 50";
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", user);
            var editionIds = (await _db.RunQueryAsync<(uint editionId, string objectId)>(sql, parameters)).ToList();
            return editionIds[3];
        }

        /// <summary>
        ///     This tests if an anonymous user can retrieve imaged object institutions
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectInstitutions()
        {
            // Act
            var (response, msg) = await Request.SendHttpRequestAsync<string, ImageInstitutionListDTO>(
                _client,
                HttpMethod.Get,
                imagedObjectInstitutions,
                null
            );

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotEmpty(msg.institutions);
        }

        /// <summary>
        ///     This tests if an anonymous user can retrieve an imaged object belonging to an edition
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
            var (response, msg) = await Request.SendHttpRequestAsync<string, ImagedObjectDTO>(
                _client,
                HttpMethod.Get,
                path,
                null
            );

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Null(msg.artefacts);
        }

        /// <summary>
        ///     This tests if an anonymous user can retrieve an imaged object of an edition with artefacts
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectOfEditionWithArtefacts()
        {
            // Arrange
            var (editionId, objectId) = await GetEditionImagesWithArtefact();
            var path = editionImagedObjectbyId.Replace("$EditionId", editionId.ToString())
                           .Replace("$ImageObjectId", objectId)
                       + "?optional=artefacts";

            // Act
            var (response, msg) = await Request.SendHttpRequestAsync<string, ImagedObjectDTO>(
                _client,
                HttpMethod.Get,
                path,
                null
            );

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(msg.artefacts);
        }

        /// <summary>
        ///     This tests if an anonymous user can retrieve an imaged object of an edition with artefacts and masks
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectOfEditionWithArtefactsAndMasks()
        {
            // Arrange
            var (editionId, objectId) = await GetEditionImagesWithArtefact();
            var path = editionImagedObjectbyId.Replace("$EditionId", editionId.ToString())
                           .Replace("$ImageObjectId", objectId)
                       + "?optional=artefacts&optional=masks";

            // Act
            var (response, msg) = await Request.SendHttpRequestAsync<string, ImagedObjectDTO>(
                _client,
                HttpMethod.Get,
                path,
                null
            );

            // Assert
            response.EnsureSuccessStatusCode();
            var foundArtefactWithMask = false;
            foreach (var art in msg.artefacts)
                if (!string.IsNullOrEmpty(art.mask.mask))
                {
                    foundArtefactWithMask = true;
                    break;
                }

            Assert.True(foundArtefactWithMask);
        }

        /// <summary>
        ///     This tests if an anonymous user can retrieve imaged objects
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectsOfEdition()
        {
            // Arrange
            var editionId = await GetEditionWithImages();
            var path = editionImagedObjects.Replace("$EditionId", editionId.ToString());

            // Act
            var (response, msg) = await Request.SendHttpRequestAsync<string, ImagedObjectListDTO>(
                _client,
                HttpMethod.Get,
                path,
                null
            );

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotEmpty(msg.imagedObjects);
            foreach (var io in msg.imagedObjects) Assert.Null(io.artefacts);
        }

        /// <summary>
        ///     This tests if an anonymous user can retrieve imaged objects with artefacts
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectsOfEditionWithArtefacts()
        {
            // Arrange
            var editionId = await GetEditionWithImages();
            var path = editionImagedObjects.Replace("$EditionId", editionId.ToString()) + "?optional=artefacts";

            // Act
            var (response, msg) = await Request.SendHttpRequestAsync<string, ImagedObjectListDTO>(
                _client,
                HttpMethod.Get,
                path,
                null
            );

            // Assert
            response.EnsureSuccessStatusCode();
            var foundArtefact = false;
            foreach (var io in msg.imagedObjects)
                if (io.artefacts != null
                    && string.IsNullOrEmpty(io.artefacts.First().mask.mask))
                {
                    foundArtefact = true;
                    break;
                }

            Assert.True(foundArtefact);
        }

        /// <summary>
        ///     This tests if an anonymous user can retrieve imaged objects with artefacts and masks
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanGetImagedObjectsOfEditionWithArtefactsAndMasks()
        {
            // Arrange
            var editionId = await GetEditionWithImages();
            var path = editionImagedObjects.Replace("$EditionId", editionId.ToString())
                       + "?optional=artefacts&optional=masks";

            // Act
            var (response, msg) = await Request.SendHttpRequestAsync<string, ImagedObjectListDTO>(
                _client,
                HttpMethod.Get,
                path,
                null
            );

            // Assert
            response.EnsureSuccessStatusCode();
            var foundArtefactWithMask = false;
            foreach (var io in msg.imagedObjects)
                if (io.artefacts != null)
                    foreach (var art in io.artefacts)
                        if (!string.IsNullOrEmpty(art.mask.mask))
                        {
                            foundArtefactWithMask = true;
                            break;
                        }

            Assert.True(foundArtefactWithMask);
        }
    }
}