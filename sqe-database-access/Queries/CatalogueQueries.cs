using System.Collections.Generic;
using System.Linq;

namespace SQE.DatabaseAccess.Queries
{
	internal enum CatalogueQueryFilterType
	{
		Edition
		, ImagedObject
		, TextFragment
		, Manuscript
		, Match
		,
	}

	internal static class CatalogueQuery
	{
		public const string _GetQuery = @"
SELECT image_catalog_id AS ImageCatalogId,
       institution AS Institution,
       catalog_number_1 AS CatalogueNumber1,
       catalog_number_2 AS CatalogueNumber2,
       catalog_side AS CatalogSide,
       url AS Url,
       proxy AS Proxy,
       suffix AS Suffix,
       license AS License,
       filename AS Filename,
       object_id AS ImagedObjectId,
       iaa_edition_catalog_id AS IaaEditionCatalogueId,
       manuscript_id AS ManuscriptId,
       edition_name AS EditionName,
       edition_volume AS EditionVolume,
       edition_location_1 AS EditionLocation1,
       edition_location_2 AS EditionLocation2,
       edition_side AS EditionSide,
       comment AS Comment,
       text_fragment_id AS TextFragmentId,
       name AS Name,
       manuscript_name AS ManuscriptName,
       edition_id AS EditionId,
       iecc1.email AS MatchAuthor,
       iecc1.time AS MatchDate,
       MAX(iecc2.confirmed) AS Confirmed,
       IF(iecc2.confirmed IS NULL, NULL, iecc2.email) AS MatchConfirmationAuthor,
       IF(iecc2.confirmed IS NULL, NULL, iecc2.time) AS MatchConfirmationDate,
       image_text_fragment_match_catalogue.iaa_edition_catalog_to_text_fragment_id AS MatchId
FROM image_text_fragment_match_catalogue
    $Latest
$Where
GROUP BY object_id, catalog_side, text_fragment_id, edition_name, edition_volume, edition_location_1, edition_location_2, edition_side
";

		private const string latestFilter = @"
JOIN (
    SELECT iecttfc.iaa_edition_catalog_to_text_fragment_id, iecttfc.time, iecttfc.confirmed, user.email
    FROM iaa_edition_catalog_to_text_fragment_confirmation AS `iecttfc`
    LEFT JOIN user ON user.user_id = iecttfc.user_id
    WHERE iecttfc.time = (
        SELECT time
        FROM iaa_edition_catalog_to_text_fragment_confirmation
        WHERE iaa_edition_catalog_to_text_fragment_confirmation.iaa_edition_catalog_to_text_fragment_id = iecttfc.iaa_edition_catalog_to_text_fragment_id
        ORDER BY time ASC LIMIT 1
    )
) AS iecc1 ON iecc1.iaa_edition_catalog_to_text_fragment_id = image_text_fragment_match_catalogue.iaa_edition_catalog_to_text_fragment_id
JOIN (
    SELECT iecttfc.iaa_edition_catalog_to_text_fragment_id, iecttfc.time, iecttfc.confirmed, user.email
    FROM iaa_edition_catalog_to_text_fragment_confirmation AS `iecttfc`
    LEFT JOIN user ON user.user_id = iecttfc.user_id
    WHERE iecttfc.time = (
        SELECT time
        FROM iaa_edition_catalog_to_text_fragment_confirmation
        WHERE iaa_edition_catalog_to_text_fragment_confirmation.iaa_edition_catalog_to_text_fragment_id = iecttfc.iaa_edition_catalog_to_text_fragment_id
        ORDER BY time DESC LIMIT 1
    )
) AS iecc2 ON iecc2.iaa_edition_catalog_to_text_fragment_id = image_text_fragment_match_catalogue.iaa_edition_catalog_to_text_fragment_id";

		private const string allFilter =
				@"JOIN SQE.iaa_edition_catalog_to_text_fragment_confirmation AS iecc1 USING(iaa_edition_catalog_to_text_fragment_id)
JOIN SQE.iaa_edition_catalog_to_text_fragment_confirmation AS iecc2 USING(iaa_edition_catalog_to_text_fragment_id)";

		private const string editionFilter =
				"WHERE image_text_fragment_match_catalogue.edition_id = @EditionId";

		private const string imagedObjectFilter =
				"WHERE image_text_fragment_match_catalogue.object_id = @ImagedObjectId";

		private const string textFragmentFilter =
				"WHERE image_text_fragment_match_catalogue.text_fragment_id = @TextFragmentId";

		private const string manuscriptFilter =
				"WHERE image_text_fragment_match_catalogue.manuscript_id = @ManuscriptId";

		private const string matchFilter =
				"WHERE image_text_fragment_match_catalogue.iaa_edition_catalog_to_text_fragment_id = @MatchId";

		public static string GetQuery(CatalogueQueryFilterType filter, bool onlyLatestMatch = true)
		{
			var where = filter switch
						{
								CatalogueQueryFilterType.Edition        => editionFilter
								, CatalogueQueryFilterType.ImagedObject => imagedObjectFilter
								, CatalogueQueryFilterType.TextFragment => textFragmentFilter
								, CatalogueQueryFilterType.Manuscript   => manuscriptFilter
								, CatalogueQueryFilterType.Match        => matchFilter
								, _                                     => ""
								,
						};

			var group = onlyLatestMatch
					? latestFilter
					: allFilter;

			return _GetQuery.Replace("$Where", where).Replace("$Latest", group);
		}
	}

	internal static class FullCatalogueQuery
	{
		internal const string GetQuery = @"
SELECT DISTINCT image_catalog.image_catalog_id AS ImageCatalogId,
       image_catalog.institution AS Institution,
       image_catalog.catalog_number_2 AS CatalogueNumber1,
       image_catalog.catalog_number_2 AS CatalogueNumber2,
       image_catalog.catalog_side AS CatalogSide,
       image_catalog.object_id AS ImagedObjectId,
       image_urls.proxy AS Proxy,
       image_urls.url AS Url,
       SQE_image.filename AS Filename,
       image_urls.suffix AS Suffix,
       image_urls.license AS License,
       iaa_edition_catalog.iaa_edition_catalog_id AS IaaEditionCatalogueId,
       iaa_edition_catalog.manuscript_id AS ManuscriptId,
       iaa_edition_catalog.manuscript AS ManuscriptName,
       iaa_edition_catalog.edition_name AS EditionName,
       iaa_edition_catalog.edition_volume AS EditionVolume,
       iaa_edition_catalog.edition_location_1 AS EditionLocation1,
       iaa_edition_catalog.edition_location_2 AS EditionLocation2,
       iaa_edition_catalog.edition_side AS EditionSide,
       iaa_edition_catalog.comment AS Comment,
       iaa_edition_catalog_to_text_fragment.text_fragment_id AS TextFragmentId,
       text_fragment_data.name AS Name,
       text_fragment_data_owner.edition_id AS EditionId,
       IF(match_rejection.time IS NULL AND match_confirmation.time IS NULL, NULL, IF (match_rejection.time > match_confirmation.time, 0, 1)) AS Confirmed,
       catalog_creator.email AS MatchAuthor,
       IF(match_rejection.time IS NULL AND match_confirmation.time IS NULL, NULL, IF (match_rejection.time > match_confirmation.time, catalog_rejection.email, catalog_confirmation.email)) AS MatchConfirmationAuthor,
       iaa_edition_catalog_to_text_fragment.iaa_edition_catalog_to_text_fragment_id MatchId,
       match_creation.time AS MatchDate,
       IF(match_rejection.time IS NULL AND match_confirmation.time IS NULL, NULL, IF (match_rejection.time > match_confirmation.time, match_rejection.time, match_confirmation.time)) AS MatchConfirmationDate
FROM iaa_edition_catalog_to_text_fragment_confirmation
JOIN iaa_edition_catalog_to_text_fragment USING(iaa_edition_catalog_to_text_fragment_id)
JOIN iaa_edition_catalog USING(iaa_edition_catalog_id)
JOIN iaa_edition_catalog_author USING(iaa_edition_catalog_id)
JOIN text_fragment_data USING(text_fragment_id)
JOIN text_fragment_data_owner USING(text_fragment_data_id)
JOIN edition_editor USING(edition_id)
LEFT JOIN iaa_edition_catalog_to_text_fragment_confirmation AS match_creation
    ON match_creation.iaa_edition_catalog_to_text_fragment_id = iaa_edition_catalog_to_text_fragment_confirmation.iaa_edition_catalog_to_text_fragment_id
    AND (match_creation.confirmed IS NULL)
LEFT JOIN iaa_edition_catalog_to_text_fragment_confirmation AS match_confirmation
    ON match_confirmation.iaa_edition_catalog_to_text_fragment_id = iaa_edition_catalog_to_text_fragment_confirmation.iaa_edition_catalog_to_text_fragment_id
    AND (match_confirmation.confirmed = 1)
LEFT JOIN iaa_edition_catalog_to_text_fragment_confirmation AS match_rejection
    ON match_rejection.iaa_edition_catalog_to_text_fragment_id = iaa_edition_catalog_to_text_fragment_confirmation.iaa_edition_catalog_to_text_fragment_id
    AND (match_rejection.confirmed = 0)
LEFT JOIN user AS catalog_creator ON  catalog_creator.user_id = match_creation.user_id
LEFT JOIN user AS catalog_confirmation ON  catalog_confirmation.user_id = match_confirmation.user_id
LEFT JOIN user AS catalog_rejection ON  catalog_rejection.user_id = match_rejection.user_id
JOIN image_to_iaa_edition_catalog USING(iaa_edition_catalog_id)
JOIN image_catalog USING(image_catalog_id)
LEFT JOIN SQE_image ON SQE_image.image_catalog_id = image_catalog.image_catalog_id
    AND SQE_image.is_master = 1
LEFT JOIN image_urls ON image_urls.image_urls_id = SQE_image.image_urls_id
WHERE edition_editor.user_id = 1
ORDER BY iaa_edition_catalog.manuscript,
         iaa_edition_catalog_to_text_fragment.iaa_edition_catalog_to_text_fragment_id,
         iaa_edition_catalog_to_text_fragment_confirmation.time DESC
";
	}

	internal static class EditionCatalogueQuery
	{
		private const string _GetQuery = @"
SELECT iaa_edition_catalog_id AS IaaEditionCatalogId,
       manuscript AS Manuscript,
       edition_name AS EditionName,
       edition_volume AS EditionVolume,
       edition_location_1 AS EditionLocation1,
       edition_location_2 AS EditionLocation2,
       edition_side AS EditionSide,
       comment AS Comment,
       manuscript_id AS ManuscriptId,
       edition_id AS EditionId
FROM iaa_edition_catalog
    JOIN manuscript_data USING(manuscript_id)
    JOIN SQE.manuscript_data_owner USING(manuscript_data_id)
$Where
";

		public static string GetQuery(
				bool   iaaEditionCatalogId
				, bool manuscript
				, bool editionName
				, bool editionVolume
				, bool editionLocation1
				, bool editionLocation2
				, bool editionSide
				, bool comment
				, bool manuscriptId
				, bool editionId)
		{
			var searchOptions = new List<string>();

			if (iaaEditionCatalogId)
				searchOptions.Add("iaa_edition_catalog_id = @IaaEditionCatalogId");

			if (manuscript)
				searchOptions.Add("manuscript = @Manuscript");

			if (editionName)
				searchOptions.Add("edition_name = @EditionName");

			if (editionVolume)
				searchOptions.Add("edition_volume = @EditionVolume");

			if (editionLocation1)
				searchOptions.Add("edition_location_1 = @EditionLocation1");

			if (editionLocation2)
				searchOptions.Add("edition_location_2 = @EditionLocation2");

			if (editionSide)
				searchOptions.Add("edition_side = @EditionSide");

			if (comment)
				searchOptions.Add("comment = @Comment");

			if (manuscriptId)
				searchOptions.Add("manuscript_id = @ManuscriptId");

			if (editionId)
				searchOptions.Add("edition_id = @EditionId");

			return _GetQuery.Replace(
					"$Where"
					, searchOptions.Any()
							? "WHERE " + string.Join(" AND ", searchOptions)
							: "");
		}
	}

	internal static class EditionCatalogueInsertQuery
	{
		public const string GetQuery = @"
INSERT INTO iaa_edition_catalog (manuscript,
                                 edition_name,
                                 edition_volume,
                                 edition_location_1,
                                 edition_location_2,
                                 edition_side,
                                 comment,
                                 manuscript_id)
SELECT @Manuscript,
      @EditionName,
      @EditionVolume,
      @EditionLocation1,
      @EditionLocation2,
      @EditionSide,
      @Comment,
      manuscript_id
FROM manuscript_data_owner
JOIN manuscript_data USING(manuscript_data_id)
JOIN users_system_roles ON users_system_roles.user_id = @UserId
    AND users_system_roles.system_roles_id = 2
WHERE edition_id = @EditionId
";
	}

	internal static class EditionCatalogueAuthorInsertQuery
	{
		public const string GetQuery = @"
INSERT INTO iaa_edition_catalog_author (iaa_edition_catalog_id, user_id)
SELECT @IaaEditionCatalogId, users_system_roles.user_id
FROM users_system_roles
WHERE users_system_roles.user_id = @UserId
    AND users_system_roles.system_roles_id = 2
    AND NOT EXISTS
  ( SELECT iaa_edition_catalog_id, user_id
    FROM iaa_edition_catalog_author
    WHERE (iaa_edition_catalog_id, user_id) = (@IaaEditionCatalogId, @UserId)
  ) LIMIT 1
";
	}

	internal static class EditionCatalogTextFragmentMatchInsertQuery
	{
		public const string GetQuery = @"
INSERT INTO iaa_edition_catalog_to_text_fragment (iaa_edition_catalog_id, text_fragment_id)
SELECT @IaaEditionCatalogId, @TextFragmentId
FROM users_system_roles
WHERE users_system_roles.user_id = @UserId
    AND users_system_roles.system_roles_id = 2
    AND NOT EXISTS
  ( SELECT iaa_edition_catalog_id, text_fragment_id
    FROM iaa_edition_catalog_to_text_fragment
    WHERE (iaa_edition_catalog_id, text_fragment_id) = (@IaaEditionCatalogId, @TextFragmentId)
  ) LIMIT 1
";
	}

	internal static class EditionCatalogTextFragmentMatchConfirmationInsertQuery
	{
		public const string GetQuery = @"
INSERT INTO iaa_edition_catalog_to_text_fragment_confirmation (iaa_edition_catalog_to_text_fragment_id, user_id, confirmed)
SELECT @IaaEditionCatalogToTextFragmentId, @UserId, @Confirmed
FROM users_system_roles
WHERE users_system_roles.user_id = @UserId
    AND users_system_roles.system_roles_id = 2
";
	}

	internal static class EditionCatalogTextFragmentMatchConfirmationUpdateQuery
	{
		public const string GetQuery = @"
UPDATE iaa_edition_catalog_to_text_fragment_confirmation
JOIN users_system_roles USING(user_id)
    SET iaa_edition_catalog_to_text_fragment_confirmation.user_id = @UserId,
        iaa_edition_catalog_to_text_fragment_confirmation.confirmed = @Confirmed
WHERE iaa_edition_catalog_to_text_fragment_id = @IaaEditionCatalogToTextFragmentId
    AND (iaa_edition_catalog_to_text_fragment_confirmation.user_id != @UserId
        AND iaa_edition_catalog_to_text_fragment_confirmation.confirmed != @Confirmed)
    AND users_system_roles.user_id = @UserId
    AND users_system_roles.system_roles_id = 2
";
	}

	internal static class EditionCatalogImageCatalogMatchInsertQuery
	{
		public const string GetQuery = @"
INSERT INTO SQE.image_to_iaa_edition_catalog (iaa_edition_catalog_id, image_catalog_id)
SELECT @IaaEditionCatalogId, image_catalog_id
FROM image_catalog
JOIN users_system_roles ON users_system_roles.user_id = @UserId
    AND users_system_roles.system_roles_id = 2
WHERE object_id = @ImagedObjectId
  AND image_catalog.catalog_side = @Side
  AND NOT EXISTS
  ( SELECT iaa_edition_catalog_id, image_catalog_id
    FROM image_to_iaa_edition_catalog
    JOIN image_catalog USING(image_catalog_id)
    WHERE (iaa_edition_catalog_id, object_id) = (@IaaEditionCatalogId, @ImagedObjectId)
  ) LIMIT 1
";
	}
}
