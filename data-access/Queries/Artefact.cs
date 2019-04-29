using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal class ArtefactQuery
    {
        private const string _getArtefact = @"select artefact.artefact_id As Id,
artefact_data.name As Name,
artefact_data_owner.scroll_version_id As scrollVersionId,
artefact_position.transform_matrix As transformMatrix,
artefact_position.z_index As zOrder
from artefact 
join artefact_data using(artefact_id)
join artefact_data_owner using(artefact_data_id)
join artefact_position using (artefact_id)
where artefact.artefact_id = @artefactId";

        public static string GetArtefact(bool scrollVersionId)
        {
            if(!scrollVersionId)
                return _getArtefact;
            StringBuilder str = new StringBuilder(_getArtefact);
            str.Append(" and artefact_data_owner.scroll_version_id=@ScrollVersionId");
            return str.ToString();
        }
    }

    public static class ArtefactNamesOfEditionQuery
    {
        private const string _getArtefact = @"
SELECT artefact_data.name, 
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
ORDER BY image_catalog.catalog_number_1, image_catalog.catalog_number_2, image_catalog.catalog_side";

        public static string GetQuery(uint? userId)
        {
            return _getArtefact.Replace("$Restriction", userId.HasValue 
                ? "(edition_editor.user_id = @UserID OR edition_editor.user_id = 1)"
                : "edition_editor.user_id = 1");
        }

        public class Result
        {
            public  uint artefact_id { get; set; }
            public string name { get; set; }
            public string institution { get; set; }
            public string catalog_number_1 { get; set; }
            public string catalog_number_2 { get; set; }
            public byte catalog_side { get; set; }
            public uint image_catalog_id { get; set; }
        }
    }
}
