using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers
{
    [Authorize]
    [ApiController]
    public class ImagedObjectController : ControllerBase
    {
        private readonly IImagedObjectService _imagedObjectService;
        private readonly IImageService _imageService;
        private readonly IUserService _userService;

        public ImagedObjectController(IImagedObjectService imagedObjectService,
            IImageService imageService,
            IUserService userService)
        {
            _imagedObjectService = imagedObjectService;
            _imageService = imageService;
            _userService = userService;
        }

        /// <summary>
        ///     Provides information for the specified imaged object related to the specified edition, can include images and also
        ///     their masks with optional.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
        /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
        [AllowAnonymous]
        [HttpGet("v1/imaged-objects/{imagedObjectId}")]
        public async Task<ActionResult<SimpleImageListDTO>> GetImagedObject([FromRoute] string imagedObjectId)
        {
            return await _imagedObjectService.GetImagedObjectImagesAsync(imagedObjectId);
        }

        /// <summary>
        ///     Provides information for the specified imaged object related to the specified edition, can include images and also
        ///     their masks with optional.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
        /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
        [AllowAnonymous]
        [HttpGet("v1/editions/{editionId}/imaged-objects/{imagedObjectId}")]
        public async Task<ActionResult<ImagedObjectDTO>> GetEditionImagedObject([FromRoute] uint editionId,
            [FromRoute] string imagedObjectId,
            [FromQuery] List<string> optional)
        {
            return await _imagedObjectService.GetImagedObjectAsync(
                await _userService.GetCurrentUserObjectAsync(editionId),
                imagedObjectId,
                optional
            );
        }

        /// <summary>
        ///     Provides a listing of imaged objects related to the specified edition, can include images and also their masks with
        ///     optional.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
        [AllowAnonymous]
        [HttpGet("v1/editions/{editionId}/imaged-objects")]
        public async Task<ActionResult<ImagedObjectListDTO>> GetImagedObjects([FromRoute] uint editionId,
            [FromQuery] List<string> optional)
        {
            return await _imagedObjectService.GetEditionImagedObjectsAsync(
                await _userService.GetCurrentUserObjectAsync(editionId),
                optional
            );
        }

        /// <summary>
        ///     Provides a list of all institutional image providers.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("v1/imaged-objects/institutions")]
        public async Task<ActionResult<ImageInstitutionListDTO>> ListImageInstitutions()
        {
            return await _imageService.GetImageInstitutionsAsync();
        }

        /// <summary>
        ///     Provides a list of all institutional image providers.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("v1/imaged-objects/institutions/{institution}")]
        public async Task<ActionResult<InstitutionalImageListDTO>> ListInstitutionImages([FromRoute] string institution)
        {
            return await _imageService.GetInstitutionImagesAsync(institution);
        }

        /// <summary>
        /// Provides a list of all text fragments that should correspond to the imaged object.
        /// </summary>
        /// <param name="imagedObjectId">Id of the imaged object</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("v1/imaged-objects/{imagedObjectId}/text-fragments")]
        public async Task<ActionResult<List<ImagedObjectTextFragmentMatchDTO>>> ListImageTextFragments([FromRoute] string imagedObjectId)
        {
            return await _imageService.GetImageTextFragmentsAsync(imagedObjectId);
        }
    }
}