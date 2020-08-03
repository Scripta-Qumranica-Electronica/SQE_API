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

using SQE.DatabaseAccess.Helpers;

using System.Text.Json;

using SQE.API.Server.Helpers;

namespace SQE.API.Server.RealtimeHubs
{
    public partial class MainHub
    {
        /// <summary>
        ///     Creates a new artefact with the provided data.
        ///
        ///     If no mask is provided, a placeholder mask will be created with the values:
        ///     "POLYGON((0 0,1 1,1 0,0 0))" (the system requires a valid WKT polygon mask for
        ///     every artefact). It is not recommended to leave the mask, name, or work status
        ///     blank or null. It will often be advantageous to leave the transformation null
        ///     when first creating a new artefact.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">A CreateArtefactDTO with the data for the new artefact</param>
        [Authorize]
        public async Task<ArtefactDTO> PostV1EditionsEditionIdArtefacts(uint editionId, CreateArtefactDTO payload)

        {
            try
            {
                return await _artefactService.CreateArtefactAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), payload, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Deletes the specified artefact
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [Authorize]
        public async Task DeleteV1EditionsEditionIdArtefactsArtefactId(uint editionId, uint artefactId)

        {
            try
            {
                await _artefactService.DeleteArtefactAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), artefactId, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        public async Task<ArtefactDTO> GetV1EditionsEditionIdArtefactsArtefactId(uint editionId, uint artefactId, List<string> optional)

        {
            try
            {
                return await _artefactService.GetEditionArtefactAsync(await _userService.GetCurrentUserObjectAsync(editionId), artefactId, optional);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides a listing of all rois belonging to an artefact in the specified edition
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [AllowAnonymous]
        public async Task<InterpretationRoiDTOList> GetV1EditionsEditionIdArtefactsArtefactIdRois(uint editionId, uint artefactId)

        {
            try
            {
                return await _roiService.GetRoisByArtefactIdAsync(await _userService.GetCurrentUserObjectAsync(editionId), artefactId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        public async Task<ArtefactListDTO> GetV1EditionsEditionIdArtefacts(uint editionId, List<string> optional)

        {
            try
            {
                return await _artefactService.GetEditionArtefactListingsAsync(await _userService.GetCurrentUserObjectAsync(editionId), optional);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides a listing of text fragments that have text in the specified artefact.
        ///     With the optional query parameter "suggested", this endpoint will also return
        ///     any text fragment that the system suggests might have text in the artefact.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="optional">Add "suggested" to include possible matches suggested by the system</param>
        [AllowAnonymous]
        public async Task<ArtefactTextFragmentMatchListDTO> GetV1EditionsEditionIdArtefactsArtefactIdTextFragments(uint editionId, uint artefactId, List<string> optional)

        {
            try
            {
                return await _artefactService.ArtefactTextFragmentsAsync(await _userService.GetCurrentUserObjectAsync(editionId), artefactId, optional);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Updates the specified artefact.
        /// 
        ///     There are many possible attributes that can be changed for
        ///     an artefact.  The caller should only input only those that
        ///     should be changed. Attributes with a null value will be ignored.
        ///     For instance, setting the mask to null or "" will result in
        ///     no changes to the current mask, and no value for the mask will
        ///     be returned (or broadcast). Likewise, the transformation, name,
        ///     or status message may be set to null and no change will be made
        ///     to those entities (though any unchanged values will be returned
        ///     along with the changed values and also broadcast to co-editors).
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">An UpdateArtefactDTO with the desired alterations to the artefact</param>
        [Authorize]
        public async Task<ArtefactDTO> PutV1EditionsEditionIdArtefactsArtefactId(uint editionId, uint artefactId, UpdateArtefactDTO payload)

        {
            try
            {
                return await _artefactService.UpdateArtefactAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), artefactId, payload, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Updates the positional data for a batch of artefacts
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">A BatchUpdateArtefactTransformDTO with a list of the desired updates</param>
        /// <returns></returns>
        [Authorize]
        public async Task<BatchUpdatedArtefactTransformDTO> PostV1EditionsEditionIdArtefactsBatchTransformation(uint editionId, BatchUpdateArtefactPlacementDTO payload)

        {
            try
            {
                return await _artefactService.BatchUpdateArtefactTransformAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), payload, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Gets a listing of all artefact groups in the edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<ArtefactGroupListDTO> GetV1EditionsEditionIdArtefactGroups(uint editionId)

        {
            try
            {
                return await _artefactService.ArtefactGroupsOfEditionAsync(await _userService.GetCurrentUserObjectAsync(editionId));
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Gets the details of a specific artefact group in the edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactGroupId">Id of the desired artefact group</param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<ArtefactGroupDTO> GetV1EditionsEditionIdArtefactGroupsArtefactGroupId(uint editionId, uint artefactGroupId)

        {
            try
            {
                return await _artefactService.GetArtefactGroupDataAsync(await _userService.GetCurrentUserObjectAsync(editionId), artefactGroupId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Creates a new artefact group with the submitted data.
        ///     The new artefact must have a list of artefacts that belong to the group.
        ///     It is not necessary to give the group a name.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">Parameters of the new artefact group</param>
        /// <returns></returns>
        [Authorize]
        public async Task<ArtefactGroupDTO> PostV1EditionsEditionIdArtefactGroups(uint editionId, CreateArtefactGroupDTO payload)

        {
            try
            {
                return await _artefactService.CreateArtefactGroupAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), payload, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Updates the details of an artefact group.
        ///     The artefact group will now only contain the artefacts listed in the JSON payload.
        ///     If the name is null, no change will be made, otherwise the name will also be updated.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactGroupId">Id of the artefact group to be updated</param>
        /// <param name="payload">Parameters that the artefact group should be changed to</param>
        /// <returns></returns>
        [Authorize]
        public async Task<ArtefactGroupDTO> PutV1EditionsEditionIdArtefactGroupsArtefactGroupId(uint editionId, uint artefactGroupId, UpdateArtefactGroupDTO payload)

        {
            try
            {
                return await _artefactService.UpdateArtefactGroupAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), artefactGroupId, payload, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Deletes the specified artefact group.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactGroupId">Unique Id of the artefact group to be deleted</param>
        /// <returns></returns>
        [Authorize]
        public async Task<DeleteDTO> DeleteV1EditionsEditionIdArtefactGroupsArtefactGroupId(uint editionId, uint artefactGroupId)

        {
            try
            {
                return await _artefactService.DeleteArtefactGroupAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), artefactGroupId, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


    }
}
