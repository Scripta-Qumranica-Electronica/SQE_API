using System;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;

namespace SQE.ApiTest.Helpers
{
	public static class ArtefactHelpers
	{
		/// <summary>
		///  Gets the artefacts of the specified edition
		/// </summary>
		/// <param name="editionId">Id of to search for artefacts.</param>
		/// <param name="client">The http client.</param>
		/// <param name="user">The details of the user making the request.</param>
		/// <returns></returns>
		public static async Task<ArtefactListDTO> GetEditionArtefacts(
				uint                      editionId
				, HttpClient              client
				, Request.UserAuthDetails user = null)
		{
			var apiRequest = new Get.V1_Editions_EditionId_Artefacts(editionId);

			await apiRequest.SendAsync(
					client
					, null
					, true
					, user);

			var (response, artefactResponse) = (apiRequest.HttpResponseMessage
												, apiRequest.HttpResponseObject);

			response.EnsureSuccessStatusCode();

			return artefactResponse;
		}

		/// <summary>
		///  Delete an artefact via the API
		/// </summary>
		/// <param name="editionId"></param>
		/// <param name="artefactId"></param>
		/// <param name="client"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public static async Task DeleteArtefact(
				uint                      editionId
				, uint                    artefactId
				, HttpClient              client
				, Request.UserAuthDetails user = null)
		{
			var apiRequest =
					new Delete.V1_Editions_EditionId_Artefacts_ArtefactId(editionId, artefactId);

			await apiRequest.SendAsync(
					client
					, null
					, true
					, user);

			apiRequest.HttpResponseMessage.EnsureSuccessStatusCode();
		}

		public static async Task<InterpretationRoiDTOList> GetArtefactRois(
				uint                                editionId
				, uint                              artefactId
				, HttpClient                        client
				, Func<string, Task<HubConnection>> signalr
				, bool                              auth = false)
		{
			var request =
					new Get.V1_Editions_EditionId_Artefacts_ArtefactId_Rois(editionId, artefactId);

			await request.SendAsync(client, signalr, auth);

			request.HttpResponseObject.ShouldDeepEqual(request.SignalrResponseObject);

			return request.HttpResponseObject;
		}
	}
}
