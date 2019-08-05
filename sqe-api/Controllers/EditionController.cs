using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeApi.Server.DTOs;
using SQE.SqeApi.Server.Services;

namespace SQE.SqeApi.Server.Controllers
{
    [Authorize]
    [ApiController]
    public class EditionController : ControllerBase
    {
        private readonly IEditionService _editionService;
        private readonly IUserService _userService;

        public EditionController(IEditionService editionService, IUserService userService)
        {
            _editionService = editionService;
            _userService = userService;
        }

        /// <summary>
        /// Adds an editor to the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        [HttpPost("v1/[controller]s/{editionId}/editors")]
        public async Task<ActionResult<EditorRightsDTO>> AddEditionEditor([FromRoute] uint editionId,
            [FromBody] EditorRightsDTO payload)
        {
            return await _editionService.AddEditionEditor(
                _userService.GetCurrentUserObject(editionId),
                payload);
        }

        /// <summary>
        /// Changes the rights for an editor of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        [HttpPut("v1/[controller]s/{editionId}/editors")]
        public async Task<ActionResult<EditorRightsDTO>> AlterEditionEditorRights([FromRoute] uint editionId,
            [FromBody] EditorRightsDTO payload)
        {
            return await _editionService.ChangeEditionEditorRights(
                _userService.GetCurrentUserObject(editionId),
                payload);
        }

        /// <summary>
        /// Creates a copy of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
        [HttpPost("v1/[controller]s/{editionId}")]
        public async Task<ActionResult<EditionDTO>> CopyEdition([FromRoute] uint editionId,
            [FromBody] EditionCopyDTO request)
        {
            return await _editionService.CopyEditionAsync(
                _userService.GetCurrentUserObject(editionId),
                request);
        }

        /// <summary>
        /// Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Optional parameters: 'deleteForAllEditors'</param>
        /// <param name="token">token required when using optional 'deleteForAllEditors'</param>
        [HttpDelete("v1/[controller]s/{editionId}")]
        public async Task<ActionResult<DeleteTokenDTO>> DeleteEdition([FromRoute] uint editionId,
            [FromQuery] List<string> optional, [FromQuery] string token)
        {
            return await _editionService.DeleteEditionAsync(
                _userService.GetCurrentUserObject(editionId),
                token,
                optional);
        }

        /// <summary>
        /// Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [AllowAnonymous]
        [HttpGet("v1/[controller]s/{editionId}")]
        public async Task<ActionResult<EditionGroupDTO>> GetEditionInfo([FromRoute] uint editionId)
        {
            return await _editionService.GetEditionAsync(_userService.GetCurrentUserObject(editionId));
        }

        /// <summary>
        /// Provides a listing of all editions accessible to the current user
        /// </summary>
        [AllowAnonymous]
        [HttpGet("v1/[controller]s")]
        public async Task<ActionResult<EditionListDTO>> ListEditions()
        {
            return await _editionService.ListEditionsAsync(_userService.GetCurrentUserId());
        }

        /// <summary>
        /// Updates data for the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be updated</param>
        [HttpPut("v1/[controller]s/{editionId}")]
        public async Task<ActionResult<EditionDTO>> UpdateEdition([FromRoute] uint editionId,
            [FromBody] EditionUpdateRequestDTO request)
        {
            return await _editionService.UpdateEditionAsync(
                _userService.GetCurrentUserObject(editionId),
                request.name,
                request.copyrightHolder,
                request.collaborators);
        }
    }
}