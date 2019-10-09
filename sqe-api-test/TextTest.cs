using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Internal;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
	public class TextTest : WebControllerTest
	{
		public TextTest(WebApplicationFactory<Startup> factory) : base(factory)
		{
			_db = new DatabaseQuery();
			_editionBase = $"/{version}/{editionsController}/$EditionId";
			_getTextFragmentsData = $"{_editionBase}/{controller}";
			_getTextFragments = $"{_getTextFragmentsData}/$TextFragmentId";
			_getTextLinesData = $"{_getTextFragments}/{linesController}";
			_getTextLines = $"{_editionBase}/{linesController}/$LineId";
			_postTextFragment = _getTextFragmentsData;
		}

		private readonly DatabaseQuery _db;

		private const string version = "v1";
		private const string editionsController = "editions";
		private const string linesController = "lines";
		private const string controller = "text-fragments";
		private readonly string _editionBase;
		private readonly string _getTextFragmentsData;
		private readonly string _getTextFragments;
		private readonly string _getTextLinesData;
		private readonly string _getTextLines;
		private readonly string _postTextFragment;

		private async Task<(uint editionId, uint textFragmentId)> _getTextFragmentIds(uint? editionId = null)
		{
			var editionDTO = editionId.HasValue
				? await EditionHelpers.GetEdition(_client, editionId.Value)
				: await EditionHelpers.GetEdition(_client);

			var (fragmentsResponse, textFragments) = await HttpRequest.SendAsync<string, TextFragmentDataListDTO>(
				_client,
				HttpMethod.Get,
				_getTextFragmentsData.Replace("$EditionId", editionDTO.id.ToString()),
				null
			);
			fragmentsResponse.EnsureSuccessStatusCode();

			return (editionDTO.id, textFragmentId: textFragments.textFragments.First().id);
		}

		private async Task<uint> _getClonedEdition()
		{
			var (editionId, _) = await _getTextFragmentIds(); // Get an edition with text fragments
			return await EditionHelpers.CreateCopyOfEdition(_client, editionId); // Clone it
		}

		private async Task<TextFragmentDataListDTO> _getEditionTextFragments(uint editionId, string jwt = null)
		{
			var (fragmentsResponse, textFragments) = await HttpRequest.SendAsync<string, TextFragmentDataListDTO>(
				_client,
				HttpMethod.Get,
				_getTextFragmentsData.Replace("$EditionId", editionId.ToString()),
				null,
				jwt
			);
			fragmentsResponse.EnsureSuccessStatusCode();
			return textFragments;
		}

		private async Task<(HttpResponseMessage response, TextFragmentDataDTO msg)> _createTextFragment(uint editionId,
			string textFragmentName,
			bool shouldSucceed = true,
			uint? previousTextFragmentId = null,
			uint? nextTextFragmentId = null,
			bool authenticated = true)
		{
			var (response, msg) = await HttpRequest.SendAsync<CreateTextFragmentDTO, TextFragmentDataDTO>(
				_client,
				HttpMethod.Post,
				_postTextFragment.Replace("$EditionId", editionId.ToString()),
				new CreateTextFragmentDTO
				{
					name = textFragmentName,
					previousTextFragmentId = previousTextFragmentId,
					nextTextFragmentId = nextTextFragmentId
				},
				authenticated ? await HttpRequest.GetJWTAsync(_client) : null
			);
			if (shouldSucceed)
				response.EnsureSuccessStatusCode();
			return (response, msg);
		}

		private async Task<(uint editionId, uint textFragmentId, uint lineId)> _getLine()
		{
			uint editionId = 0, textFragmentId = 0;
			var (lineResponse, lines) = (new HttpResponseMessage(), new LineDataListDTO(new List<LineDataDTO>()));
			while (editionId == 0 || textFragmentId == 0 || lines == null || !lines.lines.Any())
			{
				(editionId, textFragmentId) = await _getTextFragmentIds();
				(lineResponse, lines) = await HttpRequest.SendAsync<string, LineDataListDTO>(
					_client,
					HttpMethod.Get,
					_getTextLinesData.Replace("$EditionId", editionId.ToString())
						.Replace("$TextFragmentId", textFragmentId.ToString()),
					null
				);
				lineResponse.EnsureSuccessStatusCode();
			}

			return (editionId, textFragmentId, lines.lines.First().lineId);
		}

		private static void _verifyLineTextDTO(LineTextDTO msg)
		{
			Assert.NotNull(msg.licence);
			Assert.NotEmpty(msg.editors);
			Assert.NotEqual((uint)0, msg.lineId);
			Assert.NotEmpty(msg.signs);
			Assert.NotEqual(
				(uint)0,
				msg.signs.First().signInterpretations.First().nextSignInterpretations.First().nextSignInterpretationId
			);
			Assert.NotEmpty(msg.signs.First().signInterpretations);
			Assert.NotEqual((uint)0, msg.signs.First().signInterpretations.First().signInterpretationId);
			Assert.NotEmpty(msg.signs.First().signInterpretations.First().attributes);
			Assert.NotEqual(
				(uint)0,
				msg.signs.First().signInterpretations.First().attributes.First().interpretationAttributeId
			);

			var editorIds = new List<uint> { msg.editorId };
			foreach (var sign in msg.signs)
			{
				foreach (var signInterpretation in sign.signInterpretations)
				{
					foreach (var attr in signInterpretation.attributes)
						if (!msg.editors.ContainsKey(attr.editorId))
							editorIds.Add(attr.editorId);

					foreach (var roi in signInterpretation.rois)
						if (!msg.editors.ContainsKey(roi.editorId))
							editorIds.Add(roi.editorId);

					foreach (var nexSign in signInterpretation.nextSignInterpretations)
						if (!msg.editors.ContainsKey(nexSign.editorId))
							editorIds.Add(nexSign.editorId);
				}
			}

			Assert.NotEmpty(editorIds);
			foreach (var editorId in editorIds) Assert.True(msg.editors.ContainsKey(editorId));
		}

		private static void _verifyTextEditionDTO(TextEditionDTO msg)
		{
			Assert.NotNull(msg.licence);
			Assert.NotEmpty(msg.editors);
			Assert.NotEqual((uint)0, msg.manuscriptId);
			Assert.NotEmpty(msg.textFragments);
			Assert.NotEqual((uint)0, msg.textFragments.First().textFragmentId);
			Assert.NotEmpty(msg.textFragments.First().lines);
			Assert.NotEqual((uint)0, msg.textFragments.First().lines.First().lineId);
			Assert.NotEmpty(msg.textFragments.First().lines.First().signs);
			Assert.NotEqual(
				(uint)0,
				msg.textFragments.First()
					.lines.First()
					.signs.First()
					.signInterpretations.First()
					.nextSignInterpretations.First()
					.nextSignInterpretationId
			);
			Assert.NotEmpty(msg.textFragments.First().lines.First().signs.First().signInterpretations);
			Assert.NotEqual(
				(uint)0,
				msg.textFragments.First().lines.First().signs.First().signInterpretations.First().signInterpretationId
			);
			Assert.NotEmpty(
				msg.textFragments.First().lines.First().signs.First().signInterpretations.First().attributes
			);
			Assert.NotEqual(
				(uint)0,
				msg.textFragments.First()
					.lines.First()
					.signs.First()
					.signInterpretations.First()
					.attributes.First()
					.interpretationAttributeId
			);

			var editorIds = new List<uint> { msg.editorId };
			foreach (var textFragment in msg.textFragments)
			{
				if (!msg.editors.ContainsKey(textFragment.editorId))
					editorIds.Add(textFragment.editorId);

				foreach (var line in textFragment.lines)
				{
					if (!msg.editors.ContainsKey(line.editorId))
						editorIds.Add(line.editorId);

					foreach (var sign in line.signs)
					{
						foreach (var signInterpretation in sign.signInterpretations)
						{
							foreach (var attr in signInterpretation.attributes)
								if (!msg.editors.ContainsKey(attr.editorId))
									editorIds.Add(attr.editorId);

							foreach (var roi in signInterpretation.rois)
								if (!msg.editors.ContainsKey(roi.editorId))
									editorIds.Add(roi.editorId);

							foreach (var nexSign in signInterpretation.nextSignInterpretations)
								if (!msg.editors.ContainsKey(nexSign.editorId))
									editorIds.Add(nexSign.editorId);
						}
					}
				}
			}

			Assert.NotEmpty(editorIds);
			foreach (var editorId in editorIds) Assert.True(msg.editors.ContainsKey(editorId));
		}

		[Fact]
		public async Task CanAddTextFragmentAfter()
		{
			// Arrange
			var editionId = await _getClonedEdition(); // Get a newly cloned edition with text fragments
			var textFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get all the text fragments in the edition
			const string textFragmentName = "my can add after col";
			var previousFragmentId =
				textFragments.textFragments.First().id; // We will make the new text fragment number two
			var numberOfTextFragments = textFragments.textFragments.Count;

			// Act
			var (response, msg) = await _createTextFragment(
				editionId,
				textFragmentName,
				previousTextFragmentId: previousFragmentId
			);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.Empty(textFragments.textFragments.Where(x => x.id == msg.id || x.name == msg.name));
			var updatedTextFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get the updated list of text fragments in the edition
			Assert.NotEmpty(updatedTextFragments.textFragments.Where(x => x.id == msg.id));
			var index = updatedTextFragments.textFragments.Select(x => x.id).IndexOf(msg.id);
			Assert.Equal(1, index); //Index 1 would be the second position
			Assert.Equal(msg.id, updatedTextFragments.textFragments[index].id);
			Assert.Equal(msg.name, updatedTextFragments.textFragments[index].name);
			Assert.Equal(numberOfTextFragments + 1, updatedTextFragments.textFragments.Count);

			// Make sure that nothing else has changed to the pre-existing text fragments
			for (var i = 0; i < textFragments.textFragments.Count; i++)
				textFragments.textFragments[i]
					.ShouldDeepEqual(
						updatedTextFragments.textFragments[
							i > 0 ? i + 1 : i // skip index 1, which is the new text fragment
						]
					);

			await EditionHelpers.DeleteEdition(_client, editionId, true);
		}

		[Fact]
		public async Task CanAddTextFragmentBefore()
		{
			// Arrange
			var editionId = await _getClonedEdition(); // Get a newly cloned edition with text fragments
			var textFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get all the text fragments in the edition
			const string textFragmentName = "my can add before col";
			var nextFragmentId =
				textFragments.textFragments.Last().id; // We will make the new text fragment second to last
			var numberOfTextFragments = textFragments.textFragments.Count;

			// Act
			var (response, msg) = await _createTextFragment(
				editionId,
				textFragmentName,
				nextTextFragmentId: nextFragmentId
			);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.Empty(textFragments.textFragments.Where(x => x.id == msg.id || x.name == msg.name));
			var updatedTextFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get the updated list of text fragments in the edition
			Assert.NotEmpty(updatedTextFragments.textFragments.Where(x => x.id == msg.id));
			var index = updatedTextFragments.textFragments.Select(x => x.id).IndexOf(msg.id);
			Assert.Equal(updatedTextFragments.textFragments.Count - 2, index); // Check that it is second to last
			Assert.Equal(msg.id, updatedTextFragments.textFragments[index].id);
			Assert.Equal(msg.name, updatedTextFragments.textFragments[index].name);
			Assert.Equal(numberOfTextFragments + 1, updatedTextFragments.textFragments.Count);

			// Make sure that nothing else has changed to the pre-existing text fragments
			for (var i = 0; i < textFragments.textFragments.Count; i++)
				textFragments.textFragments[i]
					.ShouldDeepEqual(
						updatedTextFragments.textFragments[
							i > updatedTextFragments.textFragments.Count - 3
								? i + 1
								: i // skip the second to last, which is the new text fragment
						]
					);

			await EditionHelpers.DeleteEdition(_client, editionId, true);
		}

		[Fact]
		public async Task CanAddTextFragmentBeforeAndAfter()
		{
			// Arrange
			var editionId = await _getClonedEdition(); // Get a newly cloned edition with text fragments
			var textFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get all the text fragments in the edition
			while (textFragments.textFragments.Count < 2) // Make sure we have at least 2 text fragments
			{
				editionId = await _getClonedEdition(); // Get a newly cloned edition with text fragments
				textFragments = await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get all the text fragments in the edition
			}

			const string textFragmentName = "my can add before and after col";
			var previousFragmentId =
				textFragments.textFragments.First().id; // We will make the new text fragment number two
			var nextFragmentId = textFragments.textFragments[1].id; // We get the next one in sequence
			var numberOfTextFragments = textFragments.textFragments.Count;

			// Act
			var (response, msg) = await _createTextFragment(
				editionId,
				textFragmentName,
				previousTextFragmentId: previousFragmentId,
				nextTextFragmentId: nextFragmentId
			);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.Empty(textFragments.textFragments.Where(x => x.id == msg.id || x.name == msg.name));
			var updatedTextFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get the updated list of text fragments in the edition
			Assert.NotEmpty(updatedTextFragments.textFragments.Where(x => x.id == msg.id));
			var index = updatedTextFragments.textFragments.Select(x => x.id).IndexOf(msg.id);
			Assert.Equal(1, index); //Index 1 would be the second position
			Assert.Equal(msg.id, updatedTextFragments.textFragments[index].id);
			Assert.Equal(msg.name, updatedTextFragments.textFragments[index].name);
			Assert.Equal(numberOfTextFragments + 1, updatedTextFragments.textFragments.Count);

			// Make sure that nothing else has changed to the pre-existing text fragments
			for (var i = 0; i < textFragments.textFragments.Count; i++)
				textFragments.textFragments[i]
					.ShouldDeepEqual(
						updatedTextFragments.textFragments[
							i > 0 ? i + 1 : i // skip index 1, which is the new text fragment
						]
					);

			await EditionHelpers.DeleteEdition(_client, editionId, true);
		}

		[Fact]
		public async Task CanAddTextFragmentToEnd()
		{
			// Arrange
			var editionId = await _getClonedEdition(); // Get a newly cloned edition with text fragments
			var textFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get all the text fragments in the edition
			const string textFragmentName = "my new can add to end col";
			var numberOfTextFragments = textFragments.textFragments.Count;

			// Act
			var (response, msg) = await _createTextFragment(
				editionId,
				textFragmentName
			);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.Empty(textFragments.textFragments.Where(x => x.id == msg.id || x.name == msg.name));
			var updatedTextFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get the updated list of text fragments in the edition
			Assert.NotEmpty(updatedTextFragments.textFragments.Where(x => x.id == msg.id));
			Assert.Equal(msg.id, updatedTextFragments.textFragments.Last().id);
			Assert.Equal(msg.name, updatedTextFragments.textFragments.Last().name);
			Assert.Equal(numberOfTextFragments + 1, updatedTextFragments.textFragments.Count);

			for (var i = 0; i < textFragments.textFragments.Count; i++)
				textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);

			await EditionHelpers.DeleteEdition(_client, editionId, true);
		}

		[Fact]
		public async Task CanGetAnonymousEditionTextFragment()
		{
			// Arrange
			var (editionId, textFragmentId) = await _getTextFragmentIds();

			// Act
			var (response, msg) = await HttpRequest.SendAsync<string, TextEditionDTO>(
				_client,
				HttpMethod.Get,
				_getTextFragments.Replace("$EditionId", editionId.ToString())
					.Replace("$TextFragmentId", textFragmentId.ToString()),
				null
			);

			// Assert
			response.EnsureSuccessStatusCode();
			_verifyTextEditionDTO(msg); // Verify we got expected data
		}

		[Fact]
		public async Task CanGetAnonymousEditionTextFragmentData()
		{
			// Arrange
			var edition = EditionHelpers.GetEdition(_client);
			var editionId = edition.Id;

			// Act
			var (response, msg) = await HttpRequest.SendAsync<string, TextFragmentDataListDTO>(
				_client,
				HttpMethod.Get,
				_getTextFragmentsData.Replace("$EditionId", editionId.ToString()),
				null
			);

			// Assert
			response.EnsureSuccessStatusCode();
		}

		[Fact]
		public async Task CanGetAnonymousEditionTextLine()
		{
			// Arrange
			var (editionId, textFragmentId, lineId) = await _getLine();

			// Act
			var (response, msg) = await HttpRequest.SendAsync<string, LineTextDTO>(
				_client,
				HttpMethod.Get,
				_getTextLines.Replace("$EditionId", editionId.ToString())
					.Replace("$LineId", lineId.ToString()),
				null
			);

			// Assert
			response.EnsureSuccessStatusCode();
			_verifyLineTextDTO(msg); // Verify we got expected data
		}

		[Fact]
		public async Task CanGetAnonymousEditionTextLineData()
		{
			// Arrange
			var (editionId, textFragmentId) = await _getTextFragmentIds();

			// Act
			var (response, msg) = await HttpRequest.SendAsync<string, LineDataListDTO>(
				_client,
				HttpMethod.Get,
				_getTextLinesData.Replace("$EditionId", editionId.ToString())
					.Replace("$TextFragmentId", textFragmentId.ToString()),
				null
			);

			// Assert
			response.EnsureSuccessStatusCode();
			Assert.NotEmpty(msg.lines);
			Assert.NotEqual((uint)0, msg.lines[0].lineId);
		}

		[Fact]
		public async Task CannotAddTextFragmentAfterTextFragmentNotInEdition()
		{
			// Arrange
			var editionId = await _getClonedEdition(); // Get a newly cloned edition with text fragments
			var textFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get all the text fragments in the edition
			const string textFragmentName = "my can add after col";
			const uint previousFragmentId = 0; // There is no text fragments 0 possible
			var numberOfTextFragments = textFragments.textFragments.Count;

			// Act
			var (response, msg) = await _createTextFragment(
				editionId,
				textFragmentName,
				previousTextFragmentId: previousFragmentId,
				shouldSucceed: false
			);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
			var updatedTextFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get the list of text fragments in the edition again
			Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

			// Make sure that nothing else has changed to the pre-existing text fragments
			for (var i = 0; i < textFragments.textFragments.Count; i++)
				textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);

			await EditionHelpers.DeleteEdition(_client, editionId, true);
		}

		[Fact]
		public async Task CannotAddTextFragmentBeforeTextFragmentNotInEdition()
		{
			// Arrange
			var editionId = await _getClonedEdition(); // Get a newly cloned edition with text fragments
			var textFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get all the text fragments in the edition
			const string textFragmentName = "my can add after col";
			const uint nextFragmentId = 0; // There is no text fragments 0 possible
			var numberOfTextFragments = textFragments.textFragments.Count;

			// Act
			var (response, msg) = await _createTextFragment(
				editionId,
				textFragmentName,
				nextTextFragmentId: nextFragmentId,
				shouldSucceed: false
			);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
			var updatedTextFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get the list of text fragments in the edition again
			Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

			// Make sure that nothing else has changed to the pre-existing text fragments
			for (var i = 0; i < textFragments.textFragments.Count; i++)
				textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);

			await EditionHelpers.DeleteEdition(_client, editionId, true);
		}

		[Fact]
		public async Task CannotAddTextFragmentBetweenNonSequentialTextFragments()
		{
			// Arrange
			var editionId = await _getClonedEdition(); // Get a newly cloned edition with text fragments
			var textFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get all the text fragments in the edition
			while (textFragments.textFragments.Count < 2) // Make sure we have at least 2 text fragments
			{
				editionId = await _getClonedEdition(); // Get a newly cloned edition with text fragments
				textFragments = await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get all the text fragments in the edition
			}

			const string textFragmentName = "my can add after col";
			var nextFragmentId = textFragments.textFragments.First().id; // We get these out of order
			var previousFragmentId = textFragments.textFragments[1].id; // We get these out of order
			var numberOfTextFragments = textFragments.textFragments.Count;

			// Act
			var (response, msg) = await _createTextFragment(
				editionId,
				textFragmentName,
				nextTextFragmentId: nextFragmentId,
				previousTextFragmentId: previousFragmentId,
				shouldSucceed: false
			);

			// Assert
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
			var updatedTextFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get the list of text fragments in the edition again
			Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

			// Make sure that nothing else has changed to the pre-existing text fragments
			for (var i = 0; i < textFragments.textFragments.Count; i++)
				textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);

			await EditionHelpers.DeleteEdition(_client, editionId, true);
		}

		[Fact]
		public async Task CannotAddTextFragmentWithoutPermission()
		{
			// Arrange
			var editionId = await _getClonedEdition(); // Get a newly cloned edition with text fragments
			const string textFragmentName = "my new can add to end col";

			// Act
			var (response, msg) = await _createTextFragment(
				editionId,
				textFragmentName,
				authenticated: false,
				shouldSucceed: false
			);

			// Assert
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

			await EditionHelpers.DeleteEdition(_client, editionId, true);
		}

		[Fact]
		public async Task CanNotAddTwoTextFragmentsWithTheSameName()
		{
			// Arrange
			var editionId = await _getClonedEdition(); // Get a newly cloned edition with text fragments
			var textFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get all the text fragments in the edition
			const string textFragmentName = "my new can add to end col";
			var numberOfTextFragments = textFragments.textFragments.Count;

			// Act
			var (response, msg) = await _createTextFragment(
				editionId,
				textFragmentName
			);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.Empty(textFragments.textFragments.Where(x => x.id == msg.id || x.name == msg.name));
			var updatedTextFragments =
				await _getEditionTextFragments(
					editionId,
					await HttpRequest.GetJWTAsync(_client)
				); // Get the updated list of text fragments in the edition
			Assert.NotEmpty(updatedTextFragments.textFragments.Where(x => x.id == msg.id));
			Assert.Equal(msg.id, updatedTextFragments.textFragments.Last().id);
			Assert.Equal(msg.name, updatedTextFragments.textFragments.Last().name);
			Assert.Equal(numberOfTextFragments + 1, updatedTextFragments.textFragments.Count);

			for (var i = 0; i < textFragments.textFragments.Count; i++)
				textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);

			// Act
			(response, msg) = await _createTextFragment(
				editionId,
				textFragmentName,
				false
			);

			// Assert
			Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
			var secondUpdateTextFragments = await _getEditionTextFragments(
				editionId,
				await HttpRequest.GetJWTAsync(_client)
			); // Get the updated list of text fragments in the edition
			updatedTextFragments.textFragments.ShouldDeepEqual(secondUpdateTextFragments.textFragments);

			await EditionHelpers.DeleteEdition(_client, editionId, true);
		}

		// TODO: authenticated retrieval and blocking of unauthorized requests
	}
}