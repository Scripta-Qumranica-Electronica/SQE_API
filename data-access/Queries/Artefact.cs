using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.Backend.DataAccess.Queries
{
    internal class ArtefactQuery
    {
        private static string _getArtefact = @"select artefact.artefact_id As Id,
artefact_data.name As Name,
artefact_data_owner.scroll_version_id As scrollVersionId,
artefact_position.transform_matrix As transformMatrix,
artefact_position.z_index As zOrder
from artefact 
join artefact_data using(artefact_id)
join artefact_data_owner using(artefact_data_id)
join artefact_position using (artefact_id)
where artefact.artefact_id = @artefactId";

        public static string GetArtefact()
        {
            return _getArtefact;
        }
    }
}
