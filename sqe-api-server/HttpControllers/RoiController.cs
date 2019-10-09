using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers
{
	[Authorize]
	[ApiController]
	public class RoiController : ControllerBase
	{
		private readonly IRoiService _roiService;
		private readonly IUserService _userService;

		public RoiController(IRoiService roiService, IUserService userService)
		{
			_roiService = roiService;
			_userService = userService;
		}

		/// <summary>
		///     Get the details for a ROI in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="roiId">A JSON object with the new ROI to be created</param>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/rois/{roiId}")]
		public async Task<ActionResult<InterpretationRoiDTO>> GetRoi(
			[FromRoute] uint editionId,
			[FromRoute] uint roiId)
		{
			return await _roiService.GetRoiAsync(await _userService.GetCurrentUserObjectAsync(editionId), roiId);
		}

		/// <summary>
		///     Creates new sign ROI in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="newRoi">A JSON object with the new ROI to be created</param>
		[HttpPost("v1/editions/{editionId}/rois")]
		public async Task<ActionResult<InterpretationRoiDTO>> CreateRoi(
			[FromRoute] uint editionId,
			[FromBody] SetInterpretationRoiDTO newRoi)
		{
			return await _roiService.CreateRoiAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				newRoi
			);
		}

		/// <summary>
		///     Creates new sign ROI's in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="newRois">A JSON object with an array of the new ROI's to be created</param>
		[HttpPost("v1/editions/{editionId}/rois/batch")]
		public async Task<ActionResult<InterpretationRoiDTOList>> CreateRois(
			[FromRoute] uint editionId,
			[FromBody] SetInterpretationRoiDTOList newRois)
		{
			return await _roiService.CreateRoisAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				newRois
			);
		}

		/// <summary>
		///     Update an existing sign ROI in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="roiId">Id of the ROI to be updated</param>
		/// <param name="updateRoi">A JSON object with the updated ROI details</param>
		[HttpPut("v1/editions/{editionId}/rois/{roiId}")]
		public async Task<ActionResult<UpdatedInterpretationRoiDTO>> UpdateRoi(
			[FromRoute] uint editionId,
			[FromRoute] uint roiId,
			[FromBody] SetInterpretationRoiDTO updateRoi)
		{
			return await _roiService.UpdateRoiAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				roiId,
				updateRoi
			);
		}

		/// <summary>
		///     Update existing sign ROI's in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="updateRois">A JSON object with an array of the updated ROI details</param>
		[HttpPut("v1/editions/{editionId}/rois/batch")]
		public async Task<ActionResult<UpdatedInterpretationRoiDTOList>> UpdateRois(
			[FromRoute] uint editionId,
			[FromBody] InterpretationRoiDTOList updateRois)
		{
			return await _roiService.UpdateRoisAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				updateRois
			);
		}

		/// <summary>
		///     Deletes a sign ROI from the given edition of a scroll
		/// </summary>
		/// <param name="roiId">Id of the ROI to be deleted</param>
		/// <param name="editionId">Id of the edition</param>
		[HttpDelete("v1/editions/{editionId}/rois/{roiId}")]
		public async Task<ActionResult> DeleteRoi(
			[FromRoute] uint editionId,
			[FromRoute] uint roiId)
		{
			return await _roiService.DeleteRoiAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				roiId
			);
		}
	}
}