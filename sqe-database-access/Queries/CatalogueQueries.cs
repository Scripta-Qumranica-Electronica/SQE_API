using System;
using System.Collections.Generic;
using System.Linq;

namespace SQE.DatabaseAccess.Queries
{
    enum CatalogueQueryFilterType
    {
        Edition,
        ImagedObject,
        TextFragment,
        Manuscript
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
       iaa_edition_catalog_to_text_fragment_confirmation.confirmed AS Confirmed,
       user.email AS MatchAuthor,
       iaa_edition_catalog_to_text_fragment_confirmation.time AS Date
FROM image_text_fragment_match_catalogue
    JOIN SQE.iaa_edition_catalog_to_text_fragment_confirmation USING(iaa_edition_catalog_to_text_fragment_id)
    LEFT JOIN user USING(user_id)
$Where
$Group
";

        private const string latestFilter = @"
GROUP BY iaa_edition_catalog_to_text_fragment_confirmation.iaa_edition_catalog_to_text_fragment_id
            HAVING MAX(iaa_edition_catalog_to_text_fragment_confirmation.time)";

        private const string editionFilter = "WHERE image_text_fragment_match_catalogue.edition_id = @EditionId";
        private const string imagedObjectFilter = "WHERE image_text_fragment_match_catalogue.object_id = @ImagedObjectId";
        private const string textFragmentFilter = "WHERE image_text_fragment_match_catalogue.text_fragment_id = @TextFragmentId";
        private const string manuscriptFilter = "WHERE image_text_fragment_match_catalogue.manuscript_id = @ManuscriptId";
        public static string GetQuery(CatalogueQueryFilterType filter, bool onlyLatestMatch = true)
        {
            var where = filter switch
            {
                CatalogueQueryFilterType.Edition => editionFilter,
                CatalogueQueryFilterType.ImagedObject => imagedObjectFilter,
                CatalogueQueryFilterType.TextFragment => textFragmentFilter,
                CatalogueQueryFilterType.Manuscript => manuscriptFilter,
                _ => ""
            };
            var group = onlyLatestMatch ? latestFilter : "";

            return _GetQuery.Replace("$Where", where).Replace("$Group", group);
        }
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

        public static string GetQuery(bool iaaEditionCatalogId, bool manuscript, bool editionName, bool editionVolume,
            bool editionLocation1, bool editionLocation2, bool editionSide, bool comment, bool manuscriptId, bool editionId)
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
            return _GetQuery.Replace("$Where", searchOptions.Any() ? "WHERE " + string.Join(" AND ", searchOptions) : "");
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
WHERE edition_id = @EditionId
";
    }

    internal static class EditionCatalogueAuthorInsertQuery
    {
        public const string GetQuery = @"
INSERT INTO iaa_edition_catalog_author (iaa_edition_catalog_id, user_id) 
SELECT @IaaEditionCatalogId, @UserId
FROM dual
WHERE NOT EXISTS
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
FROM dual
WHERE NOT EXISTS
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
FROM dual
WHERE NOT EXISTS
  ( SELECT iaa_edition_catalog_to_text_fragment_id, user_id, confirmed
    FROM iaa_edition_catalog_to_text_fragment_confirmation
    WHERE (iaa_edition_catalog_to_text_fragment_id, user_id, confirmed) = (@IaaEditionCatalogToTextFragmentId, @UserId, @Confirmed)
  ) LIMIT 1
";
    }

    internal static class EditionCatalogImageCatalogMatchInsertQuery
    {
        public const string GetQuery = @"
INSERT INTO SQE.image_to_iaa_edition_catalog (iaa_edition_catalog_id, image_catalog_id)
SELECT @IaaEditionCatalogId, image_catalog_id
FROM image_catalog
WHERE object_id = @ImagedObjectId AND NOT EXISTS
  ( SELECT iaa_edition_catalog_id, image_catalog_id
    FROM image_to_iaa_edition_catalog
    JOIN image_catalog USING(image_catalog_id)
    WHERE (iaa_edition_catalog_id, object_id) = (@IaaEditionCatalogId, @ImagedObjectId)
  ) LIMIT 1
";
    }
}