using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.Services;
using SQE.SqeHttpApi.Server.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers
{
    [Authorize]
    [Route("v1")]
    [ApiController]
    public class ArtefactController : ControllerBase
    {
        private IUserService _userService;
        private IArtefactService _artefactService;

        public ArtefactController(IUserService userService, IArtefactService artefactService)
        {
            this._artefactService = artefactService;
            this._userService = userService;
        }
        
        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        /// <param Name="images">Defines whether image references should be included in the response</param>
        [AllowAnonymous]
        [HttpGet("edition/{editionId}/artefact/list")]
        public async Task<ActionResult<ArtefactListDTO>> GetArtefacts([FromRoute] uint editionId, [FromQuery] string images = "false")
        {
            try
            {
                return images.Equals("true", StringComparison.InvariantCultureIgnoreCase) 
                    ? Ok(await _artefactService.GetEditionArtefactListingsWithImagesAsync(_userService.GetCurrentUserId(), editionId)) 
                    : Ok(await _artefactService.GetEditionArtefactListingsAsync(_userService.GetCurrentUserId(), editionId));
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
        
        /// <summary>
        /// Updates the specified artefact
        /// </summary>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        [HttpPut("edition/{editionId}/artefact/{artefactId}")]
        public async Task<ActionResult<ArtefactListDTO>> UpdateArtefact(
            [FromRoute] uint editionId, 
            [FromRoute] uint artefactId, 
            [FromBody] UpdateArtefactDTO payload
            )
        {
            try
            {
                return Ok(await _artefactService.UpdateArtefact(
                    _userService.GetCurrentUserObject(), 
                    editionId, 
                    artefactId, 
                    payload.mask, 
                    payload.name, 
                    payload.position));
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
        
        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition, including data about
        /// the related images
        /// </summary>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        [HttpPost("edition/{editionId}/artefact")]
        public async Task<ActionResult<ArtefactListDTO>> CreateArtefact(
            [FromRoute] uint editionId, 
            [FromRoute] uint artefactId, 
            [FromBody] CreateArtefactDTO payload
        )
        {
            try
            {
                return Ok(await _artefactService.CreateArtefact(
                    _userService.GetCurrentUserObject(), 
                    editionId, 
                    payload.masterImageId, 
                    payload.mask, 
                    payload.name, 
                    payload.position));
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        public class UpdateArtefactMask
        {
            public string mask { get; set; }
        }

        public class UpdateArtefactDTO
        {
            public string mask { get; set; }
            public string name { get; set; }
            public string position { get; set; }
        }
        public class CreateArtefactDTO
        {
            public uint masterImageId { get; set; }
            public string mask { get; set; }
            public string name { get; set; }
            public string position { get; set; }
        }
    }
}