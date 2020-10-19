using System.Net;
using System.Threading.Tasks;
using NetTopologySuite.IO;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using Xunit;

namespace SQE.ApiTest
{
	/// <summary>
	///  This a suite of integration tests for the utils controller.
	/// </summary>
	public partial class WebControllerTest
	{
		// There are more extensive tests of the polygon validation method in ValidationTests.cs
		// The tests here are geared towards checking the API endpoints

		[Fact]
		[Trait("Category", "Utilities")]
		public async Task CanRecognizeValidWktPolygons()
		{
			var goodPolygon = new WktPolygonDTO
			{
					wktPolygon = "POLYGON((0 0,0 10,10 10,10 0,0 0))",
			};

			var polygonValidation = new Post.V1_Utils_RepairWktPolygon(goodPolygon);

			await polygonValidation.SendAsync(_client, StartConnectionAsync, true);

			var (validationResponse, validation, rtResponse) = (
					polygonValidation.HttpResponseMessage, polygonValidation.HttpResponseObject
					, polygonValidation.SignalrResponseObject);

			var wkr = new WKTReader();

			// Assert
			Assert.True(
					wkr.Read(goodPolygon.wktPolygon).EqualsExact(wkr.Read(validation.wktPolygon)));

			Assert.Equal(HttpStatusCode.OK, validationResponse.StatusCode);

			Assert.True(
					wkr.Read(goodPolygon.wktPolygon).EqualsExact(wkr.Read(rtResponse.wktPolygon)));
		}

		[Fact]
		[Trait("Category", "Utilities")]
		public async Task RepairsInvalidWktPolygons()
		{
			var badPolygon = new WktPolygonDTO { wktPolygon = "POLYGON((0 0,10 0,10 10,0 10))" };

			var goodPolygon = new WktPolygonDTO
			{
					wktPolygon = "POLYGON((0 0,0 10,10 10,10 0,0 0))",
			};

			var polygonValidation = new Post.V1_Utils_RepairWktPolygon(badPolygon);

			await polygonValidation.SendAsync(
					_client
					, StartConnectionAsync
					, true
					, shouldSucceed: false);

			var (validationResponse, validation, rtValidation) = (
					polygonValidation.HttpResponseMessage, polygonValidation.HttpResponseObject
					, polygonValidation.SignalrResponseObject);

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

			Assert.True(
					wkr.Read(goodPolygon.wktPolygon)
					   .EqualsTopologically(wkr.Read(validation.wktPolygon)));

			Assert.Equal(HttpStatusCode.OK, validationResponse.StatusCode);

			Assert.True(
					wkr.Read(goodPolygon.wktPolygon)
					   .EqualsTopologically(wkr.Read(rtValidation.wktPolygon)));
		}
	}
}
