using System.Collections.Generic;
using System.Linq;
using SQE.SqeApi.DataAccess.Models;
using SQE.SqeApi.Server.DTOs;

namespace SQE.SqeApi.Server.Helpers
{
    public static class ArtefactDTOTransformer
    {
        public static ArtefactDTO QueryArtefactToArtefactDTO(ArtefactModel artefact, uint editionId)
        {
            return new ArtefactDTO()
            {
                id = artefact.ArtefactId,
                editionId = editionId,
                mask = new PolygonDTO()
                {
                    mask = artefact.Mask,
                    transformMatrix = artefact.TransformMatrix
                },

                imagedObjectId = artefact.ImagedObjectId,

                name = artefact.Name,
                side = artefact.CatalogSide == 0 ? ArtefactDTO.ArtefactSide.recto : ArtefactDTO.ArtefactSide.verso, 
                zOrder = artefact.ZIndex,
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
