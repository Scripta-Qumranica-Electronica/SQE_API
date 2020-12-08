using System.Linq;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
	public partial class WebControllerTest
	{
		[Fact]
		[Trait("Category", "Catalogue")]
		public async Task CanGetImagedObjectsForTextFragments()
		{
			var requestObj = new Get.V1_Catalogue_TextFragments_TextFragmentId_ImagedObjects(9977);

			await requestObj.SendAsync(_client, StartConnectionAsync);

			var (respCode, response, rtResponse) = (
					requestObj.HttpResponseMessage, requestObj.HttpResponseObject
					, requestObj.SignalrResponseObject);

			respCode.EnsureSuccessStatusCode();
			response.ShouldDeepEqual(rtResponse);

			Assert.True(
					response.matches.Count(
							x => (x.imagedObjectId == "IAA-1094-1") && (x.textFragmentId == 9977))
					== 2); // 894 9977
		}

		[Fact]
		[Trait("Category", "Catalogue")]
		public async Task CanGetTextFragmentsForImagedObjects()
		{
			var requestObj =
					new Get.V1_Catalogue_ImagedObjects_ImagedObjectId_TextFragments("IAA-1094-1");

			await requestObj.SendAsync(_client, StartConnectionAsync);

			var (respCode, response, rtResponse) = (
					requestObj.HttpResponseMessage, requestObj.HttpResponseObject
					, requestObj.SignalrResponseObject);

			respCode.EnsureSuccessStatusCode();
			response.ShouldDeepEqual(rtResponse);

			Assert.True(
					response.matches.Count(x => (x.editionId == 894) && (x.textFragmentId == 9977))
					== 2); // 894 9977
		}

		[Fact]
		[Trait("Category", "Catalogue")]
		public async Task CanGetImagedObjectsAndTextFragmentsOfEdition()
		{
			// Act
			await CatalogueHelpers.GetImagedObjectsAndTextFragmentsOfEdition(
					894
					, _client
					, StartConnectionAsync);
		}

		[Fact]
		[Trait("Category", "Catalogue")]
		public async Task CanGetImagedObjectsAndTextFragmentsOfManuscript()
		{
			// Act
			await CatalogueHelpers.GetImagedObjectsAndTextFragmentsOfManuscript(
					894
					, _client
					, StartConnectionAsync);
		}

		// TODO: look into this test, it seems to fail randomly (not terribly often)
		[Theory]
		[Trait("Category", "Catalogue")]
		[InlineData(true)]
		[InlineData(false)]
		public async Task CanCreateNewImagedObjectTextFragmentMatch(bool realtime)
		{
			// Arrange
			var availableImagedObjects =
					await ImagedObjectHelpers.GetInstitutionImagedObjects(
							"IAA"
							, _client
							, StartConnectionAsync);

			var textFragments =
					await TextHelpers.GetEditionTextFragments(1, _client, StartConnectionAsync);

			var imagedObjectId = availableImagedObjects.institutionalImages.First().id;

			var textFragmentId = textFragments.textFragments.First().id;

			// Act
			await CatalogueHelpers.CreateImagedObjectTextFragmentMatch(
					SideDesignation.recto
					, imagedObjectId
					, 1
					, "DJD"
					, "Some Volume"
					, "Some text number designation"
					, "Some fragment designation"
					, SideDesignation.recto
					, "This is test of the system"
					, textFragmentId
					, 1
					, "1Q28"
					, _client
					, StartConnectionAsync
					, realtime);
		}

		// TODO: the following test is correct, but the code in the API still needs to be fixed
		// [Theory]
		// [InlineData(true)]
		// [InlineData(false)]
		// public async Task CanConfirmAndUnconfirmImagedObjectTextFragmentMatch(bool realtime)
		// {
		// 	// Arrange
		// 	const uint editionId = 894U;
		//
		// 	var matches = await CatalogueHelpers.GetImagedObjectsAndTextFragmentsOfEdition(
		// 			editionId
		// 			, _client
		// 			, StartConnectionAsync);
		//
		// 	var firstUnconfirmedMatch = matches.matches.First(x => x.confirmed == null);
		//
		// 	// Act
		// 	await CatalogueHelpers.ConfirmTextFragmentImagedObjectMatch(
		// 			editionId
		// 			, firstUnconfirmedMatch.matchId
		// 			, _client
		// 			, StartConnectionAsync
		// 			, realtime
		// 			, Request.DefaultUsers.User1);
		//
		// 	await Task.Delay(
		// 			150); // Wait a tiny amount of time so we don't go faster than MySQL Data resolution
		//
		// 	await CatalogueHelpers.UnconfirmTextFragmentImagedObjectMatch(
		// 			editionId
		// 			, firstUnconfirmedMatch.matchId
		// 			, _client
		// 			, StartConnectionAsync
		// 			, realtime
		// 			, Request.DefaultUsers.User1);
		// }
	}
}
