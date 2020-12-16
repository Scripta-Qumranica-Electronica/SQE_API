using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Models;

namespace SQE.DatabaseAccess
{
	public interface ISearchRepository
	{
		Task<IEnumerable<uint>> SearchEditions(uint userId, string editionName, bool exact);

		Task<List<TextFragmentSearch>> SearchTextFragments(
				uint                userId
				, string            textFragmentName
				, IEnumerable<uint> editionIds
				, bool              exact);

		Task<List<EditionArtefact>> SearchArtefacts(
				uint                userId
				, string            artefactName
				, IEnumerable<uint> editionIds
				, bool              exact);

		Task<IEnumerable<SearchImagedObject>> SearchImagedObjects(
				string imagedObjectName
				, bool exact);
	}

	public class SearchRepository : DbConnectionBase
									, ISearchRepository
	{
		public SearchRepository(IConfiguration config) : base(config) { }

		public async Task<IEnumerable<uint>> SearchEditions(
				uint     userId
				, string editionName
				, bool   exact)
		{
			using (var conn = OpenConnection())
			{
				var sql = @"
SELECT manuscript_data_owner.edition_id
FROM manuscript_data
JOIN manuscript_data_owner USING(manuscript_data_id)
JOIN edition USING(edition_id)
JOIN edition_editor ON edition_editor.edition_id = edition.edition_id
WHERE manuscript_data.name $Match
    AND (edition.public = 1 OR edition_editor.user_id = @UserId)
LIMIT 100
";

				sql = sql.Replace(
						"$Match"
						, exact
								? "= @ManuscriptName"
								: "LIKE CONCAT('%', @ManuscriptName, '%')");

				return await conn.QueryAsync<uint>(
						sql
						, new { ManuscriptName = editionName, UserId = userId });
			}
		}

		public async Task<List<TextFragmentSearch>> SearchTextFragments(
				uint                userId
				, string            textFragmentName
				, IEnumerable<uint> editionIds
				, bool              exact)
		{
			using (var conn = OpenConnection())
			{
				var sql = @"
SELECT text_fragment_data_owner.edition_id AS EditionId,
       manuscript_data.name AS EditionName,
       text_fragment_data.text_fragment_id AS TextFragmentId,
       text_fragment_data.name AS Name,
       IF(GROUP_CONCAT(DISTINCT editors.name) = '', 'sqe_api', GROUP_CONCAT(DISTINCT editors.name)) AS Editors
FROM text_fragment_data
JOIN text_fragment_data_owner USING(text_fragment_data_id)
JOIN edition USING(edition_id)
JOIN edition_editor USING(edition_id)
JOIN manuscript_data_owner USING(edition_id)
JOIN manuscript_data USING(manuscript_data_id)
JOIN (
    SELECT TRIM(CONCAT_WS('', forename, ' ', surname)) AS name, edition_id
    FROM user
    JOIN edition_editor USING(user_id)
) AS editors USING(edition_id)
WHERE text_fragment_data.name $Match
   AND (edition.public = 1 OR edition_editor.user_id = @UserId)
$Where
GROUP BY text_fragment_data.text_fragment_id
LIMIT 100";

				sql = sql.Replace(
								 "$Where"
								 , editionIds.Any()
										 ? "AND edition_id in @EditionIds"
										 : "")
						 .Replace(
								 "$Match"
								 , exact
										 ? "= @TextFragmentName"
										 : "LIKE CONCAT('%', @TextFragmentName, '%')");

				return (await conn.QueryAsync<TextFragmentSearch>(
						sql
						, new
						{
								TextFragmentName = textFragmentName
								, UserId = userId
								, EditionIds = editionIds
								,
						})).AsList();
			}
		}

		public async Task<List<EditionArtefact>> SearchArtefacts(
				uint                userId
				, string            artefactName
				, IEnumerable<uint> editionIds
				, bool              exact)
		{
			using (var conn = OpenConnection())
			{
				var sql = @"
SELECT DISTINCT edition_id AS EditionId, artefact_id AS ArtefactId
FROM artefact_data
JOIN artefact_data_owner USING(artefact_data_id)
JOIN edition USING(edition_id)
JOIN edition_editor USING(edition_id)
WHERE artefact_data.name $Match
    AND (edition.public = 1 OR edition_editor.user_id = @UserId)
$Where
LIMIT 100";

				sql = sql.Replace(
								 "$Where"
								 , editionIds.Any()
										 ? "AND edition_id in @EditionIds"
										 : "")
						 .Replace(
								 "$Match"
								 , exact
										 ? "= @ArtefactName"
										 : "LIKE CONCAT('%', @ArtefactName, '%')");

				return (await conn.QueryAsync<EditionArtefact>(
						sql
						, new
						{
								ArtefactName = artefactName
								, UserId = userId
								, EditionIds = editionIds
								,
						})).AsList();
			}
		}

		public async Task<IEnumerable<SearchImagedObject>> SearchImagedObjects(
				string imagedObjectName
				, bool exact)
		{
			using (var conn = OpenConnection())
			{
				var sql = @"
SELECT DISTINCT image_catalog.object_id AS Id,
                CONCAT_WS('', image_urls.proxy, image_urls.url, SQE_image.filename, '/full/150,/0/', image_urls.suffix) AS RectoThumbnail,
				CONCAT_WS('', vers_urls.proxy, vers_urls.url, vers_image.filename, '/full/150,/0/', vers_urls.suffix) AS VersoThumbnail
FROM image_catalog
JOIN SQE_image ON SQE_image.image_catalog_id = image_catalog.image_catalog_id
	AND SQE_image.is_master = 1
JOIN image_urls USING(image_urls_id)
LEFT JOIN image_catalog AS vers_catalog ON vers_catalog.object_id = image_catalog.object_id
	AND vers_catalog.catalog_side = 1
LEFT JOIN SQE_image AS vers_image ON vers_image.image_catalog_id = vers_catalog.image_catalog_id
	AND vers_image.is_master = 1
LEFT JOIN image_urls AS vers_urls ON vers_urls.image_urls_id = vers_image.image_urls_id
WHERE image_catalog.object_id $Match
	AND image_catalog.catalog_side = 0
LIMIT 100";

				sql = sql.Replace(
						"$Match"
						, exact
								? "= @ImagedObjectName"
								: "LIKE CONCAT('%', @ImagedObjectName, '%')");

				return await conn.QueryAsync<SearchImagedObject>(
						sql
						, new { ImagedObjectName = imagedObjectName });
			}
		}
	}
}
