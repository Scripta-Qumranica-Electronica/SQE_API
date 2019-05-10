using System.Collections.Generic;
using System.Linq;
using SQE.SqeHttpApi.DataAccess.Queries;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.SqeHttpApi.Server.Services
{
    public static class ArtefactDTOTransform
    {
        public static ArtefactDTO QueryArtefactToArtefactDTO(ArtefactsOfEditionQuery.Result artefact, uint editionId)
        {
            return new ArtefactDTO()
            {
                id = artefact.artefact_id,
                editionId = editionId,
                mask = new PolygonDTO()
                {
                    mask = artefact.mask,
                    transformMatrix = ""
                },

                imagedObjectId = ImagedObjectIdFormat.Serialize(artefact.institution, artefact.catalog_number_1, artefact.catalog_number_2),

                name = artefact.name,
                side = artefact.catalog_side == 0 ? ArtefactDTO.ArtefactSide.recto : ArtefactDTO.ArtefactSide.verso, 
                zOrder = 0,
            };
        }
        
        public static ArtefactListDTO QueryArtefactListToArtefactListDTO(List<ArtefactsOfEditionQuery.Result> artefacts, uint editionId)
        {
            return new ArtefactListDTO()
            {
                artefacts = artefacts.Select(x => QueryArtefactToArtefactDTO(x, editionId)).ToList()
            };
        }
    }
}