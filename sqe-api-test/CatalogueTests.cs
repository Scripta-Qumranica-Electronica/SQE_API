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
            await requestObj.Send(_client, StartConnectionAsync);
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
            await requestObj.Send(_client, StartConnectionAsync);
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
            var requestobj = new Get.V1_Catalogue_Editions_EditionId_ImagedObjectTextFragmentMatches(894);
            await requestobj.Send(_client, StartConnectionAsync);

            // Assert
            requestobj.HttpResponseObject.ShouldDeepEqual(requestobj.SignalrResponseObject);
            Assert.NotEmpty(requestobj.HttpResponseObject.matches);
            var firstMatch = requestobj.HttpResponseObject.matches.First();
            ConfirmValidMatch(firstMatch);
        }

        [Fact]
        public async Task CanGetImagedObjectsAndTextFragmentsOfManuscript()
        {
            // Act
            var requestobj = new Get.V1_Catalogue_Manuscripts_ManuscriptId_ImagedObjectTextFragmentMatches(894);
            await requestobj.Send(_client, StartConnectionAsync);

            // Assert
            requestobj.HttpResponseObject.ShouldDeepEqual(requestobj.SignalrResponseObject);
            Assert.NotEmpty(requestobj.HttpResponseObject.matches);
            var firstMatch = requestobj.HttpResponseObject.matches.First();
            ConfirmValidMatch(firstMatch);
        }

        private void ConfirmValidMatch(CatalogueMatchDTO match)
        {
            Assert.NotNull(match.matchAuthor);
            if (match.confirmed.HasValue)
            {
                Assert.NotNull(match.matchConfirmationAuthor);
                Assert.NotNull(match.dateOfConfirmation);
            }
            if (!match.confirmed.HasValue)
            {
                Assert.Null(match.matchConfirmationAuthor);
                Assert.Null(match.dateOfConfirmation);
            }
            Assert.Null(match.matchConfirmationAuthor);
            Assert.NotNull(match.manuscriptName);
            Assert.NotNull(match.name);
            Assert.NotNull(match.imagedObjectId);
            Assert.NotNull(match.institution);
            Assert.NotNull(match.editionName);
            Assert.NotNull(match.filename);
            Assert.NotNull(match.thumbnail);
            Assert.NotNull(match.url);
            Assert.NotNull(match.dateOfMatch);
        }
    }
}