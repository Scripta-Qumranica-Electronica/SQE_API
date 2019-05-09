using System;
using System.Collections.Generic;
using System.Text;
using MySqlX.XDevAPI.Common;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    public static class ArtefactOfEditionQuery
    {
        private const string _artefactIdRestriction = " AND artefact_shape.artefact_id = @ArtefactId";
        public static string GetQuery(uint? userId, bool mask = false)
        {
            return ArtefactsOfEditionQuery.GetQuery(userId, mask, ordered: false) + _artefactIdRestriction;
        }
        
        public class Result : ArtefactsOfEditionQuery.Result{}
    }

    public static class ArtefactsOfEditionQuery
    {
        private const string _getArtefact = @"
SELECT artefact_data.name, 
       $Mask as mask,
       artefact_shape.artefact_id, 
       image_catalog.institution, 
       image_catalog.catalog_number_1, 
       image_catalog.catalog_number_2, 
       image_catalog.catalog_side, 
       SQE_image.image_catalog_id
FROM SQE_image
JOIN image_catalog USING(image_catalog_id)
JOIN artefact_shape USING(sqe_image_id)
JOIN artefact_shape_owner USING(artefact_shape_id)
JOIN artefact_data USING(artefact_id)
JOIN artefact_data_owner USING(artefact_data_id)
JOIN edition ON edition.edition_id = artefact_shape_owner.edition_id
    AND edition.edition_id = artefact_data_owner.edition_id
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

        public class Result
        {
            public  uint artefact_id { get; set; }
            public string name { get; set; }
            public string mask { get; set; }
            public string institution { get; set; }
            public string catalog_number_1 { get; set; }
            public string catalog_number_2 { get; set; }
            public byte catalog_side { get; set; }
            public uint image_catalog_id { get; set; }
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

    public static class FindArtefactShapeSqeImageId
    {
        public const string GetQuery = @"
        SELECT sqe_image_id
            FROM artefact_shape
            JOIN artefact_shape_owner USING(artefact_shape_id)
            WHERE artefact_shape.artefact_id = @ArtefactId
                AND artefact_shape_owner.edition_id = @EditionId";
    }
}
