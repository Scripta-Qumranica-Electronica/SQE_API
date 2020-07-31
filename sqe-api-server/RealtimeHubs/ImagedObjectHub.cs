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
        ///     Provides information for the specified imaged object.
        /// </summary>
        /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
        [AllowAnonymous]
        public async Task<SimpleImageListDTO> GetV1ImagedObjectsImagedObjectId(string imagedObjectId)

        {
            try
            {
                return await _imagedObjectService.GetImagedObjectImagesAsync(imagedObjectId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


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
            try
            {
                return await _imagedObjectService.GetImagedObjectAsync(await _userService.GetCurrentUserObjectAsync(editionId), imagedObjectId, optional);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
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
            try
            {
                return await _imagedObjectService.GetEditionImagedObjectsAsync(await _userService.GetCurrentUserObjectAsync(editionId), optional);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides a list of all institutional image providers.
        /// </summary>
        [AllowAnonymous]
        public async Task<ImageInstitutionListDTO> GetV1ImagedObjectsInstitutions()

        {
            try
            {
                return await _imageService.GetImageInstitutionsAsync();
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides a list of all institutional image providers.
        /// </summary>
        [AllowAnonymous]
        public async Task<InstitutionalImageListDTO> GetV1ImagedObjectsInstitutionsInstitution(string institution)

        {
            try
            {
                return await _imageService.GetInstitutionImagesAsync(institution);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides a list of all text fragments that should correspond to the imaged object.
        /// </summary>
        /// <param name="imagedObjectId">Id of the imaged object</param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<List<ImagedObjectTextFragmentMatchDTO>> GetV1ImagedObjectsImagedObjectIdTextFragments(string imagedObjectId)

        {
            try
            {
                return await _imageService.GetImageTextFragmentsAsync(imagedObjectId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


    }
}
