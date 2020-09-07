using System;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;

namespace SQE.ApiTest.Helpers
{
    public static class ImagedObjectHelpers
    {
        /// <summary>
        /// Return all imaged objects belonging to an Institution
        /// </summary>
        /// <param name="institution"></param>
        /// <param name="client"></param>
        /// <param name="signalr"></param>
        /// <returns></returns>
        public static async Task<InstitutionalImageListDTO> GetInstitutionImagedObjects(
            string institution,
            HttpClient client,
            Func<string, Task<HubConnection>> signalr)
        {
            var apiRequest = new Get.V1_ImagedObjects_Institutions_InstitutionName(institution);
            await apiRequest.SendAsync(client, signalr);

            apiRequest.HttpResponseObject.ShouldDeepEqual(apiRequest.SignalrResponseObject);
            return apiRequest.HttpResponseObject;
        }
    }
}