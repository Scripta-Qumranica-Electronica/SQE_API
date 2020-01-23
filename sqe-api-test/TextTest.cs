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
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
    public class TextTest : WebControllerTest
    {
        public TextTest(WebApplicationFactory<Startup> factory) : base(factory)
        {
        }

        private async Task<(uint editionId, uint textFragmentId)> _getTextFragmentIds(uint? editionId = null)
        {
            var usedEditionId = editionId ?? EditionHelpers.GetEditionId();

            var textFragmentRequestObject = new Get.V1_Editions_EditionId_TextFragments(usedEditionId);
            var (fragmentsResponse, textFragments, _, _) =
                await Request.Send(
                    textFragmentRequestObject,
                    _client,
                    StartConnectionAsync
                );
            fragmentsResponse.EnsureSuccessStatusCode();

            return (usedEditionId, textFragmentId: textFragments.textFragments.First().id);
        }

        private async Task<uint> _getClonedEdition()
        {
            return await EditionHelpers.CreateCopyOfEdition(_client, EditionHelpers.GetEditionId()); // Clone it
        }

        /// <summary>
        ///     Returns a listing of text fragments for the specified edition id
        /// </summary>
        /// <param name="editionId">Id of the edition to search for text fragments</param>
        /// <param name="auth">Whether the request should be authorized with user credentials, default is false</param>
        /// <param name="user">
        ///     The user object to use for authorization, if auth is true and this is unspecfied/null
        ///     `Request.DefaultUsers.User1` will be used
        /// </param>
        /// <returns>All text fragments in the edition</returns>
        private async Task<TextFragmentDataListDTO> _getEditionTextFragments(
            uint editionId,
            bool auth = false,
            Request.UserAuthDetails user = null)
        {
            if (auth && user == null)
                user = Request.DefaultUsers.User1;

            var newTextFragReqObj = new Get.V1_Editions_EditionId_TextFragments(editionId);
            var (fragmentsResponse, textFragments, _, _) = await Request.Send(
                newTextFragReqObj,
                _client,
                StartConnectionAsync,
                user1: user,
                auth: auth
            );

            fragmentsResponse.EnsureSuccessStatusCode();
            return textFragments;
        }

        private async Task<(uint editionId, TextFragmentDataListDTO textFragments)> _createEditionWithTextFragments(
            EditionHelpers.EditionCreator editionCreator)
        {
            // TODO make this disposable, and use using (we want to delete the new edition regardless of test success)
            var editionId =
                await editionCreator.CreateEdition(); // Get a newly cloned edition
            var textFragments =
                await _getEditionTextFragments(editionId, true); // Get all the text fragments in the edition
            return (editionId, textFragments);
        }

        private async Task<(HttpResponseMessage response, TextFragmentDataDTO msg)> _createTextFragment(
            uint editionId,
            string textFragmentName,
            bool shouldSucceed = true,
            uint? previousTextFragmentId = null,
            uint? nextTextFragmentId = null,
            bool authenticated = true,
            Request.UserAuthDetails user = null,
            bool realtime = false)
        {
            if (authenticated && user == null)
                user = Request.DefaultUsers.User1;
            var newTextFragmentRequestObject = new Post.V1_Editions_EditionId_TextFragments(
                editionId,
                new CreateTextFragmentDTO
                {
                    previousTextFragmentId = previousTextFragmentId,
                    name = textFragmentName,
                    nextTextFragmentId = nextTextFragmentId
                }
            );
            // You can run this realtime or HTTP, both should be tested at least once
            var (response, msg, realtimeMsg, _) = realtime
                ? await Request.Send(
                    newTextFragmentRequestObject,
                    null,
                    StartConnectionAsync,
                    user1: user,
                    auth: authenticated,
                    shouldSucceed: shouldSucceed,
                    deterministic: false
                )
                : await Request.Send(
                    newTextFragmentRequestObject,
                    _client,
                    null,
                    user1: user,
                    auth: authenticated,
                    shouldSucceed: shouldSucceed,
                    deterministic: false
                );
            if (shouldSucceed && !realtime)
                response.EnsureSuccessStatusCode();
            return (response, realtime ? realtimeMsg : msg);
        }

        private async Task<(uint editionId, uint textFragmentId, uint lineId)> _getLine()
        {
            var (editionId, textFragmentId) = await _getTextFragmentIds();
            var getLineDataRequestObject = new Get.V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(
                editionId,
                textFragmentId
            );
            var (lineResponse, lines, _, _) = await Request.Send(
                getLineDataRequestObject,
                _client,
                StartConnectionAsync
            );
            lineResponse.EnsureSuccessStatusCode();

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
            Assert.NotNull(msg.signs.First().signInterpretations.First().attributes.First().attributeValueString);

            var editorIds = new List<uint> { msg.editorId };
            foreach (var sign in msg.signs)
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
            Assert.NotNull(
                msg.textFragments.First()
                    .lines.First()
                    .signs.First()
                    .signInterpretations.First()
                    .attributes.First()
                    .attributeValueString
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

            Assert.NotEmpty(editorIds);
            foreach (var editorId in editorIds) Assert.True(msg.editors.ContainsKey(editorId));
        }

        [Fact]
        public async Task CanAddTextFragmentAfter()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);
                const string textFragmentName = "my can add after col";
                var previousFragmentId =
                    textFragments.textFragments.First().id; // We will make the new text fragment number two
                var numberOfTextFragments = textFragments.textFragments.Count;

                // Act
                var newTextFragmentRequestObject = new Post.V1_Editions_EditionId_TextFragments(
                    editionId,
                    new CreateTextFragmentDTO
                    {
                        previousTextFragmentId = previousFragmentId,
                        name = textFragmentName,
                        nextTextFragmentId = null
                    }
                );
                var (response, msg, _, _) = await Request.Send(
                    newTextFragmentRequestObject,
                    _client,
                    null,
                    auth: true,
                    deterministic: false
                );

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Empty(textFragments.textFragments.Where(x => x.id == msg.id || x.name == msg.name));
                var updatedTextFragments =
                    await _getEditionTextFragments(
                        editionId,
                        true
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
            }
        }

        [Fact]
        public async Task CanAddTextFragmentBefore()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);
                const string textFragmentName = "my can add before col";
                var nextFragmentId =
                    textFragments.textFragments.Last().id; // We will make the new text fragment second to last
                var numberOfTextFragments = textFragments.textFragments.Count;

                // Act
                var newTextFragmentRequestObject = new Post.V1_Editions_EditionId_TextFragments(
                    editionId,
                    new CreateTextFragmentDTO
                    {
                        previousTextFragmentId = null,
                        name = textFragmentName,
                        nextTextFragmentId = nextFragmentId
                    }
                );
                var (response, msg, _, _) = await Request.Send(
                    newTextFragmentRequestObject,
                    _client,
                    null,
                    auth: true,
                    deterministic: false
                );

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Empty(textFragments.textFragments.Where(x => x.id == msg.id || x.name == msg.name));
                var updatedTextFragments =
                    await _getEditionTextFragments(
                        editionId,
                        true
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
            }
        }

        [Fact]
        public async Task CanAddTextFragmentBeforeAndAfter()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);
                {
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
                            true
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
                }
            }
        }

        [Fact]
        public async Task CanAddTextFragmentToEnd()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);
                const string textFragmentName = "my new can add to end col";
                var numberOfTextFragments = textFragments.textFragments.Count;

                // Act
                var (_, msg) = await _createTextFragment(
                    editionId,
                    textFragmentName,
                    realtime: true
                );

                // Assert
                Assert.Empty(textFragments.textFragments.Where(x => x.id == msg.id || x.name == msg.name));
                var updatedTextFragments =
                    await _getEditionTextFragments(
                        editionId,
                        true
                    ); // Get the updated list of text fragments in the edition
                Assert.NotEmpty(updatedTextFragments.textFragments.Where(x => x.id == msg.id));
                Assert.Equal(msg.id, updatedTextFragments.textFragments.Last().id);
                Assert.Equal(msg.name, updatedTextFragments.textFragments.Last().name);
                Assert.Equal(numberOfTextFragments + 1, updatedTextFragments.textFragments.Count);

                for (var i = 0; i < textFragments.textFragments.Count; i++)
                    textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);
            }
        }

        [Fact]
        public async Task CanMoveTextFragmentAfter()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);

                // Act
                var newTextFragmentRequestObject = new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
                    editionId,
                    textFragments.textFragments.First().id,
                    new UpdateTextFragmentDTO()
                    {
                        previousTextFragmentId = textFragments.textFragments.Last().id,
                        name = null,
                        nextTextFragmentId = null
                    }
                );
                var (response, msg, _, _) = await Request.Send(
                    newTextFragmentRequestObject,
                    _client,
                    null,
                    auth: true,
                    deterministic: false
                );

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var updatedTextFragments =
                    await _getEditionTextFragments(
                        editionId,
                        true
                    ); // Get the updated list of text fragments in the edition
                // Check that nothing has changed for the moved text fragment
                textFragments.textFragments.First().ShouldDeepEqual(updatedTextFragments.textFragments.Last());

                // Make sure that nothing else has changed to the pre-existing text fragments
                var originalShifted = textFragments.textFragments.Skip(1);
                var updatedShifted =
                    updatedTextFragments.textFragments.Take(updatedTextFragments.textFragments.Count - 1);
                originalShifted.ShouldDeepEqual(updatedShifted);
            }
        }

        [Fact]
        public async Task CanMoveTextFragmentBefore()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);

                // Act
                var newTextFragmentRequestObject = new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
                    editionId,
                    textFragments.textFragments.Last().id,
                    new UpdateTextFragmentDTO()
                    {
                        previousTextFragmentId = null,
                        name = null,
                        nextTextFragmentId = textFragments.textFragments.First().id
                    }
                );
                var (response, msg, _, _) = await Request.Send(
                    newTextFragmentRequestObject,
                    _client,
                    null,
                    auth: true,
                    deterministic: false
                );

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var updatedTextFragments =
                    await _getEditionTextFragments(
                        editionId,
                        true
                    ); // Get the updated list of text fragments in the edition
                // Check that nothing has changed for the moved text fragment
                textFragments.textFragments.Last().ShouldDeepEqual(updatedTextFragments.textFragments.First());

                // Make sure that nothing else has changed to the pre-existing text fragments
                var originalShifted = textFragments.textFragments.Take(textFragments.textFragments.Count - 1);
                var updatedShifted =
                    updatedTextFragments.textFragments.Skip(1);
                originalShifted.ShouldDeepEqual(updatedShifted);
            }
        }

        [Fact]
        public async Task CanMoveTextFragmentBeforeAndAfter()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);

                // Act
                var newTextFragmentRequestObject = new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
                    editionId,
                    textFragments.textFragments.First().id,
                    new UpdateTextFragmentDTO()
                    {
                        previousTextFragmentId = textFragments.textFragments[1].id,
                        name = null,
                        nextTextFragmentId = textFragments.textFragments[2].id
                    }
                );
                var (response, msg, _, _) = await Request.Send(
                    newTextFragmentRequestObject,
                    _client,
                    null,
                    auth: true,
                    deterministic: false
                );

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var updatedTextFragments =
                    await _getEditionTextFragments(
                        editionId,
                        true
                    ); // Get the updated list of text fragments in the edition
                // Check that nothing has changed for the moved text fragment
                textFragments.textFragments.First().ShouldDeepEqual(updatedTextFragments.textFragments[1]);

                // Make sure that nothing else has changed to the pre-existing text fragments
                var originalShifted = textFragments.textFragments.Skip(1).ToList();
                var updatedShifted = updatedTextFragments.textFragments.Take(1).ToList().Concat(updatedTextFragments.textFragments.Skip(2).ToList());
                originalShifted.ShouldDeepEqual(updatedShifted);
            }
        }

        [Fact]
        public async Task CanGetAnonymousEditionTextFragment()
        {
            // Arrange
            var (editionId, textFragmentId) = await _getTextFragmentIds();

            // Act
            var textFragmentRequestObject = new Get.V1_Editions_EditionId_TextFragments_TextFragmentId(
                editionId,
                textFragmentId
            );
            var (response, msg, _, _) = await Request.Send(
                textFragmentRequestObject,
                _client,
                StartConnectionAsync
            );

            // Assert
            response.EnsureSuccessStatusCode();
            _verifyTextEditionDTO(msg); // Verify we got expected data
        }

        [Fact]
        public async Task CanGetAnonymousEditionTextFragmentData()
        {
            // Arrange
            var edition = await EditionHelpers.GetEdition(_client);
            var editionId = edition.id;

            // Act
            var textFragmentDataRequestObject = new Get.V1_Editions_EditionId_TextFragments(editionId);
            var (response, msg, _, _) = await Request.Send(
                textFragmentDataRequestObject,
                _client,
                StartConnectionAsync
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
            var textLineRequestObject = new Get.V1_Editions_EditionId_Lines_LineId(editionId, lineId);
            var (response, msg, _, _) = await Request.Send(
                textLineRequestObject,
                _client,
                StartConnectionAsync
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
            var lineDataRequestObject = new Get.V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(
                editionId,
                textFragmentId
            );
            var (response, msg, _, _) = await Request.Send(
                lineDataRequestObject,
                _client,
                StartConnectionAsync
            );

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotEmpty(msg.lines);
            Assert.NotEqual((uint)0, msg.lines[0].lineId);
        }

        [Fact]
        public async Task CannotAddTextFragmentWithBlankName()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);
                const string textFragmentName = "";
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
                        true
                    ); // Get the list of text fragments in the edition again
                Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

                // Make sure that nothing else has changed to the pre-existing text fragments
                for (var i = 0; i < textFragments.textFragments.Count; i++)
                    textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);
            }
        }

        [Fact]
        public async Task CannotAddTextFragmentWithNullName()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);
                const string textFragmentName = null;
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
                        true
                    ); // Get the list of text fragments in the edition again
                Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

                // Make sure that nothing else has changed to the pre-existing text fragments
                for (var i = 0; i < textFragments.textFragments.Count; i++)
                    textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);
            }
        }

        [Fact]
        public async Task CannotAddTextFragmentAfterTextFragmentNotInEdition()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);
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
                        true
                    ); // Get the list of text fragments in the edition again
                Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

                // Make sure that nothing else has changed to the pre-existing text fragments
                for (var i = 0; i < textFragments.textFragments.Count; i++)
                    textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);
            }
        }

        [Fact]
        public async Task CannotAddTextFragmentBeforeTextFragmentNotInEdition()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);
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
                        true
                    ); // Get the list of text fragments in the edition again
                Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

                // Make sure that nothing else has changed to the pre-existing text fragments
                for (var i = 0; i < textFragments.textFragments.Count; i++)
                    textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);
            }
        }

        // It is probably best from the perspective of the API consumer that this test should pass.
        [Fact]
        public async Task CannotAddTextFragmentBetweenNonSequentialTextFragments()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);
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
                        true
                    ); // Get the list of text fragments in the edition again
                Assert.Equal(numberOfTextFragments, updatedTextFragments.textFragments.Count);

                // Make sure that nothing else has changed to the pre-existing text fragments
                for (var i = 0; i < textFragments.textFragments.Count; i++)
                    textFragments.textFragments[i].ShouldDeepEqual(updatedTextFragments.textFragments[i]);
            }
        }

        [Fact]
        public async Task CannotAddTextFragmentWithoutPermission()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);
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
            }
        }

        [Fact]
        public async Task CannotMoveTextFragmentAfterTextFragmentNotInEdition()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);

                // Act
                var newTextFragmentRequestObject = new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
                    editionId,
                    textFragments.textFragments.First().id,
                    new UpdateTextFragmentDTO()
                    {
                        previousTextFragmentId = 0,
                        name = null,
                        nextTextFragmentId = null
                    }
                );
                var (response, msg, _, _) = await Request.Send(
                    newTextFragmentRequestObject,
                    _client,
                    null,
                    auth: true,
                    deterministic: false,
                    shouldSucceed: false
                );

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

                var updatedTextFragments =
                    await _getEditionTextFragments(
                        editionId,
                        true
                    ); // Get the list of text fragments in the edition again
                textFragments.ShouldDeepEqual(updatedTextFragments);
            }
        }

        [Fact]
        public async Task CannotMoveTextFragmentBeforeTextFragmentNotInEdition()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);

                // Act
                var newTextFragmentRequestObject = new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
                    editionId,
                    textFragments.textFragments.First().id,
                    new UpdateTextFragmentDTO()
                    {
                        previousTextFragmentId = null,
                        name = null,
                        nextTextFragmentId = 0
                    }
                );
                var (response, msg, _, _) = await Request.Send(
                    newTextFragmentRequestObject,
                    _client,
                    null,
                    auth: true,
                    deterministic: false,
                    shouldSucceed: false
                );

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

                var updatedTextFragments =
                    await _getEditionTextFragments(
                        editionId,
                        true
                    ); // Get the list of text fragments in the edition again
                textFragments.ShouldDeepEqual(updatedTextFragments);
            }
        }

        [Fact]
        public async Task CanMoveTextFragmentBetweenNonsequentialTextFragments()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var (editionId, textFragments) = await _createEditionWithTextFragments(editionCreator);

                // Act
                var newTextFragmentRequestObject = new Put.V1_Editions_EditionId_TextFragments_TextFragmentId(
                    editionId,
                    textFragments.textFragments.First().id,
                    new UpdateTextFragmentDTO()
                    {
                        previousTextFragmentId = textFragments.textFragments.Last().id,
                        name = null,
                        nextTextFragmentId = textFragments.textFragments.First().id
                    }
                );
                var (response, msg, _, _) = await Request.Send(
                    newTextFragmentRequestObject,
                    _client,
                    null,
                    auth: true,
                    deterministic: false,
                    shouldSucceed: false
                );

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

                var updatedTextFragments =
                    await _getEditionTextFragments(
                        editionId,
                        true
                    ); // Get the list of text fragments in the edition again
                textFragments.ShouldDeepEqual(updatedTextFragments);
            }
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