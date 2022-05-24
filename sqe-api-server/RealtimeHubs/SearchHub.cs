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
		///  Basic searching of the Qumranica database. Results are truncated
		///  to 100 results per search category.
		/// </summary>
		/// <param name="searchParameters">The parameters of the search</param>
		/// <returns></returns>
[AllowAnonymous]
public async Task<DetailedSearchResponseDTO> PostV1Search(DetailedSearchRequestDTO searchParameters)

    {
        try
        {
             return  await _searchService.PerformDetailedSearchAsync(_userService.GetCurrentUserId(), searchParameters);
        }
        catch (ApiException err)
        {
            throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
        }
    }


	}
}
