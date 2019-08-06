using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SQE.SqeApi.Server.DTOs;

namespace SQE.SqeApi.Server.Hubs
{
    public partial class MainHub : Hub
    {
        /// <summary>
        /// Creates a new artefact with the provided data.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">A CreateArtefactDTO with the data for the new artefact</param>
        [Authorize]
        public async Task<ArtefactDTO> PostV1EditionsEditionIdArtefacts(uint editionId, CreateArtefactDTO payload)
        {
            return await _artefactService.CreateArtefactAsync(
                _userService.GetCurrentUserObject(editionId),
                editionId,
                payload.masterImageId,
                payload.mask,
                payload.name,
                payload.position,
                clientId: Context.ConnectionId);
        }

        /// <summary>
        /// Deletes the specified artefact
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [Authorize]
        public async Task DeleteV1EditionsEditionIdArtefactsArtefactId(uint artefactId, uint editionId)
        {
            await _artefactService.DeleteArtefactAsync(
                _userService.GetCurrentUserObject(editionId),
                artefactId,
                clientId: Context.ConnectionId);
        }

        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        public async Task<ArtefactDTO> GetV1EditionsEditionIdArtefactsArtefactId(uint artefactId, uint editionId,
            List<string> optional)
        {
            return await _artefactService.GetEditionArtefactAsync(
                _userService.GetCurrentUserObject(editionId),
                artefactId,
                optional);
        }

        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        public async Task<ArtefactListDTO> GetV1EditionsEditionIdArtefacts(uint editionId, List<string> optional)
        {
            return await _artefactService.GetEditionArtefactListingsAsync(
                _userService.GetCurrentUserId(),
                editionId,
                optional);
        }

        /// <summary>
        /// Updates the specified artefact
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">An UpdateArtefactDTO with the desired alterations to the artefact</param>
        [Authorize]
        public async Task<ArtefactDTO> PutV1EditionsEditionIdArtefactsArtefactId(uint artefactId, uint editionId,
            UpdateArtefactDTO payload)
        {
            return await _artefactService.UpdateArtefactAsync(
                _userService.GetCurrentUserObject(editionId),
                editionId,
                artefactId,
                payload.mask,
                payload.name,
                payload.position,
                clientId: Context.ConnectionId);
        }
    }
}