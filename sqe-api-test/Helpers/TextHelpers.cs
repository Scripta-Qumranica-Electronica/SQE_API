using System;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;

namespace SQE.ApiTest.Helpers
{
	public static class TextHelpers
	{
		public static async Task<TextFragmentDataListDTO> GetEditionTextFragments(
				uint                                editionId
				, HttpClient                        client
				, Func<string, Task<HubConnection>> signalr)
		{
			var apiRequest = new Get.V1_Editions_EditionId_TextFragments(editionId);

			await apiRequest.SendAsync(client, signalr);

			apiRequest.HttpResponseObject.ShouldDeepEqual(apiRequest.SignalrResponseObject);

			return apiRequest.HttpResponseObject;
		}
	}
}
