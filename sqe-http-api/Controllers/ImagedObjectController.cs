using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Services;

namespace SQE.SqeHttpApi.Server.Controllers
{
    [Authorize]
    [Route("v1")]
    [ApiController]
    public class ImagedObjectController : ControllerBase
    {

        private readonly IUserService _userService;
        private readonly IImagedObjectService _imagedObjectService;
        private readonly IImageService _imageService;

        public ImagedObjectController(IUserService userService, IImagedObjectService imagedObjectService, IImageService imageService)
        {
            this._imagedObjectService = imagedObjectService;
            this._userService = userService;
            this._imageService = imageService;
        }

        private void ParseOptionals(List<string> optionals, out bool artefacts, out bool masks)
        {
            artefacts = masks = false;
            if (optionals == null) 
                return;
            artefacts = optionals.Contains("artefacts");
            if (!optionals.Contains("masks")) 
                return;
            masks = true;
            artefacts = true;
        }

        /// <summary>
        /// Provides a list of all imaged objects in the system
        /// </summary>
        /// <returns></returns>
        // Bronson: This endpoint is not documented
        [AllowAnonymous]
        [HttpGet("imaged-objects/list")]
        public async Task<ActionResult<ImageGroupListDTO>> ListImageGroups()
        {
            var images = await _imageService.GetImageAsync(_userService.GetCurrentUserId(), new List<uint>());
            return images;
        }

        /// <summary>
        /// Provides detailed information about a specific imaged object reference
        /// </summary>
        /// <param Name="imageReferenceId"></param>
        /// <returns></returns>
        // Bronson: This endpoint is not documented
        [AllowAnonymous]
        [HttpGet("imaged-objects/{imageReferenceId}")]
        public async Task<ActionResult<ImageGroupListDTO>> ListImageGroupsOfScroll([FromRoute] uint imageReferenceId)
        {
            var images = await _imageService.GetImageAsync(_userService.GetCurrentUserId(), new List<uint>(new uint[] {imageReferenceId }));
            return images;
        }

        /// <summary>
        /// Provides a list of all institutional image providers.
        /// </summary>
        // Bronson: This endpoint is not documented
        [AllowAnonymous]
        [HttpGet("imaged-objects/institutions/list")]
        public async Task<ActionResult<ImageInstitutionListDTO>> ListImageInstitutions()
        {
            var institutions = await _imageService.GetImageInstitutionsAsync();

            return institutions;
        }

        /// <summary>
        /// Provides a listing of imaged objects related to the specified edition, including artefacts.
        /// </summary>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        /// <param Name="artefacts">Set this to true to receive artefact data</param>
        /// <param Name="withMask">Set this to true to receive the mask polygon data</param>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/imaged-objects")]
        public async Task<ActionResult<ImagedObjectListDTO>> GetImagedObjectsWithArtefacts([FromRoute] uint editionId, [FromQuery] List<string> optional)
        {
            bool artefacts, masks;
            ParseOptionals(optional, out artefacts, out masks);
            ParseOptionals(optional, out artefacts, out masks);

            try
            {
                if(artefacts)
                {
                    return await _imagedObjectService.GetImagedObjectsWithArtefactsAsync(_userService.GetCurrentUserId(), editionId, masks);
                }
                else
                {
                    return await _imagedObjectService.GetImagedObjectsAsync(_userService.GetCurrentUserId(), editionId);
                }
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
        
        /// <summary>
        /// Provides a listing of imaged objects related to the specified edition
        /// </summary>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        /// <param Name="imagedObjectId">Unique Id of the desired object from the imaging institution</param>
        /// <param Name="optional">Optional data includes "artefacts" for a list of all artefacts in the object image
        /// and "masks" to retrieve the polygon masks along with the artefacts.</param>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/imaged-objects/{imagedObjectId}")]
        public async Task<ActionResult<ImagedObjectDTO>> GetImagedObject([FromRoute] uint editionId, string imagedObjectId, [FromQuery] List<string> optional)
        {
            ParseOptionals(optional, out var artefacts, out var masks);

            try
            {
                // Bronson: Add support for artefacts and masks here
                return await _imagedObjectService.GetImagedObjectAsync(_userService.GetCurrentUserId(), editionId, imagedObjectId, artefacts, masks);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
    }
}