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
    ///     This test suite tests all the current endpoints in the RoiController
    /// </summary>
    public class RoiTests : WebControllerTest
    {
        public RoiTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
        }

        [Fact]
        public async Task CanCreateEditionRoi()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                await RoiHelpers.CreateRoiInEdition(_client, StartConnectionAsync, newEdition);
            }
        }

        [Fact]
        public async Task CanBatchCreateEditionRoi()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                await RoiHelpers.CreateRoiInEdition(_client, StartConnectionAsync, newEdition, true);
            }
        }

        [Fact]
        public async Task CanGetEditionRoi()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                var (_, rois) = await RoiHelpers.CreateRoiInEdition(_client, StartConnectionAsync, newEdition);

                // Act
                var getRoi = await RoiHelpers.GetEditionRoiInfo(_client, StartConnectionAsync, newEdition, rois.First().interpretationRoiId);
                rois.First().ShouldDeepEqual(getRoi);
            }
        }

        [Fact]
        public async Task CanDeleteEditionRoi()
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                var (artefactId, rois) = await RoiHelpers.CreateRoiInEdition(_client, StartConnectionAsync, newEdition);
                // Check that the roi exists
                var getRoi = await RoiHelpers.GetEditionRoiInfo(_client, StartConnectionAsync, newEdition, rois.First().interpretationRoiId);
                rois.First().ShouldDeepEqual(getRoi);

                // Act
                var deleteRoi1 = await RoiHelpers.DeleteEditionRoi(_client, StartConnectionAsync, newEdition,
                    rois.First().interpretationRoiId);
                var deleteRoi2 = await RoiHelpers.DeleteEditionRoi(null, StartConnectionAsync, newEdition,
                    rois.Last().interpretationRoiId);

                // Assert
                Assert.Equal(EditionEntities.roi, deleteRoi1.entity);
                Assert.Single(deleteRoi1.ids);
                Assert.Equal(EditionEntities.roi, deleteRoi2.entity);
                Assert.Single(deleteRoi2.ids);
                Assert.Equal(rois.First().interpretationRoiId, deleteRoi1.ids.First());
                Assert.Equal(rois.Last().interpretationRoiId, deleteRoi2.ids.First());

                // Check that it is not returned with a get
                var updatedRoiList =
                    await ArtefactHelpers.GetArtefactRois(newEdition, artefactId, _client, StartConnectionAsync, true);
                Assert.DoesNotContain(updatedRoiList.rois, (x => x.interpretationRoiId == rois.First().interpretationRoiId));
                Assert.DoesNotContain(updatedRoiList.rois, (x => x.interpretationRoiId == rois.Last().interpretationRoiId));
            }
        }

        [Fact]
        public async Task CanUpdateEditionRoi()
        {
            await UpdateEditionRoi(false);
        }

        [Fact]
        public async Task CanBatchUpdateEditionRoi()
        {
            await UpdateEditionRoi(true);
        }

        [Fact]
        public async Task CanBatchEditRois()
        {
            // Test the HTTP transport
            await BatchEditRois(false);

            // Test the SignalR transport
            await BatchEditRois(true);
        }

        private async Task UpdateEditionRoi(bool batch)
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                var (artefactId, rois) = await RoiHelpers.CreateRoiInEdition(_client, StartConnectionAsync, newEdition);
                // Check that the roi exists
                var getRoi = await RoiHelpers.GetEditionRoiInfo(_client, StartConnectionAsync, newEdition, rois.First().interpretationRoiId);
                rois.First().ShouldDeepEqual(getRoi);

                var updateRoi1 = new SetInterpretationRoiDTO()
                {
                    artefactId = artefactId,
                    exceptional = true,
                    shape = "POLYGON((100 200,100 250,200 250,200 200,100 200))",
                    signInterpretationId = getRoi.signInterpretationId,
                    stanceRotation = 180,
                    translate = new TranslateDTO()
                    {
                        x = 3000,
                        y = 4079
                    },
                    valuesSet = true
                };
                var updateRoi2 = new SetInterpretationRoiDTO()
                {
                    artefactId = artefactId,
                    exceptional = true,
                    shape = "POLYGON((100 199,100 250,200 250,200 200,100 199))",
                    signInterpretationId = getRoi.signInterpretationId,
                    stanceRotation = 12,
                    translate = new TranslateDTO()
                    {
                        x = 3030,
                        y = 4029
                    },
                    valuesSet = false
                };

                // Act
                var updatedRoi1 = await RoiHelpers.UpdateEditionRoi(_client, StartConnectionAsync, newEdition,
                    rois.First().interpretationRoiId, updateRoi1, batch);
                var updatedRoi2 = await RoiHelpers.UpdateEditionRoi(null, StartConnectionAsync, newEdition,
                    rois.Last().interpretationRoiId, updateRoi2, batch);

                // Assert
                // Check that it is not returned with a get
                var updatedRoiList =
                    await ArtefactHelpers.GetArtefactRois(newEdition, artefactId, _client, StartConnectionAsync, true);
                Assert.Contains(updatedRoiList.rois, (x => x.interpretationRoiId == updatedRoi1.interpretationRoiId));
                Assert.Contains(updatedRoiList.rois, (x => x.interpretationRoiId == updatedRoi2.interpretationRoiId));
                var retrievedUpdatedRoi1 =
                    updatedRoiList.rois.First(x => x.interpretationRoiId == updatedRoi1.interpretationRoiId);
                retrievedUpdatedRoi1.Matches(updatedRoi1);
                var retrievedUpdatedRoi2 =
                    updatedRoiList.rois.First(x => x.interpretationRoiId == updatedRoi2.interpretationRoiId);
                retrievedUpdatedRoi2.Matches(updatedRoi2);
            }
        }

        public async Task BatchEditRois(bool realtime)
        {
            using (var editionCreator = new EditionHelpers.EditionCreator(_client))
            {
                // Arrange
                // Create two new rois, one will get altered, one will be deleted
                var newEdition = await editionCreator.CreateEdition(); // Clone new edition
                var (artefactId, rois) = await RoiHelpers.CreateRoiInEdition(_client, StartConnectionAsync, newEdition);
                var roiForUpdate = rois.First();
                var updatedRoi = new UpdateInterpretationRoiDTO
                {
                    artefactId = roiForUpdate.artefactId,
                    exceptional = true,
                    interpretationRoiId = roiForUpdate.interpretationRoiId,
                    shape = roiForUpdate.shape,
                    signInterpretationId = roiForUpdate.signInterpretationId,
                    stanceRotation = 354,
                    translate = roiForUpdate.translate,
                    valuesSet = true
                };
                var roiForDelete = rois.Last();
                var newRoi = new SetInterpretationRoiDTO()
                {
                    artefactId = artefactId,
                    exceptional = false,
                    shape = "POLYGON((1 1,2 1,2 2,1 2,1 1))",
                    signInterpretationId = updatedRoi.signInterpretationId,
                    stanceRotation = 77,
                    valuesSet = true,
                    translate = new TranslateDTO()
                    {
                        x = 100,
                        y = 100
                    }
                };
                var batchRoiRequest = new BatchEditRoiDTO()
                {
                    createRois = new List<SetInterpretationRoiDTO>() { newRoi },
                    deleteRois = new List<uint>() { roiForDelete.interpretationRoiId },
                    updateRois = new List<UpdateInterpretationRoiDTO>() { updatedRoi }
                };

                // Act
                var request = new Post.V1_Editions_EditionId_Rois_BatchEdit(newEdition, batchRoiRequest);
                await request.Send(
                    realtime ? null : _client,
                    StartConnectionAsync,
                    auth: true,
                    requestRealtime: realtime,
                    listeningFor: request.AvailableListeners.EditedRoisBatch);

                // Assert
                var controllerResponse = realtime ? request.SignalrResponseObject : request.HttpResponseObject;
                controllerResponse.ShouldDeepEqual(request.EditedRoisBatch);
            }
        }
    }
}