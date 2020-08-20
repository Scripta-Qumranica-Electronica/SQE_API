using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Dapper;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.ApiRequests;
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

        private async Task<(uint editionId, string objectId)> GetEditionImagesWithArtefact(uint user = 1)
        {
            var editionId = EditionHelpers.GetEditionId();
            var req = new Get.V1_Editions_EditionId_ImagedObjects(editionId);
            var (httpResponse, httpData, signalrData, _) = await Request.Send(req, _client, StartConnectionAsync);
            httpResponse.EnsureSuccessStatusCode();
            httpData.ShouldDeepEqual(signalrData);
            return (editionId, httpData.imagedObjects.FirstOrDefault().id);
            const string sql = @"
SELECT DISTINCT artefact_shape_owner.edition_id, image_catalog.object_id
FROM artefact_shape
JOIN artefact_shape_owner ON artefact_shape.artefact_shape_id = artefact_shape_owner.artefact_shape_id
JOIN SQE_image ON artefact_shape.sqe_image_id = SQE_image.sqe_image_id
JOIN image_catalog ON SQE_image.image_catalog_id = image_catalog.image_catalog_id
JOIN edition_editor ON artefact_shape_owner.edition_editor_id = edition_editor.edition_editor_id
  AND edition_editor.user_id = @UserId
WHERE artefact_shape_owner.edition_id = @EditionId AND artefact_shape.region_in_sqe_image IS NOT NULL 
LIMIT 50";
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", user);
            parameters.Add("@EditionId", editionId);
            var editionIds = (await _db.RunQueryAsync<(uint editionId, string objectId)>(sql, parameters)).ToList();
            return editionIds.FirstOrDefault();
        }

        /// <summary>
        ///     Can recognize imaged object ids with url encoded values
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanDecodeImagedObjectIdWithUrlEncodedValue()
        {
            // Note: the dotnet HTTP Request system automatically escapes the URL's we submit,
            // so we need to encode the URL first (remember this!!!).
            var id = HttpUtility.UrlEncode("IAA-275%2F1-1");
            var textFragRequest = new Get.V1_ImagedObjects_ImagedObjectId_TextFragments(id);

            var (response, msg, _, _) = await Request.Send(
                textFragRequest,
                _client);

            response.EnsureSuccessStatusCode();
            Assert.Equal(1, msg.matches.Count);
            Assert.Equal("4Q7", msg.matches.First().manuscriptName);
            Assert.Equal("frg. 1", msg.matches.First().textFragmentName);
            Assert.Equal((uint)9423, msg.matches.First().textFragmentId);
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
                if (!string.IsNullOrEmpty(art.mask))
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
            var editionId = EditionHelpers.GetEditionId();
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
            var editionId = EditionHelpers.GetEditionId();
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
                    && string.IsNullOrEmpty(io.artefacts.First().mask))
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
            var editionId = EditionHelpers.GetEditionId();
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
                        if (!string.IsNullOrEmpty(art.mask))
                        {
                            foundArtefactWithMask = true;
                            break;
                        }

            Assert.True(foundArtefactWithMask);
        }
    }
}