// TODO: Add all documentation

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.DatabaseAccess
{
	public interface IScriptRepository
	{
		Task<IEnumerable<Glyph>> GetEditionScribalFontGlyphs(UserInfo user, uint scribalFontId);

		Task<IEnumerable<KerningPair>> GetEditionScribalFontKernPairs(
				UserInfo user
				, uint   scribalFontId);

		Task<FontInfo> GetEditionScribalFontInfo(UserInfo user, uint scribalFontId);
		Task<uint>     CreateNewScribalFontId(UserInfo    user);

		Task SetScribalFontInfo(
				UserInfo user
				, uint   scribalFontId
				, ushort wordSpace
				, ushort lineSpace);

		Task DeleteScribalFont(UserInfo user, uint scribalFontId);

		Task SetScribalFontKern(
				UserInfo user
				, uint   scribalFontId
				, char   firstCharacter
				, char   secondCharacter
				, short  xKern
				, short  yKern);

		Task DeleteScribalFontKern(
				UserInfo user
				, uint   scribalFontId
				, char   firstCharacter
				, char   secondCharacter);

		Task SetScribalFontGlyph(
				UserInfo user
				, uint   scribalFontId
				, char   character
				, string shape
				, short  yOffset);

		Task DeleteScribalFontGlyph(UserInfo user, uint scribalFontId, char character);
		Task<IEnumerable<uint>> GetEditionScribalFontIds(UserInfo user);
	}

	public class ScriptRepository : DbConnectionBase
									, IScriptRepository
	{
		private readonly IDatabaseWriter _databaseWriter;

		public ScriptRepository(IConfiguration config, IDatabaseWriter databaseWriter) :
				base(config) => _databaseWriter = databaseWriter;

		public async Task<IEnumerable<Glyph>> GetEditionScribalFontGlyphs(
				UserInfo user
				, uint   scribalFontId)
		{
			const string sql = @"
SELECT 	unicode_char AS `Character`,
		ST_ASTEXT(shape) AS Shape,
		creator_id AS CreatorId,
		edition_editor.edition_editor_id AS EditorId,
		y_offset AS YOffset,
       	scribal_font_id AS ScribalFontId
FROM scribal_font_glyph_metrics
JOIN scribal_font_glyph_metrics_owner USING(scribal_font_glyph_metrics_id)
JOIN edition USING(edition_id)
JOIN edition_editor USING(edition_id)
WHERE scribal_font_glyph_metrics.scribal_font_id = @ScribalFontId
	AND scribal_font_glyph_metrics_owner.edition_id = @EditionId
	AND (edition.public = 1 OR edition_editor.user_id = @UserId)
";

			using (var conn = OpenConnection())
			{
				return await conn.QueryAsync<Glyph>(
						sql
						, new
						{
								user.EditionId
								, UserId = user.userId
								, ScribalFontId = scribalFontId
								,
						});
			}
		}

		public async Task<IEnumerable<KerningPair>> GetEditionScribalFontKernPairs(
				UserInfo user
				, uint   scribalFontId)
		{
			const string sql = @"
SELECT	first_unicode_char AS FirstCharacter,
		second_unicode_char AS SecondCharacter,
		kerning_x AS XKern,
		kerning_y AS YKern,
		creator_id AS CreatorId,
		edition_editor.edition_editor_id AS EditorId,
       	scribal_font_id AS ScribalFontId
FROM scribal_font_kerning
JOIN scribal_font_kerning_owner USING(scribal_font_kerning_id)
JOIN edition USING(edition_id)
JOIN edition_editor USING(edition_id)
WHERE scribal_font_kerning.scribal_font_id = @ScribalFontId
	AND scribal_font_kerning_owner.edition_id = @EditionId
	AND (edition.public = 1 OR edition_editor.user_id = @UserId)
";

			using (var conn = OpenConnection())
			{
				return await conn.QueryAsync<KerningPair>(
						sql
						, new
						{
								user.EditionId
								, UserId = user.userId
								, ScribalFontId = scribalFontId
								,
						});
			}
		}

		public async Task<FontInfo> GetEditionScribalFontInfo(UserInfo user, uint scribalFontId)
		{
			const string sql = @"
SELECT DISTINCT	default_word_space AS SpaceSize,
				default_interlinear_space AS LineSpaceSize,
				scribal_font_metrics.creator_id AS CreatorId,
                scribal_font_metrics_owner.editor_id AS EditorId,
       			scribal_font_id AS ScribalFontId
FROM scribal_font_metrics
JOIN scribal_font_metrics_owner USING(scribal_font_metrics_id)
JOIN edition USING(edition_id)
JOIN edition_editor USING(edition_id)
WHERE scribal_font_metrics.scribal_font_id = @ScribalFontId
	AND scribal_font_metrics_owner.edition_id = @EditionId
	AND (edition.public = 1 OR edition_editor.user_id = @UserId)
";

			using (var conn = OpenConnection())
			{
				return (await conn.QueryAsync<FontInfo>(
						sql
						, new
						{
								user.EditionId
								, UserId = user.userId
								, ScribalFontId = scribalFontId,
						})).FirstOrDefault();
			}
		}

		public async Task SetScribalFontInfo(
				UserInfo user
				, uint   scribalFontId
				, ushort wordSpace
				, ushort lineSpace)
		{
			using (var transaction = new TransactionScope())
			using (var conn = OpenConnection())
			{
				var fontInfoParameters = new DynamicParameters();
				fontInfoParameters.Add("@scribal_font_id", wordSpace);
				fontInfoParameters.Add("@default_word_space", wordSpace);
				fontInfoParameters.Add("@default_interlinear_space", lineSpace);
				MutationRequest request;

				// Collect the scribal font metrics id
				const string scribalFontMetricsIdSQL = @"SELECT scribal_font_metrics_id
FROM scribal_font_metrics
JOIN scribal_font_metrics_owner USING(scribal_font_metrics_id)
JOIN edition USING(edition_id)
JOIN edition_editor USING(edition_id)
WHERE scribal_font_metrics.scribal_font_id = @ScribalFontId
	AND scribal_font_metrics_owner.edition_id = @EditionId
	AND (edition.public = 1 OR edition_editor.user_id = @UserId)";

				var scribalFontMetricsIds = await conn.QueryAsync<uint>(
						scribalFontMetricsIdSQL
						, new
						{
								user.EditionId
								, UserId = user.userId
								, ScribalFontId = scribalFontId
								,
						});

				// Check if this is an update operation
				if (scribalFontMetricsIds.Any())
				{
					var scribalFontMetricsId = scribalFontMetricsIds.First();

					request = new MutationRequest(
							MutateType.Update
							, fontInfoParameters
							, ScribalFontTableNames.Metrics
							, scribalFontMetricsId);
				}
				else // It is a create operation
				{
					request = new MutationRequest(
							MutateType.Create
							, fontInfoParameters
							, ScribalFontTableNames.Metrics);
				}

				var execute = await _databaseWriter.WriteToDatabaseAsync(user, request);

				if (!execute.Any())
					throw new StandardExceptions.DataNotWrittenException(
							"set scribal font metrics");

				transaction.Complete();
			}
		}

		public async Task DeleteScribalFont(UserInfo user, uint scribalFontId)
		{
			using (var transaction = new TransactionScope())
			{
				var mutations = new List<MutationRequest>();

				foreach (var table in ScribalFontTableNames.All())
				{
					var pks = await _getScribalFontPks(user, scribalFontId, table);

					mutations.AddRange(
							pks.Select(
									pk => new MutationRequest(
											MutateType.Delete
											, new DynamicParameters()
											, table
											, pk)));
				}

				await _databaseWriter.WriteToDatabaseAsync(user, mutations);

				transaction.Complete();
			}
		}

		public async Task SetScribalFontKern(
				UserInfo user
				, uint   scribalFontId
				, char   firstCharacter
				, char   secondCharacter
				, short  xKern
				, short  yKern)
		{
			using (var transaction = new TransactionScope())
			{
				var scribalFontKernId = await _getScriptKernId(
						user
						, scribalFontId
						, firstCharacter
						, secondCharacter
						, false);

				var kernParameters = new DynamicParameters();
				kernParameters.Add("@scribal_font_id", scribalFontId);
				kernParameters.Add("@first_unicode_char", firstCharacter);
				kernParameters.Add("@second_unicode_char", secondCharacter);
				kernParameters.Add("@kerning_x", xKern);
				kernParameters.Add("@kerning_y", yKern);

				var request = scribalFontKernId.HasValue
						? new MutationRequest(
								MutateType.Update
								, kernParameters
								, ScribalFontTableNames.Kerning
								, scribalFontKernId.Value)
						: new MutationRequest(
								MutateType.Create
								, kernParameters
								, ScribalFontTableNames.Kerning);

				await _databaseWriter.WriteToDatabaseAsync(user, request);

				transaction.Complete();
			}
		}

		public async Task DeleteScribalFontKern(
				UserInfo user
				, uint   scribalFontId
				, char   firstCharacter
				, char   secondCharacter)
		{
			using (var transaction = new TransactionScope())
			{
				var scribalFontKernId = await _getScriptKernId(
						user
						, scribalFontId
						, firstCharacter
						, secondCharacter
						, true);

				var request = new MutationRequest(
						MutateType.Delete
						, null
						, ScribalFontTableNames.Kerning
						, scribalFontKernId);

				await _databaseWriter.WriteToDatabaseAsync(user, request);

				transaction.Complete();
			}
		}

		public async Task SetScribalFontGlyph(
				UserInfo user
				, uint   scribalFontId
				, char   character
				, string shape
				, short  yOffset)
		{
			using (var transaction = new TransactionScope())
			{
				var scriptGlyphId = await _getScriptGlyphId(
						user
						, scribalFontId
						, character
						, false);

				var glyphParameters = new DynamicParameters();
				glyphParameters.Add("@scribal_font_id", scribalFontId);
				glyphParameters.Add("@unicode_char", character);
				glyphParameters.Add("@shape", shape);
				glyphParameters.Add("@y_offset", yOffset);

				var request = scriptGlyphId.HasValue
						? new MutationRequest(
								MutateType.Update
								, glyphParameters
								, ScribalFontTableNames.Glyph
								, scriptGlyphId.Value)
						: new MutationRequest(
								MutateType.Create
								, glyphParameters
								, ScribalFontTableNames.Glyph);

				await _databaseWriter.WriteToDatabaseAsync(user, request);

				transaction.Complete();
			}
		}

		public async Task DeleteScribalFontGlyph(UserInfo user, uint scribalFontId, char character)
		{
			using (var transaction = new TransactionScope())
			{
				var scriptGlyphId = await _getScriptGlyphId(
						user
						, scribalFontId
						, character
						, true);

				var request = new MutationRequest(
						MutateType.Delete
						, null
						, ScribalFontTableNames.Glyph
						, scriptGlyphId.Value);

				await _databaseWriter.WriteToDatabaseAsync(user, request);

				transaction.Complete();
			}
		}

		/// <summary>
		///  Create a new scribal font id to use for the edition.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public async Task<uint> CreateNewScribalFontId(UserInfo user)
		{
			using (var conn = OpenConnection())
			{
				const string sql = "INSERT INTO scribal_font (scribal_font_id) VALUES(null)";
				await conn.ExecuteAsync(sql);

				return await conn.QuerySingleAsync<uint>("SELECT LAST_INSERT_ID()");
			}
		}

		/// <summary>
		///  Get the scribal font id for the edition. If none exists, a new one is created.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public async Task<IEnumerable<uint>> GetEditionScribalFontIds(UserInfo user)
		{
			using (var conn = OpenConnection())
			{
				const string sql = @"
SELECT scribal_font_id
FROM scribal_font_metrics
JOIN scribal_font_metrics_owner USING(scribal_font_metrics_id)
WHERE scribal_font_metrics_owner.edition_id = @EditionId";

				var scribalFontIds = await conn.QueryAsync<uint>(sql, new { user.EditionId });

				if (scribalFontIds.Any())
					return scribalFontIds;

				// The edition does not yet have a scribal font, so create one
				return new List<uint> { await CreateNewScribalFontId(user) };
			}
		}

		private async Task<uint?> _getScriptKernId(
				UserInfo user
				, uint   scribalFontId
				, char   firstCharacter
				, char   secondCharacter
				, bool   shouldExist)
		{
			using (var conn = OpenConnection())
			{
				const string scriptKernIdsSQL = @"
SELECT scribal_font_kerning_id
FROM scribal_font_kerning
JOIN scribal_font_kerning_owner USING(scribal_font_kerning_id)
WHERE scribal_font_kerning_owner = @EditionId
  	AND scribal_font_kerning.scribal_font_id = @ScribalFontId
	AND scribal_font_kerning.first_unicode_char = @FirstChar
	AND scribal_font_kerning.second_unicode_char = @SecondChar
";

				var scriptKernIds = await conn.QueryAsync<uint>(
						scriptKernIdsSQL
						, new
						{
								user.EditionId
								, ScribalFontId = scribalFontId
								, FirstChar = firstCharacter
								, SecondChar = secondCharacter
								,
						});

				if (scriptKernIds.Any())
					return scriptKernIds.First();

				if (shouldExist)
				{
					throw new StandardExceptions.DataNotFoundException(
							$"kerning pair {firstCharacter}, {secondCharacter}"
							, scribalFontId
							, "scribal font kerning");
				}

				return null;
			}
		}

		private async Task<uint?> _getScriptGlyphId(
				UserInfo user
				, uint   scribalFontId
				, char   character
				, bool   shouldExist)
		{
			using (var conn = OpenConnection())
			{
				const string scriptKernIdsSQL = @"
SELECT scribal_font_glyph_metrics_id
FROM scribal_font_glyph_metrics
JOIN scribal_font_glyph_metrics_owner USING(scribal_font_glyph_metrics_id)
WHERE scribal_font_glyph_metrics_owner = @EditionId
  	AND scribal_font_glyph_metrics.scribal_font_id = @ScribalFontId
	AND scribal_font_glyph_metrics.unicode_char = @Character
";

				var scriptGlyphIds = await conn.QueryAsync<uint>(
						scriptKernIdsSQL
						, new
						{
								user.EditionId
								, ScribalFontId = scribalFontId
								, Character = character
								,
						});

				if (scriptGlyphIds.Any())
					return scriptGlyphIds.First();

				if (shouldExist)
				{
					throw new StandardExceptions.DataNotFoundException(
							$"glyph {character}"
							, scribalFontId
							, "scribal font glyph metrics");
				}

				return null;
			}
		}

		private async Task<IEnumerable<uint>> _getScribalFontPks(
				UserInfo user
				, uint   scribalFontId
				, string tableName)
		{
			using (var conn = OpenConnection())
			{
				var sql = $@"
SELECT {
							tableName
						}_id
FROM {
							tableName
						}_owner
JOIN {
							tableName
						} USING({
							tableName
						}_id)
WHERE edition_id = @EditionId
	AND scribal_font_id = @ScribalFontId";

				return await conn.QueryAsync<uint>(
						sql
						, new { user.EditionId, ScribalFontId = scribalFontId });
			}
		}

		private static class ScribalFontTableNames
		{
			public const string Metrics  = "scribal_font_metrics";
			public const string Glyph    = "scribal_font_glyph_metrics";
			public const string Kerning  = "scribal_font_kerning";
			public const string FontFile = "font_file";

			public static IEnumerable<string> All() => new List<string>
			{
					Metrics
					, Glyph
					, Kerning
					, FontFile
					,
			};
		}
	}
}
