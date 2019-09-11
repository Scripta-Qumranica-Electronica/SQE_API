using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Services;

namespace SQE.SqeApi.Server.Controllers
{
	[Authorize]
	[ApiController]
	public class ArtefactController : ControllerBase
	{
		private readonly IArtefactService _artefactService;
		private readonly IUserService _userService;

		public ArtefactController(IArtefactService artefactService, IUserService userService)
		{
			_artefactService = artefactService;
			_userService = userService;
		}

		/// <summary>
		///     Creates a new artefact with the provided data.
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="payload">A CreateArtefactDTO with the data for the new artefact</param>
		[HttpPost("v1/editions/{editionId}/[controller]s")]
		public async Task<ActionResult<ArtefactDTO>> CreateArtefact([FromRoute] uint editionId,
			[FromBody] CreateArtefactDTO payload)
		{
			return await _artefactService.CreateArtefactAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				editionId,
				payload.masterImageId,
				payload.mask,
				payload.name,
				payload.position
			);
		}

		/// <summary>
		///     Deletes the specified artefact
		/// </summary>
		/// <param name="artefactId">Unique Id of the desired artefact</param>
		/// <param name="editionId">Unique Id of the desired edition</param>
		[HttpDelete("v1/editions/{editionId}/[controller]s/{artefactId}")]
		public async Task<ActionResult> DeleteArtefact([FromRoute] uint artefactId, [FromRoute] uint editionId)
		{
			return await _artefactService.DeleteArtefactAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				artefactId
			);
		}

		/// <summary>
		///     Provides a listing of all artefacts that are part of the specified edition
		/// </summary>
		/// <param name="artefactId">Unique Id of the desired artefact</param>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/[controller]s/{artefactId}")]
		public async Task<ActionResult<ArtefactDTO>> GetArtefact([FromRoute] uint artefactId,
			[FromRoute] uint editionId,
			[FromQuery] List<string> optional)
		{
			return await _artefactService.GetEditionArtefactAsync(
				await _userService.GetCurrentUserObjectAsync(editionId),
				artefactId,
				optional
			);
		}

		/// <summary>
		///     Provides a listing of all artefacts that are part of the specified edition
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/[controller]s")]
		public async Task<ActionResult<ArtefactListDTO>> GetArtefacts([FromRoute] uint editionId,
			[FromQuery] List<string> optional)
		{
			return await _artefactService.GetEditionArtefactListingsAsync(
				await _userService.GetCurrentUserObjectAsync(editionId),
				optional
			);
		}

		/// <summary>
		///     Provides a listing of text fragments that may match the specified artefact
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="artefactId">Unique Id of the desired artefact</param>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/[controller]s/{artefactId}/suggested-text-fragments")]
		public async Task<ActionResult<TextFragmentDataListDTO>> GetArtefactSuggestedTextFragments(
			[FromRoute] uint editionId,
			[FromRoute] uint artefactId)
		{
			return await _artefactService.ArtefactSuggestedTextFragmentsAsync(
				await _userService.GetCurrentUserObjectAsync(editionId),
				artefactId
			);
		}

		/// <summary>
		///     Updates the specified artefact
		/// </summary>
		/// <param name="artefactId">Unique Id of the desired artefact</param>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="payload">An UpdateArtefactDTO with the desired alterations to the artefact</param>
		[HttpPut("v1/editions/{editionId}/[controller]s/{artefactId}")]
		public async Task<ActionResult<ArtefactDTO>> UpdateArtefact([FromRoute] uint artefactId,
			[FromRoute] uint editionId,
			[FromBody] UpdateArtefactDTO payload)
		{
			return await _artefactService.UpdateArtefactAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				editionId,
				artefactId,
				payload.mask,
				payload.name,
				payload.position
			);
		}
	}
}