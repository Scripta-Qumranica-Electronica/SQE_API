using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

// TODO: It would be nice to be able to generate random polygons for these testing purposes.
namespace SQE.ApiTest
{
    /// <summary>
    ///     This test suite tests all the current endpoints in the ArtefactController
    /// </summary>
    public class ArtefactGroupTests : WebControllerTest
    {
        public ArtefactGroupTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
        }

        // artefactEditions is a list of all editions that have artefacts more that 10 artefacts
        private static readonly uint[] artefactEditions =
        {
            3, 22, 66, 67, 76, 78, 80, 82, 86, 87, 89, 90, 93, 94, 95, 96, 98, 100, 102, 106, 107, 108, 109, 111, 114,
            115, 116, 117, 121, 123, 124, 125, 128, 129, 130, 131, 132, 133, 134, 136, 137, 138, 140, 141, 144, 148,
            149, 150, 152, 154, 155, 156, 158, 160, 164, 165, 166, 167, 171, 209, 210, 213, 216, 217, 218, 219, 220,
            221, 226, 227, 228, 229, 230, 231, 232, 233, 234, 238, 240, 242, 243, 244, 246, 247, 248, 256, 257, 258,
            259, 260, 265, 269, 271, 273, 274, 275, 278, 280, 294, 298, 308, 323, 324, 326, 327, 328, 329, 331, 332,
            334, 338, 339, 340, 341, 342, 343, 345, 350, 352, 354, 355, 356, 357, 358, 359, 362, 363, 365, 366, 367,
            369, 370, 373, 377, 380, 381, 382, 383, 387, 388, 389, 390, 391, 393, 394, 395, 396, 397, 400, 403, 404,
            406, 407, 414, 430, 432, 433, 434, 436, 470, 472, 483, 487, 493, 495, 496, 502, 509, 511, 513, 515, 519,
            521, 522, 524, 526, 528, 530, 531, 532, 533, 534, 535, 536, 541, 542, 546, 548, 550, 552, 554, 556, 557,
            558, 561, 569, 570, 573, 575, 576, 578, 579, 587, 593, 596, 598, 615, 616, 617, 622, 623, 625, 631, 632,
            633, 634, 637, 641, 642, 643, 644, 645, 646, 650, 651, 653, 655, 657, 661, 720, 728, 743, 760, 806, 810,
            819, 820, 830, 833, 838, 839, 842, 848, 849, 850, 853, 856, 861, 862, 863, 864, 865, 868, 870, 871, 873,
            874, 875, 876, 877, 878, 879, 882, 885, 888, 889, 890, 891, 894, 897, 898, 899, 900, 901, 902, 903, 904,
            905, 910, 912, 918, 920, 924, 926, 927, 928, 930, 931, 932, 933, 934, 935, 936, 937, 939, 941, 944, 956,
            957, 960, 968, 969, 970, 972, 973, 1014, 1017, 1019, 1021, 1022, 1023, 1028, 1029, 1042, 1050, 1060, 1062,
            1063, 1064, 1067, 1071, 1075, 1076, 1077, 1080, 1081, 1082, 1083, 1089, 1090, 1091, 1092, 1093, 1094, 1097,
            1098, 1099, 1102, 1107, 1108, 1109, 1110, 1111, 1112, 1118, 1119, 1120, 1122, 1131, 1137, 1159, 1160, 1181,
            1188, 1198, 1208, 1211, 1217, 1236, 1239, 1245, 1246, 1256, 1258, 1259, 1264, 1267, 1269, 1287, 1290, 1291,
            1292, 1293, 1295, 1296, 1298, 1299, 1303, 1308, 1309, 1311, 1313, 1317, 1326, 1331, 1608, 1615, 1616, 1628,
            1629, 1634, 1637, 1638, 1642, 1643
        };


        private static uint artefactEditionCount = 0;


        /// <summary>
        ///     Check that an artefact group can be created and deleted.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanCreateAndDeleteArtefactGroups()
        {
            // Arrange
            using (var editionCreator =
                new EditionHelpers.EditionCreator(_client, artefactEditions[++artefactEditionCount]))
            {
                var editionId = await editionCreator.CreateEdition();
                var artefacts = (await ArtefactHelpers.GetEditionArtefacts(editionId, _client)).artefacts;
                var artefactGroupName = "artefact group 1";

                // Act
                var (_, createdArtefactGroup, _, _) = await _createArtefactGroupAsync(editionId, artefactGroupName, new List<uint>()
                {
                    artefacts.FirstOrDefault().id,
                    artefacts.LastOrDefault().id
                });
                var (_, artefactList, _) = await _getArtefactGroupsAsync(editionId);

                // Assert
                Assert.Equal(1, artefactList.artefactGroups.Count());
                artefactList.artefactGroups.FirstOrDefault().ShouldDeepEqual(createdArtefactGroup);

                _deleteArtefactGroupAsync(editionId, createdArtefactGroup.id);
            }
        }

        /// <summary>
        ///     Check that an artefact group can be created and deleted.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUpdateArtefactGroups()
        {
            // Arrange
            using (var editionCreator =
                new EditionHelpers.EditionCreator(_client, artefactEditions[++artefactEditionCount]))
            {
                var editionId = await editionCreator.CreateEdition();
                var artefacts = (await ArtefactHelpers.GetEditionArtefacts(editionId, _client)).artefacts;
                var artefactGroupName = "artefact group 1";

                // Act
                var (_, createdArtefactGroup, _, _) = await _createArtefactGroupAsync(editionId, artefactGroupName, new List<uint>()
                {
                    artefacts.FirstOrDefault().id,
                    artefacts.LastOrDefault().id
                });
                var (_, artefactList, _) = await _getArtefactGroupsAsync(editionId);

                // Assert
                Assert.Equal(1, artefactList.artefactGroups.Count());
                artefactList.artefactGroups.FirstOrDefault().ShouldDeepEqual(createdArtefactGroup);

                // Arrange
                var updatedArtefactGroupName = "artefact group 2";
                var updatedArtefacts = new List<uint>() { artefacts[2].id, artefacts[4].id };

                // Act
                var (_, updatedAG, _, _) = await _updateArtefactGroupAsync(editionId, createdArtefactGroup.id,
                    updatedArtefactGroupName, updatedArtefacts);
                (_, artefactList, _) = await _getArtefactGroupsAsync(editionId);

                // Assert
                Assert.Equal(updatedArtefactGroupName, updatedAG.name);
                updatedArtefacts.Sort();
                updatedAG.artefacts.Sort();
                Assert.Equal(updatedArtefacts, updatedAG.artefacts);
                Assert.Equal(createdArtefactGroup.id, updatedAG.id);
                Assert.Equal(1, artefactList.artefactGroups.Count());

                _deleteArtefactGroupAsync(editionId, createdArtefactGroup.id);
            }
        }

        /// <summary>
        /// Get a listing of all the artefact groups in an edition
        /// </summary>
        /// <param name="editionId">The edition to search for artefact groups</param>
        /// <param name="shouldSucceed">Flag whether the operation is expected to succeed</param>
        /// <param name="user">Credentials for the user making the request</param>
        /// <returns></returns>
        private async
            Task<(HttpResponseMessage httpResponseMessage, ArtefactGroupListDTO httpResponseBody, ArtefactGroupListDTO
                signalrResponse)> _getArtefactGroupsAsync(uint editionId,
                bool shouldSucceed = true, Request.UserAuthDetails user = null)
        {
            // Arrange
            user ??= Request.DefaultUsers.User1;

            // Act
            var getApiRequest = new Get.V1_Editions_EditionId_ArtefactGroups(editionId);
            var (httpMessage, httpBody, signalr, _) = await Request.Send(
                getApiRequest,
                _client,
                StartConnectionAsync,
                true,
                user,
                shouldSucceed: shouldSucceed,
                listenToEdition: false
            );

            return (httpMessage, httpBody, signalr);
        }


        /// <summary>
        /// Creates a new artefact group in the specified edition
        /// </summary>
        /// <param name="editionId">The edition the artefact is part of</param>
        /// <param name="artefactGroupName">Name for the new artefact group</param>
        /// <param name="artefacts">Artefact IDs to include in the new group</param>
        /// <param name="shouldSucceed">Flag whether the operation is expected to succeed</param>
        /// <param name="user">Credentials for the user making the request</param>
        /// <param name="user2">Credentials for a user who should be notified of the request</param>
        /// <returns></returns>
        private async
            Task<(HttpResponseMessage httpResponseMessage, ArtefactGroupDTO httpResponseBody, ArtefactGroupDTO
                signalrResponse, ArtefactGroupDTO listenerResponse)> _createArtefactGroupAsync(uint editionId,
                string artefactGroupName, List<uint> artefacts, bool shouldSucceed = true,
                Request.UserAuthDetails user = null, Request.UserAuthDetails user2 = null)
        {
            // Arrange
            user ??= Request.DefaultUsers.User1;
            var artefactGroup = new CreateArtefactGroupDTO()
            {
                name = artefactGroupName,
                artefacts = artefacts
            };

            // Act
            var createApiRequest = new Post.V1_Editions_EditionId_ArtefactGroups(editionId, artefactGroup);
            var (httpMessage, httpBody, signalr, listener) = await Request.Send(
                createApiRequest,
                _client,
                null,
                true,
                user,
                user2,
                shouldSucceed: shouldSucceed,
                deterministic: false,
                listenToEdition: user2 != null
            );

            // Assert
            Assert.Equal(artefactGroupName, httpBody.name);
            artefactGroup.artefacts.Sort();
            httpBody.artefacts.Sort();
            Assert.Equal(artefactGroup.artefacts, httpBody.artefacts);

            return (httpMessage, httpBody, signalr, listener);
        }

        /// <summary>
        /// Updates an artefact group in the specified edition
        /// </summary>
        /// <param name="editionId">The edition the artefact is part of</param>
        /// <param name="artefactGroupId">Id of the artefact group to be updated</param>
        /// <param name="artefactGroupName">Name for the artefact group</param>
        /// <param name="artefacts">Artefact IDs to include in the group</param>
        /// <param name="shouldSucceed">Flag whether the operation is expected to succeed</param>
        /// <param name="user">Credentials for the user making the request</param>
        /// <param name="user2">Credentials for a user who should be notified of the request</param>
        /// <returns></returns>
        private async
            Task<(HttpResponseMessage httpResponseMessage, ArtefactGroupDTO httpResponseBody, ArtefactGroupDTO
                signalrResponse, ArtefactGroupDTO listenerResponse)> _updateArtefactGroupAsync(uint editionId, uint artefactGroupId,
                string artefactGroupName, List<uint> artefacts, bool shouldSucceed = true,
                Request.UserAuthDetails user = null, Request.UserAuthDetails user2 = null)
        {
            // Arrange
            user ??= Request.DefaultUsers.User1;
            var artefactGroup = new UpdateArtefactGroupDTO()
            {
                name = artefactGroupName,
                artefacts = artefacts
            };

            // Act
            var updateApiRequest = new Put.V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId(editionId, artefactGroupId, artefactGroup);
            var (httpMessage, httpBody, signalr, listener) = await Request.Send(
                updateApiRequest,
                _client,
                null,
                true,
                user,
                user2,
                shouldSucceed: shouldSucceed,
                deterministic: false,
                listenToEdition: user2 != null
            );

            // Assert
            Assert.Equal(artefactGroupName, httpBody.name);
            artefactGroup.artefacts.Sort();
            httpBody.artefacts.Sort();
            Assert.Equal(artefactGroup.artefacts, httpBody.artefacts);

            return (httpMessage, httpBody, signalr, listener);
        }

        /// <summary>
        /// Delete an artefact group
        /// </summary>
        /// <param name="editionId">The edition the artefact is part of</param>
        /// <param name="artefactGroupId">Id of the artefact group to be deleted</param>
        /// <param name="shouldSucceed">Flag whether the operation is expected to succeed</param>
        /// <param name="user">Credentials for the user making the request</param>
        /// <param name="user2">Credentials for a user who should be notified of the request</param>
        /// <returns></returns>
        private async
            Task<(HttpResponseMessage httpResponseMessage, DeleteDTO httpResponseBody, DeleteDTO signalrResponse,
                DeleteDTO listenerResponse)> _deleteArtefactGroupAsync(uint editionId, uint artefactGroupId,
                bool shouldSucceed = true, Request.UserAuthDetails user = null, Request.UserAuthDetails user2 = null)
        {
            // Arrange
            user ??= Request.DefaultUsers.User1;

            // Act
            var deleteApiRequest =
                new Delete.V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId(editionId, artefactGroupId);
            var (httpMessage, httpBody, signalr, listener) = await Request.Send(
                deleteApiRequest,
                _client,
                StartConnectionAsync,
                true,
                user,
                user2,
                shouldSucceed,
                false,
                listenToEdition: user2 != null
            );

            // Assert
            Assert.Equal(EditionEntities.artefactGroup, httpBody.entity);
            Assert.Equal(artefactGroupId, httpBody.ids.FirstOrDefault());
            return (httpMessage, httpBody, signalr, listener);
        }
    }
}