using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
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

        #region Polygon Validation
        // There are more extensive tests of the polygon validation method in ValidationTests.cs
        // The tests here are geared towards checking the API endpoints

        [Fact]
        public async Task CanRecognizeValidWktPolygons()
        {
            var goodPolygon = new WktPolygonDTO()
            {
                wktPolygon = "POLYGON((0 0,0 10,10 10,10 0,0 0))"
            };

            var polygonValidation = new Post.V1_Utils_ValidateWkt(goodPolygon);

            var (validationResponse, validation, rtValidationResponse, rtValidation) =
                await Request.Send(
                    polygonValidation,
                    _client,
                    StartConnectionAsync,
                    auth: true
                );

            // Assert
            // The response should indicate a bad request
            Assert.Null(validation);
            Assert.Equal(HttpStatusCode.NoContent, validationResponse.StatusCode);
            Assert.Null(rtValidation);
            Assert.Null(rtValidationResponse);
        }

        [Fact]
        public async Task RejectsInvalidWktPolygons()
        {
            var goodPolygon = new WktPolygonDTO()
            {
                wktPolygon = "POLYGON((0 0,0 10,10 10,10 0))"
            };

            var polygonValidation = new Post.V1_Utils_ValidateWkt(goodPolygon);

            var (validationResponse, validation, rtValidationResponse, rtValidation) =
                await Request.Send(
                    polygonValidation,
                    _client,
                    StartConnectionAsync,
                    auth: true,
                    shouldSucceed: false
                );
            var content = await validationResponse.Content.ReadAsStringAsync();
            // Assert
            // The response should indicate a bad request
            Assert.Null(validation);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, validationResponse.StatusCode);
            Assert.Equal(
                "{\"internalErrorName\":\"httpException\",\"msg\":\"The submitted wktPolygon is malformed. Check the data property of this error message for a possible valid substitute.\",\"data\":{\"wktPolygon\":\"POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0))\"}}",
                content
            );
        }
        #endregion


    }
}