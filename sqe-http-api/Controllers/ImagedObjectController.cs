﻿using System;
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
        
        /// <summary>
        /// Provides a list of all imaged objects in the system
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("imaged-object/list")]
        public async Task<ActionResult<ImageGroupListDTO>> ListImageGroups()
        {
            var images = await _imageService.GetImageAsync(_userService.GetCurrentUserId(), new List<uint>());
            return Ok(images);
        }

        /// <summary>
        /// Provides detailed information about a specific imaged object reference
        /// </summary>
        /// <param Name="imageReferenceId"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("imaged-object/{imageReferenceId}")]
        public async Task<ActionResult<ImageGroupListDTO>> ListImageGroupsOfScroll([FromRoute] uint imageReferenceId)
        {
            var images = await _imageService.GetImageAsync(_userService.GetCurrentUserId(), new List<uint>(new uint[] {imageReferenceId }));
            return Ok(images);
        }

        /// <summary>
        /// Provides a list of all institutional image providers.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("imaged-object/institution/list")]
        public async Task<ActionResult<ImageInstitutionListDTO>> ListImageInstitutions()
        {
            var institutions = await _imageService.GetImageInstitutionsAsync();
            return Ok(institutions);
        }

        /// <summary>
        /// Provides a listing of imaged objects related to the specified edition, including artefacts.
        /// </summary>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        /// <param Name="artefacts">Set this to true to receive artefact data</param>
        /// <param Name="withMask">Set this to true to receive the mask polygon data</param>
        [AllowAnonymous]
        [HttpGet("edition/{editionId}/imaged-object/list")]
        public async Task<ActionResult<ImagedObjectListDTO>> GetImagedObjectsWithArtefacts([FromRoute] uint editionId, [FromQuery] string artefacts = "false", [FromQuery] string withMask = "false")
        {
            try
            {
                return Ok( 
                    artefacts.Equals("true", StringComparison.InvariantCultureIgnoreCase)
                        ? await _imagedObjectService.GetImagedObjectsWithArtefactsAsync(
                            _userService.GetCurrentUserId(), 
                            editionId, 
                            withMask.Equals("true", StringComparison.InvariantCultureIgnoreCase)
                        )
                        : await _imagedObjectService.GetImagedObjectsAsync(_userService.GetCurrentUserId(), editionId)
                );
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
        [AllowAnonymous]
        [HttpGet("edition/{editionId}/imaged-object/{imagedObjectId}")]
        public async Task<ActionResult<ImagedObjectDTO>> GetImagedObject([FromRoute] uint editionId, string imagedObjectId)
        {
            try
            {
                return Ok(await _imagedObjectService.GetImagedObjectAsync(_userService.GetCurrentUserId(), editionId, imagedObjectId));
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
    }
}