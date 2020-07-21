using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        /// <summary>
        ///     Check that an artefact group can be created and deleted.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanCreateAndDeleteArtefactGroups()
        {
            // Arrange
            using (var editionCreator =
                new EditionHelpers.EditionCreator(_client))
            {
                /**
                 * Create a new edition and also a new artefact group in it.
                 */

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

                /**
                 * Delete the new artefact group.
                 */
                _deleteArtefactGroupAsync(editionId, createdArtefactGroup.id);
            }
        }

        /// <summary>
        ///     Check that an artefact group can be updated.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUpdateArtefactGroups()
        {
            // Arrange
            using (var editionCreator =
                new EditionHelpers.EditionCreator(_client))
            {
                /**
                 * Create a new edition and a new artefact group in it.
                 */

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

                /**
                 * Prepare updates to the new artefact group by changing its name and
                 * deleting artefacts.LastOrDefault().id by omitting it, while also adding
                 * two new artefacts.
                 */

                // Arrange
                var updatedArtefactGroupName = "artefact group 2";
                var updatedArtefacts = new List<uint>() { artefacts.FirstOrDefault().id, artefacts[2].id, artefacts[4].id };

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
        ///     Check that an artefact group can be updated.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotPerformBadUpdateToArtefactGroup()
        {
            // Arrange
            using (var editionCreator =
                new EditionHelpers.EditionCreator(_client))
            {
                /**
                 * Create new edition and a new artefact group 
                 */

                var editionId = await editionCreator.CreateEdition();
                var artefacts = (await ArtefactHelpers.GetEditionArtefacts(editionId, _client)).artefacts;
                const string artefactGroupName = "artefact group 1";

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

                /**
                 * Prepare updates to the new artefact group, which include an artefact ID
                 * that is not part of this edition. The update must fail with a BadRequest.
                 */

                // Arrange
                var validArtefacIds = artefacts.Select(x => x.id).ToList();
                var badArtefactId = (uint)Enumerable.Range(1, 100000).FirstOrDefault(num => !validArtefacIds.Contains((uint)num));
                const string updatedArtefactGroupName = "artefact group 2";
                var updatedArtefacts = new List<uint>() { badArtefactId, artefacts.FirstOrDefault().id, artefacts.LastOrDefault().id };

                // Act
                var (updateHttpMsg, updatedAG, _, _) = await _updateArtefactGroupAsync(editionId, createdArtefactGroup.id,
                    updatedArtefactGroupName, updatedArtefacts, shouldSucceed: false);
                var errorMsg = await updateHttpMsg.Content.ReadAsStringAsync();
                var (_, artefactGroup, _) = await _getArtefactGroupAsync(editionId, createdArtefactGroup.id);

                // Assert
                Assert.True(errorMsg.Contains(badArtefactId.ToString()));
                Assert.True(errorMsg.Contains("not part of this edition"));
                Assert.Equal(HttpStatusCode.BadRequest, updateHttpMsg.StatusCode);
                Assert.Equal(artefactGroupName, artefactGroup.name);
                createdArtefactGroup.artefacts.Sort();
                artefactGroup.artefacts.Sort();
                Assert.Equal(createdArtefactGroup.artefacts, artefactGroup.artefacts);
                Assert.Equal(createdArtefactGroup.id, artefactGroup.id);

                _deleteArtefactGroupAsync(editionId, createdArtefactGroup.id);
            }
        }

        /// <summary>
        ///     Check that an artefact group can be updated.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CannotReuseArtefactsInArtefactGroup()
        {
            // Arrange
            using (var editionCreator =
                new EditionHelpers.EditionCreator(_client))
            {
                /**
                 * Create new edition and a new artefact group 
                 */

                var editionId = await editionCreator.CreateEdition();
                var artefacts = (await ArtefactHelpers.GetEditionArtefacts(editionId, _client)).artefacts;
                const string artefactGroupName = "artefact group 1";

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

                /**
                 * Create a second artefact group with an artefact ID from the previous artefact group.
                 * This should fail with a BadRequest and notification of the offenting artefact ID.
                 */

                // Arrange
                const string secondArtefactGroupName = "invalid artefact group";
                var secondGroupArtefacts = new List<uint>() { artefacts.FirstOrDefault().id, artefacts[1].id };

                // Act
                var (secondHttpMsg, _, _, _) = await _createArtefactGroupAsync(editionId, secondArtefactGroupName, secondGroupArtefacts, shouldSucceed: false);
                var errorMsg = await secondHttpMsg.Content.ReadAsStringAsync();
                var (_, artefactGroupList, _) = await _getArtefactGroupsAsync(editionId);

                // Assert
                Assert.True(errorMsg.Contains(artefacts.FirstOrDefault().id.ToString()));
                Assert.True(errorMsg.Contains("already in another group"));
                Assert.Equal(HttpStatusCode.BadRequest, secondHttpMsg.StatusCode);
                Assert.Equal(artefactGroupName, artefactGroupList.artefactGroups.FirstOrDefault().name);
                Assert.Equal(1, artefactGroupList.artefactGroups.Count());
                createdArtefactGroup.artefacts.Sort();
                artefactGroupList.artefactGroups.FirstOrDefault().artefacts.Sort();
                Assert.Equal(createdArtefactGroup.artefacts, artefactGroupList.artefactGroups.FirstOrDefault().artefacts);
                Assert.Equal(createdArtefactGroup.id, artefactGroupList.artefactGroups.FirstOrDefault().id);

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
        /// Get a single artefact groups in an edition
        /// </summary>
        /// <param name="editionId">The edition to search for the specified artefact group</param>
        /// <param name="artefactGroupId">The ID of the desired artefact group</param>
        /// <param name="shouldSucceed">Flag whether the operation is expected to succeed</param>
        /// <param name="user">Credentials for the user making the request</param>
        /// <returns></returns>
        private async
            Task<(HttpResponseMessage httpResponseMessage, ArtefactGroupDTO httpResponseBody, ArtefactGroupDTO
                signalrResponse)> _getArtefactGroupAsync(uint editionId, uint artefactGroupId,
                bool shouldSucceed = true, Request.UserAuthDetails user = null)
        {
            // Arrange
            user ??= Request.DefaultUsers.User1;

            // Act
            var getApiRequest = new Get.V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId(editionId, artefactGroupId);
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
            if (shouldSucceed)
            {
                Assert.Equal(artefactGroupName, httpBody.name);
                artefactGroup.artefacts.Sort();
                httpBody.artefacts.Sort();
                Assert.Equal(artefactGroup.artefacts, httpBody.artefacts);
            }

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
            if (shouldSucceed)
            {
                Assert.Equal(artefactGroupName, httpBody.name);
                artefactGroup.artefacts.Sort();
                httpBody.artefacts.Sort();
                Assert.Equal(artefactGroup.artefacts, httpBody.artefacts);
            }

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
            if (shouldSucceed)
            {
                Assert.Equal(EditionEntities.artefactGroup, httpBody.entity);
                Assert.Equal(artefactGroupId, httpBody.ids.FirstOrDefault());
            }

            return (httpMessage, httpBody, signalr, listener);
        }
    }
}