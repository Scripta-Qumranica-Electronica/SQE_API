using System.Linq;
using System.Threading.Tasks;
using SQE.ApiTest.ApiRequests;
using Xunit;

// TODO: It would be nice to be able to generate random polygons for these testing purposes.
namespace SQE.ApiTest
{
	/// <summary>
	///  This test suite tests all the current endpoints in the RoiController
	/// </summary>
	public partial class WebControllerTest
	{
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		[Trait("Category", "QWB Proxy")]
		public async Task CanGetQwbVariants(bool realtime)
		{
			// Arrange
			const uint qwbWordId = 4071;

			//Act
			var request = new Get.V1_QwbProxy_Words_QwbWordId_WordVariants(qwbWordId);

			await request.SendAsync(
					realtime
							? null
							: _client
					, StartConnectionAsync
					, requestRealtime: realtime);

			// Assert
			var result = realtime
					? request.SignalrResponseObject
					: request.HttpResponseObject;

			Assert.NotEmpty(result.variants);
			Assert.NotEmpty(result.variants.First().bibliography);

			Assert.False(
					string.IsNullOrEmpty(result.variants.First().bibliography.First().shortTitle));

			Assert.False(
					string.IsNullOrEmpty(
							result.variants.First().bibliography.First().pageReference));

			Assert.NotEqual(0u, result.variants.First().bibliography.First().bibliographyId);
			Assert.False(string.IsNullOrEmpty(result.variants.First().variantReading));
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		[Trait("Category", "QWB Proxy")]
		public async Task CanGetQwbParallels(bool realtime)
		{
			// Arrange
			const uint qwbStartWordId = 4071;
			const uint qwbEndWordId = 4100;

			//Act
			var request =
					new Get.V1_QwbProxy_Parallels_StartWord_QwbStartWordId_EndWord_QwbEndWordId(
							qwbStartWordId
							, qwbEndWordId);

			await request.SendAsync(
					realtime
							? null
							: _client
					, StartConnectionAsync
					, requestRealtime: realtime);

			// Assert
			var result = realtime
					? request.SignalrResponseObject
					: request.HttpResponseObject;

			Assert.NotEmpty(result.parallels);
			Assert.NotEmpty(result.parallels.First().parallelWords);
			Assert.False(string.IsNullOrEmpty(result.parallels.First().parallelWords.First().word));
			Assert.NotEqual(0u, result.parallels.First().parallelWords.First().qwbWordId);
			Assert.NotEqual(0u, result.parallels.First().parallelWords.First().relatedQwbWordId);
			Assert.False(string.IsNullOrEmpty(result.parallels.First().qwbTextReference));
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		[Trait("Category", "QWB Proxy")]
		public async Task CanGetQwbBibliography(bool realtime)
		{
			// Arrange
			const uint qwbBiblioId = 1;

			//Act
			var request = new Get.V1_QwbProxy_Bibliography_QwbBibliographyId(qwbBiblioId);

			await request.SendAsync(
					realtime
							? null
							: _client
					, StartConnectionAsync
					, requestRealtime: realtime);

			// Assert
			var result = realtime
					? request.SignalrResponseObject
					: request.HttpResponseObject;

			Assert.False(string.IsNullOrEmpty(result.entry));
		}
	}
}
