using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using NetTopologySuite.Geometries.Utilities;
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
                var (httpResponse, httpData, _, _) =
                    await Request.Send(request, _client, auth: true, deterministic: false);

                // Assert
                httpResponse.EnsureSuccessStatusCode();
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
                Assert.Equal(0, responseAttr.sequence);
                Assert.Equal(0, responseAttr.value);
                Assert.NotEqual<uint>(0, responseAttr.creatorId);
                Assert.NotEqual<uint>(0, responseAttr.editorId);
                Assert.NotEqual<uint>(0, responseAttr.attributeId);
                Assert.NotEqual<uint>(0, responseAttr.attributeValueId);
                Assert.NotEqual<uint>(0, responseAttr.interpretationAttributeId);
                Assert.False(string.IsNullOrEmpty(responseAttr.attributeValueString));
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