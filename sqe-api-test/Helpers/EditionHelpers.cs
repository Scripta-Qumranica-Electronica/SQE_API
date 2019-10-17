using System.Net.Http;
using System.Threading.Tasks;
using SQE.API.DTO;
using Xunit;

namespace SQE.ApiTest.Helpers
{
    public static class EditionHelpers
    {
        private const string version = "v1";
        private const string controller = "editions";
        private static int cloneCount;

        /// <summary>
        ///     Retrieves an Edition object wither randomly or using a specified editionId.
        /// </summary>
        /// <param name="client">The HttpClient used to make the request.</param>
        /// <param name="editionId">Specifies the editionId to be used.</param>
        /// <param name="jwt">A JWT can be added the request to access private editions.</param>
        /// <returns>a randomly selected EditionDTO</returns>
        public static async Task<EditionDTO> GetEdition(
            HttpClient client,
            uint editionId = 3,
            string jwt = null)
        {
            var url = $"/{version}/{controller}/{editionId}";
            var (response, editionResponse) = await Request.SendHttpRequestAsync<string, EditionGroupDTO>(
                client,
                HttpMethod.Get,
                url,
                null,
                jwt
            );
            response.EnsureSuccessStatusCode();

            return editionResponse.primary;
        }

        /// <summary>
        ///     Creates a new edition. This will be a copy of editionId 1 if no other editionId is entered.
        /// </summary>
        /// <param name="client">The HttpClient</param>
        /// <param name="editionId">Optional id of the edition to be cloned</param>
        /// <param name="name">Optional name for the new edition</param>
        /// <param name="userAuthDetails">User object with the user login credentials</param>
        /// <returns>The ID of the new edition</returns>
        public static async Task<uint> CreateCopyOfEdition(HttpClient client,
            uint editionId = 1,
            string name = "",
            Request.UserAuthDetails userAuthDetails = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                cloneCount++;
                name = "test-name-" + cloneCount;
            }

            var newScrollRequest = new EditionUpdateRequestDTO(name, null, null);

            var (response, msg) = await Request.SendHttpRequestAsync<EditionUpdateRequestDTO, EditionDTO>(
                client,
                HttpMethod.Post,
                $"/v1/editions/{editionId}",
                newScrollRequest,
                await Request.GetJwtViaHttpAsync(
                    client,
                    userAuthDetails
                )
            );
            response.EnsureSuccessStatusCode();
            return msg.id;
        }

        /// <summary>
        ///     Deletes an edition for all editors.
        /// </summary>
        /// <param name="client">The HttpClient</param>
        /// <param name="editionId"></param>
        /// <param name="authenticated">Optional, whether the request should be made by an authenticated user</param>
        /// <param name="shouldSucceed">Optional, whether the delete action is expected to succeed</param>
        /// <param name="email">Optional, the email of the user who is admin for the edition</param>
        /// <param name="pwd">Optional, the password of the user who is admin for the edition</param>
        /// <returns>void</returns>
        public static async Task DeleteEdition(HttpClient client,
            uint editionId,
            bool authenticated = false,
            bool shouldSucceed = true,
            Request.UserAuthDetails userAuthDetails = null)
        {
            var (response, msg) = await Request.SendHttpRequestAsync<string, DeleteTokenDTO>(
                client,
                HttpMethod.Delete,
                $"/v1/editions/{editionId}?optional=deleteForAllEditors",
                null,
                authenticated ? await Request.GetJwtViaHttpAsync(client, userAuthDetails) : null
            );
            if (shouldSucceed)
            {
                response.EnsureSuccessStatusCode();
                Assert.NotNull(msg.token);
                Assert.Equal(editionId, msg.editionId);
                var (response2, msg2) = await Request.SendHttpRequestAsync<string, DeleteTokenDTO>(
                    client,
                    HttpMethod.Delete,
                    $"/v1/editions/{msg.editionId}?optional=deleteForAllEditors&token={msg.token}",
                    null,
                    authenticated ? await Request.GetJwtViaHttpAsync(client, userAuthDetails) : null
                );
                response2.EnsureSuccessStatusCode();
                Assert.Null(msg2);
            }
        }
    }
}