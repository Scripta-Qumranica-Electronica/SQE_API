/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-api-server` project. Changes made
 * there will automatically be incorporated here the next time the 
 * `sqe-realtime-hub-builder` is run.
 */

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
        ///     Creates a new text fragment in the given edition of a scroll
        /// </summary>
        /// <param name="createFragment">A JSON object with the details of the new text fragment to be created</param>
        /// <param name="editionId">Id of the edition</param>
[Authorize]
public async Task<TextFragmentDataDTO> PostV1EditionsEditionIdTextFragments(uint editionId, CreateTextFragmentDTO createFragment)

    {
        try
        {
                        return await _textService.CreateTextFragmentAsync(                await _userService.GetCurrentUserObjectAsync(editionId, true),                createFragment, clientId: Context.ConnectionId);              
        }
        catch (ApiException err)
        {
            throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
        }
    }


/// <summary>
        ///     Updates the specified text fragment with the submitted properties
        /// </summary>
        /// <param name="editionId">Edition of the text fragment being updates</param>
        /// <param name="textFragmentId">Id of the text fragment being updates</param>
        /// <param name="updatedTextFragment">Details of the updated text fragment</param>
        /// <returns>The details of the updated text fragment</returns>
[Authorize]
public async Task<TextFragmentDataDTO> PutV1EditionsEditionIdTextFragmentsTextFragmentId(uint editionId, uint textFragmentId, UpdateTextFragmentDTO updatedTextFragment)

    {
        try
        {
                        return await _textService.UpdateTextFragmentAsync(                await _userService.GetCurrentUserObjectAsync(editionId),                textFragmentId,                updatedTextFragment, clientId: Context.ConnectionId);              
        }
        catch (ApiException err)
        {
            throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
        }
    }


/// <summary>
        ///     Retrieves the ids of all fragments in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <returns>An array of the text fragment ids in correct sequence</returns>
[AllowAnonymous]
public async Task<TextFragmentDataListDTO> GetV1EditionsEditionIdTextFragments(uint editionId)

    {
        try
        {
                        return await _textService.GetFragmentDataAsync(await _userService.GetCurrentUserObjectAsync(editionId));              
        }
        catch (ApiException err)
        {
            throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
        }
    }


/// <summary>
        ///     Retrieves the ids of all lines in the given textFragmentName
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>An array of the line ids in the proper sequence</returns>
[AllowAnonymous]
public async Task<ArtefactDataListDTO> GetV1EditionsEditionIdTextFragmentsTextFragmentIdArtefacts(uint editionId, uint textFragmentId)

    {
        try
        {
                        return await _textService.GetArtefactsAsync(                await _userService.GetCurrentUserObjectAsync(editionId),                textFragmentId);              
        }
        catch (ApiException err)
        {
            throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
        }
    }


/// <summary>
        ///     Retrieves the ids of all lines in the given textFragmentName
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>An array of the line ids in the proper sequence</returns>
[AllowAnonymous]
public async Task<LineDataListDTO> GetV1EditionsEditionIdTextFragmentsTextFragmentIdLines(uint editionId, uint textFragmentId)

    {
        try
        {
                        return await _textService.GetLineIdsAsync(                await _userService.GetCurrentUserObjectAsync(editionId),                textFragmentId);              
        }
        catch (ApiException err)
        {
            throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
        }
    }


/// <summary>
        ///     Retrieves all signs and their data from the given textFragmentName
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>
        ///     A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
        ///     sequence
        /// </returns>
[AllowAnonymous]
public async Task<TextEditionDTO> GetV1EditionsEditionIdTextFragmentsTextFragmentId(uint editionId, uint textFragmentId)

    {
        try
        {
                        return await _textService.GetFragmentByIdAsync(                await _userService.GetCurrentUserObjectAsync(editionId),                textFragmentId);              
        }
        catch (ApiException err)
        {
            throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
        }
    }


/// <summary>
        ///     Retrieves all signs and their data from the given line
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="lineId">Id of the line</param>
        /// <returns>
        ///     A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
        ///     sequence
        /// </returns>
[AllowAnonymous]
public async Task<LineTextDTO> GetV1EditionsEditionIdLinesLineId(uint editionId, uint lineId)

    {
        try
        {
                        return await _textService.GetLineByIdAsync(await _userService.GetCurrentUserObjectAsync(editionId), lineId);              
        }
        catch (ApiException err)
        {
            throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
        }
    }


	}
}
