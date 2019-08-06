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
        /// Adds an editor to the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        [Authorize]
        public async Task<EditorRightsDTO> PostV1EditionsEditionIdEditors(uint editionId, EditorRightsDTO payload)
        {
            return await _editionService.AddEditionEditor(
                _userService.GetCurrentUserObject(editionId),
                payload);
        }

        /// <summary>
        /// Changes the rights for an editor of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        [Authorize]
        public async Task<EditorRightsDTO> PutV1EditionsEditionIdEditors(uint editionId, EditorRightsDTO payload)
        {
            return await _editionService.ChangeEditionEditorRights(
                _userService.GetCurrentUserObject(editionId),
                payload);
        }

        /// <summary>
        /// Creates a copy of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
        [Authorize]
        public async Task<EditionDTO> PostV1EditionsEditionId(uint editionId, EditionCopyDTO request)
        {
            return await _editionService.CopyEditionAsync(
                _userService.GetCurrentUserObject(editionId),
                request);
        }

        /// <summary>
        /// Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Optional parameters: 'deleteForAllEditors'</param>
        /// <param name="token">token required when using optional 'deleteForAllEditors'</param>
        [Authorize]
        public async Task<DeleteTokenDTO> DeleteV1EditionsEditionId(uint editionId, List<string> optional, string token)
        {
            return await _editionService.DeleteEditionAsync(
                _userService.GetCurrentUserObject(editionId),
                token,
                optional,
                clientId: Context.ConnectionId);
        }

        /// <summary>
        /// Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [AllowAnonymous]
        public async Task<EditionGroupDTO> GetV1EditionsEditionId(uint editionId)
        {
            return await _editionService.GetEditionAsync(_userService.GetCurrentUserObject(editionId));
        }

        /// <summary>
        /// Provides a listing of all editions accessible to the current user
        /// </summary>
        [AllowAnonymous]
        public async Task<EditionListDTO> GetV1Editions()
        {
            return await _editionService.ListEditionsAsync(_userService.GetCurrentUserId());
        }

        /// <summary>
        /// Updates data for the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be updated</param>
        [Authorize]
        public async Task<EditionDTO> PutV1EditionsEditionId(uint editionId, EditionUpdateRequestDTO request)
        {
            return await _editionService.UpdateEditionAsync(
                _userService.GetCurrentUserObject(editionId),
                request.name,
                request.copyrightHolder,
                request.collaborators,
                clientId: Context.ConnectionId);
        }
    }
}