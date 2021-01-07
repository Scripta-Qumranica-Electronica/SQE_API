using System.Linq;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
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
		[Trait("Category", "Scribal Font")]
		public async Task CanWriteToScribalFont(bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var newEdition = await editionCreator.CreateEdition();

				// Act
				var newFont = await _createScribalFont(
						newEdition
						, 10
						, 5
						, realtime);

				// Assert
				var getResponse = await _getEditionScribalFonts(newEdition, realtime);
				Assert.NotEmpty(getResponse.scripts);
				getResponse.scripts.First().IsDeepEqual(newFont);
			}
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		[Trait("Category", "Scribal Font")]
		public async Task CanChangeScribalFont(bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var newEdition = await editionCreator.CreateEdition();

				var newFont = await _createScribalFont(
						newEdition
						, 10
						, 5
						, realtime);

				// Assert
				var getResponse = await _getEditionScribalFonts(newEdition, realtime);
				Assert.NotEmpty(getResponse.scripts);
				getResponse.scripts.First().IsDeepEqual(newFont);

				// Arrange
				var updatedScript = new CreateScriptDataDTO
				{
						lineSpace = 20
						, wordSpace = 10
						,
				};

				// Act
				var request =
						new Put.V1_Editions_EditionId_Scribalfonts_ScribalFontId_ScribalFontData(
								newEdition
								, newFont.scribalFontId
								, updatedScript);

				await request.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime
						, listeningFor: request.AvailableListeners.UpdatedScribalFontInfo);

				// Assert
				getResponse = await _getEditionScribalFonts(newEdition, realtime);
				Assert.NotEmpty(getResponse.scripts);
				Assert.Equal(updatedScript.lineSpace, getResponse.scripts.First().lineSpace);
				Assert.Equal(updatedScript.wordSpace, getResponse.scripts.First().wordSpace);
				Assert.NotEqual(0u, getResponse.scripts.First().creatorId);
				Assert.NotEqual(0u, getResponse.scripts.First().editorId);
				Assert.NotEqual(0u, getResponse.scripts.First().scribalFontId);
			}
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		[Trait("Category", "Scribal Font")]
		public async Task CanDeleteScribalFont(bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var newEdition = await editionCreator.CreateEdition();

				// Act
				var newFont = await _createScribalFont(
						newEdition
						, 10
						, 5
						, realtime);

				// Assert
				var getResponse = await _getEditionScribalFonts(newEdition, realtime);
				Assert.NotEmpty(getResponse.scripts);
				getResponse.scripts.First().IsDeepEqual(newFont);

				// Act
				var request = new Delete.V1_Editions_EditionId_Scribalfonts_ScribalFontId(
						newEdition
						, newFont.scribalFontId);

				await request.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime
						, listeningFor: request.AvailableListeners.DeletedScribalFont);

				// Assert
				var scribalFonts = await _getEditionScribalFonts(newEdition, realtime);

				Assert.DoesNotContain(
						scribalFonts.scripts
						, x => x.scribalFontId == newFont.scribalFontId);
			}
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		[Trait("Category", "Scribal Font")]
		public async Task CanCreateGlyphForScribalFont(bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var newEdition = await editionCreator.CreateEdition();

				var newFont = await _createScribalFont(
						newEdition
						, 10
						, 5
						, realtime);

				// Act
				var _ = await _createGlyph(
						newEdition
						, newFont.scribalFontId
						, 'ז'
						, "POLYGON((0 0,10 0,10 10,0 10,0 0))"
						, 10
						, realtime);
			}
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		[Trait("Category", "Scribal Font")]
		public async Task CanDeleteGlyphForScribalFont(bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var newEdition = await editionCreator.CreateEdition();

				var newFont = await _createScribalFont(
						newEdition
						, 10
						, 5
						, realtime);

				const char character = 'ז';

				var _ = await _createGlyph(
						newEdition
						, newFont.scribalFontId
						, character
						, "POLYGON((0 0,10 0,10 10,0 10,0 0))"
						, 10
						, realtime);

				// Act
				var request =
						new Delete.
								V1_Editions_EditionId_Scribalfonts_ScribalFontId_Glyphs_GlyphCharacter(
										newEdition
										, newFont.scribalFontId
										, character);

				await request.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime
						, listeningFor: request.AvailableListeners.DeletedScribalFontGlyph);

				// Assert
				var getResponse = await _getEditionScribalFonts(newEdition, realtime);
				Assert.NotEmpty(getResponse.scripts);

				var script =
						getResponse.scripts.FirstOrDefault(
								x => x.scribalFontId == newFont.scribalFontId);

				Assert.NotNull(script);
				Assert.Empty(script.glyphs);
			}
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		[Trait("Category", "Scribal Font")]
		public async Task CanCreateKernPairForScribalFont(bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var newEdition = await editionCreator.CreateEdition();

				var newFont = await _createScribalFont(
						newEdition
						, 10
						, 5
						, realtime);

				// Act
				var _ = await _createKernPair(
						newEdition
						, newFont.scribalFontId
						, 'ז'
						, 'ז'
						, 10
						, 0
						, realtime);
			}
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		[Trait("Category", "Scribal Font")]
		public async Task CanDeleteKernPairForScribalFont(bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var newEdition = await editionCreator.CreateEdition();

				var newFont = await _createScribalFont(
						newEdition
						, 10
						, 5
						, realtime);

				const char firstCharacter = 'ז';
				const char secondCharacter = 'ז';

				var _ = await _createKernPair(
						newEdition
						, newFont.scribalFontId
						, firstCharacter
						, secondCharacter
						, 10
						, 0
						, realtime);

				// Act
				var request =
						new Delete.
								V1_Editions_EditionId_Scribalfonts_ScribalFontId_KerningPairs_FirstCharacter_SecondCharacter(
										newEdition
										, newFont.scribalFontId
										, firstCharacter
										, secondCharacter);

				await request.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime
						, listeningFor: request.AvailableListeners.DeletedScribalFontKerningPair);

				// Assert
				var getResponse = await _getEditionScribalFonts(newEdition, realtime);
				Assert.NotEmpty(getResponse.scripts);

				var script =
						getResponse.scripts.FirstOrDefault(
								x => x.scribalFontId == newFont.scribalFontId);

				Assert.NotNull(script);
				Assert.Empty(script.kerningPairs);
			}
		}

		private async Task<ScriptDataListDTO> _getEditionScribalFonts(
				uint   editionId
				, bool realtime
				, bool shouldSucceed = true)
		{
			var getRequest = new Get.V1_Editions_EditionId_Scribalfonts(editionId);

			await getRequest.SendAsync(
					realtime
							? null
							: _client
					, StartConnectionAsync
					, true
					, requestRealtime: realtime
					, shouldSucceed: shouldSucceed);

			// Assert
			return realtime
					? getRequest.SignalrResponseObject
					: getRequest.HttpResponseObject;
		}

		private async Task<ScriptDataDTO> _createScribalFont(
				uint     editionId
				, ushort lineSpace
				, ushort wordSpace
				, bool   realtime
				, bool   shouldSucceed = true)
		{
			var font = new CreateScriptDataDTO
			{
					lineSpace = lineSpace
					, wordSpace = wordSpace
					,
			};

			// Act
			var request = new Post.V1_Editions_EditionId_Scribalfonts(editionId, font);

			await request.SendAsync(
					realtime
							? null
							: _client
					, StartConnectionAsync
					, true
					, requestRealtime: realtime
					, listeningFor: request.AvailableListeners.CreatedScribalFontInfo
					, shouldSucceed: shouldSucceed);

			// Assert
			var response = realtime
					? request.SignalrResponseObject
					: request.HttpResponseObject;

			if (!shouldSucceed)
				return response;

			Assert.Equal(font.lineSpace, response.lineSpace);
			Assert.Equal(font.wordSpace, response.wordSpace);
			Assert.NotEqual(0u, response.creatorId);
			Assert.NotEqual(0u, response.editorId);
			Assert.NotEqual(0u, response.scribalFontId);

			return response;
		}

		private async Task<GlyphDataDTO> _createGlyph(
				uint     editionId
				, uint   scribalFontId
				, char   character
				, string shape
				, short  yOffset
				, bool   realtime
				, bool   shouldSucceed = true)
		{
			var newGlyph = new CreateGlyphDataDTO
			{
					character = character
					, shape = shape
					, yOffset = yOffset
					,
			};

			// Act
			var request = new Post.V1_Editions_EditionId_Scribalfonts_ScribalFontId_Glyphs(
					editionId
					, scribalFontId
					, newGlyph);

			await request.SendAsync(
					realtime
							? null
							: _client
					, StartConnectionAsync
					, true
					, requestRealtime: realtime
					, listeningFor: request.AvailableListeners.CreatedScribalFontGlyph
					, shouldSucceed: shouldSucceed);

			// Assert
			var response = realtime
					? request.SignalrResponseObject
					: request.HttpResponseObject;

			if (!shouldSucceed)
				return response;

			Assert.Equal(newGlyph.character, response.character);
			Assert.Equal(newGlyph.shape, response.shape);
			Assert.Equal(newGlyph.yOffset, response.yOffset);
			Assert.NotEqual(0u, response.creatorId);
			Assert.NotEqual(0u, response.editorId);
			Assert.NotEqual(0u, response.scribalFontId);

			// Assert
			var getResponse = await _getEditionScribalFonts(editionId, realtime);
			Assert.NotEmpty(getResponse.scripts);
			Assert.Contains(getResponse.scripts.First().glyphs, x => x.IsDeepEqual(response));

			return response;
		}

		private async Task<KernPairDTO> _createKernPair(
				uint    editionId
				, uint  scribalFontId
				, char  firstCharacter
				, char  secondCharacter
				, short xKern
				, short yKern
				, bool  realtime
				, bool  shouldSucceed = true)
		{
			var kernPair = new CreateKernPairDTO
			{
					firstCharacter = firstCharacter
					, secondCharacter = secondCharacter
					, xKern = xKern
					, yKern = yKern
					,
			};

			// Act
			var request =
					new Post.V1_Editions_EditionId_Scribalfonts_ScribalFontId_KerningPairs(
							editionId
							, scribalFontId
							, kernPair);

			await request.SendAsync(
					realtime
							? null
							: _client
					, StartConnectionAsync
					, true
					, requestRealtime: realtime
					, listeningFor: request.AvailableListeners.CreatedScribalFontKerningPair
					, shouldSucceed: shouldSucceed);

			// Assert
			var response = realtime
					? request.SignalrResponseObject
					: request.HttpResponseObject;

			if (!shouldSucceed)
				return response;

			Assert.Equal(kernPair.firstCharacter, response.firstCharacter);
			Assert.Equal(kernPair.secondCharacter, response.secondCharacter);
			Assert.Equal(kernPair.xKern, response.xKern);
			Assert.Equal(kernPair.yKern, response.yKern);
			Assert.NotEqual(0u, response.creatorId);
			Assert.NotEqual(0u, response.editorId);
			Assert.NotEqual(0u, response.scribalFontId);

			var getResponse = await _getEditionScribalFonts(editionId, realtime);
			Assert.NotEmpty(getResponse.scripts);
			Assert.Contains(getResponse.scripts.First().kerningPairs, x => x.IsDeepEqual(response));

			return response;
		}
	}
}
