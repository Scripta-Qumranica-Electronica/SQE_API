using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Helpers;

namespace SQE.SqeHttpApi.Server.Controllers
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
        
        private void ParseOptionals(List<string> optionals, out bool images, out bool masks)
        {
            images = masks = false;
            if (optionals == null) 
                return;
            images = optionals.Contains("images");
            masks = optionals.Contains("masks");
        }
        
        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="simplify">Request artefact masks to be simplified by the desired max-distance.
        /// This only works when "optional" includes "masks".  0 or non-negative values are ignored.</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/artefacts")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ArtefactListDTO>> GetArtefacts([FromRoute] uint editionId, 
            [FromQuery] List<string> optional, [FromQuery] double simplify = 0)
        {
            ParseOptionals(optional, out var images, out var masks);
            try
            {
                return await _artefactService.GetEditionArtefactListingsAsync(_userService.GetCurrentUserId(), editionId, masks, images, simplify);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
        
        /// <summary>
        /// Provides a information for the specified artefact in the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="optional">Add "masks" to include artefact polygons</param>
        /// <param name="simplify">Request artefact masks to be simplified by the desired max-distance.
        /// This only works when "optional" includes "masks".  0 or non-negative values are ignored.</param>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/artefacts/{artefactId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ArtefactDTO>> GetArtefact([FromRoute] uint editionId, [FromRoute] uint artefactId, 
            [FromQuery] List<string> optional, [FromQuery] double simplify = 0)
        {
            ParseOptionals(optional, out var images, out var masks);
            try
            {
                return await _artefactService.GetEditionArtefactAsync(_userService.GetCurrentUserObject(editionId), artefactId, masks, simplify);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
        
        /// <summary>
        /// Updates the specified artefact
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="payload">An UpdateArtefactDTO with the desired alterations to the artefact</param>
        [HttpPut("editions/{editionId}/artefacts/{artefactId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ArtefactDTO>> UpdateArtefact(
            [FromRoute] uint editionId, 
            [FromRoute] uint artefactId, 
            [FromBody] UpdateArtefactDTO payload
            )
        {
            try
            {
                return await _artefactService.UpdateArtefact(
                    _userService.GetCurrentUserObject(editionId), 
                    editionId, 
                    artefactId, 
                    payload.mask, 
                    payload.name, 
                    payload.position);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
        
        /// <summary>
        /// Creates a new artefact with the provided data.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">A CreateArtefactDTO with the data for the new artefact</param>
        [HttpPost("editions/{editionId}/artefacts")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ArtefactDTO>> CreateArtefact(
            [FromRoute] uint editionId,
            [FromBody] CreateArtefactDTO payload
        )
        {
            try
            {
                return await _artefactService.CreateArtefact(
                    _userService.GetCurrentUserObject(editionId), 
                    editionId, 
                    payload.masterImageId, 
                    payload.mask, 
                    payload.name, 
                    payload.position);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
    }
}