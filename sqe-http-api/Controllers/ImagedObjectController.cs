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
	public class ImagedObjectController : ControllerBase
	{
		private readonly IImagedObjectService _imagedObjectService;
		private readonly IImageService _imageService;

		private readonly IUserService _userService;

		public ImagedObjectController(IUserService userService,
			IImagedObjectService imagedObjectService,
			IImageService imageService)
		{
			_imagedObjectService = imagedObjectService;
			_userService = userService;
			_imageService = imageService;
		}

		private void ParseOptionals(List<string> optionals, out bool artefacts, out bool masks)
		{
			artefacts = masks = false;
			if (optionals == null)
				return;
			artefacts = optionals.Contains("artefacts");
			if (!optionals.Contains("masks"))
				return;
			masks = true;
			artefacts = true;
		}

		/// <summary>
		///     Provides a list of all imaged objects in the system
		/// </summary>
		/// <returns></returns>
		// Bronson: This endpoint is not documented
		[AllowAnonymous]
		[HttpGet("imaged-objects")]
		[ProducesResponseType(200)]
		public async Task<ActionResult> ListImageGroups()
		{
			await Task.CompletedTask;
			return StatusCode(501);
		}

		/// <summary>
		///     Provides detailed information about a specific imaged object reference
		/// </summary>
		/// <param name="imageReferenceId"></param>
		/// <returns></returns>
		// Bronson: This endpoint is not documented
		[AllowAnonymous]
		[HttpGet("imaged-objects/{imageReferenceId}")]
		[ProducesResponseType(200)]
		public async Task<ActionResult> ListImageGroupsOfImagedObject([FromRoute] uint imageReferenceId)
		{
			await Task.CompletedTask;
			return StatusCode(501);
		}

		/// <summary>
		///     Provides a list of all institutional image providers.
		/// </summary>
		// Bronson: This endpoint is not documented
		[AllowAnonymous]
		[HttpGet("imaged-objects/institutions")]
		[ProducesResponseType(200)]
		public async Task<ActionResult<ImageInstitutionListDTO>> ListImageInstitutions()
		{
			return await _imageService.GetImageInstitutionsAsync();
		}

		/// <summary>
		///     Provides a listing of imaged objects related to the specified edition, can include
		///     images and also their masks with optional.
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="optional">Set "artefacts" to receive related artefact data and "masks" to include the artefact masks</param>
		/// <response code="200">Imaged object(s) found and returned</response>
		/// <response code="404">No imaged objects were found, perhaps an id was incorrect</response>
		[AllowAnonymous]
		[HttpGet("editions/{editionId}/imaged-objects")]
		[ProducesResponseType(200)]
		[ProducesResponseType(404)]
		public async Task<ActionResult<ImagedObjectListDTO>> GetImagedObjects([FromRoute] uint editionId,
			[FromQuery] List<string> optional)
		{
			ParseOptionals(optional, out var artefacts, out var masks);

			return await _imagedObjectService.GetImagedObjectsAsync(
				_userService.GetCurrentUserId(),
				editionId,
				artefacts,
				masks
			);
		}

		/// <summary>
		///     Provides information for the specified imaged object related to the specified edition, can include
		///     images and also their masks with optional.
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
		/// <param name="optional">Set "artefacts" to receive related artefact data and "masks" to include the artefact masks</param>
		/// <response code="200">Imaged object(s) found and returned</response>
		/// <response code="404">No imaged objects were found, perhaps an id was incorrect</response>
		[AllowAnonymous]
		[HttpGet("editions/{editionId}/imaged-objects/{imagedObjectId}")]
		[ProducesResponseType(200)]
		[ProducesResponseType(404)]
		public async Task<ActionResult<ImagedObjectDTO>> GetImagedObject([FromRoute] uint editionId,
			string imagedObjectId,
			[FromQuery] List<string> optional)
		{
			ParseOptionals(optional, out var artefacts, out var masks);
			return await _imagedObjectService.GetImagedObjectAsync(
				_userService.GetCurrentUserId(),
				editionId,
				imagedObjectId,
				artefacts,
				masks
			);
		}
	}
}