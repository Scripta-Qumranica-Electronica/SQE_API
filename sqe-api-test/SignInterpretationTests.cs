using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace SQE.ApiTest
{
    public class SignInterpretationTests : WebControllerTest
    {
        public SignInterpretationTests(WebApplicationFactory<Startup> factory, ITestOutputHelper output) : base(factory)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;

        /// <summary>
        ///     Find a sign interpretation id in the edition
        /// </summary>
        /// <param name="editionId"></param>
        /// <returns></returns>
        private async Task<SignInterpretationDTO> GetEditionSignInterpretation(uint editionId)
        {
            var textFragmentsRequest = new Get.V1_Editions_EditionId_TextFragments(editionId);
            await textFragmentsRequest.Send(_client, auth: true);
            var textFragments = textFragmentsRequest.HttpResponseObject;
            foreach (var textRequest in textFragments.textFragments.Select(tf =>
                new Get.V1_Editions_EditionId_TextFragments_TextFragmentId(editionId, tf.id)))
            {
                await textRequest.Send(_client, auth: true);
                var text = textRequest.HttpResponseObject;
                foreach (var si in from ttf
                        in text.textFragments
                                   from tl
                                       in ttf.lines
                                   from sign
                                       in tl.signs
                                   from si
                                       in sign.signInterpretations
                                   from att
                                       in si.attributes
                                   where att.attributeValueId == 1 && !string.IsNullOrEmpty(si.character)
                                   select si)
                    return si;
            }

            throw new Exception($"Edition {editionId} has no letters, this may be a problem the database.");
        }

        [Fact]
        public async Task CanAddAttributeToEdition()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var editionId = await editionCreator.CreateEdition();

                var request = new Get.V1_Editions_EditionId_SignInterpretationsAttributes(editionId);
                await request.Send(_client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                var httpData = request.HttpResponseObject;
                CreateAttributeValueDTO[] newAttrValues =
                {
                    new CreateAttributeValueDTO
                    {
                        value = "the first type",
                        cssDirectives = "color: blue;",
                        description = "This is the first of a new way to describe a piece of data"
                    }
                };
                var newAttribute = new CreateAttributeDTO
                {
                    attributeName = "new attr",
                    description = "Some very exciting new way to describe a piece of data",
                    values = newAttrValues
                };

                // Create the new attribute
                var createRequest =
                    new Post.V1_Editions_EditionId_SignInterpretationsAttributes(editionId, newAttribute);
                await createRequest.Send(
                    _client,
                    StartConnectionAsync,
                    true,
                    listenerUser: Request.DefaultUsers.User1,
                    deterministic: false,
                    requestRealtime: false,
                    listeningFor: createRequest.AvailableListeners.CreatedAttribute
                );
                var (respInfo, createdHttpAttribute, createdListenerAttribute) = (createRequest.HttpResponseMessage,
                    createRequest.HttpResponseObject, createRequest.CreatedAttribute);

                // Assert
                respInfo.EnsureSuccessStatusCode();
                createdHttpAttribute.ShouldDeepEqual(createdListenerAttribute);
                await request.Send(_client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                var newAttrs = request.HttpResponseObject;
                Assert.Single(newAttrs.attributes.Where(x => x.attributeName == newAttribute.attributeName
                                                             && x.description == newAttribute.description));
                var returnedNewAttr = newAttrs.attributes
                    .First(x => x.attributeName == newAttribute.attributeName
                                && x.description == newAttribute.description);
                Assert.Single(returnedNewAttr.values);
                Assert.Single(returnedNewAttr.values.Where(x => x.value == newAttrValues.First().value
                                                                && x.description == newAttrValues.First().description
                                                                && x.cssDirectives ==
                                                                newAttrValues.First().cssDirectives));
                Assert.Equal(httpData.attributes.Length + 1, newAttrs.attributes.Length);
            }
        }

        [Fact]
        public async Task CanCreateNewAttributeForSignInterpretation()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var editionId = await editionCreator.CreateEdition();
                var signInterpretation = await GetEditionSignInterpretation(editionId);
                var signInterpretationAddAttribute = new InterpretationAttributeCreateDTO
                {
                    attributeId = 8,
                    attributeValueId = 33
                };
                var request = new Post.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(
                    editionId,
                    signInterpretation.signInterpretationId, signInterpretationAddAttribute);

                // Act
                await request.Send(_client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1,
                    deterministic: true, requestRealtime: false,
                    listeningFor: request.AvailableListeners.UpdatedSignInterpretation);
                var (httpResponse, httpData, listenerData) = (request.HttpResponseMessage, request.HttpResponseObject,
                    request.UpdatedSignInterpretation);

                // Assert
                httpResponse.EnsureSuccessStatusCode();
                httpData.ShouldDeepEqual(listenerData);
                Assert.Equal(signInterpretation.attributes.Length + 1, httpData.attributes.Length);
                Assert.Equal(signInterpretation.signInterpretationId, httpData.signInterpretationId);
                Assert.Equal(signInterpretation.character, httpData.character);
                Assert.Equal(signInterpretation.commentary, httpData.commentary);
                Assert.Equal(signInterpretation.isVariant, httpData.isVariant);
                signInterpretation.rois.ShouldDeepEqual(httpData.rois);
                Assert.True(httpData.attributes.Any(x =>
                    x.attributeValueId == signInterpretationAddAttribute.attributeValueId));
                var responseAttr = httpData.attributes.FirstOrDefault(x =>
                    x.attributeValueId == signInterpretationAddAttribute.attributeValueId);
                Assert.Null(responseAttr.commentary);
                Assert.True(responseAttr.sequence.HasValue);
                Assert.Equal(0, responseAttr.sequence.Value);
                Assert.Equal(0, responseAttr.value);
                Assert.NotEqual<uint>(0, responseAttr.creatorId);
                Assert.NotEqual<uint>(0, responseAttr.editorId);
                Assert.NotEqual<uint>(0, responseAttr.attributeId);
                Assert.NotEqual<uint>(0, responseAttr.attributeValueId);
                Assert.NotEqual<uint>(0, responseAttr.interpretationAttributeId);
                Assert.False(string.IsNullOrEmpty(responseAttr.attributeValueString));
            }
        }

        [Fact]
        public async Task CanCreateNewSignInterpretation()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var editionId = await editionCreator.CreateEdition();
                var textFragments =
                    await EditionHelpers.GetEditionTextFragmentWithSigns(editionId, _client,
                        Request.DefaultUsers.User1);
                var textFragment = textFragments.textFragments.First(x => x.lines.Any(y => y.signs.Count > 2));
                var line = textFragment.lines.First(y => y.signs.Count > 2);
                var previousSignInterpretation = line.signs.First().signInterpretations.Last().signInterpretationId;
                var nextSignInterpretation = line.signs[1].signInterpretations.First().signInterpretationId;
                var newSignInterpretation = new SignInterpretationCreateDTO
                {
                    character = "ט",
                    isVariant = false,
                    commentary = new CommentaryCreateDTO
                    {
                        commentary = "I just made this one up."
                    },
                    attributes = new[]
                    {
                        new InterpretationAttributeCreateDTO
                        {
                            attributeId = 1,
                            attributeValueId = 1,
                            sequence = 1
                        }
                    },
                    lineId = line.lineId,
                    previousSignInterpretationIds = new[] { previousSignInterpretation },
                    nextSignInterpretationIds = new[] { nextSignInterpretation },
                    rois = new SetInterpretationRoiDTO[0]
                };

                // Act
                var newSignInterpretationRequest =
                    new Post.V1_Editions_EditionId_SignInterpretations(editionId, newSignInterpretation);
                await newSignInterpretationRequest.Send(
                    _client,
                    StartConnectionAsync,
                    true,
                    listenToEdition: true,
                    listeningFor: newSignInterpretationRequest.AvailableListeners.CreatedSignInterpretation,
                    requestRealtime: false);

                // Assert
                newSignInterpretationRequest.HttpResponseMessage.EnsureSuccessStatusCode();
                newSignInterpretationRequest.HttpResponseObject.ShouldDeepEqual(newSignInterpretationRequest
                    .CreatedSignInterpretation);
                Assert.Equal(2, newSignInterpretationRequest.HttpResponseObject.signInterpretations.Length);

                // Get the text of this text fragment again
                var alteredTextFragment =
                    await EditionHelpers.GetEditionTextFragmentWithSigns(editionId, _client,
                        Request.DefaultUsers.User1);
                var signs = alteredTextFragment.textFragments
                    .First(x => x.textFragmentId == textFragment.textFragmentId)
                    .lines.First(x => x.lineId == line.lineId).signs;

                // Make sure the two updated/new sign interpretations are in the stream
                signs.First().signInterpretations.First()
                    .ShouldDeepEqual(newSignInterpretationRequest.HttpResponseObject.signInterpretations.First());
                Assert.Contains(signs,
                    x => x.signInterpretations.Any(y =>
                        y.IsDeepEqual(newSignInterpretationRequest.HttpResponseObject.signInterpretations.First())));
                // Set our commentary to null for deep equal test
                newSignInterpretationRequest.HttpResponseObject.signInterpretations.Last().commentary = null;
                newSignInterpretationRequest.HttpResponseObject.signInterpretations.Last().attributes.First()
                    .commentary = null;
                var interpretationMatchingCreate = signs.Where(x => x.signInterpretations.Any(y =>
                    y.signInterpretationId == newSignInterpretationRequest.HttpResponseObject.signInterpretations
                        .Last().signInterpretationId));
                if (!interpretationMatchingCreate.Any())
                {
                    _output.WriteLine("********************************************");
                    _output.WriteLine("*Failure for CanCreateNewSignInterpretation:");
                    _output.WriteLine("*Desired new sign interpretation");
                    _output.WriteLine("********************************************");
                    _output.WriteLine(JsonSerializer.Serialize(newSignInterpretation, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                    }));
                    _output.WriteLine("\n********************************************");
                    _output.WriteLine("*Alterations from newly created sign interpretation:");
                    _output.WriteLine(
                        $"*Edition {editionId}, fragment {textFragment.textFragmentId}, line id {line.lineId}.");
                    _output.WriteLine("********************************************");
                    _output.WriteLine(JsonSerializer.Serialize(
                        newSignInterpretationRequest.HttpResponseObject.signInterpretations, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                        }));
                    _output.WriteLine("\n********************************************");
                    _output.WriteLine("*Get request sign interpretations:");
                    _output.WriteLine("********************************************");
                    _output.WriteLine(JsonSerializer.Serialize(signs, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                    }));

                    Assert.NotEmpty(interpretationMatchingCreate);
                }

                Assert.NotEmpty(interpretationMatchingCreate);
            }
        }

        [Fact]
        public async Task CanCreateSignInterpretationCommentary()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
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
                };
                var request = new Put.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary(
                    editionId,
                    signInterpretation.signInterpretationId, commentary);

                // Act
                await request.Send(_client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1,
                    deterministic: true, requestRealtime: false,
                    listeningFor: request.AvailableListeners.UpdatedSignInterpretation);
                var (httpResponse, httpData, listenerData) = (request.HttpResponseMessage,
                    request.HttpResponseObject, request.UpdatedSignInterpretation);

                // Assert
                httpResponse.EnsureSuccessStatusCode();
                httpData.ShouldDeepEqual(listenerData);
                Assert.Equal(signInterpretation.attributes.Length, httpData.attributes.Length);
                Assert.Equal(signInterpretation.rois.Length, httpData.rois.Length);
                Assert.Equal(signInterpretation.nextSignInterpretations.Length,
                    httpData.nextSignInterpretations.Length);
                Assert.Equal(signInterpretation.signInterpretationId, httpData.signInterpretationId);
                Assert.Equal(signInterpretation.character, httpData.character);
                Assert.NotEqual(signInterpretation.commentary, httpData.commentary);
                Assert.Equal(commentary.commentary, httpData.commentary.commentary);
                Assert.Equal(signInterpretation.isVariant, httpData.isVariant);

                // Try setting commentary to null
                commentary = new CommentaryCreateDTO
                {
                    commentary = null
                };
                var request2 = new Put.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary(
                    editionId,
                    signInterpretation.signInterpretationId, commentary);

                // Act
                await request2.Send(
                    _client,
                    StartConnectionAsync,
                    true,
                    listenerUser: Request.DefaultUsers.User1,
                    deterministic: true,
                    requestRealtime: false,
                    listeningFor: request2.AvailableListeners.UpdatedSignInterpretation);
                var (http2Response, http2Data, listener2Data) = (request2.HttpResponseMessage,
                    request2.HttpResponseObject, request2.UpdatedSignInterpretation);

                // Assert
                http2Response.EnsureSuccessStatusCode();
                http2Data.ShouldDeepEqual(listener2Data);
                Assert.Equal(signInterpretation.attributes.Length, http2Data.attributes.Length);
                Assert.Equal(signInterpretation.rois.Length, http2Data.rois.Length);
                Assert.Equal(signInterpretation.nextSignInterpretations.Length,
                    http2Data.nextSignInterpretations.Length);
                Assert.Equal(signInterpretation.signInterpretationId, http2Data.signInterpretationId);
                Assert.Equal(signInterpretation.character, http2Data.character);
                Assert.Null(http2Data.commentary);
                Assert.Equal(signInterpretation.isVariant, http2Data.isVariant);
            }
        }

        [Fact]
        public async Task CanDeleteAttributeFromEdition()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var editionId = await editionCreator.CreateEdition();

                var request = new Get.V1_Editions_EditionId_SignInterpretationsAttributes(editionId);
                await request.Send(_client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                var httpData = request.HttpResponseObject;
                var deleteAttribute = httpData.attributes.Last().attributeId;

                // Delete the new attribute
                var deleteRequest =
                    new Delete.V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(editionId,
                        deleteAttribute);
                await deleteRequest.Send(
                    _client,
                    StartConnectionAsync,
                    true,
                    listenerUser: Request.DefaultUsers.User1,
                    deterministic: false,
                    requestRealtime: false,
                    listeningFor: deleteRequest.AvailableListeners.DeletedAttribute
                );
                var (respInfo, updatedListenerAttribute) =
                    (deleteRequest.HttpResponseMessage, deleteRequest.DeletedAttribute);

                // Assert
                respInfo.EnsureSuccessStatusCode();
                Assert.Equal(deleteAttribute, updatedListenerAttribute.ids.First());
                Assert.Equal(EditionEntities.attribute, updatedListenerAttribute.entity);
                await request.Send(_client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                var newAttrs = request.HttpResponseObject;
                Assert.Empty(newAttrs.attributes.Where(x => x.attributeId == deleteAttribute));
                Assert.Equal(httpData.attributes.Length - 1, newAttrs.attributes.Length);
            }
        }

        [Fact]
        public async Task CanDeleteAttributeFromSignInterpretation()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var editionId = await editionCreator.CreateEdition();
                var signInterpretation = await GetEditionSignInterpretation(editionId);
                var signInterpretationAddAttribute = new InterpretationAttributeCreateDTO
                {
                    attributeId = 8,
                    attributeValueId = 33
                };
                var request = new Post.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(
                    editionId,
                    signInterpretation.signInterpretationId, signInterpretationAddAttribute);

                // Act
                await request.Send(
                    _client,
                    StartConnectionAsync,
                    true,
                    listenerUser: Request.DefaultUsers.User1,
                    deterministic: true,
                    requestRealtime: false,
                    listeningFor: request.AvailableListeners.UpdatedSignInterpretation);
                var (httpResponse, httpData, listenerData) = (request.HttpResponseMessage,
                    request.HttpResponseObject, request.UpdatedSignInterpretation);

                // Assert
                httpResponse.EnsureSuccessStatusCode();
                httpData.ShouldDeepEqual(listenerData);
                Assert.Equal(signInterpretation.attributes.Length + 1, httpData.attributes.Length);
                Assert.Equal(signInterpretation.signInterpretationId, httpData.signInterpretationId);
                Assert.Equal(signInterpretation.character, httpData.character);
                Assert.Equal(signInterpretation.commentary, httpData.commentary);
                Assert.Equal(signInterpretation.isVariant, httpData.isVariant);
                signInterpretation.rois.ShouldDeepEqual(httpData.rois);
                Assert.True(httpData.attributes.Any(x =>
                    x.attributeValueId == signInterpretationAddAttribute.attributeValueId));
                var responseAttr = httpData.attributes.FirstOrDefault(x =>
                    x.attributeValueId == signInterpretationAddAttribute.attributeValueId);
                Assert.Null(responseAttr.commentary);
                Assert.True(responseAttr.sequence.HasValue);
                Assert.Equal(0, responseAttr.sequence.Value);
                Assert.Equal(0, responseAttr.value);
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
                            editionId,
                            signInterpretation.signInterpretationId, 33);
                await delRequest.Send(
                    _client,
                    StartConnectionAsync,
                    true,
                    listenerUser: Request.DefaultUsers.User1,
                    deterministic: true,
                    requestRealtime: false,
                    listeningFor: delRequest.AvailableListeners.UpdatedSignInterpretation);
                var (delResponse, delListenerData) =
                    (delRequest.HttpResponseMessage, delRequest.UpdatedSignInterpretation);

                // Assert
                delResponse.EnsureSuccessStatusCode();
                Assert.Equal(signInterpretation.attributes.Length, delListenerData.attributes.Length);
                Assert.Equal(signInterpretation.signInterpretationId, delListenerData.signInterpretationId);
                Assert.Equal(signInterpretation.character, delListenerData.character);
                Assert.Equal(signInterpretation.commentary, delListenerData.commentary);
                Assert.Equal(signInterpretation.isVariant, delListenerData.isVariant);
                signInterpretation.rois.ShouldDeepEqual(delListenerData.rois);
                Assert.False(delListenerData.attributes.Any(x =>
                    x.attributeValueId == signInterpretationAddAttribute.attributeValueId));
            }
        }

        [Fact]
        public async Task CanGetAllEditionSignInterpretationAttributes()
        {
            // Arrange
            var request = new Get.V1_Editions_EditionId_SignInterpretationsAttributes(EditionHelpers.GetEditionId());

            // Act
            await request.Send(
                _client,
                StartConnectionAsync,
                true);
            var (httpResponse, httpData, signalRData) = (request.HttpResponseMessage, request.HttpResponseObject,
                request.SignalrResponseObject);

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

        [Fact]
        public async Task CanGetAttributesOfSpecificSignInterpretation()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var editionId = await editionCreator.CreateEdition();
                var signInterpretation = await GetEditionSignInterpretation(editionId);

                var request =
                    new Get.V1_Editions_EditionId_SignInterpretations_SignInterpretationId(editionId,
                        signInterpretation.signInterpretationId);

                // Act
                await request.Send(_client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1,
                    deterministic: true, requestRealtime: true);
                var (httpResponse, httpData, signalrData) = (request.HttpResponseMessage, request.HttpResponseObject,
                    request.SignalrResponseObject);

                // Assert
                httpResponse.EnsureSuccessStatusCode();
                httpData.ShouldDeepEqual(signalrData);
                Assert.Equal(signInterpretation.attributes.Length, httpData.attributes.Length);
                Assert.Equal(signInterpretation.signInterpretationId, httpData.signInterpretationId);
                Assert.Equal(signInterpretation.character, httpData.character);
                Assert.Equal(signInterpretation.commentary, httpData.commentary);
                Assert.Equal(signInterpretation.isVariant, httpData.isVariant);
                signInterpretation.rois.ShouldDeepEqual(httpData.rois);
                signInterpretation.attributes.ShouldDeepEqual(httpData.attributes);
            }
        }

        [Fact]
        public async Task CanUpdateAttributeInEdition()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var editionId = await editionCreator.CreateEdition();

                var request = new Get.V1_Editions_EditionId_SignInterpretationsAttributes(editionId);
                await request.Send(_client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                var httpData = request.HttpResponseObject;
                var attributeValueForUpdate = httpData.attributes.FirstOrDefault().values.FirstOrDefault();
                var attributeValueForDelete = httpData.attributes.FirstOrDefault().values.Last();
                var updateAttribute = new UpdateAttributeDTO
                {
                    createValues = new CreateAttributeValueDTO[1]
                    {
                        new CreateAttributeValueDTO
                        {
                            cssDirectives = "vertical-align: super; font-size: 50%;",
                            description = "Some small raised thing",
                            value = "TINY_UPPER"
                        }
                    },
                    updateValues = new UpdateAttributeValueDTO[1]
                    {
                        new UpdateAttributeValueDTO
                        {
                            id = attributeValueForUpdate.id,
                            cssDirectives = "font-weight: bold;",
                            description = attributeValueForUpdate.description,
                            value = "AMAZING_NEW_VALUE"
                        }
                    },
                    deleteValues = new uint[1] { attributeValueForDelete.id }
                };

                // Act
                var updateRequest = new Put.V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(editionId,
                    httpData.attributes.First().attributeId, updateAttribute);
                await updateRequest.Send(
                    _client,
                    StartConnectionAsync,
                    true,
                    listenerUser: Request.DefaultUsers.User1,
                    deterministic: false,
                    requestRealtime: false,
                    listenToEdition: true,
                    listeningFor: updateRequest.AvailableListeners.UpdatedAttribute
                );

                // Assert
                // The listener should return the same as the http request
                updateRequest.HttpResponseObject.ShouldDeepEqual(updateRequest.UpdatedAttribute);
                // The update contains the expected data
                Assert.Contains(updateRequest.HttpResponseObject.values, x =>
                    x.description == updateAttribute.createValues.FirstOrDefault().description
                    && x.cssDirectives == updateAttribute.createValues.FirstOrDefault().cssDirectives
                    && x.value == updateAttribute.createValues.FirstOrDefault().value);
                Assert.Contains(updateRequest.HttpResponseObject.values, x =>
                    x.id != attributeValueForUpdate.id
                    && x.description == updateAttribute.updateValues.FirstOrDefault().description
                    && x.cssDirectives == updateAttribute.updateValues.FirstOrDefault().cssDirectives
                    && x.value == updateAttribute.updateValues.FirstOrDefault().value);
                Assert.DoesNotContain(updateRequest.HttpResponseObject.values, x => x.id == attributeValueForDelete.id);

                // The update appears when getting all attributes
                await request.Send(_client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                var newAttrs = request.HttpResponseObject;
                Assert.Contains(newAttrs.attributes, x => x.attributeId == httpData.attributes.First().attributeId);
                newAttrs.attributes.First(x => x.attributeId == httpData.attributes.First().attributeId)
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
        public async Task CanUpdateAttributeOfSignInterpretation()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var editionId = await editionCreator.CreateEdition();
                var signInterpretation = await GetEditionSignInterpretation(editionId);
                const uint attributeValueId = 33;
                var signInterpretationAddAttribute = new InterpretationAttributeCreateDTO
                {
                    attributeId = 8,
                    attributeValueId = attributeValueId
                };
                var request = new Post.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(
                    editionId,
                    signInterpretation.signInterpretationId, signInterpretationAddAttribute);

                // Act
                await request.Send(
                    _client,
                    StartConnectionAsync,
                    true,
                    listenerUser: Request.DefaultUsers.User1,
                    deterministic: true,
                    requestRealtime: false,
                    listeningFor: request.AvailableListeners.UpdatedSignInterpretation);
                var (httpResponse, httpData, listenerData) = (request.HttpResponseMessage,
                    request.HttpResponseObject, request.UpdatedSignInterpretation);

                // Assert
                httpResponse.EnsureSuccessStatusCode();
                httpData.ShouldDeepEqual(listenerData);
                Assert.Equal(signInterpretation.attributes.Length + 1, httpData.attributes.Length);
                Assert.Equal(signInterpretation.signInterpretationId, httpData.signInterpretationId);
                Assert.Equal(signInterpretation.character, httpData.character);
                Assert.Equal(signInterpretation.commentary, httpData.commentary);
                Assert.Equal(signInterpretation.isVariant, httpData.isVariant);
                signInterpretation.rois.ShouldDeepEqual(httpData.rois);
                Assert.True(httpData.attributes.Any(x =>
                    x.attributeValueId == signInterpretationAddAttribute.attributeValueId));
                var responseAttr = httpData.attributes.FirstOrDefault(x =>
                    x.attributeValueId == signInterpretationAddAttribute.attributeValueId);
                Assert.Null(responseAttr.commentary);
                Assert.True(responseAttr.sequence.HasValue);
                Assert.Equal(0, responseAttr.sequence.Value);
                Assert.Equal(0, responseAttr.value);
                Assert.NotEqual<uint>(0, responseAttr.creatorId);
                Assert.NotEqual<uint>(0, responseAttr.editorId);
                Assert.NotEqual<uint>(0, responseAttr.attributeId);
                Assert.NotEqual<uint>(0, responseAttr.attributeValueId);
                Assert.NotEqual<uint>(0, responseAttr.interpretationAttributeId);
                Assert.False(string.IsNullOrEmpty(responseAttr.attributeValueString));

                // Update sequence and value of the new attribute
                const byte seq = 1;
                const float val = (float)1.2;
                var updateSignInterpretationAddAttribute = new InterpretationAttributeCreateDTO
                {
                    attributeId = 8,
                    attributeValueId = attributeValueId,
                    sequence = seq,
                    value = val
                };
                var updRequest1 =
                    new Put.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(
                        editionId,
                        signInterpretation.signInterpretationId, attributeValueId,
                        updateSignInterpretationAddAttribute);
                await updRequest1.Send(
                    _client,
                    StartConnectionAsync,
                    true,
                    listenerUser: Request.DefaultUsers.User1,
                    deterministic: false,
                    requestRealtime: false,
                    listeningFor: updRequest1.AvailableListeners.UpdatedSignInterpretation);
                var (updResponse, updData, updListenerData) = (updRequest1.HttpResponseMessage,
                    updRequest1.HttpResponseObject, updRequest1.UpdatedSignInterpretation);

                // Assert
                updResponse.EnsureSuccessStatusCode();
                updData.ShouldDeepEqual(updListenerData);
                Assert.Equal(signInterpretation.attributes.Length + 1, updData.attributes.Length);
                Assert.Equal(signInterpretation.signInterpretationId, updData.signInterpretationId);
                Assert.Equal(signInterpretation.character, updData.character);
                Assert.Equal(signInterpretation.commentary, updData.commentary);
                Assert.Equal(signInterpretation.isVariant, updData.isVariant);
                Assert.True(updData.attributes
                    .Any(x => x.attributeValueId == attributeValueId));
                var upd1SignInterpretationAttribute = updData.attributes
                    .First(x => x.attributeValueId == attributeValueId);
                Assert.Equal(seq, upd1SignInterpretationAttribute.sequence);
                Assert.Equal(val, upd1SignInterpretationAttribute.value);
                Assert.Null(upd1SignInterpretationAttribute.commentary);

                // Update sequence and value of the new attribute
                const string commentary = "Here is a comment about בְּרֵאשִׁ֖ית";
                updateSignInterpretationAddAttribute = new InterpretationAttributeCreateDTO
                {
                    attributeId = 8,
                    attributeValueId = attributeValueId,
                    sequence = seq,
                    value = val,
                    commentary = commentary
                };
                var updRequest2 =
                    new Put.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(
                        editionId,
                        signInterpretation.signInterpretationId, attributeValueId,
                        updateSignInterpretationAddAttribute);
                await updRequest2.Send(
                    _client,
                    StartConnectionAsync,
                    true,
                    listenerUser: Request.DefaultUsers.User1,
                    deterministic: false,
                    requestRealtime: false,
                    listeningFor: updRequest2.AvailableListeners.UpdatedSignInterpretation);
                (updResponse, updData, updListenerData) = (updRequest2.HttpResponseMessage,
                    updRequest2.HttpResponseObject,
                    updRequest2.UpdatedSignInterpretation);

                // Assert
                updResponse.EnsureSuccessStatusCode();
                updData.ShouldDeepEqual(updListenerData);
                Assert.Equal(signInterpretation.attributes.Length + 1, updData.attributes.Length);
                Assert.Equal(signInterpretation.signInterpretationId, updData.signInterpretationId);
                Assert.Equal(signInterpretation.character, updData.character);
                Assert.Equal(signInterpretation.commentary, updData.commentary);
                Assert.Equal(signInterpretation.isVariant, updData.isVariant);
                Assert.True(updData.attributes
                    .Any(x => x.attributeValueId == attributeValueId));
                var upd2SignInterpretationAttribute = updData.attributes
                    .First(x => x.attributeValueId == attributeValueId);
                Assert.Equal(seq, upd2SignInterpretationAttribute.sequence);
                Assert.Equal(val, upd2SignInterpretationAttribute.value);
                Assert.NotNull(upd2SignInterpretationAttribute.commentary);
                Assert.Equal(commentary, upd2SignInterpretationAttribute.commentary.commentary);
            }
        }
    }
}