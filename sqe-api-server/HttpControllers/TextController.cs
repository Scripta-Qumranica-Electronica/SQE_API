using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers
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
		///  Creates a new text fragment in the given edition of a scroll
		/// </summary>
		/// <param name="createFragment">A JSON object with the details of the new text fragment to be created</param>
		/// <param name="editionId">Id of the edition</param>
		[HttpPost("v1/editions/{editionId}/text-fragments")]
		public async Task<ActionResult<TextFragmentDataDTO>> CreateTextFragment(
				[FromRoute]  uint                  editionId
				, [FromBody] CreateTextFragmentDTO createFragment)
			=> await _textService.CreateTextFragmentAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, createFragment);

		/// <summary>
		///  Updates the specified text fragment with the submitted properties
		/// </summary>
		/// <param name="editionId">Edition of the text fragment being updates</param>
		/// <param name="textFragmentId">Id of the text fragment being updates</param>
		/// <param name="updatedTextFragment">Details of the updated text fragment</param>
		/// <returns>The details of the updated text fragment</returns>
		[HttpPut("v1/editions/{editionId}/text-fragments/{textFragmentId}")]
		public async Task<ActionResult<TextFragmentDataDTO>> UpdateTextFragmentById(
				[FromRoute]   uint                  editionId
				, [FromRoute] uint                  textFragmentId
				, [FromBody]  UpdateTextFragmentDTO updatedTextFragment)
			=> await _textService.UpdateTextFragmentAsync(
					await _userService.GetCurrentUserObjectAsync(editionId)
					, textFragmentId
					, updatedTextFragment);

		/// <summary>
		///  Retrieves the ids of all Fragments of all fragments in the given edition of a scroll
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <returns>An array of the text fragment ids in correct sequence</returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/text-fragments")]
		public async Task<ActionResult<TextFragmentDataListDTO>>
				RetrieveFragmentIds([FromRoute] uint editionId)
			=> await _textService.GetFragmentDataAsync(
					await _userService.GetCurrentUserObjectAsync(editionId));

		/// <summary>
		///  Retrieves the ids of all Artefacts in the given textFragmentName
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="textFragmentId">Id of the text fragment</param>
		/// <returns>An array of the line ids in the proper sequence</returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/text-fragments/{textFragmentId}/artefacts")]
		public async Task<ActionResult<ArtefactDataListDTO>> RetrieveArtefacts(
				[FromRoute]   uint editionId
				, [FromRoute] uint textFragmentId) => await _textService.GetArtefactsAsync(
				await _userService.GetCurrentUserObjectAsync(editionId)
				, textFragmentId);

		/// <summary>
		///  Retrieves the ids of all lines in the given textFragmentName
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="textFragmentId">Id of the text fragment</param>
		/// <returns>An array of the line ids in the proper sequence</returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/text-fragments/{textFragmentId}/lines")]
		public async Task<ActionResult<LineDataListDTO>> RetrieveLineIds(
				[FromRoute]   uint editionId
				, [FromRoute] uint textFragmentId) => await _textService.GetLineIdsAsync(
				await _userService.GetCurrentUserObjectAsync(editionId)
				, textFragmentId);

		/// <summary>
		///  Retrieves all signs and their data from the given textFragmentName
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="textFragmentId">Id of the text fragment</param>
		/// <returns>
		///  A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
		///  sequence
		/// </returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/text-fragments/{textFragmentId}")]
		public async Task<ActionResult<TextEditionDTO>> RetrieveTextOfFragmentById(
				[FromRoute]   uint editionId
				, [FromRoute] uint textFragmentId) => await _textService.GetFragmentByIdAsync(
				await _userService.GetCurrentUserObjectAsync(editionId)
				, textFragmentId);

		/// <summary>
		///  Retrieves all signs and their data from the entire edition
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <returns>
		///  A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
		///  sequence
		/// </returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/full-text")]
		public async Task<ActionResult<TextEditionDTO>> RetrieveTextFragmentsOfEdition(
				[FromRoute] uint editionId) => await _textService.GetFragmentsOfEditionAsync(
				await _userService.GetCurrentUserObjectAsync(editionId));

		/// <summary>
		///  Retrieves all signs and their data from the given line
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="lineId">Id of the line</param>
		/// <returns>
		///  A manuscript edition object including the fragments and their lines in a
		///  hierarchical order and in correct sequence
		/// </returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/lines/{lineId}")]
		public async Task<ActionResult<LineTextDTO>> RetrieveTextOfLineById(
				[FromRoute]   uint editionId
				, [FromRoute] uint lineId) => await _textService.GetLineByIdAsync(
				await _userService.GetCurrentUserObjectAsync(editionId)
				, lineId);

		/// <summary>
		///  Changes the details of the line (currently the lines name)
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="lineId">Id of the line</param>
		/// <param name="lineData">The updated line data</param>
		/// <returns>
		///  The updated details concerning the line sequence
		/// </returns>
		[HttpPut("v1/editions/{editionId}/lines/{lineId}")]
		public async Task<ActionResult<LineDataDTO>> UpdateLineById(
				[FromRoute]   uint          editionId
				, [FromRoute] uint          lineId
				, [FromBody]  UpdateLineDTO lineData) => await _textService.UpdateLineByIdAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true)
				, lineId
				, lineData);

		/// <summary>
		///  Delete a full line from a text fragment
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="lineId">Id of the line to be deleted</param>
		/// <returns>
		///  The updated details concerning the line sequence
		/// </returns>
		[HttpDelete("v1/editions/{editionId}/lines/{lineId}")]
		public async Task<ActionResult> DeleteLineById(
				[FromRoute]   uint editionId
				, [FromRoute] uint lineId) => await _textService.DeleteLineByIdAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true)
				, lineId);

		/// <summary>
		///  Creates a new line before or after another line.
		/// </summary>
		/// <param name="editionId">Id of the edition</param>
		/// <param name="textFragmentId">
		///  Id of the text fragment where the line will be
		///  added
		/// </param>
		/// <param name="lineData">The information about the line to be created</param>
		/// <returns>
		///  The details concerning the newly created line
		/// </returns>
		[HttpPost("v1/editions/{editionId}/text-fragments/{textFragmentId}/lines")]
		public async Task<ActionResult<LineDataDTO>> CreateNewLine(
				[FromRoute]   uint          editionId
				, [FromRoute] uint          textFragmentId
				, [FromBody]  CreateLineDTO lineData) => await _textService.CreateLineAsync(
				await _userService.GetCurrentUserObjectAsync(editionId, true)
				, textFragmentId
				, lineData);

		/// <summary>
		///  Alter the text between two sign interpretation ids.
		///  The system will try as best it can to figure out
		///  how the next text aligns with any text already
		///  existing at that location in the edition.
		/// </summary>
		/// <param name="editionId">Id of the edition to be updated</param>
		/// <param name="payload">Details of the text replacement request</param>
		/// <returns>
		///  Information about all sign interpretations that were
		///  created, updated, and deleted as a result of the operation.
		/// </returns>
		[HttpPut("v1/editions/{editionId}/diff-replace-text")]
		public async Task<ActionResult<DiffReplaceResponseDTO>> DiffReplaceText(
				[FromRoute]  uint                  editionId
				, [FromBody] DiffReplaceRequestDTO payload) => await _textService.DiffReplaceText(
				await _userService.GetCurrentUserObjectAsync(editionId, true)
				, payload);
	}
}
