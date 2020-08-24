using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
    public class SignInterpretationTests : WebControllerTest
    {
        public SignInterpretationTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
        }

        [Fact]
        public async Task CanGetAllEditionSignInterpretationAttributes()
        {
            // Arrange
            var request = new Get.V1_Editions_EditionId_SignInterpretationsAttributes(EditionHelpers.GetEditionId());

            // Act
            var (httpResponse, httpData, signalRData, _) = await Request.Send(
                request,
                _client,
                StartConnectionAsync,
                true);

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

                var request = new Get.V1_Editions_EditionId_SignInterpretations_SignInterpretationId(editionId, signInterpretation.signInterpretationId);

                // Act
                var (httpResponse, httpData, signalrData, _) =
                    await Request.Send(request, _client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1, deterministic: true, requestRealtime: true);

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
        public async Task CanCreateNewAttributeForSignInterpretation()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var editionId = await editionCreator.CreateEdition();
                var signInterpretation = await GetEditionSignInterpretation(editionId);
                var signInterpretationAddAttribute = new InterpretationAttributeCreateDTO()
                {
                    attributeId = 8,
                    attributeValueId = 33,
                };
                var request = new Post.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(editionId,
                    signInterpretation.signInterpretationId, signInterpretationAddAttribute);

                // Act
                var (httpResponse, httpData, _, listenerData) =
                    await Request.Send(request, _client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1, deterministic: true, requestRealtime: false);

                // Assert
                httpResponse.EnsureSuccessStatusCode();
                httpData.ShouldDeepEqual(listenerData);
                Assert.Equal(signInterpretation.attributes.Length + 1, httpData.attributes.Length);
                Assert.Equal(signInterpretation.signInterpretationId, httpData.signInterpretationId);
                Assert.Equal(signInterpretation.character, httpData.character);
                Assert.Equal(signInterpretation.commentary, httpData.commentary);
                Assert.Equal(signInterpretation.isVariant, httpData.isVariant);
                signInterpretation.rois.ShouldDeepEqual(httpData.rois);
                Assert.True(httpData.attributes.Any(x => x.attributeValueId == signInterpretationAddAttribute.attributeValueId));
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
        public async Task CanAddAttributeToEdition()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var editionId = await editionCreator.CreateEdition();

                var request = new Get.V1_Editions_EditionId_SignInterpretationsAttributes(editionId);
                var (_, httpData, _, _) =
                    await Request.Send(request, _client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                CreateAttributeValueDTO[] newAttrValues = {new CreateAttributeValueDTO()
                {
                    value = "the first type",
                    cssDirectives = "color: blue;",
                    description = "This is the first of a new way to describe a piece of data"
                }};
                var newAttribute = new CreateAttributeDTO()
                {
                    attributeName = "new attr",
                    description = "Some very exciting new way to describe a piece of data",
                    values = newAttrValues
                };

                // Create the new attribute
                var createRequest = new Post.V1_Editions_EditionId_SignInterpretationsAttributes(editionId, newAttribute);
                var (respInfo, createdHttpAttribute, _, createdListenerAttribute) =
                    await Request.Send(
                        createRequest,
                        _client,
                        StartConnectionAsync,
                        true,
                        listenerUser: Request.DefaultUsers.User1,
                        deterministic: false,
                        requestRealtime: false
                    );

                // Assert
                respInfo.EnsureSuccessStatusCode();
                createdHttpAttribute.ShouldDeepEqual(createdListenerAttribute);
                var (_, newAttrs, _, _) =
                    await Request.Send(request, _client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                Assert.Single(newAttrs.attributes.Where(x => x.attributeName == newAttribute.attributeName
                                                               && x.description == newAttribute.description));
                var returnedNewAttr = newAttrs.attributes
                    .First(x => x.attributeName == newAttribute.attributeName
                                && x.description == newAttribute.description);
                Assert.Single(returnedNewAttr.values);
                Assert.Single(returnedNewAttr.values.Where(x => x.value == newAttrValues.First().value
                    && x.description == newAttrValues.First().description
                    && x.cssDirectives == newAttrValues.First().cssDirectives));
                Assert.Equal(httpData.attributes.Length + 1, newAttrs.attributes.Length);
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
                var (_, httpData, _, _) =
                    await Request.Send(request, _client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                var attributeValueForUpdate = httpData.attributes.FirstOrDefault().values.FirstOrDefault();
                var attributeValueForDelete = httpData.attributes.FirstOrDefault().values.Last();
                var updateAttribute = new UpdateAttributeDTO()
                {
                    createValues = new CreateAttributeValueDTO[1] {new CreateAttributeValueDTO()
                    {
                        cssDirectives = "vertical-align: super; font-size: 50%;",
                        description = "Some small raised thing",
                        value = "TINY_UPPER"
                    }},
                    updateValues = new UpdateAttributeValueDTO[1] {new UpdateAttributeValueDTO()
                    {
                        id = attributeValueForUpdate.id,
                        cssDirectives = "font-weight: bold;",
                        description = attributeValueForUpdate.description,
                        value = "AMAZING_NEW_VALUE"
                    }},
                    deleteValues = new uint[1] { attributeValueForDelete.id }
                };

                // Act
                var updateRequest = new Put.V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(editionId, httpData.attributes.First().attributeId, updateAttribute);
                await updateRequest.Send(
                        _client,
                        StartConnectionAsync,
                        true,
                        listenerUser: Request.DefaultUsers.User1,
                        deterministic: false,
                        requestRealtime: false,
                        listenToEdition: true,
                        listeningFor: new List<ListenerMethods>() { updateRequest.AvailableListeners.UpdatedAttribute }
                    );

                // Assert
                updateRequest.HttpResponseObject.ShouldDeepEqual(updateRequest.UpdatedAttribute);
                // The update contains the expected data
                Assert.Contains(updateRequest.HttpResponseObject.values, (x =>
                    x.description == updateAttribute.createValues.FirstOrDefault().description
                    && x.cssDirectives == updateAttribute.createValues.FirstOrDefault().cssDirectives
                    && x.value == updateAttribute.createValues.FirstOrDefault().value));
                Assert.Contains(updateRequest.HttpResponseObject.values, (x =>
                    x.id != attributeValueForUpdate.id
                    && x.description == updateAttribute.updateValues.FirstOrDefault().description
                    && x.cssDirectives == updateAttribute.updateValues.FirstOrDefault().cssDirectives
                    && x.value == updateAttribute.updateValues.FirstOrDefault().value));
                Assert.DoesNotContain(updateRequest.HttpResponseObject.values, (x => x.id == attributeValueForDelete.id));

                // The update appears when getting all attributes
                var (_, newAttrs, _, _) =
                    await Request.Send(request, _client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                Assert.Contains(newAttrs.attributes, (x => x.attributeId == httpData.attributes.First().attributeId));
                newAttrs.attributes.First(x => x.attributeId == httpData.attributes.First().attributeId)
                    .ShouldDeepEqual(updateRequest.HttpResponseObject);
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
                var (_, httpData, _, _) =
                    await Request.Send(request, _client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                var deleteAttribute = httpData.attributes.Last().attributeId;

                // Delete the new attribute
                var deleteRequest = new Delete.V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(editionId, deleteAttribute);
                var (respInfo, updatedHttpAttribute, _, updatedListenerAttribute) =
                    await Request.Send(
                        deleteRequest,
                        _client,
                        StartConnectionAsync,
                        true,
                        listenerUser: Request.DefaultUsers.User1,
                        deterministic: false,
                        requestRealtime: false
                    );

                // Assert
                respInfo.EnsureSuccessStatusCode();
                Assert.Equal(deleteAttribute, updatedListenerAttribute.ids.First());
                Assert.Equal(EditionEntities.attribute, updatedListenerAttribute.entity);
                var (_, newAttrs, _, _) =
                    await Request.Send(request, _client, StartConnectionAsync, true, deterministic: true, requestRealtime: true);
                Assert.Empty(newAttrs.attributes.Where(x => x.attributeId == deleteAttribute));
                Assert.Equal(httpData.attributes.Length - 1, newAttrs.attributes.Length);
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
                var commentary = new CommentaryCreateDTO()
                {
                    commentary = @"#Commentary on the sign level

##Heading two

[qumranica](https://www.qumranica.org)

* point one",
                };
                var request = new Put.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary(editionId,
                    signInterpretation.signInterpretationId, commentary);

                // Act
                var (httpResponse, httpData, _, listenerData) =
                    await Request.Send(request, _client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1, deterministic: true, requestRealtime: false);

                // Assert
                httpResponse.EnsureSuccessStatusCode();
                httpData.ShouldDeepEqual(listenerData);
                Assert.Equal(signInterpretation.attributes.Length, httpData.attributes.Length);
                Assert.Equal(signInterpretation.rois.Length, httpData.rois.Length);
                Assert.Equal(signInterpretation.nextSignInterpretations.Length, httpData.nextSignInterpretations.Length);
                Assert.Equal(signInterpretation.signInterpretationId, httpData.signInterpretationId);
                Assert.Equal(signInterpretation.character, httpData.character);
                Assert.NotEqual(signInterpretation.commentary, httpData.commentary);
                Assert.Equal(commentary.commentary, httpData.commentary.commentary);
                Assert.Equal(signInterpretation.isVariant, httpData.isVariant);

                // Try setting commentary to null
                commentary = new CommentaryCreateDTO()
                {
                    commentary = null
                };
                var request2 = new Put.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary(editionId,
                    signInterpretation.signInterpretationId, commentary);

                // Act
                var (http2Response, http2Data, _, listener2Data) =
                    await Request.Send(request2, _client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1, deterministic: true, requestRealtime: false);

                // Assert
                http2Response.EnsureSuccessStatusCode();
                http2Data.ShouldDeepEqual(listener2Data);
                Assert.Equal(signInterpretation.attributes.Length, http2Data.attributes.Length);
                Assert.Equal(signInterpretation.rois.Length, http2Data.rois.Length);
                Assert.Equal(signInterpretation.nextSignInterpretations.Length, http2Data.nextSignInterpretations.Length);
                Assert.Equal(signInterpretation.signInterpretationId, http2Data.signInterpretationId);
                Assert.Equal(signInterpretation.character, http2Data.character);
                Assert.Null(http2Data.commentary);
                Assert.Equal(signInterpretation.isVariant, http2Data.isVariant);
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
                var signInterpretationAddAttribute = new InterpretationAttributeCreateDTO()
                {
                    attributeId = 8,
                    attributeValueId = attributeValueId,
                };
                var request = new Post.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(editionId,
                    signInterpretation.signInterpretationId, signInterpretationAddAttribute);

                // Act
                var (httpResponse, httpData, _, listenerData) =
                    await Request.Send(request, _client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1, deterministic: true, requestRealtime: false);

                // Assert
                httpResponse.EnsureSuccessStatusCode();
                httpData.ShouldDeepEqual(listenerData);
                Assert.Equal(signInterpretation.attributes.Length + 1, httpData.attributes.Length);
                Assert.Equal(signInterpretation.signInterpretationId, httpData.signInterpretationId);
                Assert.Equal(signInterpretation.character, httpData.character);
                Assert.Equal(signInterpretation.commentary, httpData.commentary);
                Assert.Equal(signInterpretation.isVariant, httpData.isVariant);
                signInterpretation.rois.ShouldDeepEqual(httpData.rois);
                Assert.True(httpData.attributes.Any(x => x.attributeValueId == signInterpretationAddAttribute.attributeValueId));
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
                var updateSignInterpretationAddAttribute = new InterpretationAttributeCreateDTO()
                {
                    attributeId = 8,
                    attributeValueId = attributeValueId,
                    sequence = seq,
                    value = val,
                };
                var updRequest1 =
                    new Put.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(editionId,
                        signInterpretation.signInterpretationId, attributeValueId, updateSignInterpretationAddAttribute);
                var (updResponse, updData, _, updListenerData) =
                    await Request.Send(updRequest1, _client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1, deterministic: false, requestRealtime: false);

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
                updateSignInterpretationAddAttribute = new InterpretationAttributeCreateDTO()
                {
                    attributeId = 8,
                    attributeValueId = attributeValueId,
                    sequence = seq,
                    value = val,
                    commentary = commentary
                };
                var updRequest2 =
                    new Put.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(editionId,
                        signInterpretation.signInterpretationId, attributeValueId, updateSignInterpretationAddAttribute);
                (updResponse, updData, _, updListenerData) =
                    await Request.Send(updRequest2, _client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1, deterministic: false, requestRealtime: false);

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

        [Fact]
        public async Task CanDeleteAttributeFromSignInterpretation()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var editionId = await editionCreator.CreateEdition();
                var signInterpretation = await GetEditionSignInterpretation(editionId);
                var signInterpretationAddAttribute = new InterpretationAttributeCreateDTO()
                {
                    attributeId = 8,
                    attributeValueId = 33,
                };
                var request = new Post.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(editionId,
                    signInterpretation.signInterpretationId, signInterpretationAddAttribute);

                // Act
                var (httpResponse, httpData, _, listenerData) =
                    await Request.Send(request, _client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1, deterministic: true, requestRealtime: false);

                // Assert
                httpResponse.EnsureSuccessStatusCode();
                httpData.ShouldDeepEqual(listenerData);
                Assert.Equal(signInterpretation.attributes.Length + 1, httpData.attributes.Length);
                Assert.Equal(signInterpretation.signInterpretationId, httpData.signInterpretationId);
                Assert.Equal(signInterpretation.character, httpData.character);
                Assert.Equal(signInterpretation.commentary, httpData.commentary);
                Assert.Equal(signInterpretation.isVariant, httpData.isVariant);
                signInterpretation.rois.ShouldDeepEqual(httpData.rois);
                Assert.True(httpData.attributes.Any(x => x.attributeValueId == signInterpretationAddAttribute.attributeValueId));
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
                    new Delete.V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(editionId,
                        signInterpretation.signInterpretationId, 33);
                var (delResponse, delData, _, delListenerData) =
                    await Request.Send(delRequest, _client, StartConnectionAsync, true, listenerUser: Request.DefaultUsers.User1, deterministic: true, requestRealtime: false);

                // Assert
                delResponse.EnsureSuccessStatusCode();
                Assert.Equal(signInterpretation.attributes.Length, delListenerData.attributes.Length);
                Assert.Equal(signInterpretation.signInterpretationId, delListenerData.signInterpretationId);
                Assert.Equal(signInterpretation.character, delListenerData.character);
                Assert.Equal(signInterpretation.commentary, delListenerData.commentary);
                Assert.Equal(signInterpretation.isVariant, delListenerData.isVariant);
                signInterpretation.rois.ShouldDeepEqual(delListenerData.rois);
                Assert.False(delListenerData.attributes.Any(x => x.attributeValueId == signInterpretationAddAttribute.attributeValueId));
            }
        }

        /// <summary>
        /// Find a sign interpretation id in the edition
        /// </summary>
        /// <param name="editionId"></param>
        /// <returns></returns>
        private async Task<SignInterpretationDTO> GetEditionSignInterpretation(uint editionId)
        {
            var textFragmentsRequest = new Get.V1_Editions_EditionId_TextFragments(editionId);
            var (_, textFragments, _, _) = await Request.Send(textFragmentsRequest, _client, auth: true);
            foreach (var textRequest in textFragments.textFragments.Select(tf => new Get.V1_Editions_EditionId_TextFragments_TextFragmentId(editionId, tf.id)))
            {
                var (_, text, _, _) = await Request.Send(textRequest, _client, auth: true);
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
                {
                    return si;
                }
            }
            throw new Exception($"Edition {editionId} has no letters, this may be a problem the database.");
        }
    }
}