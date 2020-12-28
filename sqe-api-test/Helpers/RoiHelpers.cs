using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;
using SQE.API.Server.Serialization;
using SQE.ApiTest.ApiRequests;
using Xunit;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.ApiTest.Helpers
{
	public static class RoiHelpers
	{
		public static async Task<(uint artefactId, IEnumerable<InterpretationRoiDTO> rois)>
				CreateRoiInEdition(
						HttpClient                          client
						, Func<string, Task<HubConnection>> signalr
						, uint                              editionId
						, bool                              batch = false)
		{
			var allArtefacts =
					(await ArtefactHelpers.GetEditionArtefacts(editionId, client)).artefacts;

			var artefact = allArtefacts.First();

			var signInterpretation =
					await SignInterpretationHelpers.GetEditionSignInterpretation(editionId, client);

			var newRoi1 = new SetInterpretationRoiDTO
			{
					signInterpretationId = signInterpretation.signInterpretationId
					, artefactId = artefact.id
					, shape = "POLYGON((0 0,0 10,10 10,10 0,0 0))"
					, translate = new TranslateDTO { x = 10, y = 20 }
					, stanceRotation = 10
					, exceptional = false
					, valuesSet = true
					,
			};

			var newRoi2 = new SetInterpretationRoiDTO
			{
					signInterpretationId = signInterpretation.signInterpretationId
					, artefactId = artefact.id
					, shape = "POLYGON((20 20,20 30,30 30,30 20,20 20))"
					, translate = new TranslateDTO
					{
							x = 1000
							, y = 2534
							,
					}
					, stanceRotation = 1
					, exceptional = false
					, valuesSet = true
					,
			};

			if (!batch)
			{
				// Act
				var request1 = new Post.V1_Editions_EditionId_Rois(editionId, newRoi1);

				await request1.SendAsync(
						client
						, signalr
						, true
						, deterministic: false
						, requestRealtime: false
						, listeningFor: request1.AvailableListeners
												.CreatedRoisBatch);

				var request2 = new Post.V1_Editions_EditionId_Rois(editionId, newRoi2);

				await request2.SendAsync(
						null
						, signalr
						, true
						, deterministic: false
						, listeningFor: request2.AvailableListeners
												.CreatedRoisBatch);

				// Assert
				request1.HttpResponseObject.ShouldDeepEqual(request1.CreatedRoisBatch.rois.First());

				request2.SignalrResponseObject.ShouldDeepEqual(
						request2.CreatedRoisBatch.rois.First());

				request1.HttpResponseObject.Matches(newRoi1);
				request2.SignalrResponseObject.Matches(newRoi2);

				return (artefact.id
						, new List<InterpretationRoiDTO>
						{
								request1.HttpResponseObject
								, request2.SignalrResponseObject
								,
						});
			}

			// Act
			var batchRois1 = new SetInterpretationRoiDTOList
			{
					rois = new List<SetInterpretationRoiDTO> { newRoi1 },
			};

			var batchRequest1 = new Post.V1_Editions_EditionId_Rois_Batch(editionId, batchRois1);

			await batchRequest1.SendAsync(
					client
					, signalr
					, true
					, deterministic: false
					, requestRealtime: false
					, listeningFor: batchRequest1.AvailableListeners
												 .EditedRoisBatch);

			var batchRois2 = new SetInterpretationRoiDTOList
			{
					rois = new List<SetInterpretationRoiDTO> { newRoi2 },
			};

			var batchRequest2 = new Post.V1_Editions_EditionId_Rois_Batch(editionId, batchRois2);

			await batchRequest2.SendAsync(
					null
					, signalr
					, true
					, deterministic: false
					, listeningFor: batchRequest2.AvailableListeners
												 .EditedRoisBatch);

			// Assert
			batchRequest1.HttpResponseObject.rois.ShouldDeepEqual(
					batchRequest1.EditedRoisBatch.createRois);

			batchRequest2.SignalrResponseObject.rois.ShouldDeepEqual(
					batchRequest2.EditedRoisBatch.createRois);

			batchRequest1.HttpResponseObject.Matches(batchRois1);
			batchRequest2.SignalrResponseObject.Matches(batchRois2);

			return (artefact.id
					, new List<InterpretationRoiDTO>
					{
							batchRequest1.HttpResponseObject.rois.First()
							, batchRequest2.SignalrResponseObject.rois.First()
							,
					});
		}

		public static async Task<InterpretationRoiDTO> GetEditionRoiInfo(
				HttpClient                          client
				, Func<string, Task<HubConnection>> signalr
				, uint                              editionId
				, uint                              roiId)
		{
			var request = new Get.V1_Editions_EditionId_Rois_RoiId(editionId, roiId);

			await request.SendAsync(client, signalr, true);

			request.HttpResponseObject.ShouldDeepEqual(request.SignalrResponseObject);

			return request.HttpResponseObject;
		}

		public static async Task<DeleteDTO> DeleteEditionRoi(
				HttpClient                          client
				, Func<string, Task<HubConnection>> signalr
				, uint                              editionId
				, uint                              roiId)
		{
			var request = new Delete.V1_Editions_EditionId_Rois_RoiId(editionId, roiId);

			await request.SendAsync(
					client
					, signalr
					, true
					, requestRealtime: client == null
					, listeningFor: request.AvailableListeners.DeletedRoi);

			request.HttpResponseObject.ShouldDeepEqual(request.SignalrResponseObject);

			return request.DeletedRoi;
		}

		public static async Task<UpdatedInterpretationRoiDTO> UpdateEditionRoi(
				HttpClient                          client
				, Func<string, Task<HubConnection>> signalr
				, uint                              editionId
				, uint                              roiId
				, SetInterpretationRoiDTO           updatedRoi
				, bool                              batch = false)
		{
			if (batch)
			{
				var batchUpdateRois = new UpdateInterpretationRoiDTOList
				{
						rois = new List<UpdateInterpretationRoiDTO>
						{
								updatedRoi.ToUpdateInterpretationRoiDTO(roiId),
						}
						,
				};

				var batchRequest =
						new Put.V1_Editions_EditionId_Rois_Batch(editionId, batchUpdateRois);

				await batchRequest.SendAsync(
						client
						, signalr
						, true
						, requestRealtime: client == null
						, deterministic: false
						, listeningFor: batchRequest.AvailableListeners
													.UpdatedRoisBatch);

				var batchControllerResponse = client == null
						? batchRequest.SignalrResponseObject
						: batchRequest.HttpResponseObject;

				batchRequest.UpdatedRoisBatch.rois.ShouldDeepEqual(batchControllerResponse.rois);

				Assert.Equal(roiId, batchControllerResponse.rois.First().oldInterpretationRoiId);

				batchUpdateRois.Matches(batchControllerResponse);

				return batchControllerResponse.rois.First();
			}

			var request = new Put.V1_Editions_EditionId_Rois_RoiId(editionId, roiId, updatedRoi);

			await request.SendAsync(
					client
					, signalr
					, true
					, requestRealtime: client == null
					, deterministic: false
					, listeningFor: request.AvailableListeners.EditedRoisBatch);

			var controllerResponse = client == null
					? request.SignalrResponseObject
					: request.HttpResponseObject;

			request.EditedRoisBatch.updateRois.First().ShouldDeepEqual(controllerResponse);

			Assert.Equal(roiId, controllerResponse.oldInterpretationRoiId);
			controllerResponse.Matches(updatedRoi);

			return request.EditedRoisBatch.updateRois.First();
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

		public static void Matches(
				this InterpretationRoiDTOList ird
				, SetInterpretationRoiDTOList sird)
		{
			foreach (var setInterpretationRoiDto in sird.rois)
			{
				Assert.Contains(
						ird.rois
						, x => (x.artefactId == setInterpretationRoiDto.artefactId)
							   && (x.shape == setInterpretationRoiDto.shape)
							   && x.translate.IsDeepEqual(setInterpretationRoiDto.translate)
							   && (x.stanceRotation == setInterpretationRoiDto.stanceRotation)
							   && (x.exceptional == setInterpretationRoiDto.exceptional)
							   && (x.valuesSet == setInterpretationRoiDto.valuesSet)
							   && (x.signInterpretationId
								   == setInterpretationRoiDto.signInterpretationId));
			}
		}

		public static void Matches(
				this UpdateInterpretationRoiDTOList ird
				, UpdatedInterpretationRoiDTOList   sird)
		{
			foreach (var setInterpretationRoiDto in sird.rois)
			{
				Assert.Contains(
						ird.rois
						, x => (x.artefactId == setInterpretationRoiDto.artefactId)
							   && (x.shape.Replace(" (", "(").Replace(", ", ",")
								   == setInterpretationRoiDto.shape)
							   && x.translate.IsDeepEqual(setInterpretationRoiDto.translate)
							   && (x.stanceRotation == setInterpretationRoiDto.stanceRotation)
							   && (x.exceptional == setInterpretationRoiDto.exceptional)
							   && (x.valuesSet == setInterpretationRoiDto.valuesSet)
							   && (x.signInterpretationId
								   == setInterpretationRoiDto.signInterpretationId));
			}
		}
	}
}
