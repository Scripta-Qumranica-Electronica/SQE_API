using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using NetTopologySuite.IO;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
    /// <summary>
    ///     This a suite of integration tests for the utils controller.
    /// </summary>
    public class UtilTest : WebControllerTest
    {
        public UtilTest(WebApplicationFactory<Startup> factory) : base(factory)
        {
        }
        // There are more extensive tests of the polygon validation method in ValidationTests.cs
        // The tests here are geared towards checking the API endpoints

        [Fact]
        public async Task CanRecognizeValidWktPolygons()
        {
            var goodPolygon = new WktPolygonDTO
            {
                wktPolygon = "POLYGON((0 0,0 10,10 10,10 0,0 0))"
            };

            var polygonValidation = new Post.V1_Utils_RepairWktPolygon(goodPolygon);
            await polygonValidation.Send(
                _client,
                StartConnectionAsync,
                true
            );
            var (validationResponse, validation, rtResponse) = (polygonValidation.HttpResponseMessage,
                polygonValidation.HttpResponseObject, polygonValidation.SignalrResponseObject);

            var wkr = new WKTReader();
            // Assert
            Assert.True(wkr.Read(goodPolygon.wktPolygon).EqualsExact(wkr.Read(validation.wktPolygon)));
            Assert.Equal(HttpStatusCode.OK, validationResponse.StatusCode);
            Assert.True(wkr.Read(goodPolygon.wktPolygon).EqualsExact(wkr.Read(rtResponse.wktPolygon)));
        }

        [Fact]
        public async Task RepairsInvalidWktPolygons()
        {
            var badPolygon = new WktPolygonDTO
            {
                wktPolygon = "POLYGON((0 0,10 0,10 10,0 10))"
            };
            var goodPolygon = new WktPolygonDTO
            {
                wktPolygon = "POLYGON((0 0,0 10,10 10,10 0,0 0))"
            };

            var polygonValidation = new Post.V1_Utils_RepairWktPolygon(badPolygon);
            await polygonValidation.Send(
                _client,
                StartConnectionAsync,
                true,
                shouldSucceed: false
            );
            var (validationResponse, validation, rtValidation) = (polygonValidation.HttpResponseMessage,
                polygonValidation.HttpResponseObject, polygonValidation.SignalrResponseObject);

            // Assert
            // The response should indicate a bad request
            var wkr = new WKTReader();

            var badPolyIsUnreadable = false;
            try
            {
                var _ = wkr.Read(badPolygon.wktPolygon);
            }
            catch
            {
                badPolyIsUnreadable = true;
            }

            Assert.True(badPolyIsUnreadable);

            Assert.True(wkr.Read(goodPolygon.wktPolygon).EqualsTopologically(wkr.Read(validation.wktPolygon)));
            Assert.Equal(HttpStatusCode.OK, validationResponse.StatusCode);
            Assert.True(wkr.Read(goodPolygon.wktPolygon).EqualsTopologically(wkr.Read(rtValidation.wktPolygon)));
        }
    }
}