namespace SQE.SqeHttpApi.DataAccess.Queries
{
    public static class ArtefactOfEditionQuery
    {
        private const string _artefactIdRestriction = " AND artefact_shape.artefact_id = @ArtefactId";
        public static string GetQuery(uint? userId, bool mask = false)
        {
            return ArtefactsOfEditionQuery.GetQuery(userId, mask, ordered: false) + _artefactIdRestriction;
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
       artefact_position.transform_matrix AS TransformMatrix,
       artefact_position.z_index AS ZIndex,
       artefact_position_owner.edition_editor_id AS TransformMatrixEditorId,
       image_catalog.object_id AS ImagedObjectId,
       image_catalog.catalog_side AS CatalogSide, 
       SQE_image.image_catalog_id AS ImageCatalogId
FROM edition
JOIN edition_editor USING(edition_id)
JOIN artefact_shape_owner USING(edition_id)
JOIN artefact_shape USING(artefact_shape_id)
LEFT JOIN artefact_position USING(artefact_id)
LEFT JOIN artefact_position_owner ON artefact_position.artefact_position_id = artefact_position_owner.artefact_position_id
    AND edition.edition_id = artefact_position_owner.edition_id
JOIN artefact_data USING(artefact_id)
JOIN artefact_data_owner ON artefact_data.artefact_data_id = artefact_data_owner.artefact_data_id
    AND edition.edition_id = artefact_data_owner.edition_id
JOIN SQE_image USING(sqe_image_id)
JOIN image_catalog USING(image_catalog_id)

WHERE edition.edition_id = @EditionId
   AND (
        (artefact_position_owner.edition_id IS NULL AND artefact_position.transform_matrix IS NULL) OR
        (artefact_position_owner.edition_id IS NOT NULL AND artefact_position.transform_matrix IS NOT NULL)
   ) 
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
}
