using System.Net.Http;
using System.Threading.Tasks;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;

namespace SQE.ApiTest.Helpers
{
    public static class ArtefactHelpers
    {
        /// <summary>
        ///     Selects an edition with artefacts in sequence (to avoid locks) and returns its artefacts.
        /// </summary>
        /// <param name="userId">Id of the user whose editions should be randomly selected.</param>
        /// <param name="jwt">A JWT can be added the request to access private editions.</param>
        /// <returns></returns>
        public static async Task<ArtefactListDTO> GetEditionArtefacts(uint editionId, HttpClient client,
            Request.UserAuthDetails user = null)
        {
            var apiRequest = new Get.V1_Editions_EditionId_Artefacts(editionId);
            var (response, artefactResponse, _, _) = await Request.Send(
                apiRequest,
                client,
                null,
                true,
                user
            );
            response.EnsureSuccessStatusCode();
            return artefactResponse;
        }

        /// <summary>
        ///     Delete an artefact via the API
        /// </summary>
        /// <param name="editionId"></param>
        /// <param name="artefactId"></param>
        /// <param name="client"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task DeleteArtefact(uint editionId, uint artefactId, HttpClient client,
            Request.UserAuthDetails user = null)
        {
            var apiRequest = new Delete.V1_Editions_EditionId_Artefacts_ArtefactId(editionId, artefactId);
            var (response, artefactResponse, _, _) = await Request.Send(
                apiRequest,
                client,
                null,
                true,
                user
            );
            response.EnsureSuccessStatusCode();
        }
    }
}