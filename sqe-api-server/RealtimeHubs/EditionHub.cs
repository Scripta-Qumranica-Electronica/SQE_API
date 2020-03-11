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
        ///     Adds an editor to the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        [Authorize]
        public async Task PostV1EditionsEditionIdAddEditorRequest(uint editionId, CreateEditorRightsDTO payload)

        {
            try
            {
                await _editionService.RequestNewEditionEditor(await _userService.GetCurrentUserObjectAsync(editionId, admin: true), payload, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Confirma addition of an editor to the specified edition
        /// </summary>
        /// <param name="token">JWT for verifying the request confirmation</param>
        [Authorize]
        public async Task<CreateEditorRightsDTO> PostV1EditionsConfirmEditorshipToken(string token)

        {
            try
            {
                return await _editionService.AddEditionEditor(_userService.GetCurrentUserId(), token, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Changes the rights for an editor of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="editorEmailId">Email address of the editor whose permissions are being changed</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        [Authorize]
        public async Task<CreateEditorRightsDTO> PutV1EditionsEditionIdEditorsEditorEmailId(uint editionId, string editorEmailId, UpdateEditorRightsDTO payload)

        {
            try
            {
                return await _editionService.ChangeEditionEditorRights(await _userService.GetCurrentUserObjectAsync(editionId, admin: true), editorEmailId, payload, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Creates a copy of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
        [Authorize]
        public async Task<EditionDTO> PostV1EditionsEditionId(uint editionId, EditionCopyDTO request)

        {
            try
            {
                return await _editionService.CopyEditionAsync(await _userService.GetCurrentUserObjectAsync(editionId), request, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Optional parameters: 'deleteForAllEditors'</param>
        /// <param name="token">token required when using optional 'deleteForAllEditors'</param>
        [Authorize]
        public async Task<DeleteTokenDTO> DeleteV1EditionsEditionId(uint editionId, List<string> optional, string token)

        {
            try
            {
                return await _editionService.DeleteEditionAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), token, optional, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [AllowAnonymous]
        public async Task<EditionGroupDTO> GetV1EditionsEditionId(uint editionId)

        {
            try
            {
                return await _editionService.GetEditionAsync(await _userService.GetCurrentUserObjectAsync(editionId));
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides a listing of all editions accessible to the current user
        /// </summary>
        [AllowAnonymous]
        public async Task<EditionListDTO> GetV1Editions()

        {
            try
            {
                return await _editionService.ListEditionsAsync(_userService.GetCurrentUserId());
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Updates data for the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be updated</param>
        [Authorize]
        public async Task<EditionDTO> PutV1EditionsEditionId(uint editionId, EditionUpdateRequestDTO request)

        {
            try
            {
                return await _editionService.UpdateEditionAsync(await _userService.GetCurrentUserObjectAsync(editionId, true), request, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides spatial data for all letters in the edition 
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <returns></returns>
        [Authorize]
        public async Task<EditionScriptCollectionDTO> GetV1EditionsEditionIdScriptCollection(uint editionId)

        {
            try
            {
                return await _editionService.GetEditionScriptCollection(await _userService.GetCurrentUserObjectAsync(editionId));
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


    }
}
