using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using Xunit;

namespace SQE.ApiTest.Helpers
{
    public static class RoiHelpers
    {
        public static async Task<(uint artefactId, IEnumerable<InterpretationRoiDTO> rois)> CreateRoiInEdition(
            HttpClient client,
            Func<string, Task<HubConnection>> signalr,
            uint editionId)
        {

            var allArtefacts = (await ArtefactHelpers.GetEditionArtefacts(editionId, client)).artefacts;
            var artefact = allArtefacts.First();
            var signInterpretation = await SignInterpretationHelpers.GetEditionSignInterpretation(editionId, client);
            var newRoi1 = new SetInterpretationRoiDTO()
            {
                signInterpretationId = signInterpretation.signInterpretationId,
                artefactId = artefact.id,
                shape = "POLYGON((0 0,0 10,10 10,10 0,0 0))",
                translate = new TranslateDTO()
                {
                    x = 10,
                    y = 20,
                },
                stanceRotation = 10,
                exceptional = false,
                valuesSet = true,
            };

            var newRoi2 = new SetInterpretationRoiDTO()
            {
                signInterpretationId = signInterpretation.signInterpretationId,
                artefactId = artefact.id,
                shape = "POLYGON((20 20,20 30,30 30,30 20,20 20))",
                translate = new TranslateDTO()
                {
                    x = 1000,
                    y = 2534,
                },
                stanceRotation = 1,
                exceptional = false,
                valuesSet = true,
            };

            // Act
            var request1 = new Post.V1_Editions_EditionId_Rois(editionId, newRoi1);
            await request1.Send(client, signalr, auth: true, deterministic: false, requestRealtime: false, listeningFor: request1.AvailableListeners.CreatedRoi);

            var request2 = new Post.V1_Editions_EditionId_Rois(editionId, newRoi2);
            await request2.Send(null, signalr, auth: true, deterministic: false, listeningFor: request2.AvailableListeners.CreatedRoi);

            // Assert
            request1.HttpResponseObject.ShouldDeepEqual(request1.CreatedRoi);
            request2.SignalrResponseObject.ShouldDeepEqual(request2.CreatedRoi);
            request1.HttpResponseObject.Matches(newRoi1);
            request2.SignalrResponseObject.Matches(newRoi2);

            return (artefact.id, new List<InterpretationRoiDTO>() { request1.HttpResponseObject, request2.SignalrResponseObject });
        }

        public static async Task<InterpretationRoiDTO> GetEditionRoiInfo(
            HttpClient client,
            Func<string, Task<HubConnection>> signalr,
            uint editionId,
            uint roiId)
        {
            var request = new Get.V1_Editions_EditionId_Rois_RoiId(editionId, roiId);
            await request.Send(client, signalr, auth: true);

            request.HttpResponseObject.ShouldDeepEqual(request.SignalrResponseObject);
            return request.HttpResponseObject;
        }

        public static async Task<DeleteDTO> DeleteEditionRoi(
            HttpClient client,
            Func<string, Task<HubConnection>> signalr,
            uint editionId,
            uint roiId)
        {
            var request = new Delete.V1_Editions_EditionId_Rois_RoiId(editionId, roiId);
            await request.Send(client, signalr, auth: true, requestRealtime: client == null, listeningFor: request.AvailableListeners.DeletedRoi);

            request.HttpResponseObject.ShouldDeepEqual(request.SignalrResponseObject);
            return request.DeletedRoi;
        }

        public static async Task<UpdatedInterpretationRoiDTO> UpdateEditionRoi(
            HttpClient client,
            Func<string, Task<HubConnection>> signalr,
            uint editionId,
            uint roiId,
            SetInterpretationRoiDTO updatedRoi)
        {
            var request = new Put.V1_Editions_EditionId_Rois_RoiId(editionId, roiId, updatedRoi);
            await request.Send(client, signalr, auth: true, requestRealtime: client == null, listeningFor: request.AvailableListeners.UpdatedRoi);

            var controllerResponse = client == null ? request.SignalrResponseObject : request.HttpResponseObject;
            request.UpdatedRoi.ShouldDeepEqual(controllerResponse);
            Assert.Equal(roiId, controllerResponse.oldInterpretationRoiId);
            controllerResponse.Matches(updatedRoi);
            return request.UpdatedRoi;
        }

        public static void Matches(this InterpretationRoiDTO ird, SetInterpretationRoiDTO sird)
        {
            Assert.Equal(sird.artefactId, ird.artefactId);
            Assert.Equal(sird.shape, ird.shape);
            sird.translate.ShouldDeepEqual(ird.translate);
            Assert.Equal(sird.stanceRotation, ird.stanceRotation);
            Assert.Equal(sird.exceptional, ird.exceptional);
            Assert.Equal(sird.valuesSet, ird.valuesSet);
            Assert.Equal(sird.signInterpretationId, ird.signInterpretationId);
        }
    }
}