using System.Collections.Generic;
using System.Linq;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.SqeHttpApi.Server.Helpers
{
    public static class ArtefactDTOTransformer
    {
        public static ArtefactDTO QueryArtefactToArtefactDTO(ArtefactModel artefact, uint editionId)
        {
            return new ArtefactDTO()
            {
                id = artefact.artefactId,
                editionId = editionId,
                mask = new PolygonDTO()
                {
                    mask = artefact.mask,
                    transformMatrix = artefact.transformMatrix
                },

                imagedObjectId = ImagedObjectIdFormat.Serialize(artefact.institution, artefact.catalogNumber1, artefact.catalogNumber2),

                name = artefact.name,
                side = artefact.catalogSide == 0 ? ArtefactDTO.ArtefactSide.recto : ArtefactDTO.ArtefactSide.verso, 
                zOrder = artefact.zIndex,
            };
        }
        
        public static ArtefactListDTO QueryArtefactListToArtefactListDTO(List<ArtefactModel> artefacts, uint editionId)
        {
            return new ArtefactListDTO()
            {
                artefacts = artefacts.Select(x => QueryArtefactToArtefactDTO(x, editionId)).ToList()
            };
        }
    }
}
