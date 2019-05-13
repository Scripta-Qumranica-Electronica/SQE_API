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
       $Mask as Mask,
       artefact_shape.artefact_id AS ArtefactId,
       artefact_position.transform_matrix AS TransformMatrix,
       artefact_position.z_index AS ZIndex,
       image_catalog.institution AS Institution, 
       image_catalog.catalog_number_1 AS CatalogNumber1, 
       image_catalog.catalog_number_2 AS CatalogNumber2, 
       image_catalog.catalog_side AS CatalogSide, 
       SQE_image.image_catalog_id AS ImageCatalogId
FROM SQE_image
JOIN image_catalog USING(image_catalog_id)
JOIN artefact_shape USING(sqe_image_id)
JOIN artefact_shape_owner USING(artefact_shape_id)
JOIN artefact_data USING(artefact_id)
JOIN artefact_data_owner USING(artefact_data_id)
JOIN artefact_position USING(artefact_id)
JOIN artefact_position_owner USING(artefact_position_id)
JOIN edition ON edition.edition_id = artefact_shape_owner.edition_id
    AND edition.edition_id = artefact_data_owner.edition_id
    AND edition.edition_id = artefact_position_owner.edition_id
JOIN edition_editor ON edition_editor.edition_id = edition.edition_id
WHERE edition.edition_id = @EditionId
  AND $Restriction
$Order";

        private const string _userRestriction = "(edition_editor.user_id = @UserID OR edition_editor.user_id = 1)";
        private const string _publicRestriction = "edition_editor.user_id = 1";
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
            SELECT $Table_id
            FROM $Table
            JOIN $Table_owner USING($Table_id)
            WHERE $Table.artefact_id = @ArtefactId
                AND $Table_owner.edition_id = @EditionId
            ";

        public static string GetQuery(string table)
        {
            return _getQuery.Replace("$Table", table);
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
