using System.Linq;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
    public class CatalogueTest : WebControllerTest
    {
        public CatalogueTest(WebApplicationFactory<Startup> factory) : base(factory)
        {
        }

        [Fact]
        public async Task CanGetImagedObjectsForTextFragments()
        {
            var requestObj = new Get.V1_Catalogue_TextFragments_TextFragmentId_ImagedObjects(9977);
            await requestObj.SendAsync(_client, StartConnectionAsync);
            var (respCode, response, rtResponse) = (requestObj.HttpResponseMessage,
                requestObj.HttpResponseObject, requestObj.SignalrResponseObject);
            respCode.EnsureSuccessStatusCode();
            response.ShouldDeepEqual(rtResponse);
            Assert.True(response.matches.Count(x => x.imagedObjectId == "IAA-1094-1" && x.textFragmentId == 9977) ==
                        2); // 894 9977
        }

        [Fact]
        public async Task CanGetTextFragmentsForImagedObjects()
        {
            var requestObj = new Get.V1_Catalogue_ImagedObjects_ImagedObjectId_TextFragments("IAA-1094-1");
            await requestObj.SendAsync(_client, StartConnectionAsync);
            var (respCode, response, rtResponse) = (requestObj.HttpResponseMessage,
                requestObj.HttpResponseObject, requestObj.SignalrResponseObject);
            respCode.EnsureSuccessStatusCode();
            response.ShouldDeepEqual(rtResponse);
            Assert.True(response.matches.Count(x => x.editionId == 894 && x.textFragmentId == 9977) == 2); // 894 9977
        }

        [Fact]
        public async Task CanGetImagedObjectsAndTextFragmentsOfEdition()
        {
            // Act
            await CatalogueHelpers.GetImagedObjectsAndTextFragmentsOfEdition(894, _client, StartConnectionAsync);
        }

        [Fact]
        public async Task CanGetImagedObjectsAndTextFragmentsOfManuscript()
        {
            // Act
            await CatalogueHelpers.GetImagedObjectsAndTextFragmentsOfManuscript(894, _client, StartConnectionAsync);
        }

        [Fact]
        public async Task CanCreateNewImagedObjectTextFragmentMatch()
        {
            // Arrange
            var availableImagedObjects = await ImagedObjectHelpers.GetInstitutionImagedObjects("IAA", _client, StartConnectionAsync);
            var textFragments = await TextHelpers.GetEditionTextFragments(1, _client, StartConnectionAsync);
            var imagedObjectId = availableImagedObjects.institutionalImages.First().id;
            var textFragmentId = textFragments.textFragments.First().id;

            // Act
            await CatalogueHelpers.CreateImagedObjectTextFragmentMatch(
                SideDesignation.recto,
                imagedObjectId,
                1,
                "DJD",
                "Some Volume",
                "Some text number designation",
                "Some fragment designation",
                SideDesignation.recto,
                "This is test of the system",
                textFragmentId,
                1,
                _client,
                StartConnectionAsync);
        }

        [Fact]
        public async Task CanConfirmAndUnconfirmImagedObjectTextFragmentMatch()
        {
            // Arrange
            var editionId = 894U;
            var matches = await CatalogueHelpers.GetImagedObjectsAndTextFragmentsOfEdition(editionId, _client, StartConnectionAsync);
            var firstUnconfirmedMatch = matches.matches.First(x => x.confirmed == null);

            // Act
            await CatalogueHelpers.ConfirmTextFragmentImagedObjectMatch(
                editionId,
                firstUnconfirmedMatch.matchId,
                _client,
                StartConnectionAsync,
                Request.DefaultUsers.User1);
            await CatalogueHelpers.UnconfirmTextFragmentImagedObjectMatch(
                editionId,
                firstUnconfirmedMatch.matchId,
                _client,
                StartConnectionAsync,
                Request.DefaultUsers.User1);
        }
    }
}