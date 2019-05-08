using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.Services;
using SQE.SqeHttpApi.Server.DTOs;



namespace backend.Controllers
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
            if (optionals != null)
            {
                artefacts = optionals.Contains("artefacts");
                masks = artefacts && optionals.Contains("masks");
            }
            else
            {
                artefacts = masks = false;
            }
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

            // Bronson: Please stop using Ok.
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
        public async Task<ActionResult<ImagedObjectListDTO>> GetImagedObjectsWithArtefacts([FromRoute] uint editionId, [FromQuery] List<string> optional = null)
        {
            bool artefacts, masks;
            ParseOptionals(optional, out artefacts, out masks);

            try
            {
                // Bronson: We have discussed not using the Ok function, it loses all type safety.
                // Also, the following expression is just too long and unclear, I'll break it into a normal if.
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
        // Bronson: Add support for optionals here, as well
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/imaged-objects/{imagedObjectId}")]
        public async Task<ActionResult<ImagedObjectDTO>> GetImagedObject([FromRoute] uint editionId, string imagedObjectId, [FromQuery] List<string> optional)
        {
            bool artefacts, masks;
            ParseOptionals(optional, out artefacts, out masks);

            try
            {
                // Bronson: Add support for artefacts and masks here
                return await _imagedObjectService.GetImagedObjectAsync(_userService.GetCurrentUserId(), editionId, imagedObjectId);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
    }
}