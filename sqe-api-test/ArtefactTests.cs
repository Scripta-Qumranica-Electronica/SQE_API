using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using NetTopologySuite.IO;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

// TODO: It would be nice to be able to generate random polygons for these testing purposes.
namespace SQE.ApiTest
{
    /// <summary>
    ///     This test suite tests all the current endpoints in the ArtefactController
    /// </summary>
    public partial class WebControllerTest
    {
        /// <summary>
        ///     Selects an edition with artefacts in sequence (to avoid locks) and returns its artefacts.
        /// </summary>
        /// <param name="editionId">Optional Id of the edition to acquire.</param>
        /// <returns></returns>
        public async Task<ArtefactListDTO> GetEditionArtefacts(uint? editionId = null)
        {
            editionId ??= EditionHelpers.GetEditionId();
            var optional = new List<string> { "masks" };
            if (_images)
            {
                optional.Add("images");
                _images = !_images;
            }
            var artRequest = new Get.V1_Editions_EditionId_Artefacts(editionId.Value, optional);
            await artRequest.SendAsync(
                _client,
                StartConnectionAsync,
                true,
                Request.DefaultUsers.User1,
                listenToEdition: false);
            var (response, artefactResponse, artefactRtResponse) = (artRequest.HttpResponseMessage,
                artRequest.HttpResponseObject,
                artRequest.SignalrResponseObject);
            response.EnsureSuccessStatusCode();
            artefactResponse.ShouldDeepEqual(artefactRtResponse);
            return artefactResponse;
        }

        private async Task DeleteArtefact(uint editionId, uint artefactId)
        {
            var (response, _) = await Request.SendHttpRequestAsync<string, string>(
                _client,
                HttpMethod.Delete,
                $"/v1/editions/{editionId}/artefacts/{artefactId}",
                null,
                await Request.GetJwtViaHttpAsync(_client)
            );
            response.EnsureSuccessStatusCode();
        }


        private static (decimal scale, decimal rotate, int translateX, int translateY, int zIndex) ArtefactPosition()
        {
            return (1.1m, 45m, 34765, 556, 2);
        }

        /// <summary>
        ///     Check that at least some edition has a valid artefact.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanAccessArtefacts()
        {
            // Act
            var artefacts = (await GetEditionArtefacts()).artefacts;

            // Assert
            Assert.NotEmpty(artefacts);
            var artefact = artefacts.First();
            Assert.True(artefact.editionId > 0);
            Assert.True(artefact.id > 0);
            Assert.NotNull(artefact.imagedObjectId);
            Assert.NotNull(artefact.mask);
        }

        /// <summary>
        ///     Ensure that a existing artefact can be placed and unplaced.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanBatchUnplaceArtefacts()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                var allArtefacts = (await GetEditionArtefacts(newEdition)).artefacts;
                var placement = new PlacementDTO
                {
                    scale = (decimal)1.0,
                    rotate = (decimal)0.0,
                    translate = new TranslateDTO
                    {
                        x = 100,
                        y = 223
                    },
                    zIndex = 0
                };
                // Act (update position)
                var (updateResponse, updatedArtefacts) =
                    await Request
                        .SendHttpRequestAsync<BatchUpdateArtefactPlacementDTO, BatchUpdatedArtefactTransformDTO>(
                            _client,
                            HttpMethod.Post,
                            $"/v1/editions/{newEdition}/artefacts/batch-transformation",
                            new BatchUpdateArtefactPlacementDTO
                            {
                                artefactPlacements = allArtefacts.Select(x => new UpdateArtefactPlacementDTO
                                {
                                    artefactId = x.id,
                                    isPlaced = true,
                                    placement = placement
                                }).ToList()
                            },
                            await Request.GetJwtViaHttpAsync(_client)
                        );

                // Assert (update name and set status)
                updateResponse.EnsureSuccessStatusCode();
                foreach (var art in updatedArtefacts.artefactPlacements)
                {
                    Assert.True(art.isPlaced);
                    Assert.Equal(100, art.placement.translate.x);
                    Assert.Equal(223, art.placement.translate.y);
                    Assert.Equal(1, art.placement.scale);
                    Assert.Equal(0, art.placement.rotate);
                    Assert.Equal(0, art.placement.zIndex);
                }

                // Act (update remove x/y)
                placement = new PlacementDTO
                {
                    scale = (decimal)1.0,
                    rotate = (decimal)0.0,
                    translate = null,
                    zIndex = 0
                };
                (updateResponse, updatedArtefacts) =
                    await Request
                        .SendHttpRequestAsync<BatchUpdateArtefactPlacementDTO, BatchUpdatedArtefactTransformDTO>(
                            _client,
                            HttpMethod.Post,
                            $"/v1/editions/{newEdition}/artefacts/batch-transformation",
                            new BatchUpdateArtefactPlacementDTO
                            {
                                artefactPlacements = allArtefacts.Select(x => new UpdateArtefactPlacementDTO
                                {
                                    artefactId = x.id,
                                    isPlaced = false,
                                    placement = placement
                                }).ToList()
                            },
                            await Request.GetJwtViaHttpAsync(_client)
                        );

                // Assert (update name and set status)
                updateResponse.EnsureSuccessStatusCode();
                foreach (var art in updatedArtefacts.artefactPlacements)
                {
                    Assert.False(art.isPlaced);
                    Assert.Null(art.placement.translate);
                    Assert.Equal(placement.scale, art.placement.scale);
                    Assert.Equal(placement.rotate, art.placement.rotate);
                    Assert.Equal(placement.zIndex, art.placement.zIndex);
                }

                // Act (full remove of position)
                (updateResponse, updatedArtefacts) =
                    await Request
                        .SendHttpRequestAsync<BatchUpdateArtefactPlacementDTO, BatchUpdatedArtefactTransformDTO>(
                            _client,
                            HttpMethod.Post,
                            $"/v1/editions/{newEdition}/artefacts/batch-transformation",
                            new BatchUpdateArtefactPlacementDTO
                            {
                                artefactPlacements = allArtefacts.Select(x => new UpdateArtefactPlacementDTO
                                {
                                    artefactId = x.id,
                                    isPlaced = false,
                                    placement = null
                                }).ToList()
                            },
                            await Request.GetJwtViaHttpAsync(_client)
                        );

                // Assert (update name and set status)
                updateResponse.EnsureSuccessStatusCode();
                foreach (var art in updatedArtefacts.artefactPlacements)
                {
                    Assert.False(art.isPlaced);
                    Assert.Null(art.placement.translate);
                    Assert.Equal(1, art.placement.scale);
                    Assert.Equal(0, art.placement.rotate);
                    Assert.Equal(0, art.placement.zIndex);
                }
            }
        }

        /// <summary>
        ///     Ensure that a new artefact can be created (and then deleted).
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanCreateArtefacts()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition

                const string masterImageSQL =
                    "SELECT sqe_image_id FROM SQE_image WHERE type = 0 ORDER BY RAND() LIMIT 1";
                var masterImageId = await _db.RunQuerySingleAsync<uint>(masterImageSQL, null);
                const string newArtefactShape =
                    "POLYGON((0 0,0 200,200 200,200 0,0 0),(77 80,102 80,102 92,77 92,77 80),(5 5,25 5,25 25,5 25,5 5))";
                var (newScale, newRotate, newTranslateX, newTranslateY, newZIdx) = ArtefactPosition();
                var newName = "CanCreateArtefacts.artefact א";
                var newArtefact = new CreateArtefactDTO
                {
                    mask = newArtefactShape,
                    placement = new PlacementDTO
                    {
                        scale = newScale,
                        rotate = newRotate,
                        translate = new TranslateDTO
                        {
                            x = newTranslateX,
                            y = newTranslateY
                        },
                        zIndex = newZIdx
                    },
                    name = newName,
                    masterImageId = masterImageId,
                    statusMessage = null
                };
                const string defaultStatusMessage = "New";

                // Act
                var (response, writtenArtefact) = await Request.SendHttpRequestAsync<CreateArtefactDTO, ArtefactDTO>(
                    _client,
                    HttpMethod.Post,
                    $"/v1/editions/{newEdition}/artefacts",
                    newArtefact,
                    await Request.GetJwtViaHttpAsync(_client)
                );

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(newEdition, writtenArtefact.editionId);
                Assert.Equal(newArtefact.mask, writtenArtefact.mask);
                Assert.Equal(newScale, writtenArtefact.placement.scale);
                Assert.Equal(newRotate, writtenArtefact.placement.rotate);
                Assert.Equal(newTranslateX, writtenArtefact.placement.translate.x);
                Assert.Equal(newTranslateY, writtenArtefact.placement.translate.y);
                Assert.Equal(newArtefact.name, writtenArtefact.name);
                Assert.Equal(defaultStatusMessage, writtenArtefact.statusMessage);

                // Cleanup
                await DeleteArtefact(newEdition, writtenArtefact.id);

                // Arrange
                newArtefact = new CreateArtefactDTO
                {
                    mask = newArtefactShape,
                    placement = new PlacementDTO
                    {
                        scale = newScale,
                        rotate = newRotate,
                        translate = new TranslateDTO
                        {
                            x = newTranslateX,
                            y = newTranslateY
                        }
                    },
                    name = null,
                    masterImageId = masterImageId
                };

                // Act
                (response, writtenArtefact) = await Request.SendHttpRequestAsync<CreateArtefactDTO, ArtefactDTO>(
                    _client,
                    HttpMethod.Post,
                    $"/v1/editions/{newEdition}/artefacts",
                    newArtefact,
                    await Request.GetJwtViaHttpAsync(_client)
                );

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(newEdition, writtenArtefact.editionId);
                Assert.Equal(newArtefact.mask, writtenArtefact.mask);
                Assert.Equal(newScale, writtenArtefact.placement.scale);
                Assert.Equal(newRotate, writtenArtefact.placement.rotate);
                Assert.Equal(newTranslateX, writtenArtefact.placement.translate.x);
                Assert.Equal(newTranslateY, writtenArtefact.placement.translate.y);
                Assert.Equal("", writtenArtefact.name);
            }
        }

        /// <summary>
        ///     Ensure that a existing artefact can be deleted.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanDeleteArtefacts()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                var allArtefacts = (await GetEditionArtefacts(newEdition)).artefacts;
                var artefact = allArtefacts.First();

                // Act
                var (response, _) = await Request.SendHttpRequestAsync<string, string>(
                    _client,
                    HttpMethod.Delete,
                    $"/v1/editions/{newEdition}/artefacts/{artefact.id}",
                    null,
                    await Request.GetJwtViaHttpAsync(_client)
                );

                // Assert
                response.EnsureSuccessStatusCode();
                // Ensure successful nocontent status
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                // Double check that it is really gone
                var (delResponse, _) = await Request.SendHttpRequestAsync<string, string>(
                    _client,
                    HttpMethod.Get,
                    $"/v1/editions/{newEdition}/artefacts/{artefact.id}",
                    null,
                    await Request.GetJwtViaHttpAsync(_client)
                );
                Assert.Equal(HttpStatusCode.NotFound, delResponse.StatusCode);

                await EditionHelpers.DeleteEdition(_client, StartConnectionAsync, newEdition);
            }
        }

        [Fact]
        public async Task CanGetSuggestedTextFragmentForArtefact()
        {
            // Arrange
            const uint editionId = 894;
            const uint artefactId = 10058;
            var path = $"/v1/editions/{editionId}/artefacts/{artefactId}/text-fragments?optional=suggested";

            // Act
            var (tfResponse, tfData) = await Request.SendHttpRequestAsync<string, ArtefactTextFragmentMatchListDTO>(
                _client,
                HttpMethod.Get,
                path,
                null
            );

            // Assert
            tfResponse.EnsureSuccessStatusCode();
            Assert.NotEmpty(tfData.textFragments);
            Assert.Equal((uint)10029, tfData.textFragments.First().id);
            Assert.Equal("frg. 78_79", tfData.textFragments.First().name);
            Assert.Equal((uint)894, tfData.textFragments.First().editorId);
        }

        /// <summary>
        ///     Ensure that a new artefact cannot be created in an edition not owned by the current user.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotCreateArtefactsOnUnownedEdition()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition

                const string masterImageSQL =
                    "SELECT sqe_image_id FROM SQE_image WHERE type = 0 ORDER BY RAND() LIMIT 1";
                var masterImageId = await _db.RunQuerySingleAsync<uint>(masterImageSQL, null);
                const string newArtefactShape =
                    "POLYGON((0 0,0 200,200 200,0 200,0 0),(5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))";
                var (newScale, newRotate, newTranslateX, newTranslateY, newZIdx) = ArtefactPosition();
                const string newName = "CanCreateArtefacts.artefact α";
                var newArtefact = new CreateArtefactDTO
                {
                    mask = newArtefactShape,
                    placement = new PlacementDTO
                    {
                        scale = newScale,
                        rotate = newRotate,
                        translate = new TranslateDTO
                        {
                            x = newTranslateX,
                            y = newTranslateY
                        },
                        zIndex = newZIdx
                    },
                    name = newName,
                    masterImageId = masterImageId
                };

                // Act
                var (response, _) = await Request.SendHttpRequestAsync<CreateArtefactDTO, ArtefactDTO>(
                    _client,
                    HttpMethod.Post,
                    $"/v1/editions/{newEdition}/artefacts",
                    newArtefact
                );

                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        /// <summary>
        ///     Ensure that attempts to write invalid polygons are rejected
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotCreateMalformedArtefact()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition

                const string masterImageSQL =
                    "SELECT sqe_image_id FROM SQE_image WHERE type = 0 ORDER BY RAND() LIMIT 1";
                var masterImageId = await _db.RunQuerySingleAsync<uint>(masterImageSQL, null);
                // This is a self-intersecting polygon
                const string newArtefactShape =
                    "POLYGON ((0 0, 30 110, 95 109, 146 64, 195 127, 150 210, 280 240, 150 170, 144 105, 75 84, 63 25, 0 0))";
                var (newScale, newRotate, newTranslateX, newTranslateY, newZIdx) = ArtefactPosition();
                var newName = "CannotCreateMalformedArtefact.artefact א";
                var newArtefact = new CreateArtefactDTO
                {
                    mask = newArtefactShape,
                    placement = new PlacementDTO
                    {
                        scale = newScale,
                        rotate = newRotate,
                        translate = new TranslateDTO
                        {
                            x = newTranslateX,
                            y = newTranslateY
                        },
                        zIndex = newZIdx
                    },
                    name = newName,
                    masterImageId = masterImageId,
                    statusMessage = null
                };

                // Act
                var newArtefactObject = new Post.V1_Editions_EditionId_Artefacts(newEdition, newArtefact);
                await newArtefactObject.SendAsync(
                    _client,
                    auth: true,
                    shouldSucceed: false
                );
                var (artefactResponse, _) = (newArtefactObject.HttpResponseMessage,
                    newArtefactObject.HttpResponseObject);

                // Assert
                // The response should indicate a bad request
                Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);

                // Test bad scale
                newArtefact = new CreateArtefactDTO
                {
                    mask = newArtefactShape,
                    placement = new PlacementDTO
                    {
                        scale = 100m, // 0–99.9999 is allowed range
                        rotate = newRotate,
                        translate = new TranslateDTO
                        {
                            x = newTranslateX,
                            y = newTranslateY
                        },
                        zIndex = newZIdx
                    },
                    name = newName,
                    masterImageId = masterImageId,
                    statusMessage = null
                };

                // Act
                newArtefactObject = new Post.V1_Editions_EditionId_Artefacts(newEdition, newArtefact);
                await newArtefactObject.SendAsync(
                    _client,
                    auth: true,
                    shouldSucceed: false
                );
                (artefactResponse, _) =
                    (newArtefactObject.HttpResponseMessage, newArtefactObject.HttpResponseObject);

                // Assert
                // The response should indicate a bad request
                Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
                var resp = await artefactResponse.Content.ReadAsStringAsync();
                Assert.Contains("The scale must be between 0.1 and 99.9999", resp);

                // Test scale has improper decimal value
                newArtefact = new CreateArtefactDTO
                {
                    mask = newArtefactShape,
                    placement = new PlacementDTO
                    {
                        scale = 2.43567m,
                        rotate = newRotate, // 0–9999.99 is the only allowable range
                        translate = new TranslateDTO
                        {
                            x = newTranslateX,
                            y = newTranslateY
                        },
                        zIndex = newZIdx
                    },
                    name = newName,
                    masterImageId = masterImageId,
                    statusMessage = null
                };

                // Act
                newArtefactObject = new Post.V1_Editions_EditionId_Artefacts(newEdition, newArtefact);
                await newArtefactObject.SendAsync(
                    _client,
                    auth: true,
                    shouldSucceed: false
                );
                (artefactResponse, _) =
                    (newArtefactObject.HttpResponseMessage, newArtefactObject.HttpResponseObject);

                // Assert
                // The response should indicate a bad request
                Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
                resp = await artefactResponse.Content.ReadAsStringAsync();
                Assert.Contains(
                    "The scale cannot have more than 2 digits to the left of the decimal and 4 digits to the right",
                    resp);

                // Test rotate has improper decimal value
                newArtefact = new CreateArtefactDTO
                {
                    mask = newArtefactShape,
                    placement = new PlacementDTO
                    {
                        scale = newScale,
                        rotate = 180.4576m, // 0–9999.99 is the only allowable range
                        translate = new TranslateDTO
                        {
                            x = newTranslateX,
                            y = newTranslateY
                        },
                        zIndex = newZIdx
                    },
                    name = newName,
                    masterImageId = masterImageId,
                    statusMessage = null
                };

                // Act
                newArtefactObject = new Post.V1_Editions_EditionId_Artefacts(newEdition, newArtefact);
                await newArtefactObject.SendAsync(
                    _client,
                    auth: true,
                    shouldSucceed: false
                );
                (artefactResponse, _) =
                    (newArtefactObject.HttpResponseMessage, newArtefactObject.HttpResponseObject);

                // Assert
                // The response should indicate a bad request
                Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
                resp = await artefactResponse.Content.ReadAsStringAsync();
                Assert.Contains(
                    "The rotate cannot have more than 4 digits to the left of the decimal and 2 digits to the right",
                    resp);

                // Test rotate out of range
                newArtefact = new CreateArtefactDTO
                {
                    mask = newArtefactShape,
                    placement = new PlacementDTO
                    {
                        scale = newScale,
                        rotate = -180.45m, // 0–9999.99 is the only allowable range
                        translate = new TranslateDTO
                        {
                            x = newTranslateX,
                            y = newTranslateY
                        },
                        zIndex = newZIdx
                    },
                    name = newName,
                    masterImageId = masterImageId,
                    statusMessage = null
                };

                // Act
                newArtefactObject = new Post.V1_Editions_EditionId_Artefacts(newEdition, newArtefact);
                await newArtefactObject.SendAsync(
                    _client,
                    auth: true,
                    shouldSucceed: false
                );
                (artefactResponse, _) =
                    (newArtefactObject.HttpResponseMessage, newArtefactObject.HttpResponseObject);

                // Assert
                // The response should indicate a bad request
                Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
                resp = await artefactResponse.Content.ReadAsStringAsync();
                Assert.Contains("The rotate must be between 0 and 360", resp);

                // Cannot create an artefact without a mask
                // Arrange
                newName = "CannotCreateArtefacts.artefact ב";

                newArtefact = new CreateArtefactDTO
                {
                    mask = null,
                    placement = new PlacementDTO
                    {
                        scale = newScale,
                        rotate = newRotate,
                        translate = new TranslateDTO
                        {
                            x = newTranslateX,
                            y = newTranslateY
                        }
                    },
                    name = newName,
                    masterImageId = masterImageId
                };

                // Act
                (artefactResponse, _) = await Request.SendHttpRequestAsync<CreateArtefactDTO, ArtefactDTO>(
                    _client,
                    HttpMethod.Post,
                    $"/v1/editions/{newEdition}/artefacts",
                    newArtefact,
                    await Request.GetJwtViaHttpAsync(_client)
                );

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
                resp = await artefactResponse.Content.ReadAsStringAsync();
                Assert.Contains("The mask field is required.", resp);

                // Cannot create an artefact without a mask
                // Arrange
                newName = "CannotCreateArtefacts.artefact ב";

                newArtefact = new CreateArtefactDTO
                {
                    mask = "PLYGON((0 0,10 0,10 10,0 10,0 0))",
                    placement = new PlacementDTO
                    {
                        scale = newScale,
                        rotate = newRotate,
                        translate = new TranslateDTO
                        {
                            x = newTranslateX,
                            y = newTranslateY
                        }
                    },
                    name = newName,
                    masterImageId = masterImageId
                };

                // Act
                (artefactResponse, _) = await Request.SendHttpRequestAsync<CreateArtefactDTO, ArtefactDTO>(
                    _client,
                    HttpMethod.Post,
                    $"/v1/editions/{newEdition}/artefacts",
                    newArtefact,
                    await Request.GetJwtViaHttpAsync(_client)
                );

                // Assert
                Assert.Equal(HttpStatusCode.BadRequest, artefactResponse.StatusCode);
                resp = await artefactResponse.Content.ReadAsStringAsync();
                Assert.Contains("The mask must be a valid WKT POLYGON description.", resp);
            }
        }

        /// <summary>
        ///     Ensure that a existing artefact cannot be deleted by a use who does not have access to it.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotDeleteUnownedArtefacts()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                var allArtefacts = (await GetEditionArtefacts(newEdition)).artefacts;
                var artefact = allArtefacts.First();

                // Act
                var (response, _) = await Request.SendHttpRequestAsync<string, string>(
                    _client,
                    HttpMethod.Delete,
                    $"/v1/editions/{newEdition}/artefacts/{artefact.id}",
                    null
                );

                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        /// <summary>
        ///     Ensure that a existing artefact cannot be updated by a user who does not have access.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotUpdateUnownedArtefacts()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                var allArtefacts = (await GetEditionArtefacts(newEdition)).artefacts;
                var artefact = allArtefacts.First();
                const string newArtefactName = "CannotUpdateUnownedArtefacts.artefact 😈";

                // Act (update name)
                var (nameResponse, _) = await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                    _client,
                    HttpMethod.Put,
                    $"/v1/editions/{newEdition}/artefacts/{artefact.id}",
                    new UpdateArtefactDTO
                    {
                        mask = null,
                        placement = null,
                        name = newArtefactName
                    }
                );

                // Assert (update name)
                Assert.Equal(HttpStatusCode.Unauthorized, nameResponse.StatusCode);
            }
        }

        /// <summary>
        ///     Ensure that a existing artefact can be updated.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUpdateArtefacts()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                var allArtefacts = (await GetEditionArtefacts(newEdition)).artefacts;
                var artefact = allArtefacts.First();
                const string newArtefactName = "CanUpdateArtefacts.artefact +%%$^";
                var (newScale, newRotate, newTranslateX, newTranslateY, newZIdx) = ArtefactPosition();
                const string newArtefactShape =
                    "POLYGON((0 0,0 200,200 200,200 0,0 0),(77 80,102 80,102 92,77 92,77 80),(5 5,25 5,25 25,5 25,5 5))";
                const string statusMessage = "Fully examined";

                // Act (update name and set status)
                var (nameResponse, updatedNameArtefact) =
                    await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                        _client,
                        HttpMethod.Put,
                        $"/v1/editions/{newEdition}/artefacts/{artefact.id}",
                        new UpdateArtefactDTO
                        {
                            mask = null,
                            placement = null,
                            name = newArtefactName,
                            statusMessage = statusMessage
                        },
                        await Request.GetJwtViaHttpAsync(_client)
                    );

                // Assert (update name and set status)
                nameResponse.EnsureSuccessStatusCode();
                Assert.False(updatedNameArtefact.isPlaced);
                Assert.Null(updatedNameArtefact.placement.translate);
                Assert.Equal(0, updatedNameArtefact.placement.rotate); // Expect the default value
                Assert.Equal(0, updatedNameArtefact.placement.zIndex); // Expect the default value
                Assert.Equal(1, updatedNameArtefact.placement.scale); // Expect the default value
                Assert.NotEqual(artefact.name, updatedNameArtefact.name);
                Assert.True(
                    string.IsNullOrEmpty(updatedNameArtefact
                        .mask)); // The mask was not updated so we don't send that back
                Assert.Equal(newArtefactName, updatedNameArtefact.name);
                Assert.Equal(statusMessage, updatedNameArtefact.statusMessage);

                // Act (update position)
                var (positionResponse, updatedPositionArtefact) =
                    await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                        _client,
                        HttpMethod.Put,
                        $"/v1/editions/{newEdition}/artefacts/{artefact.id}",
                        new UpdateArtefactDTO
                        {
                            mask = null,
                            placement = new PlacementDTO
                            {
                                scale = newScale,
                                rotate = newRotate,
                                translate = new TranslateDTO
                                {
                                    x = newTranslateX,
                                    y = newTranslateY
                                },
                                zIndex = newZIdx
                            },
                            name = null
                        },
                        await Request.GetJwtViaHttpAsync(_client)
                    );

                // Assert (update position)
                positionResponse.EnsureSuccessStatusCode();
                Assert.NotEqual(artefact.placement.scale, updatedPositionArtefact.placement.scale);
                Assert.NotEqual(artefact.placement.rotate, updatedPositionArtefact.placement.rotate);
                Assert.NotNull(updatedPositionArtefact.placement.translate);
                Assert.Equal(newScale, updatedPositionArtefact.placement.scale);
                Assert.Equal(newRotate, updatedPositionArtefact.placement.rotate);
                Assert.Equal(newTranslateX, updatedPositionArtefact.placement.translate.x);
                Assert.Equal(newTranslateY, updatedPositionArtefact.placement.translate.y);
                Assert.Equal(newZIdx, updatedPositionArtefact.placement.zIndex);
                Assert.Equal(newArtefactName, updatedPositionArtefact.name);

                // Act (update shape)
                var (shapeResponse, updatedShapeArtefact) =
                    await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                        _client,
                        HttpMethod.Put,
                        $"/v1/editions/{newEdition}/artefacts/{artefact.id}",
                        new UpdateArtefactDTO
                        {
                            mask = newArtefactShape,
                            placement = new PlacementDTO
                            {
                                scale = newScale,
                                rotate = newRotate,
                                translate = new TranslateDTO
                                {
                                    x = newTranslateX,
                                    y = newTranslateY
                                }
                            },
                            name = null
                        },
                        await Request.GetJwtViaHttpAsync(_client)
                    );

                // Assert (update shape)
                shapeResponse.EnsureSuccessStatusCode();
                Assert.NotEqual(artefact.mask, updatedShapeArtefact.mask);
                Assert.Equal(newArtefactShape, updatedShapeArtefact.mask);
                Assert.Equal(newScale, updatedShapeArtefact.placement.scale);
                Assert.Equal(newRotate, updatedShapeArtefact.placement.rotate);
                Assert.Equal(newTranslateX, updatedShapeArtefact.placement.translate.x);
                Assert.Equal(newTranslateY, updatedShapeArtefact.placement.translate.y);
                Assert.Equal(newArtefactName, updatedShapeArtefact.name);

                // Arrange (update all)
                var (otherScale, otherRotate, otherTranslateX, otherTranslateY, otherzIdx) = ArtefactPosition();
                // Act (update all)
                var (allResponse, updatedAllArtefact) =
                    await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                        _client,
                        HttpMethod.Put,
                        $"/v1/editions/{newEdition}/artefacts/{artefact.id}",
                        new UpdateArtefactDTO
                        {
                            mask = artefact.mask,
                            placement = new PlacementDTO
                            {
                                scale = otherScale,
                                rotate = otherRotate,
                                translate = new TranslateDTO
                                {
                                    x = otherTranslateX,
                                    y = otherTranslateY
                                },
                                zIndex = otherzIdx
                            },
                            name = artefact.name
                        },
                        await Request.GetJwtViaHttpAsync(_client)
                    );

                // Assert (update all)
                allResponse.EnsureSuccessStatusCode();
                Assert.True(_wkr.Read(artefact.mask).EqualsTopologically(_wkr.Read(updatedAllArtefact.mask)));
                Assert.Equal(otherScale, updatedAllArtefact.placement.scale);
                Assert.Equal(otherRotate, updatedAllArtefact.placement.rotate);
                Assert.Equal(otherTranslateX, updatedAllArtefact.placement.translate.x);
                Assert.Equal(otherTranslateY, updatedAllArtefact.placement.translate.y);
                Assert.Equal(otherzIdx, updatedAllArtefact.placement.zIndex);
                Assert.Equal(artefact.name, updatedAllArtefact.name);
            }
        }

        /// <summary>
        ///     Ensure that improperly formatted artefact WKT masks are rejected.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RejectsUpdateToImproperArtefactShape()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                var allArtefacts = (await GetEditionArtefacts(newEdition)).artefacts;
                var artefact = allArtefacts.First();
                const string newArtefactShape =
                    "POLYGON(0 0,0 200,200 200,0 200,0 0),5 5,5 25,25 25,25 5,5 5),(77 80,77 92,102 92,102 80,77 80))";

                // Act (update name)
                var (nameResponse, _) = await Request.SendHttpRequestAsync<UpdateArtefactDTO, ArtefactDTO>(
                    _client,
                    HttpMethod.Put,
                    $"/v1/editions/{newEdition}/artefacts/{artefact.id}",
                    new UpdateArtefactDTO
                    {
                        mask = newArtefactShape,
                        placement = null,
                        name = null
                    },
                    await Request.GetJwtViaHttpAsync(_client)
                );

                // Assert (update name)
                Assert.Equal(HttpStatusCode.BadRequest, nameResponse.StatusCode);
            }
        }

        [Fact]
        public async Task CanGetEditionArtefactRois()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client, StartConnectionAsync))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                var (artefactId, rois) = await RoiHelpers.CreateRoiInEdition(_client, StartConnectionAsync, newEdition);

                // Act
                var getArtefactRois = new Get.V1_Editions_EditionId_Artefacts_ArtefactId_Rois(newEdition, artefactId);
                await getArtefactRois.SendAsync(_client, StartConnectionAsync, auth: true);

                getArtefactRois.HttpResponseObject.ShouldDeepEqual(getArtefactRois.SignalrResponseObject);
                getArtefactRois.HttpResponseObject.rois.ShouldDeepEqual(rois);
            }
        }
    }
}