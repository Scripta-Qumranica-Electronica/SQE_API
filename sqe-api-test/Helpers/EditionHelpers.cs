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
			HAVING COUNT(manuscript_to_text_fragment_id) > 2 AND COUNT(artefact_shape_id) > 2
		 */
        private static readonly uint[] usableEditionIds =
        {
            1, 2, 3, 4, 5, 6, 7, 9, 11, 12, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
            33, 34, 35, 37, 38, 39, 40, 41, 42, 50, 55, 56, 57, 58, 59, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72,
            73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99,
            100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 111, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123,
            124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 144, 145,
            146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 163, 164, 165, 166, 167,
            168, 169, 170, 171, 172, 176, 178, 179, 180, 185, 189, 190, 196, 197, 199, 201, 202, 203, 204, 205, 207,
            208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228,
            229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249,
            250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 269, 270, 271, 272,
            273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 293, 294,
            295, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 312, 313, 314, 316, 317, 318, 320, 321,
            322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335, 336, 337, 338, 339, 340, 341, 342,
            343, 344, 345, 346, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 361, 362, 363, 364, 365,
            366, 367, 368, 369, 370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 380, 381, 382, 383, 384, 385, 387,
            388, 389, 390, 391, 392, 393, 394, 395, 396, 397, 398, 399, 400, 401, 402, 403, 404, 405, 406, 407, 408,
            409, 412, 413, 414, 415, 416, 417, 419, 420, 421, 422, 423, 424, 425, 426, 427, 428, 429, 430, 431, 432,
            433, 434, 435, 436, 438, 445, 449, 455, 457, 470, 471, 472, 475, 476, 478, 479, 481, 482, 483, 485, 486,
            487, 488, 491, 493, 494, 495, 496, 497, 498, 499, 500, 501, 502, 503, 504, 505, 506, 507, 508, 509, 510,
            511, 512, 513, 514, 515, 516, 517, 518, 519, 520, 521, 522, 523, 524, 525, 526, 527, 528, 529, 530, 531,
            532, 533, 534, 535, 536, 537, 538, 539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552,
            553, 554, 555, 556, 557, 558, 559, 560, 561, 564, 565, 567, 568, 569, 570, 571, 572, 573, 574, 575, 576,
            577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 587, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598,
            599, 600, 601, 603, 605, 606, 607, 608, 609, 610, 614, 615, 616, 617, 618, 619, 620, 621, 622, 623, 624,
            625, 626, 627, 628, 629, 630, 631, 632, 633, 634, 635, 636, 637, 638, 639, 641, 642, 643, 644, 645, 646,
            647, 648, 649, 650, 651, 652, 653, 654, 655, 656, 657, 658, 659, 660, 661, 662, 663, 664, 665, 666, 667,
            668, 669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 680, 681, 682, 683, 684, 685, 686, 687, 688,
            689, 690, 691, 692, 693, 694, 695, 696, 697, 698, 699, 700, 701, 707, 708, 709, 710, 711, 712, 713, 714,
            715, 717, 719, 720, 721, 722, 723, 725, 728, 739, 740, 741, 742, 743, 744, 745, 746, 747, 748, 751, 755,
            757, 758, 759, 760, 761, 762, 763, 764, 765, 766, 767, 768, 769, 770, 771, 772, 773, 774, 776, 793, 797,
            798, 800, 802, 803, 804, 805, 806, 807, 808, 809, 810, 811, 812, 813, 814, 815, 816, 818, 819, 820, 821,
            822, 823, 824, 825, 826, 827, 828, 829, 830, 831, 832, 833, 834, 835, 836, 837, 838, 839, 840, 842, 843,
            844, 845, 848, 849, 850, 851, 852, 853, 854, 855, 856, 857, 858, 859, 860, 861, 862, 863, 864, 865, 866,
            868, 870, 871, 872, 873, 874, 875, 876, 877, 878, 879, 880, 881, 882, 883, 884, 885, 886, 887, 888, 889,
            890, 891, 892, 893, 894, 895, 896, 897, 898, 899, 900, 901, 902, 903, 904, 905, 906, 907, 908, 909, 910,
            911, 912, 913, 914, 915, 916, 918, 919, 920, 923, 924, 925, 926, 927, 928, 929, 930, 931, 932, 933, 934,
            935, 936, 937, 938, 939, 940, 941, 942, 943, 944, 945, 946, 947, 948, 949, 953, 955, 956, 957, 958, 959,
            960, 961, 962, 963, 964, 965, 966, 967, 968, 969, 970, 971, 972, 973, 974, 975, 976, 977, 979, 980, 983,
            988, 989, 991, 992, 993, 994, 995, 996, 1000, 1003, 1004, 1006, 1007, 1008, 1009, 1011, 1012, 1013, 1014,
            1015, 1016, 1017, 1018, 1019, 1020, 1021, 1022, 1023, 1024, 1025, 1026, 1027, 1028, 1029, 1034, 1035, 1037,
            1038, 1040, 1041, 1042, 1043, 1044, 1045, 1046, 1047, 1048, 1049, 1050, 1051, 1052, 1053, 1054, 1055, 1056,
            1650, 1651, 1653, 1654
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
                user1: user,
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
                user1: userAuthDetails ?? Request.DefaultUsers.User1,
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
            }

            private uint _editionId { get; set; }

            public void Dispose()
            {
                // This seems to work properly even though it is an antipattern.
                // There is no async Dispose (Task.Run...Wait() is a hack) and it is supposed to be very short running anyway.
                // Maybe using try/finally in the individual tests would ultimately be safer.
                Task.Run(async () => await DeleteEdition(_client, _editionId, userAuthDetails: _userAuthDetails))
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