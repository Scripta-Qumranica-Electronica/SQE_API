using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Services;

namespace SQE.SqeApi.Server.Controllers
{
	[Authorize]
	[ApiController]
	public class RoiController : ControllerBase
	{
		private readonly ITextService _textService;
		private readonly IUserService _userService;

		public RoiController(ITextService textService, IUserService userService)
		{
			_textService = textService;
			_userService = userService;
		}

		/// <summary>
		///     Creates new sign ROI's in the given edition of a scroll
		/// </summary>
		/// <param name="newRois">A JSON object with an array of the new ROI's to be created</param>
		/// <param name="editionId">Id of the edition</param>
		[HttpPost("v1/editions/{editionId}/rois")]
		public async Task<ActionResult<InterpretationRoiDTOList>> CreateRois(
			[FromBody] SetInterpretationRoiDTOList newRois,
			[FromRoute] uint editionId)
		{
			return null;
		}
	}
}