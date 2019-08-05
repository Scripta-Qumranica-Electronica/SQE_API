using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Helpers;

namespace SQE.SqeHttpApi.Server.Controllers
{

    [Authorize]
    [Route("v1/[controller]s")]
    [ApiController]
    public class EditionController : ControllerBase
    {
        private readonly IEditionService _editionService;
        private readonly IUserService _userService;

        public EditionController(
            IEditionService editionService, 
            IUserService userService)
        {
            _editionService = editionService;
            _userService = userService;
        }

        /// <summary>
        /// Provides a listing of all editions accessible to the current user
        /// </summary>
        [AllowAnonymous]
        [HttpGet("")]
        [ProducesResponseType(200)]
        public async Task<ActionResult<EditionListDTO>> ListEditions()
        {
            return await _editionService.ListEditionsAsync(_userService.GetCurrentUserId());
        }

        /// <summary>
        /// Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [AllowAnonymous]
        [HttpGet("{editionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EditionGroupDTO>> GetEdition([FromRoute] uint editionId)
        {
            return await _editionService.GetEditionAsync(_userService.GetCurrentUserObject(editionId));
        }

        /// <summary>
        /// Updates data for the specified edition
        /// </summary>
        /// <param name="request">JSON object with the attributes to be updated</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPut("{editionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EditionDTO>> UpdateEdition([FromBody] EditionUpdateRequestDTO request, [FromRoute] uint editionId)
        {
            return await _editionService.UpdateEditionAsync(
                _userService.GetCurrentUserObject(editionId), 
                request.name,
                request.copyrightHolder,
                request.collaborators);
        }

        /// <summary>
        /// Creates a copy of the specified edition
        /// </summary>
        /// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPost("{editionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EditionDTO>> CopyEdition([FromBody] EditionCopyDTO request, [FromRoute] uint editionId)
        {
            return await _editionService.CopyEditionAsync(_userService.GetCurrentUserObject(editionId), request);
        }

        /// <summary>
        /// Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Optional parameters: "deleteForAllEditors"</param>
        /// <param name="token">token required when using optional "deleteForAllEditors"</param>
        [HttpDelete("{editionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<DeleteTokenDTO>> DeleteEdition([FromRoute] uint editionId, 
            [FromQuery] string token, [FromQuery] List<string> optional)
        {
            return await _editionService.DeleteEditionAsync(_userService.GetCurrentUserObject(editionId), token, optional);
        }
        
        /// <summary>
        /// Adds an editor to the specified edition
        /// </summary>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPost("{editionId}/editors")]
        public async Task<ActionResult<EditorRightsDTO>> AddEditionEditor([FromBody] EditorRightsDTO payload, [FromRoute] uint editionId)
        {
            return await _editionService.AddEditionEditor(_userService.GetCurrentUserObject(editionId), payload);
        }
        
        /// <summary>
        /// Changes the rights for an editor of the specified edition
        /// </summary>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPut("{editionId}/editors")]
        public async Task<ActionResult<EditorRightsDTO>> AlterEditionEditor([FromBody] EditorRightsDTO payload, [FromRoute] uint editionId)
        {
            return await _editionService.ChangeEditionEditorRights(_userService.GetCurrentUserObject(editionId), payload);
        }
    }
}
