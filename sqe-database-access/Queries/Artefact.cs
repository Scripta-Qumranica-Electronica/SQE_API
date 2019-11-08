namespace SQE.DatabaseAccess.Queries
{
    public static class ArtefactOfEditionQuery
    {
        private const string _artefactIdRestriction = " AND artefact_shape.artefact_id = @ArtefactId";

        public static string GetQuery(uint? userId, bool mask = false)
        {
            return ArtefactsOfEditionQuery.GetQuery(userId, mask, false) + _artefactIdRestriction;
        }
    }

    public static class ArtefactsOfEditionQuery
    {
        private const string _getArtefact = @"
SELECT artefact_data.name AS Name, 
       artefact_data_owner.edition_editor_id AS ArtefactDataEditorId,
       $Mask as Mask,
       artefact_shape.artefact_id AS ArtefactId,
       artefact_shape.sqe_image_id AS ImageId,
       artefact_shape_owner.edition_editor_id AS MaskEditorId,
       artefact_position.scale AS Scale,
       artefact_position.rotate AS Rotate,
       artefact_position.translate_x AS TranslateX,
       artefact_position.translate_y AS TranslateY,
       artefact_position.z_index AS ZIndex,
       artefact_position_owner.edition_editor_id AS PositionEditorId,
       image_catalog.object_id AS ImagedObjectId,
       image_catalog.catalog_side AS CatalogSide, 
       SQE_image.image_catalog_id AS ImageCatalogId,
       work_status.work_status_message AS WorkStatusMessage
FROM artefact_shape_owner
    JOIN edition USING(edition_id)
	JOIN edition_editor USING(edition_id)
	JOIN artefact_shape USING(artefact_shape_id)
	
	LEFT JOIN artefact_position ON artefact_position.artefact_id = artefact_shape.artefact_id
	LEFT JOIN artefact_position_owner ON artefact_position_owner.artefact_position_id =  artefact_position.artefact_position_id AND artefact_position_owner.edition_id = 1646
	
	LEFT JOIN artefact_status ON artefact_status.artefact_id = artefact_shape.artefact_id
	LEFT JOIN artefact_status_owner ON artefact_status_owner.artefact_status_id = artefact_status.artefact_status_id AND artefact_status_owner.edition_id = 1646
	
	JOIN artefact_data ON artefact_data.artefact_id = artefact_shape.artefact_id
	JOIN artefact_data_owner ON artefact_data.artefact_data_id = artefact_data_owner.artefact_data_id
	    AND edition.edition_id = artefact_data_owner.edition_id
	
	LEFT JOIN SQE_image USING(sqe_image_id)
	JOIN image_catalog USING(image_catalog_id)

WHERE artefact_shape_owner.edition_id = @EditionId
  AND $Restriction
$Order";

        private const string _userRestriction = "(edition_editor.user_id = @UserID OR edition.public = 1)";
        private const string _publicRestriction = "edition.public = 1";
        private const string _mask = "ASTEXT(artefact_shape.region_in_sqe_image)";

        private const string _order =
            "ORDER BY image_catalog.catalog_number_1, image_catalog.catalog_number_2, image_catalog.catalog_side";

        public static string GetQuery(uint? userId, bool mask = false, bool ordered = true)
        {
            return _getArtefact
                .Replace("$Restriction", userId.HasValue ? _userRestriction : _publicRestriction)
                .Replace("$Mask", mask ? _mask : "\"\"")
                .Replace("$Order", ordered ? _order : "");
        }
    }

    public static class FindArtefactComponentId
    {
        private const string _getQuery = @"
            SELECT DISTINCT $Table_id
            FROM $Table
            JOIN $Table_owner USING($Table_id)
            WHERE $Where
                AND $Table_owner.edition_id = @EditionId
            ";

        private const string _normalArt = "$Table.artefact_id = @ArtefactId";
        private const string _artStack = "$Table.artefact_A_id = @ArtefactId OR $Table.artefact_B_id = @ArtefactId";

        public static string GetQuery(string table, bool stack = false)
        {
            return _getQuery.Replace("$Where", stack ? _artStack : _normalArt)
                .Replace("$Table", table);
        }
    }

    internal static class FindArtefactShapeSqeImageId
    {
        public const string GetQuery = @"
        SELECT sqe_image_id
            FROM artefact_shape
            JOIN artefact_shape_owner USING(artefact_shape_id)
            WHERE artefact_shape.artefact_id = @ArtefactId
                AND artefact_shape_owner.edition_id = @EditionId";
    }

    internal static class FindArtefactTextFragments
    {
        public const string GetQuery = @"
SELECT DISTINCT text_fragment_id AS TextFragmentId, 
       text_fragment_data.name AS TextFragmentName, 
       text_fragment_data_owner.edition_editor_id AS EditionEditorId
FROM roi_position
JOIN sign_interpretation_roi USING(roi_position_id)
JOIN sign_interpretation_roi_owner ON sign_interpretation_roi_owner.sign_interpretation_roi_id = sign_interpretation_roi.sign_interpretation_roi_id
    AND sign_interpretation_roi_owner.edition_id = @EditionId
JOIN sign_interpretation USING(sign_interpretation_id)
JOIN line_to_sign USING(sign_id)
JOIN line_to_sign_owner ON line_to_sign_owner.line_to_sign_id = line_to_sign.line_to_sign_id
    AND line_to_sign_owner.edition_id = @EditionId
JOIN text_fragment_to_line USING(line_id)
JOIN text_fragment_to_line_owner ON text_fragment_to_line_owner.text_fragment_to_line_id = text_fragment_to_line.text_fragment_to_line_id
    AND text_fragment_to_line_owner.edition_id = @EditionId
JOIN text_fragment_data USING(text_fragment_id)
JOIN text_fragment_data_owner ON text_fragment_data_owner.text_fragment_data_id = text_fragment_data.text_fragment_data_id
    AND text_fragment_data_owner.edition_id = @EditionId
JOIN edition ON edition.edition_id = @EditionId
JOIN edition_editor ON edition_editor.edition_id = @EditionId
WHERE artefact_id = @ArtefactId
    AND (edition.public = 1 OR edition_editor.user_id = @UserId)
";
    }

    internal static class FindSuggestedArtefactTextFragments
    {
        public const string GetQuery = @"
SELECT text_fragment_id AS TextFragmentId, 
       text_fragment_data.name AS TextFragmentName, 
       text_fragment_data_owner.edition_editor_id AS EditionEditorId
FROM artefact_shape
JOIN artefact_shape_owner ON artefact_shape.artefact_shape_id = artefact_shape_owner.artefact_shape_id
   AND artefact_shape_owner.edition_id = @EditionId
JOIN SQE_image USING(sqe_image_id)
JOIN image_to_iaa_edition_catalog USING(image_catalog_id)
JOIN iaa_edition_catalog_to_text_fragment USING(iaa_edition_catalog_id)
JOIN text_fragment_data USING(text_fragment_id)
JOIN text_fragment_data_owner ON text_fragment_data.text_fragment_data_id = text_fragment_data_owner.text_fragment_data_id
   AND text_fragment_data_owner.edition_id = @EditionId
JOIN edition ON edition.edition_id = @EditionId
JOIN edition_editor ON edition_editor.edition_id = @EditionId
WHERE artefact_id = @ArtefactId
   AND (edition.public = 1 OR edition_editor.user_id = @UserId)
";
    }
}