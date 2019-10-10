/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-api-server` project. Changes made
 * there will automatically be incorporated here the next time the 
 * `sqe-realtime-hub-builder` is run.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SQE.API.DTO;
using SQE.API.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace SQE.API.Server.RealtimeHubs
{
    public partial class MainHub : Hub
    {
        /// <summary>
        ///     Creates a new artefact with the provided data.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">A CreateArtefactDTO with the data for the new artefact</param>
        [Authorize]
        public async Task<ArtefactDTO> PostV1EditionsEditionIdArtefacts(uint editionId, CreateArtefactDTO payload)
        {
            return await _artefactService.CreateArtefactAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), payload, clientId: Context.ConnectionId);
        }

        /// <summary>
        ///     Deletes the specified artefact
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [Authorize]
        public async Task DeleteV1EditionsEditionIdArtefactsArtefactId(uint artefactId, uint editionId)
        {
            await _artefactService.DeleteArtefactAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), artefactId, clientId: Context.ConnectionId);
        }

        /// <summary>
        ///     Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        public async Task<ArtefactDTO> GetV1EditionsEditionIdArtefactsArtefactId(uint artefactId, uint editionId, List<string> optional)
        {
            return await _artefactService.GetEditionArtefactAsync(await _userService.GetCurrentUserObjectAsync(editionId), artefactId, optional);
        }

        /// <summary>
        ///     Provides a listing of all rois belonging to an artefact in the specified edition
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [AllowAnonymous]
        public async Task<InterpretationRoiDTOList> GetV1EditionsEditionIdArtefactsArtefactIdRois(uint artefactId, uint editionId)
        {
            return await _roiService.GetRoisByArtefactIdAsync(await _userService.GetCurrentUserObjectAsync(editionId), artefactId);
        }

        /// <summary>
        ///     Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        public async Task<ArtefactListDTO> GetV1EditionsEditionIdArtefacts(uint editionId, List<string> optional)
        {
            return await _artefactService.GetEditionArtefactListingsAsync(await _userService.GetCurrentUserObjectAsync(editionId), optional);
        }

        /// <summary>
        ///     Provides a listing of text fragments that may match the specified artefact
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        [AllowAnonymous]
        public async Task<TextFragmentDataListDTO> GetV1EditionsEditionIdArtefactsArtefactIdSuggestedTextFragments(uint editionId, uint artefactId)
        {
            return await _artefactService.ArtefactSuggestedTextFragmentsAsync(await _userService.GetCurrentUserObjectAsync(editionId), artefactId);
        }

        /// <summary>
        ///     Updates the specified artefact
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">An UpdateArtefactDTO with the desired alterations to the artefact</param>
        [Authorize]
        public async Task<ArtefactDTO> PutV1EditionsEditionIdArtefactsArtefactId(uint artefactId, uint editionId, UpdateArtefactDTO payload)
        {
            return await _artefactService.UpdateArtefactAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), artefactId, payload, clientId: Context.ConnectionId);
        }

    }
}