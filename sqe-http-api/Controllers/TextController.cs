using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Services;

namespace SQE.SqeApi.Server.Controllers
{
	[Authorize]
	[ApiController]
	public class TextController : ControllerBase
	{
		private readonly ITextService _textService;
		private readonly IUserService _userService;

		public TextController(ITextService textService, IUserService userService)
		{
			_textService = textService;
			_userService = userService;
		}

		/// <summary>
		///     Creates a new text fragment in the given edition of a scroll
		/// </summary>
		/// <param name="createFragment">A JSON object with the details of the new text fragment to be created</param>
		/// <param name="editionId">Id of the edition</param>
		[HttpPost("v1/editions/{editionId}/text-fragments")]
		public async Task<ActionResult<TextFragmentDataDTO>> CreateTextFragment(
			[FromBody] CreateTextFragmentDTO createFragment,
			[FromRoute] uint editionId)
		{
			return await _textService.CreateTextFragmentAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true),
				createFragment
			);
		}

		/// <summary>
		///     Retrieves the ids of all fragments in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <returns>An array of the text fregment ids in correct sequence</returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/text-fragments")]
		public async Task<ActionResult<TextFragmentDataListDTO>> RetrieveFragmentIds([FromRoute] uint editionId)
		{
			return await _textService.GetFragmentDataAsync(await _userService.GetCurrentUserObjectAsync(editionId));
		}

		/// <summary>
		///     Retrieves the ids of all lines in the given textFragmentName
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="textFragmentId">Id of the text fragment</param>
		/// <returns>An array of the line ids in the proper sequence</returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/text-fragments/{textFragmentId}/lines")]
		public async Task<ActionResult<LineDataListDTO>> RetrieveLineIds([FromRoute] uint editionId,
			[FromRoute] uint textFragmentId)
		{
			return await _textService.GetLineIdsAsync(
				await _userService.GetCurrentUserObjectAsync(editionId),
				textFragmentId
			);
		}

		/// <summary>
		///     Retrieves all signs and their data from the given textFragmentName
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="textFragmentId">Id of the text fragment</param>
		/// <returns>
		///     A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
		///     sequence
		/// </returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/text-fragments/{textFragmentId}")]
		public async Task<ActionResult<TextEditionDTO>> RetrieveTextOfFragmentById([FromRoute] uint editionId,
			[FromRoute] uint textFragmentId)
		{
			return await _textService.GetFragmentByIdAsync(
				await _userService.GetCurrentUserObjectAsync(editionId),
				textFragmentId
			);
		}

		/// <summary>
		///     Retrieves all signs and their data from the given line
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="lineId">Id of the line</param>
		/// <returns>
		///     A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
		///     sequence
		/// </returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/lines/{lineId}")]
		public async Task<ActionResult<LineTextDTO>> RetrieveTextOfLineById([FromRoute] uint editionId,
			[FromRoute] uint lineId)
		{
			return await _textService.GetLineByIdAsync(await _userService.GetCurrentUserObjectAsync(editionId), lineId);
		}
	}
}