using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
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
            var req = new Get.V1_Editions_EditionId_ImagedObjects(editionId, new List<string>() {"masks"});
            await req.SendAsync(_client, StartConnectionAsync);
            var (httpResponse, httpData, signalrData) =
                (req.HttpResponseMessage, req.HttpResponseObject, req.SignalrResponseObject);
            httpResponse.EnsureSuccessStatusCode();
            httpData.ShouldDeepEqual(signalrData);
            var imageWithMaskId = 0;
            foreach (var (io, index) in httpData.imagedObjects.Select((x, idx) => (x, idx)))
            {
                if (io.artefacts.Any())
                {
                    imageWithMaskId = index;
                    break;
                }
            }
            return (editionId, httpData.imagedObjects[imageWithMaskId].id);
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
            await textFragRequest.SendAsync(_client);
            var (response, msg) = (textFragRequest.HttpResponseMessage, textFragRequest.HttpResponseObject);

            response.EnsureSuccessStatusCode();
            Assert.Single(msg.matches);
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
            var request = new Get.V1_ImagedObjects_Institutions();
            await request.SendAsync(_client, StartConnectionAsync, requestRealtime: true);

            // Assert
            request.HttpResponseMessage.EnsureSuccessStatusCode();
            Assert.NotEmpty(request.HttpResponseObject.institutions);
        }

        /// <summary>
        ///     This tests if an anonymous user can retrieve an imaged object belonging to an edition
        /// </summary>
        /// <returns></returns>
        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task CanGetImagedObjectOfEdition(bool optionalArtefacts, bool optionalMasks)
        {
            // Arrange
            var (editionId, objectId) = await GetEditionImagesWithArtefact();
            var optional = new List<string>();
            if (optionalArtefacts)
                optional.Add("artefacts");
            if (optionalMasks && optionalArtefacts)
                optional.Add("masks");

            // Act
            var request = new Get.V1_Editions_EditionId_ImagedObjects_ImagedObjectId(editionId, objectId, optional);
            await request.SendAsync(_client, StartConnectionAsync, requestRealtime: true);

            // Assert
            request.HttpResponseMessage.EnsureSuccessStatusCode();
            if (!optionalArtefacts)
                Assert.Null(request.HttpResponseObject.artefacts);
            if (optionalArtefacts)
                Assert.NotNull(request.HttpResponseObject.artefacts);
            if (optionalMasks)
            {
                Assert.NotNull(request.HttpResponseObject.artefacts);
                var foundArtefactWithMask = false;
                foreach (var art in request.HttpResponseObject.artefacts)
                    if (!string.IsNullOrEmpty(art.mask))
                    {
                        foundArtefactWithMask = true;
                        break;
                    }

                Assert.True(foundArtefactWithMask);
            }
        }

        /// <summary>
        ///     This tests if an anonymous user can retrieve imaged objects
        /// </summary>
        /// <returns></returns>
        [Theory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task CanGetImagedObjectsOfEdition(bool optionalArtefacts, bool optionalMasks)
        {
            // Arrange
            var editionId = EditionHelpers.GetEditionId();
            var optional = new List<string>();
            if (optionalArtefacts)
                optional.Add("artefacts");
            if (optionalMasks && optionalArtefacts)
                optional.Add("masks");

            // Act
            var request = new Get.V1_Editions_EditionId_ImagedObjects(editionId, optional);
            await request.SendAsync(_client, StartConnectionAsync, requestRealtime: true);

            // Assert
            request.HttpResponseMessage.EnsureSuccessStatusCode();
            Assert.NotEmpty(request.HttpResponseObject.imagedObjects);
            if (!optionalArtefacts)
                foreach (var io in request.HttpResponseObject.imagedObjects) Assert.Null(io.artefacts);
            if (optionalArtefacts)
            {
                var foundArtefact = false;
                var foundArtefactWithMask = false;
                foreach (var io in request.HttpResponseObject.imagedObjects.Where(io => io.artefacts.Any()))
                {
                    foundArtefact = true;
                    if (!optionalMasks)
                        break;
                    foreach (var art in io.artefacts)
                        if (!string.IsNullOrEmpty(art.mask))
                        {
                            foundArtefactWithMask = true;
                            break;
                        }
                }

                Assert.True(foundArtefact);
                if (optionalMasks)
                    Assert.True(foundArtefactWithMask);
                else
                    Assert.False(foundArtefactWithMask);
            }
        }


        [Fact]
        public async Task CanGetInstitutionalImages()
        {
            await ImagedObjectHelpers.GetInstitutionImagedObjects("IAA", _client, StartConnectionAsync);
        }

        [Fact]
        public async Task CanGetSpecifiedImagedObject()
        {
            // Arrange
            var availableImagedObjects = await ImagedObjectHelpers.GetInstitutionImagedObjects("IAA", _client, StartConnectionAsync);

            // Act
            var imagedObject = await GetSpecificImagedObjectAsync(availableImagedObjects.institutionalImages.First().id,
                _client, StartConnectionAsync);

            // Assert
            Assert.NotEmpty(imagedObject.images);
        }

        [Fact]
        public async Task CanGetImagedObjectTextFragment()
        {
            // Note that "IAA-1039-1" had text fragment matches at the time this test was written.
            // Make sure to check the database for errors if this test fails.
            var textFragmentMatches =
                    await GetImagedObjectTextFragmentMatchesAsync("IAA-1039-1", _client, StartConnectionAsync);

            Assert.NotNull(textFragmentMatches);
            Assert.NotEmpty(textFragmentMatches.matches);
            Assert.NotNull(textFragmentMatches.matches.First().manuscriptName);
            Assert.NotNull(textFragmentMatches.matches.First().textFragmentName);
            Assert.NotEqual<uint>(0, textFragmentMatches.matches.First().editionId);
            Assert.NotEqual<uint>(0, textFragmentMatches.matches.First().textFragmentId);
        }

        public async static Task<SimpleImageListDTO> GetSpecificImagedObjectAsync(string imagedObjectId, HttpClient client, Func<string, Task<Microsoft.AspNetCore.SignalR.Client.HubConnection>> signalr)
        {
            // Act
            var request = new Get.V1_ImagedObjects_ImagedObjectId(imagedObjectId);
            await request.SendAsync(client, signalr);

            // Assert
            request.HttpResponseObject.ShouldDeepEqual(request.SignalrResponseObject);
            Assert.NotEmpty(request.HttpResponseObject.images);
            return request.HttpResponseObject;
        }

        public async static Task<ImagedObjectTextFragmentMatchListDTO> GetImagedObjectTextFragmentMatchesAsync(string imagedObjectId, HttpClient client, Func<string, Task<Microsoft.AspNetCore.SignalR.Client.HubConnection>> signalr)
        {
            // Act
            var request = new Get.V1_ImagedObjects_ImagedObjectId_TextFragments(imagedObjectId);
            await request.SendAsync(client, signalr);

            // Assert
            request.HttpResponseObject.ShouldDeepEqual(request.SignalrResponseObject);
            return request.HttpResponseObject;
        }
    }
}