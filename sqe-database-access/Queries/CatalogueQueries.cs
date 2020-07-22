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
}