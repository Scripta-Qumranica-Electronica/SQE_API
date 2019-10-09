/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-http-server` project. Changes made
 * there will automatically be incorporated here the next time the 
 * `sqe-realtime-hub-builder` is run.
 */

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;

namespace SQE.API.Realtime.Hubs
{
	public partial class MainHub : Hub
	{
		/// <summary>
		///     Get the details for a ROI in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="roiId">A JSON object with the new ROI to be created</param>
		[AllowAnonymous]
		public async Task<InterpretationRoiDTO> GetV1EditionsEditionIdRoisRoiId(uint editionId, uint roiId)
		{
			return await _roiService.GetRoiAsync(await _userService.GetCurrentUserObjectAsync(editionId), roiId);
		}


		/// <summary>
		///     Creates new sign ROI in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="newRoi">A JSON object with the new ROI to be created</param>
		[Authorize]
		public async Task<InterpretationRoiDTO> PostV1EditionsEditionIdRois(uint editionId,
			SetInterpretationRoiDTO newRoi)
		{
			return await _roiService.CreateRoiAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				newRoi,
				Context.ConnectionId
			);
		}


		/// <summary>
		///     Creates new sign ROI's in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="newRois">A JSON object with an array of the new ROI's to be created</param>
		[Authorize]
		public async Task<InterpretationRoiDTOList> PostV1EditionsEditionIdRoisBatch(uint editionId,
			SetInterpretationRoiDTOList newRois)
		{
			return await _roiService.CreateRoisAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				newRois,
				Context.ConnectionId
			);
		}


		/// <summary>
		///     Update an existing sign ROI in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="roiId">Id of the ROI to be updated</param>
		/// <param name="updateRoi">A JSON object with the updated ROI details</param>
		[Authorize]
		public async Task<UpdatedInterpretationRoiDTO> PutV1EditionsEditionIdRoisRoiId(uint editionId,
			uint roiId,
			SetInterpretationRoiDTO updateRoi)
		{
			return await _roiService.UpdateRoiAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				roiId,
				updateRoi,
				Context.ConnectionId
			);
		}


		/// <summary>
		///     Update existing sign ROI's in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="updateRois">A JSON object with an array of the updated ROI details</param>
		[Authorize]
		public async Task<UpdatedInterpretationRoiDTOList> PutV1EditionsEditionIdRoisBatch(uint editionId,
			InterpretationRoiDTOList updateRois)
		{
			return await _roiService.UpdateRoisAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				updateRois,
				Context.ConnectionId
			);
		}


		/// <summary>
		///     Deletes a sign ROI from the given edition of a scroll
		/// </summary>
		/// <param name="roiId">Id of the ROI to be deleted</param>
		/// <param name="editionId">Id of the edition</param>
		[Authorize]
		public async Task DeleteV1EditionsEditionIdRoisRoiId(uint editionId, uint roiId)
		{
			await _roiService.DeleteRoiAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				roiId,
				Context.ConnectionId
			);
		}
	}
}