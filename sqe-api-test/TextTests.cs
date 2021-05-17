using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.ApiTest
{
	public partial class WebControllerTest
	{
		private async Task<(uint editionId, uint textFragmentId)> _getRandomTextFragmentId(
				uint? editionId = null)
		{
			var usedEditionId = editionId ?? EditionHelpers.GetEditionId();

			var textFragmentRequestObject =
					new Get.V1_Editions_EditionId_TextFragments(usedEditionId);

			await textFragmentRequestObject.SendAsync(_client, StartConnectionAsync);

			var (fragmentsResponse, textFragments) = (
					textFragmentRequestObject.HttpResponseMessage
					, textFragmentRequestObject.HttpResponseObject);

			fragmentsResponse.EnsureSuccessStatusCode();

			return (usedEditionId, textFragmentId: textFragments.textFragments.First().id);
		}

		/// <summary>
		///  Returns a listing of text fragments for the specified edition id
		/// </summary>
		/// <param name="editionId">Id of the edition to search for text fragments</param>
		/// <param name="auth">Whether the request should be authorized with user credentials, default is false</param>
		/// <param name="user">
		///  The user object to use for authorization, if auth is true and this is unspecfied/null
		///  `Request.DefaultUsers.User1` will be used
		/// </param>
		/// <returns>All text fragments in the edition</returns>
		private async Task<TextFragmentDataListDTO> _getEditionTextFragments(
				uint                      editionId
				, bool                    auth = false
				, Request.UserAuthDetails user = null)
		{
			if (auth && (user == null))
				user = Request.DefaultUsers.User1;

			var newTextFragReqObj = new Get.V1_Editions_EditionId_TextFragments(editionId);

			await newTextFragReqObj.SendAsync(
					_client
					, StartConnectionAsync
					, requestUser: user
					, auth: auth);

			var (fragmentsResponse, textFragments) = (
					newTextFragReqObj.HttpResponseMessage, newTextFragReqObj.HttpResponseObject);

			fragmentsResponse.EnsureSuccessStatusCode();

			return textFragments;
		}

		private async Task<(uint editionId, TextFragmentDataListDTO textFragments)>
				_createEditionWithTextFragments(EditionHelpers.EditionCreator editionCreator)
		{
			// Get a newly cloned edition
			var editionId = await editionCreator.CreateEdition();

			// Get all the text fragments in the edition
			var textFragments = await _getEditionTextFragments(editionId, true);

			return (editionId, textFragments);
		}

		private async Task<(HttpResponseMessage response, TextFragmentDataDTO msg)>
				_createTextFragment(
						uint                      editionId
						, string                  textFragmentName
						, bool                    shouldSucceed          = true
						, uint?                   previousTextFragmentId = null
						, uint?                   nextTextFragmentId     = null
						, bool                    authenticated          = true
						, Request.UserAuthDetails user                   = null
						, bool                    realtime               = false)
		{
			if (authenticated && (user == null))
				user = Request.DefaultUsers.User1;

			var newTextFragmentRequestObject = new Post.V1_Editions_EditionId_TextFragments(
					editionId
					, new CreateTextFragmentDTO
					{
							previousTextFragmentId = previousTextFragmentId
							, name = textFragmentName
							, nextTextFragmentId = nextTextFragmentId
							,
					});

			// You can run this realtime or HTTP, both should be tested at least once
			if (realtime)
			{
				await newTextFragmentRequestObject.SendAsync(
						null
						, StartConnectionAsync
						, requestUser: user
						, auth: authenticated
						, shouldSucceed: shouldSucceed
						, deterministic: false);
			}
			else
			{
				await newTextFragmentRequestObject.SendAsync(
						_client
						, null
						, requestUser: user
						, auth: authenticated
						, shouldSucceed: shouldSucceed
						, deterministic: false);
			}

			var (response, msg, realtimeMsg) = (
					newTextFragmentRequestObject.HttpResponseMessage
					, newTextFragmentRequestObject.HttpResponseObject
					, newTextFragmentRequestObject.SignalrResponseObject);

			if (shouldSucceed && !realtime)
				response.EnsureSuccessStatusCode();

			return (response, realtime
							? realtimeMsg
							: msg);
		}

		private async Task<(uint editionId, uint textFragmentId, uint lineId)> _getLine()
		{
			var (editionId, textFragmentId) = await _getRandomTextFragmentId();

			var getLineDataRequestObject =
					new Get.V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(
							editionId
							, textFragmentId);

			await getLineDataRequestObject.SendAsync(_client, StartConnectionAsync);

			var (lineResponse, lines) = (
					getLineDataRequestObject.HttpResponseMessage
					, getLineDataRequestObject.HttpResponseObject);

			lineResponse.EnsureSuccessStatusCode();

			return (editionId, textFragmentId, lines.lines.First().lineId);
		}

		private static void _verifyLineTextDTO(LineTextDTO msg)
		{
			Assert.NotNull(msg.licence);
			Assert.NotEmpty(msg.editors);
			Assert.NotEqual((uint) 0, msg.lineId);
			Assert.NotEmpty(msg.signs);

			Assert.NotEqual(
					(uint) 0
					, msg.signs.First()
						 .signInterpretations.First()
						 .nextSignInterpretations.First()
						 .nextSignInterpretationId);

			Assert.NotEmpty(msg.signs.First().signInterpretations);

			Assert.NotEqual(
					(uint) 0
					, msg.signs.First().signInterpretations.First().signInterpretationId);

			Assert.NotEmpty(msg.signs.First().signInterpretations.First().attributes);

			Assert.NotEqual(
					(uint) 0
					, msg.signs.First()
						 .signInterpretations.First()
						 .attributes.First()
						 .interpretationAttributeId);

			Assert.NotNull(
					msg.signs.First()
					   .signInterpretations.First()
					   .attributes.First()
					   .attributeValueString);

			Assert.NotNull(
					msg.signs.First()
					   .signInterpretations.First()
					   .attributes.First()
					   .attributeString);

			var editorIds = new List<uint> { msg.editorId };

			foreach (var sign in msg.signs)
			foreach (var signInterpretation in sign.signInterpretations)
			{
				Assert.NotEqual(0u, signInterpretation.signId);

				foreach (var attr in signInterpretation.attributes)
				{
					if (!msg.editors.ContainsKey(attr.editorId.ToString()))
						editorIds.Add(attr.editorId);
				}

				foreach (var roi in signInterpretation.rois)
				{
					if (!msg.editors.ContainsKey(roi.editorId.ToString()))
						editorIds.Add(roi.editorId);
				}

				foreach (var nexSign in signInterpretation.nextSignInterpretations)
				{
					if (!msg.editors.ContainsKey(nexSign.editorId.ToString()))
						editorIds.Add(nexSign.editorId);
				}
			}

			Assert.NotEmpty(editorIds);

			foreach (var editorId in editorIds)
				Assert.True(msg.editors.ContainsKey(editorId.ToString()));
		}

		private static void _verifyTextEditionDTO(TextEditionDTO msg)
		{
			Assert.NotNull(msg.licence);
			Assert.NotEmpty(msg.editors);
			Assert.NotEqual((uint) 0, msg.manuscriptId);
			Assert.NotEmpty(msg.textFragments);

			Assert.NotEqual((uint) 0, msg.textFragments.First().textFragmentId);

			Assert.NotEmpty(msg.textFragments.First().lines);

			Assert.NotEqual((uint) 0, msg.textFragments.First().lines.First().lineId);

			Assert.NotEmpty(msg.textFragments.First().lines.First().signs);

			Assert.NotEqual(
					(uint) 0
					, msg.textFragments.First()
						 .lines.First()
						 .signs.First()
						 .signInterpretations.First()
						 .nextSignInterpretations.First()
						 .nextSignInterpretationId);

			Assert.NotEmpty(
					msg.textFragments.First().lines.First().signs.First().signInterpretations);

			Assert.NotEqual(
					(uint) 0
					, msg.textFragments.First()
						 .lines.First()
						 .signs.First()
						 .signInterpretations.First()
						 .signInterpretationId);

			Assert.NotEmpty(
					msg.textFragments.First()
					   .lines.First()
					   .signs.First()
					   .signInterpretations.First()
					   .attributes);

			Assert.NotEqual(
					(uint) 0
					, msg.textFragments.First()
						 .lines.First()
						 .signs.First()
						 .signInterpretations.First()
						 .attributes.First()
						 .interpretationAttributeId);

			Assert.NotNull(
					msg.textFragments.First()
					   .lines.First()
					   .signs.First()
					   .signInterpretations.First()
					   .attributes.First()
					   .attributeValueString);

			Assert.NotNull(
					msg.textFragments.First()
					   .lines.First()
					   .signs.First()
					   .signInterpretations.First()
					   .attributes.First()
					   .attributeString);

			var editorIds = new List<uint> { msg.editorId };

			foreach (var textFragment in msg.textFragments)
			{
				if (!msg.editors.ContainsKey(textFragment.editorId.ToString()))
					editorIds.Add(textFragment.editorId);

				foreach (var line in textFragment.lines)
				{
					if (!msg.editors.ContainsKey(line.editorId.ToString()))
						editorIds.Add(line.editorId);

					foreach (var sign in line.signs)
					foreach (var signInterpretation in sign.signInterpretations)
					{
						Assert.NotEqual(0u, signInterpretation.signId);

						foreach (var attr in signInterpretation.attributes)
						{
							if (!msg.editors.ContainsKey(attr.editorId.ToString()))
								editorIds.Add(attr.editorId);
						}

						foreach (var roi in signInterpretation.rois)
						{
							if (!msg.editors.ContainsKey(roi.editorId.ToString()))
								editorIds.Add(roi.editorId);
						}

						foreach (var nexSign in signInterpretation.nextSignInterpretations)
						{
							if (!msg.editors.ContainsKey(nexSign.editorId.ToString()))
								editorIds.Add(nexSign.editorId);
						}
					}
				}
			}

			Assert.NotEmpty(editorIds);

			foreach (var editorId in editorIds)
				Assert.True(msg.editors.ContainsKey(editorId.ToString()));
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanAddTextFragmentAfter()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				const string textFragmentName = "my can add after col";

				var previousFragmentId =
						textFragments.textFragments.First()
									 .id; // We will make the new text fragment number two

				var numberOfTextFragments = textFragments.textFragments.Count;

				// Act
				var newTextFragmentRequestObject = new Post.V1_Editions_EditionId_TextFragments(
						editionId
						, new CreateTextFragmentDTO
						{
								previousTextFragmentId = previousFragmentId
								, name = textFragmentName
								, nextTextFragmentId = null
								,
						});

				await newTextFragmentRequestObject.SendAsync(
						_client
						, null
						, true
						, deterministic: false);

				var (response, msg) = (newTextFragmentRequestObject.HttpResponseMessage
									   , newTextFragmentRequestObject.HttpResponseObject);

				// Assert
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);

				Assert.Empty(
						textFragments.textFragments.Where(
								x => (x.id == msg.id) || (x.name == msg.name)));

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the updated list of text fragments in the edition

				Assert.NotEmpty(updatedTextFragments.textFragments.Where(x => x.id == msg.id));

				var index = updatedTextFragments.textFragments.Select(x => x.id)
												.ToList()
												.IndexOf(msg.id);

				Assert.Equal(1, index); //Index 1 would be the second position

				Assert.Equal(msg.id, updatedTextFragments.textFragments[index].id);

				Assert.Equal(msg.name, updatedTextFragments.textFragments[index].name);

				Assert.Equal(numberOfTextFragments + 1, updatedTextFragments.textFragments.Count);

				// Make sure that nothing else has changed to the pre-existing text fragments
				for (var i = 0; i < textFragments.textFragments.Count; i++)
				{
					textFragments.textFragments[i]
								 .ShouldDeepEqual(
										 updatedTextFragments.textFragments[i > 0
																					? i + 1
																					: i // skip index 1, which is the new text fragment
												 ]);
				}
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanAddTextFragmentBefore()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				const string textFragmentName = "my can add before col";

				var nextFragmentId =
						textFragments.textFragments.Last()
									 .id; // We will make the new text fragment second to last

				var numberOfTextFragments = textFragments.textFragments.Count;

				// Act
				var newTextFragmentRequestObject = new Post.V1_Editions_EditionId_TextFragments(
						editionId
						, new CreateTextFragmentDTO
						{
								previousTextFragmentId = null
								, name = textFragmentName
								, nextTextFragmentId = nextFragmentId
								,
						});

				await newTextFragmentRequestObject.SendAsync(
						_client
						, null
						, true
						, deterministic: false);

				var (response, msg) = (newTextFragmentRequestObject.HttpResponseMessage
									   , newTextFragmentRequestObject.HttpResponseObject);

				// Assert
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);

				Assert.Empty(
						textFragments.textFragments.Where(
								x => (x.id == msg.id) || (x.name == msg.name)));

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the updated list of text fragments in the edition

				Assert.NotEmpty(updatedTextFragments.textFragments.Where(x => x.id == msg.id));

				var index = updatedTextFragments.textFragments.Select(x => x.id)
												.ToList()
												.IndexOf(msg.id);

				Assert.Equal(
						updatedTextFragments.textFragments.Count - 2
						, index); // Check that it is second to last

				Assert.Equal(msg.id, updatedTextFragments.textFragments[index].id);

				Assert.Equal(msg.name, updatedTextFragments.textFragments[index].name);

				Assert.Equal(numberOfTextFragments + 1, updatedTextFragments.textFragments.Count);

				// Make sure that nothing else has changed to the pre-existing text fragments
				for (var i = 0; i < textFragments.textFragments.Count; i++)
				{
					textFragments.textFragments[i]
								 .ShouldDeepEqual(
										 updatedTextFragments.textFragments[
														 i
														 > (updatedTextFragments.textFragments.Count
															- 3)
																 ? i + 1
																 : i // skip the second to last, which is the new text fragment
												 ]);
				}
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanAddTextFragmentBeforeAndAfter()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				{
					const string textFragmentName = "my can add before and after col";

					var previousFragmentId =
							textFragments.textFragments.First()
										 .id; // We will make the new text fragment number two

					var nextFragmentId =
							textFragments.textFragments[1].id; // We get the next one in sequence

					var numberOfTextFragments = textFragments.textFragments.Count;

					// Act
					var (response, msg) = await _createTextFragment(
							editionId
							, textFragmentName
							, previousTextFragmentId: previousFragmentId
							, nextTextFragmentId: nextFragmentId);

					// Assert
					Assert.Equal(HttpStatusCode.OK, response.StatusCode);

					Assert.Empty(
							textFragments.textFragments.Where(
									x => (x.id == msg.id) || (x.name == msg.name)));

					var updatedTextFragments =
							await _getEditionTextFragments(
									editionId
									, true); // Get the updated list of text fragments in the edition

					Assert.NotEmpty(updatedTextFragments.textFragments.Where(x => x.id == msg.id));

					var index = updatedTextFragments.textFragments.Select(x => x.id)
													.ToList()
													.IndexOf(msg.id);

					Assert.Equal(1, index); //Index 1 would be the second position

					Assert.Equal(msg.id, updatedTextFragments.textFragments[index].id);

					Assert.Equal(msg.name, updatedTextFragments.textFragments[index].name);

					Assert.Equal(
							numberOfTextFragments + 1
							, updatedTextFragments.textFragments.Count);

					// Make sure that nothing else has changed to the pre-existing text fragments
					for (var i = 0; i < textFragments.textFragments.Count; i++)
					{
						textFragments.textFragments[i]
									 .ShouldDeepEqual(
											 updatedTextFragments.textFragments[i > 0
																						? i + 1
																						: i // skip index 1, which is the new text fragment
													 ]);
					}
				}
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanAddTextFragmentToEnd()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				const string textFragmentName = "my new can add to end col";
				var numberOfTextFragments = textFragments.textFragments.Count;

				// Act
				var (_, msg) = await _createTextFragment(
						editionId
						, textFragmentName
						, realtime: true);

				// Assert
				Assert.Empty(
						textFragments.textFragments.Where(
								x => (x.id == msg.id) || (x.name == msg.name)));

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the updated list of text fragments in the edition

				Assert.NotEmpty(updatedTextFragments.textFragments.Where(x => x.id == msg.id));

				Assert.Equal(msg.id, updatedTextFragments.textFragments.Last().id);

				Assert.Equal(msg.name, updatedTextFragments.textFragments.Last().name);

				Assert.Equal(numberOfTextFragments + 1, updatedTextFragments.textFragments.Count);

				for (var i = 0; i < textFragments.textFragments.Count; i++)
				{
					textFragments.textFragments[i]
								 .ShouldDeepEqual(updatedTextFragments.textFragments[i]);
				}
			}
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		[Trait("Category", "Text")]
		public async Task CanGetEditionFullText(bool realtime)
		{
			// Arrange
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, _) = await _createEditionWithTextFragments(editionCreator);

				// Act
				var textRequest1 = new Get.V1_Editions_EditionId_FullText(editionId);

				await textRequest1.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime);

				// Assert
				if (!realtime)
					textRequest1.HttpResponseMessage.EnsureSuccessStatusCode();

				var resp1 = realtime
						? textRequest1.SignalrResponseObject
						: textRequest1.HttpResponseObject;

				Assert.NotEmpty(resp1.textFragments);
				Assert.False(resp1.textFragments.Count == 1);

				_verifyTextEditionDTO(resp1);

				// Act (check that cached data is valid)
				var textRequest2 = new Get.V1_Editions_EditionId_FullText(editionId);

				await textRequest2.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime);

				// Assert
				if (!realtime)
					textRequest2.HttpResponseMessage.EnsureSuccessStatusCode();

				var resp2 = realtime
						? textRequest2.SignalrResponseObject
						: textRequest2.HttpResponseObject;

				resp2.IsDeepEqual(resp1);
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanGetAnonymousEditionTextFragment()
		{
			// Arrange
			var (editionId, textFragmentId) = await _getRandomTextFragmentId();

			// Act
			var textFragmentRequestObject =
					new Get.V1_Editions_EditionId_TextFragments_TextFragmentId(
							editionId
							, textFragmentId);

			await textFragmentRequestObject.SendAsync(_client, StartConnectionAsync);

			var (response, msg) = (textFragmentRequestObject.HttpResponseMessage
								   , textFragmentRequestObject.HttpResponseObject);

			// Assert
			response.EnsureSuccessStatusCode();
			_verifyTextEditionDTO(msg); // Verify we got expected data
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanGetAnonymousArtefactsOfTextFragment()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var newEdition = await editionCreator.CreateEdition(); // Clone new edition

				var (artefactId, _) = await RoiHelpers.CreateRoiInEdition(
						_client
						, StartConnectionAsync
						, newEdition);

				var scriptRequest = new Get.V1_Editions_EditionId_ScriptLines(newEdition);

				await scriptRequest.SendAsync(_client, StartConnectionAsync, true);

				var textFragmentId = scriptRequest.HttpResponseObject.textFragments.First()
												  .textFragmentId;

				// Act
				var request =
						new Get.V1_Editions_EditionId_TextFragments_TextFragmentId_Artefacts(
								newEdition
								, textFragmentId);

				await request.SendAsync(_client, StartConnectionAsync, true);

				// assert
				request.HttpResponseObject.ShouldDeepEqual(request.HttpResponseObject);

				Assert.Contains(request.HttpResponseObject.artefacts, x => x.id == artefactId);
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanGetAnonymousEditionTextFragmentData()
		{
			// Arrange
			var edition = await EditionHelpers.GetEdition(_client);
			var editionId = edition.id;

			// Act
			var textFragmentDataRequestObject =
					new Get.V1_Editions_EditionId_TextFragments(editionId);

			await textFragmentDataRequestObject.SendAsync(_client, StartConnectionAsync);

			// Assert
			textFragmentDataRequestObject.HttpResponseMessage.EnsureSuccessStatusCode();
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanGetAnonymousEditionTextLine()
		{
			// Arrange
			var (editionId, _, lineId) = await _getLine();

			// Act
			var textLineRequestObject =
					new Get.V1_Editions_EditionId_Lines_LineId(editionId, lineId);

			await textLineRequestObject.SendAsync(_client, StartConnectionAsync);

			var (response, msg) = (textLineRequestObject.HttpResponseMessage
								   , textLineRequestObject.HttpResponseObject);

			// Assert
			response.EnsureSuccessStatusCode();
			_verifyLineTextDTO(msg); // Verify we got expected data
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanGetAnonymousEditionTextLineData()
		{
			// Arrange
			var (editionId, textFragmentId) = await _getRandomTextFragmentId();

			// Act
			var lineDataRequestObject =
					new Get.V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(
							editionId
							, textFragmentId);

			await lineDataRequestObject.SendAsync(_client, StartConnectionAsync);

			var (response, msg) = (lineDataRequestObject.HttpResponseMessage
								   , lineDataRequestObject.HttpResponseObject);

			// Assert
			response.EnsureSuccessStatusCode();
			Assert.NotEmpty(msg.lines);
			Assert.NotEqual((uint) 0, msg.lines[0].lineId);
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanMoveTextFragmentAfter()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				// Act
				var newTextFragmentRequestObject =
						new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
								editionId
								, textFragments.textFragments.First().id
								, new UpdateTextFragmentDTO
								{
										previousTextFragmentId =
												textFragments.textFragments.Last().id
										, name = null
										, nextTextFragmentId = null
										,
								});

				await newTextFragmentRequestObject.SendAsync(
						_client
						, null
						, true
						, deterministic: false);

				var (response, _) = (newTextFragmentRequestObject.HttpResponseMessage
									 , newTextFragmentRequestObject.HttpResponseObject);

				// Assert
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the updated list of text fragments in the edition

				// Check that nothing has changed for the moved text fragment
				textFragments.textFragments.First()
							 .ShouldDeepEqual(updatedTextFragments.textFragments.Last());

				// Make sure that nothing else has changed to the pre-existing text fragments
				var originalShifted = textFragments.textFragments.Skip(1);

				var updatedShifted =
						updatedTextFragments.textFragments.Take(
								updatedTextFragments.textFragments.Count - 1);

				originalShifted.ShouldDeepEqual(updatedShifted);
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanMoveTextFragmentBefore()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				// Act
				var newTextFragmentRequestObject =
						new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
								editionId
								, textFragments.textFragments.Last().id
								, new UpdateTextFragmentDTO
								{
										previousTextFragmentId = null
										, name = null
										, nextTextFragmentId = textFragments.textFragments
																			.First()
																			.id
										,
								});

				await newTextFragmentRequestObject.SendAsync(
						_client
						, null
						, true
						, deterministic: false);

				// Assert
				Assert.Equal(
						HttpStatusCode.OK
						, newTextFragmentRequestObject.HttpResponseMessage.StatusCode);

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the updated list of text fragments in the edition

				// Check that nothing has changed for the moved text fragment
				textFragments.textFragments.Last()
							 .ShouldDeepEqual(updatedTextFragments.textFragments.First());

				// Make sure that nothing else has changed to the pre-existing text fragments
				var originalShifted =
						textFragments.textFragments.Take(textFragments.textFragments.Count - 1);

				var updatedShifted = updatedTextFragments.textFragments.Skip(1);
				originalShifted.ShouldDeepEqual(updatedShifted);
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanMoveTextFragmentBeforeAndAfter()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				// Act
				var newTextFragmentRequestObject =
						new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
								editionId
								, textFragments.textFragments.First().id
								, new UpdateTextFragmentDTO
								{
										previousTextFragmentId =
												textFragments.textFragments[1].id
										, name = null
										, nextTextFragmentId =
												textFragments.textFragments[2].id
										,
								});

				await newTextFragmentRequestObject.SendAsync(
						_client
						, null
						, true
						, deterministic: false);

				// Assert
				Assert.Equal(
						HttpStatusCode.OK
						, newTextFragmentRequestObject.HttpResponseMessage.StatusCode);

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the updated list of text fragments in the edition

				// Check that nothing has changed for the moved text fragment
				textFragments.textFragments.First()
							 .ShouldDeepEqual(updatedTextFragments.textFragments[1]);

				// Make sure that nothing else has changed to the pre-existing text fragments
				var originalShifted = textFragments.textFragments.Skip(1).ToList();

				var updatedShifted = updatedTextFragments.textFragments.Take(1)
														 .ToList()
														 .Concat(
																 updatedTextFragments.textFragments
																					 .Skip(2)
																					 .ToList());

				originalShifted.ShouldDeepEqual(updatedShifted);
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CanMoveTextFragmentBetweenNonsequentialTextFragments()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				// Act
				var newTextFragmentRequestObject =
						new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
								editionId
								, textFragments.textFragments.First().id
								, new UpdateTextFragmentDTO
								{
										previousTextFragmentId =
												textFragments.textFragments.Last().id
										, name = null
										, nextTextFragmentId = textFragments.textFragments
																			.First()
																			.id
										,
								});

				await newTextFragmentRequestObject.SendAsync(
						_client
						, null
						, true
						, deterministic: false
						, shouldSucceed: false);

				// Assert
				Assert.Equal(
						HttpStatusCode.BadRequest
						, newTextFragmentRequestObject.HttpResponseMessage.StatusCode);

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the list of text fragments in the edition again

				textFragments.ShouldDeepEqual(updatedTextFragments);
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CannotAddTextFragmentAfterTextFragmentNotInEdition()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				const string textFragmentName = "my can add after col";

				const uint previousFragmentId = 0; // There is no text fragments 0 possible

				var numberOfTextFragments = textFragments.textFragments.Count;

				// Act
				var (response, _) = await _createTextFragment(
						editionId
						, textFragmentName
						, previousTextFragmentId: previousFragmentId
						, shouldSucceed: false);

				// Assert
				Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the list of text fragments in the edition again

				Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

				// Make sure that nothing else has changed to the pre-existing text fragments
				for (var i = 0; i < textFragments.textFragments.Count; i++)
				{
					textFragments.textFragments[i]
								 .ShouldDeepEqual(updatedTextFragments.textFragments[i]);
				}
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CannotAddTextFragmentBeforeTextFragmentNotInEdition()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				const string textFragmentName = "my can add after col";

				const uint nextFragmentId = 0; // There is no text fragments 0 possible

				var numberOfTextFragments = textFragments.textFragments.Count;

				// Act
				var (response, _) = await _createTextFragment(
						editionId
						, textFragmentName
						, nextTextFragmentId: nextFragmentId
						, shouldSucceed: false);

				// Assert
				Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the list of text fragments in the edition again

				Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

				// Make sure that nothing else has changed to the pre-existing text fragments
				for (var i = 0; i < textFragments.textFragments.Count; i++)
				{
					textFragments.textFragments[i]
								 .ShouldDeepEqual(updatedTextFragments.textFragments[i]);
				}
			}
		}

		// It is probably best from the perspective of the API consumer that this test should pass.
		[Fact]
		[Trait("Category", "Text")]
		public async Task CannotAddTextFragmentBetweenNonSequentialTextFragments()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				const string textFragmentName = "my can add after col";

				var nextFragmentId =
						textFragments.textFragments.First().id; // We get these out of order

				var previousFragmentId =
						textFragments.textFragments[1].id; // We get these out of order

				var numberOfTextFragments = textFragments.textFragments.Count;

				// Act
				var (response, _) = await _createTextFragment(
						editionId
						, textFragmentName
						, nextTextFragmentId: nextFragmentId
						, previousTextFragmentId: previousFragmentId
						, shouldSucceed: false);

				// Assert
				Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the list of text fragments in the edition again

				Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

				// Make sure that nothing else has changed to the pre-existing text fragments
				for (var i = 0; i < textFragments.textFragments.Count; i++)
				{
					textFragments.textFragments[i]
								 .ShouldDeepEqual(updatedTextFragments.textFragments[i]);
				}
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CannotAddTextFragmentWithBlankName()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				const string textFragmentName = "";

				const uint previousFragmentId = 0; // There is no text fragments 0 possible

				var numberOfTextFragments = textFragments.textFragments.Count;

				// Act
				var (response, _) = await _createTextFragment(
						editionId
						, textFragmentName
						, previousTextFragmentId: previousFragmentId
						, shouldSucceed: false);

				// Assert
				Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the list of text fragments in the edition again

				Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

				// Make sure that nothing else has changed to the pre-existing text fragments
				for (var i = 0; i < textFragments.textFragments.Count; i++)
				{
					textFragments.textFragments[i]
								 .ShouldDeepEqual(updatedTextFragments.textFragments[i]);
				}
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CannotAddTextFragmentWithNullName()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				const string textFragmentName = null;

				const uint previousFragmentId = 0; // There is no text fragments 0 possible

				var numberOfTextFragments = textFragments.textFragments.Count;

				// Act
				var (response, _) = await _createTextFragment(
						editionId
						, textFragmentName
						, previousTextFragmentId: previousFragmentId
						, shouldSucceed: false);

				// Assert
				Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the list of text fragments in the edition again

				Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

				// Make sure that nothing else has changed to the pre-existing text fragments
				for (var i = 0; i < textFragments.textFragments.Count; i++)
				{
					textFragments.textFragments[i]
								 .ShouldDeepEqual(updatedTextFragments.textFragments[i]);
				}
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CannotAddTextFragmentWithoutPermission()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, _) = await _createEditionWithTextFragments(editionCreator);

				const string textFragmentName = "my new can add to end col";

				// Act
				var (response, _) = await _createTextFragment(
						editionId
						, textFragmentName
						, authenticated: false
						, shouldSucceed: false);

				// Assert
				Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CannotMoveTextFragmentAfterTextFragmentNotInEdition()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				// Act
				var newTextFragmentRequestObject =
						new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
								editionId
								, textFragments.textFragments.First().id
								, new UpdateTextFragmentDTO
								{
										previousTextFragmentId = 0
										, name = null
										, nextTextFragmentId = null
										,
								});

				await newTextFragmentRequestObject.SendAsync(
						_client
						, null
						, true
						, deterministic: false
						, shouldSucceed: false);

				// Assert
				Assert.Equal(
						HttpStatusCode.BadRequest
						, newTextFragmentRequestObject.HttpResponseMessage.StatusCode);

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the list of text fragments in the edition again

				textFragments.ShouldDeepEqual(updatedTextFragments);
			}
		}

		[Fact]
		[Trait("Category", "Text")]
		public async Task CannotMoveTextFragmentBeforeTextFragmentNotInEdition()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				// Act
				var newTextFragmentRequestObject =
						new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
								editionId
								, textFragments.textFragments.First().id
								, new UpdateTextFragmentDTO
								{
										previousTextFragmentId = null
										, name = null
										, nextTextFragmentId = 0
										,
								});

				await newTextFragmentRequestObject.SendAsync(
						_client
						, null
						, true
						, deterministic: false
						, shouldSucceed: false);

				// Assert
				Assert.Equal(
						HttpStatusCode.BadRequest
						, newTextFragmentRequestObject.HttpResponseMessage.StatusCode);

				var updatedTextFragments =
						await _getEditionTextFragments(
								editionId
								, true); // Get the list of text fragments in the edition again

				textFragments.ShouldDeepEqual(updatedTextFragments);
			}
		}

		[Theory]
		[Trait("Category", "Text")]
		[InlineData(true)]
		[InlineData(false)]
		public async Task CanRenameLine(bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				var lines = await _getEditionTextFragmentLines(
						editionId
						, textFragments.textFragments.First().id
						, realtime
						, true);

				var firstLine = lines.lines.First();

				var firstLineId = firstLine.lineId;
				const string newLineName = "2aα";
				var newLine = new UpdateLineDTO { lineName = newLineName };

				// Act
				var changeLineName =
						new Put.V1_Editions_EditionId_Lines_LineId(editionId, firstLineId, newLine);

				await changeLineName.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime
						, listeningFor: changeLineName.AvailableListeners.UpdatedLine);

				// Assert
				if (!realtime)
					changeLineName.HttpResponseMessage.EnsureSuccessStatusCode();

				var response = realtime
						? changeLineName.SignalrResponseObject
						: changeLineName.HttpResponseObject;

				Assert.NotEqual(firstLine.lineName, response.lineName);
				Assert.Equal(newLineName, response.lineName);

				var updatedLines = await _getEditionTextFragmentLines(
						editionId
						, textFragments.textFragments.First().id
						, realtime
						, true);

				Assert.Equal(response.lineName, updatedLines.lines.First().lineName);
				Assert.Equal(response.editorId, updatedLines.lines.First().editorId);
				Assert.Equal(response.lineId, updatedLines.lines.First().lineId);
			}
		}

		[Theory]
		[Trait("Category", "Text")]
		[InlineData(true)]
		[InlineData(false)]
		public async Task CanPrependLine(bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				var textFragmentId = textFragments.textFragments.First().id;

				var lines = await _getEditionTextFragmentLines(
						editionId
						, textFragmentId
						, realtime
						, true);

				var firstLine = lines.lines.First();

				var firstLineId = firstLine.lineId;
				const string newLineName = "5643";
				var newLine = new CreateLineDTO(newLineName, null, firstLineId);

				// Act
				var createLine =
						new Post.V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(
								editionId
								, textFragmentId
								, newLine);

				await createLine.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime
						, listeningFor: createLine.AvailableListeners.CreatedLine);

				// Assert
				if (!realtime)
					createLine.HttpResponseMessage.EnsureSuccessStatusCode();

				var response = realtime
						? createLine.SignalrResponseObject
						: createLine.HttpResponseObject;

				Assert.Equal(newLineName, response.lineName);

				var updatedLines = await _getEditionTextFragmentLines(
						editionId
						, textFragmentId
						, realtime
						, true);

				var newFirstLine = updatedLines.lines.First();

				// The old first line should now be second
				lines.lines[0].ShouldDeepEqual(updatedLines.lines[1]);
				Assert.NotEqual(firstLine.lineId, newFirstLine.lineId);

				// The new first line should match the newly created line
				Assert.Equal(response.lineId, newFirstLine.lineId);
				Assert.Equal(response.lineName, newFirstLine.lineName);
				Assert.Equal(response.editorId, newFirstLine.editorId);
			}
		}

		[Theory]
		[Trait("Category", "Text")]
		[InlineData(true)]
		[InlineData(false)]
		public async Task CanAppendLine(bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				var textFragmentId = textFragments.textFragments.First().id;

				var lines = await _getEditionTextFragmentLines(
						editionId
						, textFragmentId
						, realtime
						, true);

				var lastLine = lines.lines.Last();

				var lastLineId = lastLine.lineId;
				const string newLineName = "klo";
				var newLine = new CreateLineDTO(newLineName, lastLineId);

				// Act
				var createLine =
						new Post.V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(
								editionId
								, textFragmentId
								, newLine);

				await createLine.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime
						, listeningFor: createLine.AvailableListeners.CreatedLine);

				// Assert
				if (!realtime)
					createLine.HttpResponseMessage.EnsureSuccessStatusCode();

				var response = realtime
						? createLine.SignalrResponseObject
						: createLine.HttpResponseObject;

				Assert.Equal(newLineName, response.lineName);

				var updatedLines = await _getEditionTextFragmentLines(
						editionId
						, textFragmentId
						, realtime
						, true);

				var newLastLine = updatedLines.lines.Last();

				// The old first line should now be second
				lines.lines.Last().ShouldDeepEqual(updatedLines.lines[^2]);
				Assert.NotEqual(lastLine.lineId, newLastLine.lineId);

				// The new first line should match the newly created line
				Assert.Equal(response.lineId, newLastLine.lineId);
				Assert.Equal(response.lineName, newLastLine.lineName);
				Assert.Equal(response.editorId, newLastLine.editorId);
			}
		}

		[Theory]
		[Trait("Category", "Text")]
		[InlineData(true)]
		[InlineData(false)]
		public async Task CanDeleteLine(bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				var textFragmentId = textFragments.textFragments.First().id;

				var lines = await _getEditionTextFragmentLines(
						editionId
						, textFragmentId
						, realtime
						, true);

				var lastLine = lines.lines.Last();

				var lastLineId = lastLine.lineId;

				// Act
				var deleteLine =
						new Delete.V1_Editions_EditionId_Lines_LineId(editionId, lastLineId);

				await deleteLine.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime
						, listeningFor: deleteLine.AvailableListeners.DeletedLine);

				// Assert
				if (!realtime)
					deleteLine.HttpResponseMessage.EnsureSuccessStatusCode();

				Assert.NotEmpty(deleteLine.DeletedLine.ids);
				Assert.Contains(lastLineId, deleteLine.DeletedLine.ids);

				var updatedLines = await _getEditionTextFragmentLines(
						editionId
						, textFragmentId
						, realtime
						, true);

				Assert.NotEqual(lines.lines.Count, updatedLines.lines.Count);
				Assert.Equal(lines.lines.Count - 1, updatedLines.lines.Count);
				Assert.DoesNotContain(updatedLines.lines, x => x.lineId == lastLineId);
			}
		}

		private async Task<LineDataListDTO> _getEditionTextFragmentLines(
				uint   editionId
				, uint textFragmentId
				, bool realtime
				, bool auth          = false
				, bool shouldSucceed = true)
		{
			var linesRequest =
					new Get.V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(
							editionId
							, textFragmentId);

			await linesRequest.SendAsync(
					realtime
							? null
							: _client
					, StartConnectionAsync
					, auth
					, shouldSucceed: shouldSucceed
					, requestRealtime: realtime);

			if (!shouldSucceed)
				return null;

			return realtime
					? linesRequest.SignalrResponseObject
					: linesRequest.HttpResponseObject;
		}

		[Theory]
		[InlineData(true, "לא אבה ללכת")]
		[InlineData(true, "")]
		[InlineData(false, "לא אבה ללכת")]
		[InlineData(false, "")]
		public async Task CanUpdateTextChunk(bool realtime, string replacementString)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var (editionId, textFragments) =
						await _createEditionWithTextFragments(editionCreator);

				var textFragmentId = textFragments.textFragments.First().id;

				var textRequest =
						new Get.V1_Editions_EditionId_TextFragments_TextFragmentId(
								editionId
								, textFragmentId);

				await textRequest.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime);

				var text = realtime
						? textRequest.SignalrResponseObject
						: textRequest.HttpResponseObject;

				var firstSignInterpretationId = text.textFragments.First()
													.lines.First()
													.signs.First()
													.signInterpretations.Last()
													.signInterpretationId;

				var lastSignInterpretationId = text.textFragments.First()
												   .lines.First()
												   .signs.Last()
												   .signInterpretations.First()
												   .signInterpretationId;

				var requestObject = new DiffReplaceRequestDTO
				{
						priorSignInterpretationId = firstSignInterpretationId
						, followingSignInterpretationId = lastSignInterpretationId
						, newText = replacementString
						,
				};

				// Act
				var diffRequest =
						new Put.V1_Editions_EditionId_DiffReplaceText(editionId, requestObject);

				await diffRequest.SendAsync(
						new List<ListenerMethods>
						{
								diffRequest.AvailableListeners.CreatedSignInterpretation
								, diffRequest.AvailableListeners.DeletedSignInterpretation
								, diffRequest.AvailableListeners.UpdatedSignInterpretations
								,
						}
						, realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime);

				var diffResponse = realtime
						? diffRequest.SignalrResponseObject
						: diffRequest.HttpResponseObject;

				// Assert
				diffResponse.created.ShouldDeepEqual(diffRequest.CreatedSignInterpretation);
				diffResponse.updated.ShouldDeepEqual(diffRequest.UpdatedSignInterpretations);
				diffResponse.deleted.ShouldDeepEqual(diffRequest.DeletedSignInterpretation);

				Assert.True(
						diffResponse.created.signInterpretations.Any()
						|| diffResponse.updated.signInterpretations.Any()
						|| diffResponse.deleted.ids.Any());

				Assert.Equal(EditionEntities.signInterpretation, diffResponse.deleted.entity);

				var updatedTextRequest =
						new Get.V1_Editions_EditionId_TextFragments_TextFragmentId(
								editionId
								, textFragmentId);

				await updatedTextRequest.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime);

				var updatedText = realtime
						? updatedTextRequest.SignalrResponseObject
						: updatedTextRequest.HttpResponseObject;

				Assert.False(updatedText.IsDeepEqual(text));

				// Build the returned text string for comparison
				var updatedString = "";

				var spacingAttributes = new List<uint>
				{
						2
						, 3
						, 4
						,
				};

				foreach (var sign in updatedText.textFragments.First()
												.lines.First()
												.signs.SelectMany(x => x.signInterpretations))
				{
					updatedString += sign.character;

					if (sign.attributes.Any(x => spacingAttributes.Contains(x.attributeValueId)))
						updatedString += " ";
				}

				Assert.Equal(
						replacementString
						, updatedString.Substring(0, replacementString.Length));
			}
		}

		[Theory]
		[InlineData(true, "לא אבה ללכת")]
		[InlineData(true, "")]
		[InlineData(false, "לא אבה ללכת")]
		[InlineData(false, "")]
		public async Task CanUpdateVirtualArtefactTextChunk(bool realtime, string replacementString)
		{
			const uint startingEditionId = 899;
			const uint artefactId = 27991;
			const uint textFragmentId = 10166;

			using (var editionCreator = new EditionHelpers.EditionCreator(
					_client
					, StartConnectionAsync
					, startingEditionId))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var textRequest =
						new Get.V1_Editions_EditionId_TextFragments_TextFragmentId(
								editionId
								, textFragmentId);

				await textRequest.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime);

				var text = realtime
						? textRequest.SignalrResponseObject
						: textRequest.HttpResponseObject;

				var textRois = new List<IndexedReplacementTextRoi>();

				for (var i = 0; i < replacementString.Length; i++)
				{
					if (replacementString[i] == ' ')
						continue;

					var roi = new SetReconstructedInterpretationRoiDTO
					{
							shape = "POLYGON((0 0,10 0,10 10,0 10,0 0))"
							, translate = new TranslateDTO
							{
									x = 604 - (i * 10) - 10
									, y = 10
									,
							}
							,
					};

					textRois.Add(new IndexedReplacementTextRoi { index = (uint) i, roi = roi });
				}

				var requestObject = new DiffReplaceReconstructionRequestDTO
				{
						virtualArtefactShape = "POLYGON((0 0,604 0,604 200,0 200,0 0))"
						, virtualArtefactPlacement = new PlacementDTO
						{
								mirrored = false
								, scale = 1
								, rotate = 0
								, zIndex = 0
								, translate = new TranslateDTO
								{
										x = 42644
										, y = 400
										,
								}
								,
						}
						, newText = replacementString
						, textRois = textRois
						,
				};

				// Act
				var diffRequest =
						new Put.V1_Editions_EditionId_Artefacts_ArtefactId_DiffReplaceTranscription(
								editionId
								, artefactId
								, requestObject);

				await diffRequest.SendAsync(
						new List<ListenerMethods>
						{
								diffRequest.AvailableListeners.CreatedSignInterpretation
								, diffRequest.AvailableListeners.DeletedSignInterpretation
								, diffRequest.AvailableListeners.UpdatedSignInterpretations
								,
						}
						, realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime);

				var diffResponse = realtime
						? diffRequest.SignalrResponseObject
						: diffRequest.HttpResponseObject;

				// Assert
				diffResponse.created.ShouldDeepEqual(diffRequest.CreatedSignInterpretation);
				diffResponse.updated.ShouldDeepEqual(diffRequest.UpdatedSignInterpretations);
				diffResponse.deleted.ShouldDeepEqual(diffRequest.DeletedSignInterpretation);

				Assert.True(
						diffResponse.created.signInterpretations.Any()
						|| diffResponse.updated.signInterpretations.Any()
						|| diffResponse.deleted.ids.Any());

				Assert.Equal(EditionEntities.signInterpretation, diffResponse.deleted.entity);

				var updatedTextRequest =
						new Get.V1_Editions_EditionId_TextFragments_TextFragmentId(
								editionId
								, textFragmentId);

				await updatedTextRequest.SendAsync(
						realtime
								? null
								: _client
						, StartConnectionAsync
						, true
						, requestRealtime: realtime);

				var updatedText = realtime
						? updatedTextRequest.SignalrResponseObject
						: updatedTextRequest.HttpResponseObject;

				Assert.False(updatedText.IsDeepEqual(text));

				// Build the returned text string for comparison
				var updatedString = "";

				var spacingAttributes = new List<uint>
				{
						2
						, 3
						, 4
						,
				};

				foreach (var sign in updatedText.textFragments.First()
												.lines.SelectMany(x => x.signs)
												.SelectMany(x => x.signInterpretations))
				{
					updatedString += sign.character;

					if (sign.attributes.Any(x => spacingAttributes.Contains(x.attributeValueId)))
						updatedString += " ";
				}

				Assert.Contains(replacementString, updatedString);
			}
		}

		private IList<ValidationResult> ValidateModel(object model)
		{
			var validationResults = new List<ValidationResult>();
			var ctx = new ValidationContext(model, null, null);

			Validator.TryValidateObject(
					model
					, ctx
					, validationResults
					, true);

			return validationResults;
		}

		// TODO: Ingo changed the logic so two text fragments with the same name are allowed, so probably remove this test.
		// [Fact]
		// public async Task CanNotAddTwoTextFragmentsWithTheSameName()
		// {
		//     using (var editionCreator = new EditionHelpers.EditionCreator(_client))
		//     {
		//         // Arrange
		//         var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);
		//         const string textFragmentName = "my new can add to end col";
		//         var numberOfTextFragments = textFragments.textFragments.Count;
		//
		//         // Act
		//         var (response, msg) = await _createTextFragment(
		//             editionId,
		//             textFragmentName
		//         );
		//
		//         // Assert
		//         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		//         Assert.Empty(textFragments.textFragments.Where(x => x.id == msg.id || x.name == msg.name));
		//         var updatedTextFragments =
		//             await _getEditionTextFragments(
		//                 editionId,
		//                 true
		//             ); // Get the updated list of text fragments in the edition
		//         Assert.NotEmpty(updatedTextFragments.textFragments.Where(x => x.id == msg.id));
		//         Assert.Equal(msg.id, updatedTextFragments.textFragments.Last().id);
		//         Assert.Equal(msg.name, updatedTextFragments.textFragments.Last().name);
		//         Assert.Equal(numberOfTextFragments + 1, updatedTextFragments.textFragments.Count);
		//
		//         for (var i = 0; i < textFragments.textFragments.Count; i++)
		//             textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);
		//
		//         // Act
		//         (response, msg) = await _createTextFragment(
		//             editionId,
		//             textFragmentName,
		//             false
		//         );
		//
		//         // Assert
		//         Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
		//         var secondUpdateTextFragments = await _getEditionTextFragments(
		//             editionId,
		//             true
		//         ); // Get the updated list of text fragments in the edition
		//         updatedTextFragments.textFragments.ShouldDeepEqual(secondUpdateTextFragments.textFragments);
		//     }
		// }

		// TODO: authenticated retrieval and blocking of unauthorized requests
	}
}
