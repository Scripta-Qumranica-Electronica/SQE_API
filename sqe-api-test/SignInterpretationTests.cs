using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
	// TODO: These tests only confirm operations that should succeed.  We also need to test the failures.
	// Itay ran into a case where inputting an incorrect Id resulted in a 500 error (not a 404).
	// That problem in the API still remains, and we still need to write tests for it.
	public partial class WebControllerTest
	{
		/// <summary>
		///  Find a sign interpretation id in the edition
		/// </summary>
		/// <param name="editionId"></param>
		/// <returns></returns>
		private async Task<SignInterpretationDTO> GetEditionSignInterpretation(uint editionId)
		{
			var textFragmentsRequest = new Get.V1_Editions_EditionId_TextFragments(editionId);

			await textFragmentsRequest.SendAsync(_client, auth: true);
			var textFragments = textFragmentsRequest.HttpResponseObject;

			foreach (var textRequest in textFragments.textFragments.Select(
					tf => new Get.V1_Editions_EditionId_TextFragments_TextFragmentId(
							editionId
							, tf.id)))
			{
				await textRequest.SendAsync(_client, auth: true);
				var text = textRequest.HttpResponseObject;

				foreach (var si in from ttf in text.textFragments
								   from tl in ttf.lines
								   from sign in tl.signs
								   from si in sign.signInterpretations
								   from att in si.attributes
								   where (att.attributeValueId == 1)
										 && !string.IsNullOrEmpty(si.character)
								   select si)
					return si;
			}

			throw new Exception(
					$"Edition {editionId} has no letters, this may be a problem the database.");
		}

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanAddAttributeToEdition()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var request =
						new Get.V1_Editions_EditionId_SignInterpretationsAttributes(editionId);

				await request.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, deterministic: true
						, requestRealtime: true);

				var httpData = request.HttpResponseObject;

				CreateAttributeValueDTO[] newAttrValues =
				{
						new CreateAttributeValueDTO
						{
								value = "the first type"
								, cssDirectives = "color: blue;"
								, description =
										"This is the first of a new way to describe a piece of data"
								,
						}
						,
				};

				var newAttribute = new CreateAttributeDTO
				{
						attributeName = "new attr"
						, description =
								"Some very exciting new way to describe a piece of data"
						, values = newAttrValues
						,
				};

				// Create the new attribute
				var createRequest =
						new Post.V1_Editions_EditionId_SignInterpretationsAttributes(
								editionId
								, newAttribute);

				await createRequest.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: false
						, requestRealtime: false
						, listeningFor: createRequest.AvailableListeners
													 .CreatedAttribute);

				var (respInfo, createdHttpAttribute, createdListenerAttribute) = (
						createRequest.HttpResponseMessage, createRequest.HttpResponseObject
						, createRequest.CreatedAttribute);

				// Assert
				respInfo.EnsureSuccessStatusCode();
				createdHttpAttribute.ShouldDeepEqual(createdListenerAttribute);

				await request.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, deterministic: true
						, requestRealtime: true);

				var newAttrs = request.HttpResponseObject;

				Assert.Single(
						newAttrs.attributes.Where(
								x => (x.attributeName == newAttribute.attributeName)
									 && (x.description == newAttribute.description)));

				var returnedNewAttr = newAttrs.attributes.First(
						x => (x.attributeName == newAttribute.attributeName)
							 && (x.description == newAttribute.description));

				Assert.Single(returnedNewAttr.values);

				Assert.Single(
						returnedNewAttr.values.Where(
								x => (x.value == newAttrValues.First().value)
									 && (x.description == newAttrValues.First().description)
									 && (x.cssDirectives == newAttrValues.First().cssDirectives)));

				Assert.Equal(httpData.attributes.Length + 1, newAttrs.attributes.Length);
			}
		}

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanCreateNewAttributeForSignInterpretation()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var signInterpretation = await GetEditionSignInterpretation(editionId);

				var signInterpretationAddAttribute =
						new InterpretationAttributeCreateDTO
						{
								attributeId = 8
								, attributeValueId = 33
								,
						};

				var request =
						new Post.
								V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(
										editionId
										, signInterpretation.signInterpretationId
										, signInterpretationAddAttribute);

				// Act
				await request.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: true
						, requestRealtime: false
						, listeningFor: request.AvailableListeners
											   .UpdatedSignInterpretation);

				var (httpResponse, httpData, listenerData) = (
						request.HttpResponseMessage, request.HttpResponseObject
						, request.UpdatedSignInterpretation);

				// Assert
				httpResponse.EnsureSuccessStatusCode();
				httpData.ShouldDeepEqual(listenerData);

				Assert.Equal(signInterpretation.attributes.Length + 1, httpData.attributes.Length);

				Assert.Equal(
						signInterpretation.signInterpretationId
						, httpData.signInterpretationId);

				Assert.Equal(signInterpretation.character, httpData.character);

				Assert.Equal(signInterpretation.commentary, httpData.commentary);

				Assert.Equal(signInterpretation.isVariant, httpData.isVariant);

				signInterpretation.rois.ShouldDeepEqual(httpData.rois);

				Assert.Contains(
						httpData.attributes
						, x => x.attributeValueId
							   == signInterpretationAddAttribute.attributeValueId);

				var responseAttr = httpData.attributes.FirstOrDefault(
						x => x.attributeValueId == signInterpretationAddAttribute.attributeValueId);

				Assert.NotNull(responseAttr);
				Assert.Null(responseAttr.commentary);
				Assert.True(responseAttr.sequence.HasValue);
				Assert.Equal(0, responseAttr.sequence.Value);
				Assert.NotEqual<uint>(0, responseAttr.creatorId);
				Assert.NotEqual<uint>(0, responseAttr.editorId);
				Assert.NotEqual<uint>(0, responseAttr.attributeId);
				Assert.NotEqual<uint>(0, responseAttr.attributeValueId);

				Assert.NotEqual<uint>(0, responseAttr.interpretationAttributeId);

				Assert.False(string.IsNullOrEmpty(responseAttr.attributeValueString));
			}
		}

		[Theory]
		[InlineData(true, true, false)]
		[InlineData(true, true, true)]
		[InlineData(false, false, false)]
		[InlineData(false, false, true)]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanCreateAndDeleteSignInterpretation(
				bool   deleteAll
				, bool testSignAfterId
				, bool realtime)
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var (textFragment, line, previousSignInterpretation, nextSignInterpretation, _) =
						await _getTestingSignInterpretationData(editionId);

				var newSignInterpretation = new SignInterpretationCreateDTO
				{
						character = "ט"
						, isVariant = false
						, commentary =
								new CommentaryCreateDTO { commentary = "I just made this one up." }
						, attributes =
								new[]
								{
										new InterpretationAttributeCreateDTO
										{
												attributeId = 1
												, attributeValueId = 1
												, sequence = 1
												,
										}
										,
								}
						, lineId = line.lineId
						, previousSignInterpretationIds = new[] { previousSignInterpretation }
						, nextSignInterpretationIds = testSignAfterId
								? new[] { nextSignInterpretation }
								: new uint[0]
						, rois = new SetInterpretationRoiDTO [0]
						,
				};

				var newlyCreatedInterpretationId = await CreateSignInterpretation(
						editionId
						, newSignInterpretation
						, textFragment
						, line
						, previousSignInterpretation
						, nextSignInterpretation
						, realtime);

				await _deleteSignInterpretationAsync(
						editionId
						, newlyCreatedInterpretationId
						, textFragment
						, line
						, deleteAll
						, realtime);
			}
		}

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanCreateVariantSignInterpretation()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var (_, line, previousSignInterpretation, copySignInterpretation,
								nextSignInterpretation) =
						await _getTestingSignInterpretationData(editionId);

				var originalSignInterpretation =
						new Get.V1_Editions_EditionId_SignInterpretations_SignInterpretationId(
								editionId
								, copySignInterpretation);

				await originalSignInterpretation.SendAsync(_client, StartConnectionAsync, true);

				var newSignInterpretation = new SignInterpretationVariantDTO
				{
						character = "ט"
						, attributeId = 1
						, attributeValueId = 1
						, sequence = null
						,
				};

				var request =
						new Post.V1_Editions_EditionId_SignInterpretations_SignInterpretationId(
								editionId
								, copySignInterpretation
								, newSignInterpretation);

				await request.SendAsync(_client, null, true);

				request.HttpResponseMessage.EnsureSuccessStatusCode();

				// TODO: Check that everything was done properly
				Assert.NotEmpty(request.HttpResponseObject.created);
				Assert.NotEmpty(request.HttpResponseObject.updated);

				Assert.NotEqual(
						originalSignInterpretation.HttpResponseObject.character
						, request.HttpResponseObject.created[0].character);

				Assert.Equal(
						newSignInterpretation.character
						, request.HttpResponseObject.created[0].character);

				Assert.Equal(
						newSignInterpretation.attributeId
						, request.HttpResponseObject.created[0].attributes[0].attributeId);

				Assert.Equal(
						newSignInterpretation.attributeValueId
						, request.HttpResponseObject.created[0].attributes[0].attributeValueId);

				Assert.Contains(
						request.HttpResponseObject.updated[0].nextSignInterpretations
						, x => x.nextSignInterpretationId
							   == originalSignInterpretation.HttpResponseObject
															.signInterpretationId);

				Assert.Contains(
						request.HttpResponseObject.updated[0].nextSignInterpretations
						, x => x.nextSignInterpretationId
							   == request.HttpResponseObject.created[0].signInterpretationId);
			}
		}

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanLinkSignInterpretations()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var signStream =
						(await EditionHelpers.GetEditionTextFragmentWithSigns(
								editionId
								, _client
								, Request.DefaultUsers.User1)).textFragments
															  .Where(
																	  x
																			  => x.lines.Any(
																					  y => y.signs
																							.Count
																						   > 2))
															  .SelectMany(x => x.lines)
															  .SelectMany(x => x.signs)
															  .SelectMany(
																	  x => x.signInterpretations)
															  .ToList();

				var firstSignInterpretation = signStream.First();
				var lastSignInterpretation = signStream.Last();

				Assert.Empty(
						firstSignInterpretation.nextSignInterpretations.Where(
								x => x.nextSignInterpretationId
									 == lastSignInterpretation.signInterpretationId));

				// Act
				var linkRequest =
						new Post.
								V1_Editions_EditionId_SignInterpretations_SignInterpretationId_LinkTo_NextSignInterpretationId(
										editionId
										, firstSignInterpretation.signInterpretationId
										, lastSignInterpretation.signInterpretationId);

				await linkRequest.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, requestRealtime: true
						, listeningFor: linkRequest.AvailableListeners
												   .UpdatedSignInterpretation);

				// Assert
				linkRequest.HttpResponseObject.ShouldDeepEqual(
						linkRequest.UpdatedSignInterpretation);

				Assert.Contains(
						linkRequest.HttpResponseObject.nextSignInterpretations
						, x => x.nextSignInterpretationId
							   == lastSignInterpretation.signInterpretationId);

				// get the sign stream again
				var newSignStream =
						(await EditionHelpers.GetEditionTextFragmentWithSigns(
								editionId
								, _client
								, Request.DefaultUsers.User1)).textFragments
															  .Where(
																	  x
																			  => x.lines.Any(
																					  y => y.signs
																							.Count
																						   > 2))
															  .SelectMany(x => x.lines)
															  .SelectMany(x => x.signs)
															  .SelectMany(
																	  x => x.signInterpretations);

				Assert.Contains(
						newSignStream.First().nextSignInterpretations
						, x => x.nextSignInterpretationId
							   == lastSignInterpretation.signInterpretationId);
			}
		}

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanUnlinkSignInterpretations()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var signStream =
						(await EditionHelpers.GetEditionTextFragmentWithSigns(
								editionId
								, _client
								, Request.DefaultUsers.User1)).textFragments
															  .Where(
																	  x
																			  => x.lines.Any(
																					  y => y.signs
																							.Count
																						   > 2))
															  .SelectMany(x => x.lines)
															  .SelectMany(x => x.signs)
															  .SelectMany(
																	  x => x.signInterpretations)
															  .ToList();

				var firstSignInterpretation = signStream.First();

				var nextSignInterpretation = signStream.First(
						x => firstSignInterpretation.nextSignInterpretations.Any(
								y => y.nextSignInterpretationId == x.signInterpretationId));

				// Act
				var linkRequest =
						new Post.
								V1_Editions_EditionId_SignInterpretations_SignInterpretationId_UnlinkFrom_NextSignInterpretationId(
										editionId
										, firstSignInterpretation.signInterpretationId
										, nextSignInterpretation.signInterpretationId);

				await linkRequest.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, requestRealtime: true
						, listeningFor: linkRequest.AvailableListeners
												   .UpdatedSignInterpretation);

				// Assert
				linkRequest.HttpResponseObject.ShouldDeepEqual(
						linkRequest.UpdatedSignInterpretation);

				Assert.DoesNotContain(
						linkRequest.HttpResponseObject.nextSignInterpretations
						, x => x.nextSignInterpretationId
							   == nextSignInterpretation.signInterpretationId);

				// get the sign stream again
				var newSignStream =
						(await EditionHelpers.GetEditionTextFragmentWithSigns(
								editionId
								, _client
								, Request.DefaultUsers.User1)).textFragments
															  .Where(
																	  x
																			  => x.lines.Any(
																					  y => y.signs
																							.Count
																						   > 2))
															  .SelectMany(x => x.lines)
															  .SelectMany(x => x.signs)
															  .SelectMany(
																	  x => x.signInterpretations);

				Assert.DoesNotContain(
						newSignStream.First().nextSignInterpretations
						, x => x.nextSignInterpretationId
							   == nextSignInterpretation.signInterpretationId);
			}
		}

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanCreateSignInterpretationCommentary()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var signInterpretation = await GetEditionSignInterpretation(editionId);

				var commentary = new CommentaryCreateDTO
				{
						commentary = @"#Commentary on the sign level

##Heading two

[qumranica](https://www.qumranica.org)

* point one"
						,
				};

				var request =
						new Put.
								V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary(
										editionId
										, signInterpretation.signInterpretationId
										, commentary);

				// Act
				await request.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: true
						, requestRealtime: false
						, listeningFor: request.AvailableListeners
											   .UpdatedSignInterpretation);

				var (httpResponse, httpData, listenerData) = (
						request.HttpResponseMessage, request.HttpResponseObject
						, request.UpdatedSignInterpretation);

				// Assert
				httpResponse.EnsureSuccessStatusCode();
				httpData.ShouldDeepEqual(listenerData);

				Assert.Equal(signInterpretation.attributes.Length, httpData.attributes.Length);

				Assert.Equal(signInterpretation.rois.Length, httpData.rois.Length);

				Assert.Equal(
						signInterpretation.nextSignInterpretations.Length
						, httpData.nextSignInterpretations.Length);

				Assert.Equal(
						signInterpretation.signInterpretationId
						, httpData.signInterpretationId);

				Assert.Equal(signInterpretation.character, httpData.character);

				Assert.NotEqual(signInterpretation.commentary, httpData.commentary);

				Assert.Equal(commentary.commentary, httpData.commentary.commentary);

				Assert.Equal(signInterpretation.isVariant, httpData.isVariant);

				// Try setting commentary to null
				commentary = new CommentaryCreateDTO { commentary = null };

				var request2 =
						new Put.
								V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary(
										editionId
										, signInterpretation.signInterpretationId
										, commentary);

				// Act
				await request2.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: true
						, requestRealtime: false
						, listeningFor: request2.AvailableListeners
												.UpdatedSignInterpretation);

				var (http2Response, http2Data, listener2Data) = (
						request2.HttpResponseMessage, request2.HttpResponseObject
						, request2.UpdatedSignInterpretation);

				// Assert
				http2Response.EnsureSuccessStatusCode();
				http2Data.ShouldDeepEqual(listener2Data);

				Assert.Equal(signInterpretation.attributes.Length, http2Data.attributes.Length);

				Assert.Equal(signInterpretation.rois.Length, http2Data.rois.Length);

				Assert.Equal(
						signInterpretation.nextSignInterpretations.Length
						, http2Data.nextSignInterpretations.Length);

				Assert.Equal(
						signInterpretation.signInterpretationId
						, http2Data.signInterpretationId);

				Assert.Equal(signInterpretation.character, http2Data.character);

				Assert.Null(http2Data.commentary);

				Assert.Equal(signInterpretation.isVariant, http2Data.isVariant);
			}
		}

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanDeleteAttributeFromEdition()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var request =
						new Get.V1_Editions_EditionId_SignInterpretationsAttributes(editionId);

				await request.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, deterministic: true
						, requestRealtime: true);

				var httpData = request.HttpResponseObject;
				var deleteAttribute = httpData.attributes.Last().attributeId;

				// Delete the new attribute
				var deleteRequest =
						new Delete.V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(
								editionId
								, deleteAttribute);

				await deleteRequest.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: false
						, requestRealtime: false
						, listeningFor: deleteRequest.AvailableListeners
													 .DeletedAttribute);

				var (respInfo, updatedListenerAttribute) = (
						deleteRequest.HttpResponseMessage, deleteRequest.DeletedAttribute);

				// Assert
				respInfo.EnsureSuccessStatusCode();

				Assert.Equal(deleteAttribute, updatedListenerAttribute.ids.First());

				Assert.Equal(EditionEntities.attribute, updatedListenerAttribute.entity);

				await request.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, deterministic: true
						, requestRealtime: true);

				var newAttrs = request.HttpResponseObject;

				Assert.Empty(newAttrs.attributes.Where(x => x.attributeId == deleteAttribute));

				Assert.Equal(httpData.attributes.Length - 1, newAttrs.attributes.Length);
			}
		}

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanDeleteAttributeFromSignInterpretation()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var signInterpretation = await GetEditionSignInterpretation(editionId);

				var signInterpretationAddAttribute =
						new InterpretationAttributeCreateDTO
						{
								attributeId = 8
								, attributeValueId = 33
								,
						};

				var request =
						new Post.
								V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(
										editionId
										, signInterpretation.signInterpretationId
										, signInterpretationAddAttribute);

				// Act
				await request.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: true
						, requestRealtime: false
						, listeningFor: request.AvailableListeners
											   .UpdatedSignInterpretation);

				var (httpResponse, httpData, listenerData) = (
						request.HttpResponseMessage, request.HttpResponseObject
						, request.UpdatedSignInterpretation);

				// Assert
				httpResponse.EnsureSuccessStatusCode();
				httpData.ShouldDeepEqual(listenerData);

				Assert.Equal(signInterpretation.attributes.Length + 1, httpData.attributes.Length);

				Assert.Equal(
						signInterpretation.signInterpretationId
						, httpData.signInterpretationId);

				Assert.Equal(signInterpretation.character, httpData.character);

				Assert.Equal(signInterpretation.commentary, httpData.commentary);

				Assert.Equal(signInterpretation.isVariant, httpData.isVariant);

				signInterpretation.rois.ShouldDeepEqual(httpData.rois);

				Assert.Contains(
						httpData.attributes
						, x => x.attributeValueId
							   == signInterpretationAddAttribute.attributeValueId);

				var responseAttr = httpData.attributes.FirstOrDefault(
						x => x.attributeValueId == signInterpretationAddAttribute.attributeValueId);

				Assert.NotNull(responseAttr);
				Assert.Null(responseAttr.commentary);
				Assert.True(responseAttr.sequence.HasValue);
				Assert.Equal(0, responseAttr.sequence.Value);
				Assert.NotEqual<uint>(0, responseAttr.creatorId);
				Assert.NotEqual<uint>(0, responseAttr.editorId);
				Assert.NotEqual<uint>(0, responseAttr.attributeId);
				Assert.NotEqual<uint>(0, responseAttr.attributeValueId);

				Assert.NotEqual<uint>(0, responseAttr.interpretationAttributeId);

				Assert.False(string.IsNullOrEmpty(responseAttr.attributeValueString));

				// Delete the new attribute
				var delRequest =
						new Delete.
								V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(
										editionId
										, signInterpretation.signInterpretationId
										, 33);

				await delRequest.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: true
						, requestRealtime: false
						, listeningFor: delRequest.AvailableListeners
												  .UpdatedSignInterpretation);

				var (delResponse, delListenerData) = (
						delRequest.HttpResponseMessage, delRequest.UpdatedSignInterpretation);

				// Assert
				delResponse.EnsureSuccessStatusCode();

				Assert.Equal(
						signInterpretation.attributes.Length
						, delListenerData.attributes.Length);

				Assert.Equal(
						signInterpretation.signInterpretationId
						, delListenerData.signInterpretationId);

				Assert.Equal(signInterpretation.character, delListenerData.character);

				Assert.Equal(signInterpretation.commentary, delListenerData.commentary);

				Assert.Equal(signInterpretation.isVariant, delListenerData.isVariant);

				signInterpretation.rois.ShouldDeepEqual(delListenerData.rois);

				Assert.DoesNotContain(
						delListenerData.attributes
						, x => x.attributeValueId
							   == signInterpretationAddAttribute.attributeValueId);
			}
		}

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanGetAllEditionSignInterpretationAttributes()
		{
			// Arrange
			var request =
					new Get.V1_Editions_EditionId_SignInterpretationsAttributes(
							EditionHelpers.GetEditionId());

			// Act
			await request.SendAsync(_client, StartConnectionAsync, true);

			var (httpResponse, httpData, signalRData) = (
					request.HttpResponseMessage, request.HttpResponseObject
					, request.SignalrResponseObject);

			// Assert
			httpResponse.EnsureSuccessStatusCode();
			httpData.ShouldDeepEqual(signalRData);
			Assert.NotEmpty(httpData.attributes);
			Assert.NotEmpty(httpData.attributes.First().values);
			Assert.NotNull(httpData.attributes.First().attributeName);
			Assert.True(httpData.attributes.First().attributeId > 0);
			Assert.True(httpData.attributes.First().creatorId > 0);
			Assert.True(httpData.attributes.First().editorId > 0);
			Assert.NotNull(httpData.attributes.First().values.First().value);
			Assert.True(httpData.attributes.First().values.First().id > 0);

			Assert.True(httpData.attributes.First().values.First().editorId > 0);

			Assert.True(httpData.attributes.First().values.First().creatorId > 0);
		}

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanGetAttributesOfSpecificSignInterpretation()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var signInterpretation = await GetEditionSignInterpretation(editionId);

				var request =
						new Get.V1_Editions_EditionId_SignInterpretations_SignInterpretationId(
								editionId
								, signInterpretation.signInterpretationId);

				// Act
				await request.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: true
						, requestRealtime: true);

				var (httpResponse, httpData, signalrData) = (
						request.HttpResponseMessage, request.HttpResponseObject
						, request.SignalrResponseObject);

				// Assert
				httpResponse.EnsureSuccessStatusCode();
				httpData.ShouldDeepEqual(signalrData);

				Assert.Equal(signInterpretation.attributes.Length, httpData.attributes.Length);

				Assert.Equal(
						signInterpretation.signInterpretationId
						, httpData.signInterpretationId);

				Assert.Equal(signInterpretation.character, httpData.character);

				Assert.Equal(signInterpretation.commentary, httpData.commentary);

				Assert.Equal(signInterpretation.isVariant, httpData.isVariant);

				signInterpretation.rois.ShouldDeepEqual(httpData.rois);

				signInterpretation.attributes.ShouldDeepEqual(httpData.attributes);
			}
		}

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanUpdateAttributeInEdition()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var request =
						new Get.V1_Editions_EditionId_SignInterpretationsAttributes(editionId);

				await request.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, deterministic: true
						, requestRealtime: true);

				var httpData = request.HttpResponseObject;

				var attributeValueForUpdate = httpData.attributes.First().values.First();

				var attributeValueForDelete = httpData.attributes.First().values.Last();

				var updateAttribute = new UpdateAttributeDTO
				{
						createValues =
								new[]
								{
										new CreateAttributeValueDTO
										{
												cssDirectives =
														"vertical-align: super; font-size: 50%;"
												, description =
														"Some small raised thing"
												, value = "TINY_UPPER"
												,
										}
										,
								}
						, updateValues = new[]
						{
								new UpdateAttributeValueDTO
								{
										id = attributeValueForUpdate.id
										, cssDirectives =
												"font-weight: bold;"
										, description =
												attributeValueForUpdate
														.description
										, value = "AMAZING_NEW_VALUE"
										,
								}
								,
						}
						, deleteValues = new[] { attributeValueForDelete.id }
						,
				};

				// Act
				var updateRequest =
						new Put.V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(
								editionId
								, httpData.attributes.First().attributeId
								, updateAttribute);

				await updateRequest.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: false
						, requestRealtime: false
						, listenToEdition: true
						, listeningFor: updateRequest.AvailableListeners
													 .UpdatedAttribute);

				// Assert
				// The listener should return the same as the http request
				updateRequest.HttpResponseObject.ShouldDeepEqual(updateRequest.UpdatedAttribute);

				// The update contains the expected data
				Assert.Contains(
						updateRequest.HttpResponseObject.values
						, x => (x.description == updateAttribute.createValues.First().description)
							   && (x.cssDirectives
								   == updateAttribute.createValues.First().cssDirectives)
							   && (x.value == updateAttribute.createValues.First().value));

				Assert.Contains(
						updateRequest.HttpResponseObject.values
						, x => (x.id != attributeValueForUpdate.id)
							   && (x.description
								   == updateAttribute.updateValues.First().description)
							   && (x.cssDirectives
								   == updateAttribute.updateValues.First().cssDirectives)
							   && (x.value == updateAttribute.updateValues.First().value));

				Assert.DoesNotContain(
						updateRequest.HttpResponseObject.values
						, x => x.id == attributeValueForDelete.id);

				// The update appears when getting all attributes
				await request.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, deterministic: true
						, requestRealtime: true);

				var newAttrs = request.HttpResponseObject;

				Assert.Contains(
						newAttrs.attributes
						, x => x.attributeId == httpData.attributes.First().attributeId);

				newAttrs.attributes
						.First(x => x.attributeId == httpData.attributes.First().attributeId)
						.ShouldDeepEqual(updateRequest.HttpResponseObject);
			}
		}

		// [Fact]
		// public async Task CannotCreateNonexistentAttributeForSignInterpretation()
		// {
		//     using (var editionCreator = new EditionHelpers.EditionCreator(_client))
		//     {
		//         // Arrange
		//         var editionId = await editionCreator.CreateEdition();
		//         var signInterpretation = await GetEditionSignInterpretation(editionId);
		//         var signInterpretationAddAttribute = new InterpretationAttributeCreateDTO()
		//         {
		//             attributeId = 23894632,
		//             attributeValueId = 999999999,
		//         };
		//         var request = new Post.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(editionId,
		//             signInterpretation.signInterpretationId, signInterpretationAddAttribute);
		//
		//         // Act
		//         var (httpResponse, httpData, _, listenerData) =
		//             await Request.Send(request, _client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1, deterministic: true, requestRealtime: false);
		//
		//         // Assert
		//         httpResponse.EnsureSuccessStatusCode();
		//         httpData.ShouldDeepEqual(listenerData);
		//         Assert.Equal(signInterpretation.attributes.Length + 1, httpData.attributes.Length);
		//         Assert.Equal(signInterpretation.signInterpretationId, httpData.signInterpretationId);
		//         Assert.Equal(signInterpretation.character, httpData.character);
		//         Assert.Equal(signInterpretation.commentary, httpData.commentary);
		//         Assert.Equal(signInterpretation.isVariant, httpData.isVariant);
		//         signInterpretation.rois.ShouldDeepEqual(httpData.rois);
		//         Assert.True(httpData.attributes.Any(x => x.attributeValueId == signInterpretationAddAttribute.attributeValueId));
		//         var responseAttr = httpData.attributes.FirstOrDefault(x =>
		//             x.attributeValueId == signInterpretationAddAttribute.attributeValueId);
		//         Assert.Null(responseAttr.commentary);
		//         Assert.Equal(0, responseAttr.sequence);
		//         Assert.Equal(0, responseAttr.value);
		//         Assert.NotEqual<uint>(0, responseAttr.creatorId);
		//         Assert.NotEqual<uint>(0, responseAttr.editorId);
		//         Assert.NotEqual<uint>(0, responseAttr.attributeId);
		//         Assert.NotEqual<uint>(0, responseAttr.attributeValueId);
		//         Assert.NotEqual<uint>(0, responseAttr.interpretationAttributeId);
		//         Assert.False(string.IsNullOrEmpty(responseAttr.attributeValueString));
		//     }
		// }

		[Fact]
		[Trait("Category", "Sign Interpretation")]
		public async Task CanUpdateAttributeOfSignInterpretation()
		{
			using (var editionCreator =
					new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
			{
				// Arrange
				var editionId = await editionCreator.CreateEdition();

				var signInterpretation = await GetEditionSignInterpretation(editionId);

				const uint attributeValueId = 33;

				var signInterpretationAddAttribute = new InterpretationAttributeCreateDTO
				{
						attributeId = 8
						, attributeValueId = attributeValueId
						,
				};

				var request =
						new Post.
								V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(
										editionId
										, signInterpretation.signInterpretationId
										, signInterpretationAddAttribute);

				// Act
				await request.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: true
						, requestRealtime: false
						, listeningFor: request.AvailableListeners
											   .UpdatedSignInterpretation);

				var (httpResponse, httpData, listenerData) = (
						request.HttpResponseMessage, request.HttpResponseObject
						, request.UpdatedSignInterpretation);

				// Assert
				httpResponse.EnsureSuccessStatusCode();
				httpData.ShouldDeepEqual(listenerData);

				Assert.Equal(signInterpretation.attributes.Length + 1, httpData.attributes.Length);

				Assert.Equal(
						signInterpretation.signInterpretationId
						, httpData.signInterpretationId);

				Assert.Equal(signInterpretation.character, httpData.character);

				Assert.Equal(signInterpretation.commentary, httpData.commentary);

				Assert.Equal(signInterpretation.isVariant, httpData.isVariant);

				signInterpretation.rois.ShouldDeepEqual(httpData.rois);

				Assert.Contains(
						httpData.attributes
						, x => x.attributeValueId
							   == signInterpretationAddAttribute.attributeValueId);

				var responseAttr = httpData.attributes.FirstOrDefault(
						x => x.attributeValueId == signInterpretationAddAttribute.attributeValueId);

				Assert.NotNull(responseAttr);
				Assert.Null(responseAttr.commentary);
				Assert.True(responseAttr.sequence.HasValue);
				Assert.Equal(0, responseAttr.sequence.Value);
				Assert.NotEqual<uint>(0, responseAttr.creatorId);
				Assert.NotEqual<uint>(0, responseAttr.editorId);
				Assert.NotEqual<uint>(0, responseAttr.attributeId);
				Assert.NotEqual<uint>(0, responseAttr.attributeValueId);

				Assert.NotEqual<uint>(0, responseAttr.interpretationAttributeId);

				Assert.False(string.IsNullOrEmpty(responseAttr.attributeValueString));

				// Update sequence and value of the new attribute
				const byte seq = 1;

				var updateSignInterpretationAddAttribute = new InterpretationAttributeCreateDTO
				{
						attributeId = 8
						, attributeValueId = attributeValueId
						, sequence = seq
						,
				};

				var updRequest1 =
						new Put.
								V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(
										editionId
										, signInterpretation.signInterpretationId
										, attributeValueId
										, updateSignInterpretationAddAttribute);

				await updRequest1.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: false
						, requestRealtime: false
						, listeningFor: updRequest1.AvailableListeners
												   .UpdatedSignInterpretation);

				var (updResponse, updData, updListenerData) = (
						updRequest1.HttpResponseMessage, updRequest1.HttpResponseObject
						, updRequest1.UpdatedSignInterpretation);

				// Assert
				updResponse.EnsureSuccessStatusCode();
				updData.ShouldDeepEqual(updListenerData);

				Assert.Equal(signInterpretation.attributes.Length + 1, updData.attributes.Length);

				Assert.Equal(signInterpretation.signInterpretationId, updData.signInterpretationId);

				Assert.Equal(signInterpretation.character, updData.character);

				Assert.Equal(signInterpretation.commentary, updData.commentary);

				Assert.Equal(signInterpretation.isVariant, updData.isVariant);

				Assert.Contains(updData.attributes, x => x.attributeValueId == attributeValueId);

				var upd1SignInterpretationAttribute =
						updData.attributes.First(x => x.attributeValueId == attributeValueId);

				Assert.Equal(seq, upd1SignInterpretationAttribute.sequence);

				Assert.Null(upd1SignInterpretationAttribute.commentary);

				// Update sequence and value of the new attribute
				const string commentary = "Here is a comment about בְּרֵאשִׁ֖ית";

				updateSignInterpretationAddAttribute = new InterpretationAttributeCreateDTO
				{
						attributeId = 8
						, attributeValueId = attributeValueId
						, sequence = seq
						, commentary = commentary
						,
				};

				var updRequest2 =
						new Put.
								V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(
										editionId
										, signInterpretation.signInterpretationId
										, attributeValueId
										, updateSignInterpretationAddAttribute);

				await updRequest2.SendAsync(
						_client
						, StartConnectionAsync
						, true
						, listenerUser: Request.DefaultUsers.User1
						, deterministic: false
						, requestRealtime: false
						, listeningFor: updRequest2.AvailableListeners
												   .UpdatedSignInterpretation);

				(updResponse, updData, updListenerData) = (
						updRequest2.HttpResponseMessage, updRequest2.HttpResponseObject
						, updRequest2.UpdatedSignInterpretation);

				// Assert
				updResponse.EnsureSuccessStatusCode();
				updData.ShouldDeepEqual(updListenerData);

				Assert.Equal(signInterpretation.attributes.Length + 1, updData.attributes.Length);

				Assert.Equal(signInterpretation.signInterpretationId, updData.signInterpretationId);

				Assert.Equal(signInterpretation.character, updData.character);

				Assert.Equal(signInterpretation.commentary, updData.commentary);

				Assert.Equal(signInterpretation.isVariant, updData.isVariant);

				Assert.Contains(updData.attributes, x => x.attributeValueId == attributeValueId);

				var upd2SignInterpretationAttribute =
						updData.attributes.First(x => x.attributeValueId == attributeValueId);

				Assert.Equal(seq, upd2SignInterpretationAttribute.sequence);

				Assert.NotNull(upd2SignInterpretationAttribute.commentary);

				Assert.Equal(commentary, upd2SignInterpretationAttribute.commentary.commentary);
			}
		}

		[Fact]
		[Trait("Category", "Sign Stream")]
		public async Task CanMaterializeSignStream()
		{
			var request = new Post.V1_MaterializeSignStreams(
					new RequestMaterializationDTO { editionIds = new uint[] { 1 } });

			await request.SendAsync(_client, null, true);

			// Fire off a bunch of unawaited requests and make sure the next
			// call picks them up
			var request2 = new Post.V1_MaterializeSignStreams(
					new RequestMaterializationDTO
					{
							editionIds = new uint[]
							{
									1
									, 2
									, 3
									, 4
									, 5
									, 6
									, 7
									, 8
									, 9
									, 10
									, 11
									, 12
									, 13
									, 14
									, 15
									, 16
									, 17
									, 18
									, 19
									, 20
									,
							}
							,
					});

			request2.SendAsync(_client, null, true);

			var request3 = new Post.V1_MaterializeSignStreams(
					new RequestMaterializationDTO { editionIds = new uint[0] });

			await request3.SendAsync(_client, null, true);
		}

		[Fact]
		[Trait("Category", "Sign Stream")]
		public async Task CanNotMaterializeSignStreamWithoutSysRole()
		{
			var request = new Post.V1_MaterializeSignStreams(
					new RequestMaterializationDTO { editionIds = new uint[] { 1 } });

			await request.SendAsync(
					_client
					, null
					, true
					, Request.DefaultUsers.User2
					, shouldSucceed: false);
		}

		private async
				Task<(TextFragmentDTO Fragments, LineDTO Line, uint PreviousSignInterpretationId,
						uint NextSignInterpretationId, uint AfterNextSignInterpretation)>
				_getTestingSignInterpretationData(uint editionId)
		{
			var textFragments = await EditionHelpers.GetEditionTextFragmentWithSigns(
					editionId
					, _client
					, Request.DefaultUsers.User1);

			var textFragment =
					textFragments.textFragments.First(x => x.lines.Any(y => y.signs.Count > 2));

			var line = textFragment.lines.First(y => y.signs.Count > 2);

			var previousSignInterpretation = line.signs.First()
												 .signInterpretations.Last()
												 .signInterpretationId;

			var nextSignInterpretation = line.signs[1]
											 .signInterpretations.First()
											 .signInterpretationId;

			var afterNextSignInterpretation = line.signs[2]
												  .signInterpretations.First()
												  .signInterpretationId;

			return (textFragment, line, previousSignInterpretation, nextSignInterpretation
					, afterNextSignInterpretation);
		}

		private async Task<uint> CreateSignInterpretation(
				uint                          editionId
				, SignInterpretationCreateDTO newSignInterpretation
				, TextFragmentDTO             textFragment
				, LineDTO                     line
				, uint                        previousSignInterpretation
				, uint                        nextSignInterpretation
				, bool                        realtime = false)
		{
			// Act
			var newSignInterpretationRequest =
					new Post.V1_Editions_EditionId_SignInterpretations(
							editionId
							, newSignInterpretation);

			await newSignInterpretationRequest.SendAsync(
					new List<ListenerMethods>
					{
							newSignInterpretationRequest.AvailableListeners
														.CreatedSignInterpretation
							, newSignInterpretationRequest.AvailableListeners
														  .UpdatedSignInterpretations
							,
					}
					, realtime
							? null
							: _client
					, StartConnectionAsync
					, true
					, listenToEdition: true
					, requestRealtime: realtime);

			// Assert
			if (!realtime)
				newSignInterpretationRequest.HttpResponseMessage.EnsureSuccessStatusCode();

			var responseObject = realtime
					? newSignInterpretationRequest.SignalrResponseObject
					: newSignInterpretationRequest.HttpResponseObject;

			responseObject.created.ShouldDeepEqual(
					newSignInterpretationRequest.CreatedSignInterpretation.signInterpretations);

			responseObject.updated.ShouldDeepEqual(
					newSignInterpretationRequest.UpdatedSignInterpretations.signInterpretations);

			Assert.Equal(1, responseObject.created.Length);

			// Get the text of this text fragment again
			var alteredTextFragment = await EditionHelpers.GetEditionTextFragmentWithSigns(
					editionId
					, _client
					, Request.DefaultUsers.User1);

			var signs = alteredTextFragment.textFragments
										   .First(
												   x => x.textFragmentId
														== textFragment.textFragmentId)
										   .lines.First(x => x.lineId == line.lineId)
										   .signs;

			responseObject.created.First().attributes = responseObject.created.First()
																	  .attributes.OrderBy(
																			  x => x
																					  .attributeValueId)
																	  .ToArray();

			// Make sure the two updated/new sign interpretations are in the stream
			signs[2].signInterpretations.First().ShouldDeepEqual(responseObject.created.First());

			Assert.Contains(
					signs
					, x => x.signInterpretations.Any(
							  y => y.IsDeepEqual(responseObject.created.First())));

			// Check that we have a matching sign interpretation in the stream
			var newlyCreatedInterpretation = responseObject.created.Last();

			var interpretationMatchingCreate = signs
											   .FirstOrDefault(
													   x => x.signInterpretations.Any(
															   y => y.signInterpretationId
																	== newlyCreatedInterpretation
																			.signInterpretationId))
											   ?.signInterpretations.First(
													   y => y.signInterpretationId
															== newlyCreatedInterpretation
																	.signInterpretationId);

			Assert.NotNull(interpretationMatchingCreate);

			Assert.Equal(
					newlyCreatedInterpretation.character
					, interpretationMatchingCreate.character);

			newlyCreatedInterpretation.nextSignInterpretations.ShouldDeepEqual(
					interpretationMatchingCreate.nextSignInterpretations);

			newlyCreatedInterpretation.rois.ShouldDeepEqual(interpretationMatchingCreate.rois);

			Assert.Equal(
					newlyCreatedInterpretation.isVariant
					, interpretationMatchingCreate.isVariant);

			Assert.Equal(
					newlyCreatedInterpretation.attributes.Length
					, interpretationMatchingCreate.attributes.Length);

			foreach (var attr in newlyCreatedInterpretation.attributes)
			{
				var attrMatch = interpretationMatchingCreate.attributes.First(
						x => x.attributeValueId == attr.attributeValueId);

				Assert.Equal(attrMatch.creatorId, attr.creatorId);
				Assert.Equal(attrMatch.editorId, attr.editorId);

				Assert.Equal(attrMatch.attributeValueString, attr.attributeValueString);

				Assert.Equal(attrMatch.interpretationAttributeId, attr.interpretationAttributeId);

				Assert.Equal(attrMatch.sequence, attr.sequence);
				Assert.Equal(attrMatch.attributeId, attr.attributeId);
			}

			return newlyCreatedInterpretation.signInterpretationId;
		}

		private async Task _deleteSignInterpretationAsync(
				uint              editionId
				, uint            newlyCreatedInterpretationId
				, TextFragmentDTO textFragment
				, LineDTO         line
				, bool            deleteAll = false
				, bool            realtime  = false)
		{
			// Act Delete new sign interpretation
			var deleteRequest =
					new Delete.V1_Editions_EditionId_SignInterpretations_SignInterpretationId(
							editionId
							, newlyCreatedInterpretationId
							, deleteAll
									? new[] { "delete-all-variants" }
									: new string[0]);

			await deleteRequest.SendAsync(
					new List<ListenerMethods>
					{
							deleteRequest.AvailableListeners.DeletedSignInterpretation
							, deleteRequest.AvailableListeners.UpdatedSignInterpretations
							,
					}
					, realtime
							? null
							: _client
					, StartConnectionAsync
					, true
					, deterministic: false
					, requestRealtime: realtime
					, listenToEdition: true);

			var deleteResponse = realtime
					? deleteRequest.SignalrResponseObject
					: deleteRequest.HttpResponseObject;

			// Assert
			Assert.Equal(
					EditionEntities.signInterpretation
					, deleteRequest.DeletedSignInterpretation.entity);

			Assert.Equal(
					newlyCreatedInterpretationId
					, deleteRequest.DeletedSignInterpretation.ids.First());

			Assert.Equal(deleteRequest.DeletedSignInterpretation.ids, deleteResponse.deletes);

			Assert.NotEmpty(deleteResponse.updates.signInterpretations);

			// Get the sign stream again
			var alteredTextFragment = await EditionHelpers.GetEditionTextFragmentWithSigns(
					editionId
					, _client
					, Request.DefaultUsers.User1);

			var signs = alteredTextFragment.textFragments
										   .First(
												   x => x.textFragmentId
														== textFragment.textFragmentId)
										   .lines.First(x => x.lineId == line.lineId)
										   .signs;

			// Make sure the deleted sign is really gone
			Assert.Empty(
					signs.Where(
							x => x.signInterpretations.Any(
									y => y.signInterpretationId == newlyCreatedInterpretationId)));

			var flattenedSigns = signs.SelectMany(x => x.signInterpretations).ToList();

			// Make sure that the sign stream is not broken; the first sign interpretation should
			// still connect to something.
			Assert.NotEmpty(
					flattenedSigns.Where(
							x => flattenedSigns.First()
											   .nextSignInterpretations.Any(
													   y => y.nextSignInterpretationId
															== x.signInterpretationId)));
		}
	}
}
