using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeApi.Server.DTOs;
using SQE.SqeApi.Server.Services;

namespace SQE.SqeApi.Server.Controllers
{
    public enum Optional {images, masks};
    
    [Authorize]
    [Route("v1")]
    [ApiController]
    public class ArtefactController : ControllerBase
    {
        private IUserService _userService;
        private IArtefactService _artefactService;

        public ArtefactController(IUserService userService, IArtefactService artefactService)
        {
            _artefactService = artefactService;
            _userService = userService;
        }
        
        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/artefacts")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ArtefactListDTO>> GetArtefacts([FromRoute] uint editionId, [FromQuery] List<string> optional)
        {
            return await _artefactService.GetEditionArtefactListingsAsync(_userService.GetCurrentUserId(), editionId, optional);
        }
        
        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="optional">Add "masks" to include artefact polygons</param>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/artefacts/{artefactId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ArtefactDTO>> GetArtefact([FromRoute] uint editionId, [FromRoute] uint artefactId, [FromQuery] List<string> optional)
        {
            return await _artefactService.GetEditionArtefactAsync(_userService.GetCurrentUserObject(editionId), artefactId, optional);
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
            return await _artefactService.UpdateArtefactAsync(
                _userService.GetCurrentUserObject(editionId),
                editionId,
                artefactId,
                payload.mask,
                payload.name,
                payload.position);
        }
        
        /// <summary>
        /// Deletes the specified artefact
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        [HttpDelete("editions/{editionId}/artefacts/{artefactId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteArtefact(
            [FromRoute] uint editionId, 
            [FromRoute] uint artefactId
        )
        {
            return await _artefactService.DeleteArtefactAsync(_userService.GetCurrentUserObject(editionId), artefactId);
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
            return await _artefactService.CreateArtefactAsync(
                _userService.GetCurrentUserObject(editionId), 
                editionId, 
                payload.masterImageId, 
                payload.mask, 
                payload.name, 
                payload.position);
        }
    }
}