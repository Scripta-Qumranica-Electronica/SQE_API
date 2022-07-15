using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers
{
	[Authorize]
	[ApiController]
	public class QwbProxyController : ControllerBase
	{
		private readonly IUserService _userService;
		private readonly IWordService _wordService;

		public QwbProxyController(
				ISearchService searchService
				, IUserService userService
				, IWordService wordService)
		{
			_userService = userService;
			_wordService = wordService;
		}

		/// <summary>
		///  Search QWB (via proxy) for any variant readings for the word that contains the submitted sign
		///  interpretation id.
		/// </summary>
		/// <param name="editionId">Edition in which the sign interpretation id is found</param>
		/// <param name="signInterpretationId">Id of the sign interpretation to search</param>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpGet(
				"v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/word-variants")]
		public async Task<ActionResult<QwbWordVariantListDTO>> GetSignInterpretationWordVariants(
				[FromRoute]   uint editionId
				, [FromRoute] uint signInterpretationId)
			=> await _wordService.GetQwbWordVariantForSignInterpretationId(
					await _userService.GetCurrentUserObjectAsync(editionId)
					, editionId
					, signInterpretationId);

		/// <summary>
		///  Search QWB (via proxy) for any variant readings for the word that contains the submitted
		///  QWB word id.
		/// </summary>
		/// <param name="qwbWordId">QWB word Id</param>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpGet("v1/qwb-proxy/words/{qwbWordId}/word-variants")]
		public async Task<ActionResult<QwbWordVariantListDTO>> GetQwbWordVariants(
				[FromRoute] uint qwbWordId)
			=> await _wordService.GetQwbWordVariantForQwbWordId(qwbWordId);

		/// <summary>
		///  Search QWB (via proxy) for any parallel text.
		/// </summary>
		/// <param name="qwbStartWordId">QWB word Id for the beginning of the text selection</param>
		/// <param name="qwbEndWordId">QWB word Id for the end of the text selection</param>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpGet("v1/qwb-proxy/parallels/start-word/{qwbStartWordId}/end-word/{qwbEndWordId}")]
		public async Task<ActionResult<QwbParallelListDTO>> GetQwbParallels(
				[FromRoute]   uint qwbStartWordId
				, [FromRoute] uint qwbEndWordId)
			=> await _wordService.GetQwbParallel(qwbStartWordId, qwbEndWordId);

		/// <summary>
		///  Get full bibliographic entry from QWB (via proxy).
		/// </summary>
		/// <param name="qwbBibliographyId">ID of the qwb bibliographical item to be retrieved</param>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpGet("v1/qwb-proxy/bibliography/{qwbBibliographyId}")]
		public async Task<ActionResult<QwbBibliographyEntryDTO>> GetQwbBibliography(
				[FromRoute] uint qwbBibliographyId)
			=> await _wordService.GetQwbBibliography(qwbBibliographyId);
	}
}
