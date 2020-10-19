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
		private readonly IUserService    _userService;

		public EditionController(IEditionService editionService, IUserService userService)
		{
			_editionService = editionService;
			_userService = userService;
		}

		/// <summary>
		///  Adds an editor to the specified edition
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="payload">JSON object with the attributes of the new editor</param>
		[HttpPost("v1/[controller]s/{editionId}/add-editor-request")]
		public async Task<ActionResult> RequestAddEditionEditor(
				[FromRoute]  uint            editionId
				, [FromBody] InviteEditorDTO payload)
			=> await _editionService.RequestNewEditionEditor(
					await _userService.GetCurrentUserObjectAsync(editionId, admin: true)
					, payload);

		/// <summary>
		///  Get a list of requests issued by the current user for other users
		///  to become editors of a shared edition
		/// </summary>
		/// <returns></returns>
		[HttpGet("v1/[controller]s/admin-share-requests")]
		public async Task<ActionResult<AdminEditorRequestListDTO>> GetAdminShareRequests()
			=> await _editionService.GetAdminEditorRequests(_userService.GetCurrentUserId() ?? 0);

		/// <summary>
		///  Get a list of invitations issued to the current user to become an editor of a shared edition
		/// </summary>
		/// <returns></returns>
		[HttpGet("v1/[controller]s/editor-invitations")]
		public async Task<ActionResult<EditorInvitationListDTO>> GetEditorInvitations()
			=> await _editionService.GetUserEditorInvitations(_userService.GetCurrentUserId() ?? 0);

		/// <summary>
		///  Confirm addition of an editor to the specified edition
		/// </summary>
		/// <param name="token">JWT for verifying the request confirmation</param>
		[HttpPost("v1/[controller]s/confirm-editorship/{token}")]
		public async Task<ActionResult<DetailedEditorRightsDTO>>
				ConfirmAddEditionEditor([FromRoute] string token)
			=> await _editionService.AddEditionEditor(_userService.GetCurrentUserId(), token);

		/// <summary>
		///  Changes the rights for an editor of the specified edition
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="editorEmailId">Email address of the editor whose permissions are being changed</param>
		/// <param name="payload">JSON object with the attributes of the new editor</param>
		[HttpPut("v1/[controller]s/{editionId}/editors/{editorEmailId}")]
		public async Task<ActionResult<DetailedEditorRightsDTO>> AlterEditionEditor(
				[FromRoute]   uint                  editionId
				, [FromRoute] string                editorEmailId
				, [FromBody]  UpdateEditorRightsDTO payload)
			=> await _editionService.ChangeEditionEditorRights(
					await _userService.GetCurrentUserObjectAsync(editionId, admin: true)
					, editorEmailId
					, payload);

		/// <summary>
		///  Creates a copy of the specified edition
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
		[HttpPost("v1/[controller]s/{editionId}")]
		public async Task<ActionResult<EditionDTO>> CopyEdition(
				[FromRoute]  uint           editionId
				, [FromBody] EditionCopyDTO request) => await _editionService.CopyEditionAsync(
				await _userService.GetCurrentUserObjectAsync(editionId)
				, request);

		/// <summary>
		///  Provides details about the specified edition and all accessible alternate editions
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="optional">Optional parameters: 'deleteForAllEditors'</param>
		/// <param name="token">token required when using optional 'deleteForAllEditors'</param>
		[HttpDelete("v1/[controller]s/{editionId}")]
		public async Task<ActionResult<DeleteTokenDTO>> DeleteEdition(
				[FromRoute]   uint         editionId
				, [FromQuery] List<string> optional
				, [FromQuery] string       token) => await _editionService.DeleteEditionAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true)
				, token
				, optional);

		/// <summary>
		///  Provides details about the specified edition and all accessible alternate editions
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		[AllowAnonymous]
		[HttpGet("v1/[controller]s/{editionId}")]
		public async Task<ActionResult<EditionGroupDTO>> GetEdition([FromRoute] uint editionId)
			=> await _editionService.GetEditionAsync(
					await _userService.GetCurrentUserObjectAsync(editionId));

		/// <summary>
		///  Provides a listing of all editions accessible to the current user
		/// </summary>
		[AllowAnonymous]
		[HttpGet("v1/[controller]s")]
		public async Task<ActionResult<EditionListDTO>> ListEditions()
			=> await _editionService.ListEditionsAsync(_userService.GetCurrentUserId());

		/// <summary>
		///  Updates data for the specified edition
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="request">JSON object with the attributes to be updated</param>
		[HttpPut("v1/[controller]s/{editionId}")]
		public async Task<ActionResult<EditionDTO>> UpdateEdition(
				[FromRoute]  uint                    editionId
				, [FromBody] EditionUpdateRequestDTO request)
			=> await _editionService.UpdateEditionAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, request);

		/// <summary>
		///  Provides spatial data for all letters in the edition
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <returns></returns>
		[HttpGet("v1/[controller]s/{editionId}/script-collection")]
		[AllowAnonymous]
		public async Task<ActionResult<EditionScriptCollectionDTO>>
				GetEditionScriptCollection([FromRoute] uint editionId)
			=> await _editionService.GetEditionScriptCollection(
					await _userService.GetCurrentUserObjectAsync(editionId));

		/// <summary>
		///  Provides spatial data for all letters in the edition organized and oriented
		///  by lines.
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <returns></returns>
		[HttpGet("v1/[controller]s/{editionId}/script-lines")]
		[AllowAnonymous]
		public async Task<ActionResult<EditionScriptLinesDTO>>
				GetEditionScriptLines([FromRoute] uint editionId)
			=> await _editionService.GetEditionScriptLines(
					await _userService.GetCurrentUserObjectAsync(editionId));
	}
}
