// TODO: Broadcast all changes on SignalR
// TODO: Add all documentation

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Serialization;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
	public interface IScriptService
	{
		Task<ScriptDataListDTO> GetEditionScribalFontData(UserInfo user);

		Task<ScriptDataDTO> CreateEditionScribalFontData(
				UserInfo              user
				, CreateScriptDataDTO data
				, string              clientId = null);

		Task<ScriptDataDTO> UpdateEditionScribalFontData(
				UserInfo              user
				, uint                scribalFontId
				, CreateScriptDataDTO data
				, string              clientId = null);

		Task<NoContentResult> DeleteScribalFont(
				UserInfo user
				, uint   scribalFontId
				, string clientId = null);

		Task<KernPairDTO> SetEditionScribalFontKerningPair(
				UserInfo            user
				, uint              scribalFontId
				, CreateKernPairDTO kernPair
				, string            clientId = null);

		Task<NoContentResult> DeleteEditionScribalFontKerningPair(
				UserInfo user
				, uint   scribalFontId
				, string firstCharacter
				, string secondCharacter
				, string clientId = null);

		Task<GlyphDataDTO> SetEditionScribalFontGlyph(
				UserInfo             user
				, uint               scribalFontId
				, CreateGlyphDataDTO glyph
				, string             clientId = null);

		Task<NoContentResult> DeleteEditionScribalFontGlyph(
				UserInfo user
				, uint   scribalFontId
				, string glyph
				, string clientId = null);
	}

	public class ScriptService : IScriptService
	{
		private readonly IHubContext<MainHub, ISQEClient> _hubContext;
		private readonly IScriptRepository                _scriptRepository;

		public ScriptService(
				IHubContext<MainHub, ISQEClient> hubContext
				, IScriptRepository              scriptRepository)
		{
			_hubContext = hubContext;
			_scriptRepository = scriptRepository;
		}

		public async Task<ScriptDataListDTO> GetEditionScribalFontData(UserInfo user)
		{
			var scribalFontIds = await _scriptRepository.GetEditionScribalFontIds(user);
			var scribalFonts = new List<ScriptDataDTO>();

			foreach (var scribalFontId in scribalFontIds)
			{
				var kernPairs =
						await _scriptRepository.GetEditionScribalFontKernPairs(user, scribalFontId);

				var glyphs =
						await _scriptRepository.GetEditionScribalFontGlyphs(user, scribalFontId);

				var fontInfo =
						await _scriptRepository.GetEditionScribalFontInfo(user, scribalFontId);

				scribalFonts.Add(
						new ScriptDataDTO
						{
								glyphs = glyphs.ToDTO()
								, kerningPairs = kernPairs.ToDTO()
								, lineSpace = fontInfo?.LineSpaceSize ?? 0
								, wordSpace = fontInfo?.SpaceSize ?? 0
								, creatorId = fontInfo?.CreatorId ?? 0
								, editorId = fontInfo?.EditorId ?? 0
								, scribalFontId = scribalFontId
								,
						});
			}

			return new ScriptDataListDTO { scripts = scribalFonts };
		}

		public async Task<ScriptDataDTO> CreateEditionScribalFontData(
				UserInfo              user
				, CreateScriptDataDTO data
				, string              clientId = null)
		{
			var scribalFontId = await _scriptRepository.CreateNewScribalFontId(user);

			var newScribalFont = await _setEditionScriptData(user, scribalFontId, data);

			// Broadcast the update
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .CreatedScribalFontInfo(newScribalFont);

			return newScribalFont;
		}

		public async Task<ScriptDataDTO> UpdateEditionScribalFontData(
				UserInfo              user
				, uint                scribalFontId
				, CreateScriptDataDTO data
				, string              clientId = null)
		{
			var updatedScribalFont = await _setEditionScriptData(user, scribalFontId, data);

			// Broadcast the update
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .UpdatedScribalFontInfo(updatedScribalFont);

			return updatedScribalFont;
		}

		public async Task<NoContentResult> DeleteScribalFont(
				UserInfo user
				, uint   scribalFontId
				, string clientId = null)
		{
			await _scriptRepository.DeleteScribalFont(user, scribalFontId);

			// Broadcast the deletion
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .DeletedScribalFont(
									 new DeleteScribalFontDTO
									 {
											 editionEditorId = user.EditionEditorId.Value
											 , scribalFontId = scribalFontId
											 ,
									 });

			return new NoContentResult();
		}

		public async Task<KernPairDTO> SetEditionScribalFontKerningPair(
				UserInfo            user
				, uint              scribalFontId
				, CreateKernPairDTO kernPair
				, string            clientId = null)
		{
			await _scriptRepository.SetScribalFontKern(
					user
					, scribalFontId
					, kernPair.firstCharacter
					, kernPair.secondCharacter
					, kernPair.xKern
					, kernPair.yKern);

			// Get the updated information
			var scriptKern = await GetEditionScribalFontData(user);

			var updatedScriptKern = scriptKern.scripts.First(x => x.scribalFontId == scribalFontId)
											  .kerningPairs.First(
													  x => x.firstCharacter
														   == kernPair.firstCharacter
														   && x.secondCharacter
														   == kernPair.secondCharacter);

			// Broadcast update as well
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .CreatedScribalFontKerningPair(updatedScriptKern);

			return updatedScriptKern;
		}

		public async Task<NoContentResult> DeleteEditionScribalFontKerningPair(
				UserInfo user
				, uint   scribalFontId
				, string firstCharacter
				, string secondCharacter
				, string clientId = null)
		{
			firstCharacter = HttpUtility.HtmlDecode(firstCharacter);
			secondCharacter = HttpUtility.HtmlDecode(secondCharacter);

			if (firstCharacter.Length != 1)
				throw new StandardExceptions.ImproperInputDataException("first character");

			if (secondCharacter.Length != 1)
				throw new StandardExceptions.ImproperInputDataException("second character");

			await _scriptRepository.DeleteScribalFontKern(
					user
					, scribalFontId
					, firstCharacter
					, secondCharacter);

			// Broadcast update
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .DeletedScribalFontKerningPair(
									 new DeleteKernPairDTO
									 {
											 editorId =
													 user.EditionEditorId
														 .Value // The value will exist because the user already passed the test of having write access
											 , firstCharacter = firstCharacter
											 , secondCharacter = secondCharacter
											 , scribalFontId = scribalFontId
											 ,
									 });

			return new NoContentResult();
		}

		public async Task<GlyphDataDTO> SetEditionScribalFontGlyph(
				UserInfo             user
				, uint               scribalFontId
				, CreateGlyphDataDTO glyph
				, string             clientId = null)
		{
			// First verify that the shape is valid!

			await _scriptRepository.SetScribalFontGlyph(
					user
					, scribalFontId
					, glyph.character
					, glyph.shape
					, glyph.yOffset);

			// Get the updated information
			var scriptInfo = await GetEditionScribalFontData(user);

			var updatedScriptGlyph = scriptInfo.scripts.First(x => x.scribalFontId == scribalFontId)
											   .glyphs.First(x => x.character == glyph.character);

			// Broadcast update as well
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .CreatedScribalFontGlyph(updatedScriptGlyph);

			return updatedScriptGlyph;
		}

		public async Task<NoContentResult> DeleteEditionScribalFontGlyph(
				UserInfo user
				, uint   scribalFontId
				, string glyph
				, string clientId = null)
		{
			glyph = HttpUtility.HtmlDecode(glyph);

			if (glyph.Length != 1)
				throw new StandardExceptions.ImproperInputDataException("glyph character");

			await _scriptRepository.DeleteScribalFontGlyph(user, scribalFontId, glyph);

			// Broadcast update
			await _hubContext.Clients.GroupExcept(user.EditionId.ToString(), clientId)
							 .DeletedScribalFontGlyph(
									 new DeleteGlyphDataDTO
									 {
											 character = glyph
											 , editorId =
													 user.EditionEditorId
														 .Value // The value will exist because the user already passed the test of having write access
											 , scribalFontId = scribalFontId
											 ,
									 });

			return new NoContentResult();
		}

		private async Task<ScriptDataDTO> _setEditionScriptData(
				UserInfo              user
				, uint                scribalFontId
				, CreateScriptDataDTO data)
		{
			await _scriptRepository.SetScribalFontInfo(
					user
					, scribalFontId
					, data.wordSpace
					, data.lineSpace);

			var scriptData = await GetEditionScribalFontData(user);

			var updatedScriptData = scriptData.scripts.First(x => x.scribalFontId == scribalFontId);

			return updatedScriptData;
		}
	}
}
