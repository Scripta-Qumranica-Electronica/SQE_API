using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.Backend.DataAccess.Queries
{
    internal class ArtefactQueries
    {
        private static string _getArtefact = @"select 
artefact.artefact_id As Id,
artefact_data.name As Name,
artefact_data_owner.scroll_version_id As scrollVersionId,
artefact_position.transform_matrix As transformMatrix,
artefact_position.z_index As zOrder,
image_catalog.catalog_number_1,
image_catalog.catalog_number_2,
image_catalog.institution
from artefact 
join artefact_data using(artefact_id)
join artefact_data_owner using(artefact_data_id)
join artefact_position using (artefact_id)
join artefact_shape using (artefact_id)
join scroll_version using (scroll_version_id)
join image_catalog where image_catalog.image_catalog_id = (select SQE_image.image_catalog_id from SQE_image where artefact_shape.id_of_sqe_image = SQE_image.sqe_image_id)
and scroll_version.user_id = @userID";

        public static string GetArtefactQuery(uint? scrollVersionId, int? artefactId, string fragmentId)
        {
            StringBuilder str = new StringBuilder(_getArtefact);
            if (scrollVersionId != null)
            {
                str.Append(" and scroll_version.scroll_version_id = @scrollVersionId");
            }
            if (artefactId != null)
            {
                str.Append(" and artefact.artefact_id = @Id");
            }
            if(fragmentId != null) //TODO check with empty fragemnt id
            {
                str.Append(" and image_catalog.catalog_number_1 = @Catalog1 and image_catalog.catalog_number_2 = @Catalog2 and image_catalog.institution = @Institution");
            }
            return str.ToString();
        }

        internal class Result
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int scrollVersionId { get; set; }
            public string transformMatrix { get; set; }
            public string zOrder { get; set; }
            public string catalog_number_1 { get; set; }
            public string catalog_number_2 { get; set; }
            public string institution { get; set; }
        }
    }
}
