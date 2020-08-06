using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    public class SignInterpretationTests : WebControllerTest
    {
        public SignInterpretationTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
        }

        [Fact]
        public async Task CanGetAllEditionSignInterpretationAttributes()
        {
            // Arrange
            var request = new Get.V1_Editions_EditionId_SignInterpretationsAttributes(EditionHelpers.GetEditionId());

            // Act
            var (httpResponse, httpData, signalRData, _) = await Request.Send(
                request,
                _client,
                StartConnectionAsync,
                true);

            // Assert
            httpResponse.EnsureSuccessStatusCode();
            httpData.ShouldDeepEqual(signalRData);
            Assert.NotEmpty(httpData.attributes);
            Assert.NotEmpty(httpData.attributes.FirstOrDefault().values);
            Assert.NotNull(httpData.attributes.FirstOrDefault().attributeName);
            Assert.True(httpData.attributes.FirstOrDefault().attributeId > 0);
            Assert.True(httpData.attributes.FirstOrDefault().creatorId > 0);
            Assert.True(httpData.attributes.FirstOrDefault().editorId > 0);
            Assert.NotNull(httpData.attributes.FirstOrDefault().values.FirstOrDefault().value);
            Assert.True(httpData.attributes.FirstOrDefault().values.FirstOrDefault().id > 0);
            Assert.True(httpData.attributes.FirstOrDefault().values.FirstOrDefault().editorId > 0);
            Assert.True(httpData.attributes.FirstOrDefault().values.FirstOrDefault().creatorId > 0);
        }
    }
}