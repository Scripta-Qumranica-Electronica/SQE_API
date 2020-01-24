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
        ///     Adds an editor to the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        [HttpPost("v1/[controller]s/{editionId}/editors")]
        public async Task<ActionResult<CreateEditorRightsDTO>> AddEditionEditor([FromRoute] uint editionId,
            [FromBody] CreateEditorRightsDTO payload)
        {
            return await _editionService.AddEditionEditor(
                await _userService.GetCurrentUserObjectAsync(editionId, admin: true),
                payload
            );
        }

        /// <summary>
        ///     Changes the rights for an editor of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="editorEmailId">Email address of the editor whose permissions are being changed</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        [HttpPut("v1/[controller]s/{editionId}/editors/{editorEmailId}")]
        public async Task<ActionResult<CreateEditorRightsDTO>> AlterEditionEditor([FromRoute] uint editionId,
            [FromRoute] string editorEmailId,
            [FromBody] UpdateEditorRightsDTO payload)
        {
            return await _editionService.ChangeEditionEditorRights(
                await _userService.GetCurrentUserObjectAsync(editionId, admin: true),
                editorEmailId,
                payload
            );
        }

        /// <summary>
        ///     Creates a copy of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
        [HttpPost("v1/[controller]s/{editionId}")]
        public async Task<ActionResult<EditionDTO>> CopyEdition([FromRoute] uint editionId,
            [FromBody] EditionCopyDTO request)
        {
            return await _editionService.CopyEditionAsync(
                await _userService.GetCurrentUserObjectAsync(editionId),
                request
            );
        }

        /// <summary>
        ///     Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Optional parameters: 'deleteForAllEditors'</param>
        /// <param name="token">token required when using optional 'deleteForAllEditors'</param>
        [HttpDelete("v1/[controller]s/{editionId}")]
        public async Task<ActionResult<DeleteTokenDTO>> DeleteEdition([FromRoute] uint editionId,
            [FromQuery] List<string> optional,
            [FromQuery] string token)
        {
            return await _editionService.DeleteEditionAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                token,
                optional
            );
        }

        /// <summary>
        ///     Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [AllowAnonymous]
        [HttpGet("v1/[controller]s/{editionId}")]
        public async Task<ActionResult<EditionGroupDTO>> GetEdition([FromRoute] uint editionId)
        {
            return await _editionService.GetEditionAsync(await _userService.GetCurrentUserObjectAsync(editionId));
        }

        /// <summary>
        ///     Provides a listing of all editions accessible to the current user
        /// </summary>
        [AllowAnonymous]
        [HttpGet("v1/[controller]s")]
        public async Task<ActionResult<EditionListDTO>> ListEditions()
        {
            return await _editionService.ListEditionsAsync(_userService.GetCurrentUserId());
        }

        /// <summary>
        ///     Updates data for the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be updated</param>
        [HttpPut("v1/[controller]s/{editionId}")]
        public async Task<ActionResult<EditionDTO>> UpdateEdition([FromRoute] uint editionId,
            [FromBody] EditionUpdateRequestDTO request)
        {
            return await _editionService.UpdateEditionAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                request
            );
        }
    }
}