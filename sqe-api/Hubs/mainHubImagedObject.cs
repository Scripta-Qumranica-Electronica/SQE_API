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
        /// Provides information for the specified imaged object related to the specified edition, can include images and also their masks with optional.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
        /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
        [AllowAnonymous]
        public async Task<ImagedObjectDTO> GetV1EditionsEditionIdImagedObjectsImagedObjectId(uint editionId,
            string imagedObjectId, List<string> optional)
        {
            return await _imagedObjectService.GetImagedObjectAsync(
                _userService.GetCurrentUserId(),
                editionId,
                imagedObjectId,
                optional);
        }

        /// <summary>
        /// Provides a listing of imaged objects related to the specified edition, can include images and also their masks with optional.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
        [AllowAnonymous]
        public async Task<ImagedObjectListDTO> GetV1EditionsEditionIdImagedObjects(uint editionId,
            List<string> optional)
        {
            return await _imagedObjectService.GetImagedObjectsAsync(
                _userService.GetCurrentUserId(),
                editionId,
                optional);
        }

        /// <summary>
        /// Provides a list of all institutional image providers.
        /// </summary>
        [AllowAnonymous]
        public async Task<ImageInstitutionListDTO> GetV1ImagedObjectsInstitutions()
        {
            return await _imageService.GetImageInstitutionsAsync();
        }
    }
}