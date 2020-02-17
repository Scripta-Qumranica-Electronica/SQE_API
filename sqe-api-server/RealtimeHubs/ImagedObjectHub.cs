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
    public partial class MainHub
    {
/// <summary>
        ///     Provides information for the specified imaged object related to the specified edition, can include images and also
        ///     their masks with optional.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
        /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
[AllowAnonymous]
public async Task<ImagedObjectDTO> GetV1EditionsEditionIdImagedObjectsImagedObjectId(uint editionId, string imagedObjectId, List<string> optional)
{
           return await _imagedObjectService.GetImagedObjectAsync(                await _userService.GetCurrentUserObjectAsync(editionId),                imagedObjectId,                optional);       
}

/// <summary>
        ///     Provides a listing of imaged objects related to the specified edition, can include images and also their masks with
        ///     optional.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
[AllowAnonymous]
public async Task<ImagedObjectListDTO> GetV1EditionsEditionIdImagedObjects(uint editionId, List<string> optional)
{
           return await _imagedObjectService.GetImagedObjectsAsync(                await _userService.GetCurrentUserObjectAsync(editionId),                optional);       
}

/// <summary>
        ///     Provides a list of all institutional image providers.
        /// </summary>
[AllowAnonymous]
public async Task<ImageInstitutionListDTO> GetV1ImagedObjectsInstitutions()
{
           return await _imageService.GetImageInstitutionsAsync();       
}

	}
}
