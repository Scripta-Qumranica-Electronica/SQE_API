using System;
using System.Net.Http;
using System.Threading.Tasks;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using Xunit;

namespace SQE.ApiTest.Helpers
{
    public static class EditionHelpers
    {
        private static int editionCount;

        // This is a list of editions that have more than one text fragment and more than one artefact shape.
        // If further constraints are necessary for the edition pool, run a query and update the results here.
        /*
			SELECT DISTINCT edition_id
			FROM manuscript_to_text_fragment_owner
			JOIN artefact_shape_owner USING(edition_id)
			GROUP BY edition_id
			HAVING COUNT(DISTINCT manuscript_to_text_fragment_id) > 3 AND COUNT(DISTINCT artefact_shape_id) > 10
		 */
        private static readonly uint[] usableEditionIds =
        {
            3, 22, 66, 67, 76, 78, 80, 86, 87, 89, 90, 93, 94, 95, 96, 98, 100, 102, 106, 107, 108, 109, 111, 114, 115,
            116, 117, 121, 123, 124, 125, 128, 129, 130, 131, 132, 133, 134, 136, 137, 140, 141, 144, 148, 149, 150,
            154, 155, 156, 158, 160, 165, 166, 167, 171, 209, 210, 213, 216, 217, 218, 219, 220, 221, 226, 227, 228,
            230, 231, 232, 233, 234, 240, 242, 243, 244, 246, 247, 248, 256, 257, 258, 259, 260, 265, 269, 271, 273,
            274, 280, 294, 298, 308, 323, 324, 326, 327, 328, 329, 331, 332, 334, 338, 339, 340, 341, 342, 343, 345,
            350, 352, 354, 355, 356, 357, 358, 359, 362, 363, 365, 366, 367, 369, 370, 373, 377, 380, 381, 382, 383,
            387, 388, 389, 390, 391, 393, 394, 395, 396, 397, 400, 403, 404, 406, 407, 414, 430, 432, 433, 434, 436,
            470, 483, 493, 495, 496, 502, 509, 511, 513, 515, 519, 521, 522, 524, 526, 528, 530, 531, 532, 533, 534,
            535, 536, 541, 542, 548, 550, 552, 554, 556, 557, 558, 569, 570, 573, 575, 576, 578, 579, 587, 593, 596,
            615, 616, 617, 622, 623, 625, 631, 632, 633, 634, 637, 641, 643, 644, 645, 646, 650, 651, 653, 655, 657,
            661, 720, 728, 743, 760, 806, 808, 810, 819, 820, 830, 833, 838, 839, 842, 848, 849, 850, 853, 856, 861,
            862, 863, 864, 865, 868, 871, 873, 874, 875, 876, 877, 878, 879, 882, 885, 888, 890, 891, 897, 898, 899,
            900, 901, 902, 903, 904, 905, 918, 920, 926, 927, 928, 930, 932, 933, 934, 935, 936, 937, 939, 941, 944,
            956, 957, 960, 968, 969, 970, 972, 1014, 1017, 1019, 1022, 1023, 1028, 1029, 1042, 1050, 1646, 1647, 1649,
            1651, 1653, 1654, 1656, 1657, 1662, 1665, 1667, 1669, 1671, 1672, 1675, 1678, 1680, 1681, 1685, 1686, 1693
        };

        private static readonly int numberOfUsableEditions = usableEditionIds.Length;

        /// <summary>
        ///     Get an editionId for an edition with > 2 text fragments and > 2 artefacts.
        ///     This iterates over a large list of valid edition ids so that the likelihood of
        ///     table lock errors in the tests is virtually 0.
        /// </summary>
        /// <returns></returns>
        public static uint GetEditionId()
        {
            editionCount = (editionCount + 1) % numberOfUsableEditions; // Increment the counts (loop back at end)
            return usableEditionIds[editionCount]; // Return the id
        }

        /// <summary>
        ///     Retrieves an Edition object either randomly or using a specified editionId.
        /// </summary>
        /// <param name="client">The HttpClient used to make the request.</param>
        /// <param name="editionId">Specifies the editionId to be used, or leave black for one to be automatically selected.</param>
        /// <param name="jwt">A JWT can be added the request to access private editions.</param>
        /// <returns>an EditionDTO for the desired edition</returns>
        public static async Task<EditionDTO> GetEdition(
            HttpClient client,
            uint editionId = 0,
            Request.UserAuthDetails user = null,
            bool auth = false)
        {
            if (editionId == 0)
                editionId = GetEditionId();
            if (auth && user == null)
                user = Request.DefaultUsers.User1;
            var getEditionObject = new Get.V1_Editions_EditionId(editionId);
            var (response, editionResponse, _, _) = await Request.Send(
                getEditionObject,
                client,
                null,
                requestUser: user,
                auth: auth
            );
            response.EnsureSuccessStatusCode();

            return editionResponse.primary;
        }

        /// <summary>
        ///     Creates a new edition. If no editionId is entered, one will be selected automatically for you.
        /// </summary>
        /// <param name="client">The HttpClient</param>
        /// <param name="editionId">Optional id of the edition to be cloned</param>
        /// <param name="name">Optional name for the new edition</param>
        /// <param name="userAuthDetails">User object with the user login credentials</param>
        /// <returns>The ID of the new edition</returns>
        public static async Task<uint> CreateCopyOfEdition(HttpClient client,
            uint editionId = 0,
            string name = "",
            Request.UserAuthDetails userAuthDetails = null)
        {
            if (editionId == 0)
                editionId = GetEditionId();
            if (string.IsNullOrEmpty(name)) name = "test-name-" + editionCount;

            var newScrollRequest = new Post.V1_Editions_EditionId(editionId, new EditionCopyDTO(name, null, null));

            var (httpMsg, httpResp, _, _) = await Request.Send(
                newScrollRequest,
                client,
                null,
                requestUser: userAuthDetails ?? Request.DefaultUsers.User1,
                auth: true,
                deterministic: false,
                shouldSucceed: false
            );
            httpMsg.EnsureSuccessStatusCode();
            return httpResp.id;
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
            bool authenticated = true,
            bool shouldSucceed = true,
            Request.UserAuthDetails userAuthDetails = null)
        {
            if (authenticated && userAuthDetails == null)
                userAuthDetails = Request.DefaultUsers.User1;
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

        /// <summary>
        ///     This class can be used in a using block to clone an edition for tests. At the end of the using block,
        ///     it will automatically delete the newly created edition.
        /// </summary>
        public class EditionCreator : IDisposable
        {
            private readonly HttpClient _client;
            private readonly string _name;
            private readonly Request.UserAuthDetails _userAuthDetails;

            /// <summary>
            /// </summary>
            /// <param name="client">An http client connection</param>
            /// <param name="editionId">The edition to clone (if blank, one will be selected automatically)</param>
            /// <param name="name">Optional name for the new edition</param>
            /// <param name="userAuthDetails">
            ///     Optional user authentication details (if blank Request.DefaultUsers.User1
            ///     will be used)
            /// </param>
            public EditionCreator(HttpClient client,
                uint editionId = 0,
                string name = "",
                Request.UserAuthDetails userAuthDetails = null)
            {
                _client = client;
                _name = name;
                _userAuthDetails = userAuthDetails;
                _editionId = editionId;
            }

            private uint _editionId { get; set; }

            // This seems to work properly even though it is an antipattern.
            // There is no async Dispose (Task.Run...Wait() is a hack) and it is supposed to be very short running anyway.
            // Maybe using try/finally in the individual tests would ultimately be safer.
            public void Dispose()
            {
                // shouldSucceed here is false, since we don't really care if it worked.
                Task.Run(async () =>
                        await DeleteEdition(_client, _editionId, userAuthDetails: _userAuthDetails,
                            shouldSucceed: false))
                    .Wait();
            }

            public async Task<uint> CreateEdition()
            {
                _editionId = await CreateCopyOfEdition(_client, _editionId, _name, _userAuthDetails);
                return _editionId;
            }
        }
    }
}