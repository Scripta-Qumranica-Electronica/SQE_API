using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers
{
	[Authorize]
	[ApiController]
	public class ScribalFontController : ControllerBase
	{
		private readonly IScriptService _scriptService;
		private readonly IUserService   _userService;

		public ScribalFontController(IScriptService scriptService, IUserService userService)
		{
			_scriptService = scriptService;
			_userService = userService;
		}

		/// <summary>
		///  Get the details of the scribal font for an edition that
		///  are needed to generate reconstructed text layout.
		/// </summary>
		/// <param name="editionId">Edition for which to get the scribal font information</param>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/[controller]s")]
		public async Task<ActionResult<ScriptDataListDTO>> GetScribalFontData(
				[FromRoute] uint editionId) => await _scriptService.GetEditionScribalFontData(
				await _userService.GetCurrentUserObjectAsync(editionId));

		/// <summary>
		///  Creates a new scribal font for the edition
		/// </summary>
		/// <param name="editionId">Edition for which to create the new scribal font</param>
		/// <param name="scriptData">Basic information about the new scribal font</param>
		/// <returns></returns>
		[HttpPost("v1/editions/{editionId}/[controller]s")]
		public async Task<ActionResult<ScriptDataDTO>> SetScribalFontData(
				[FromRoute]  uint                editionId
				, [FromBody] CreateScriptDataDTO scriptData)
			=> await _scriptService.CreateEditionScribalFontData(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, scriptData);

		/// <summary>
		///  Updates the basic information about a scribal font
		/// </summary>
		/// <param name="editionId">Edition for which to update the scribal font</param>
		/// <param name="scribalFontId">The scribal font to be updated</param>
		/// <param name="scriptData">The updated scribal font information</param>
		/// <returns></returns>
		[HttpPut("v1/editions/{editionId}/[controller]s/{scribalFontId}/scribal-font-data")]
		public async Task<ActionResult<ScriptDataDTO>> UpdateScribalFontData(
				[FromRoute]   uint                editionId
				, [FromRoute] uint                scribalFontId
				, [FromBody]  CreateScriptDataDTO scriptData)
			=> await _scriptService.UpdateEditionScribalFontData(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, scribalFontId
					, scriptData);

		/// <summary>
		///  Deletes a scribal font
		/// </summary>
		/// <param name="editionId">Edition from which to delete the scribal font</param>
		/// <param name="scribalFontId">The scribal font to be deleted</param>
		/// <returns></returns>
		[HttpDelete("v1/editions/{editionId}/[controller]s/{scribalFontId}")]
		public async Task<ActionResult> DeleteScribalFont(
				[FromRoute]   uint editionId
				, [FromRoute] uint scribalFontId) => await _scriptService.DeleteScribalFont(
				await _userService.GetCurrentUserObjectAsync(editionId, true)
				, scribalFontId);

		/// <summary>
		///  Creates or updates a kerning pair for the scribal font.
		///  If the kern pair does not yet exists, it is created.
		///  If the kern pair already exists, it is updated.
		/// </summary>
		/// <param name="editionId">Edition for which to create or update the kerning pair</param>
		/// <param name="scribalFontId">The scribal font the kerning pair belongs to</param>
		/// <param name="kernPair">The kerning information</param>
		/// <returns></returns>
		[HttpPost("v1/editions/{editionId}/[controller]s/{scribalFontId}/kerning-pairs")]
		public async Task<ActionResult<KernPairDTO>> SetScribalFontKerningPair(
				[FromRoute]   uint              editionId
				, [FromRoute] uint              scribalFontId
				, [FromBody]  CreateKernPairDTO kernPair)
			=> await _scriptService.SetEditionScribalFontKerningPair(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, scribalFontId
					, kernPair);

		/// <summary>
		///  Deletes a kerning pair from a scribal font
		/// </summary>
		/// <param name="editionId">Edition from which to delete the kerning pair</param>
		/// <param name="scribalFontId">Scribal font from which to delete the kerning pair</param>
		/// <param name="firstCharacter">The first character of the kerning pair</param>
		/// <param name="secondCharacter">The second character of the kerning pair</param>
		/// <returns></returns>
		[HttpDelete(
				"v1/editions/{editionId}/[controller]s/{scribalFontId}/kerning-pairs/{firstCharacter}/{secondCharacter}")]
		public async Task<ActionResult> DeleteScribalFontKerningPair(
				[FromRoute]   uint   editionId
				, [FromRoute] uint   scribalFontId
				, [FromRoute] string firstCharacter
				, [FromRoute] string secondCharacter)
			=> await _scriptService.DeleteEditionScribalFontKerningPair(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, scribalFontId
					, firstCharacter
					, secondCharacter);

		/// <summary>
		///  Creates or updates information about a scribal font glyph.
		///  If information for the glyph does not yet exist, a new glyph is created.
		///  If information for the glyph already exists, that glyph information is updated.
		/// </summary>
		/// <param name="editionId">Edition in which the glyph is created or updated</param>
		/// <param name="scribalFontId">Scribal font in which the glyph is created or updated</param>
		/// <param name="glyph">Information about the glyph</param>
		/// <returns></returns>
		[HttpPost("v1/editions/{editionId}/[controller]s/{scribalFontId}/glyphs")]
		public async Task<ActionResult<GlyphDataDTO>> SetScribalFontGlyph(
				[FromRoute]   uint               editionId
				, [FromRoute] uint               scribalFontId
				, [FromBody]  CreateGlyphDataDTO glyph)
			=> await _scriptService.SetEditionScribalFontGlyph(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, scribalFontId
					, glyph);

		/// <summary>
		///  Deletes glyph information from a scribal font
		/// </summary>
		/// <param name="editionId">Edition from which the glyph is deleted</param>
		/// <param name="scribalFontId">Scribal font from which the glyph is deleted</param>
		/// <param name="glyphCharacter">The glyph to be deleted</param>
		/// <returns></returns>
		[HttpDelete(
				"v1/editions/{editionId}/[controller]s/{scribalFontId}/glyphs/{glyphCharacter}")]
		public async Task<ActionResult> DeleteScribalFontGlyph(
				[FromRoute]   uint editionId
				, [FromRoute] uint scribalFontId
				, [FromRoute] string glyphCharacter)
			=> await _scriptService.DeleteEditionScribalFontGlyph(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, scribalFontId
					, glyphCharacter);
	}
}
