using System.Linq;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
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
    }
}