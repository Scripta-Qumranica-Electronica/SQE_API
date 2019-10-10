using System.Collections.Generic;
using System.Linq;
using SQE.API.DTO;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Helpers
{
    public static class ArtefactDTOTransformer
    {
        public static ArtefactDTO QueryArtefactToArtefactDTO(ArtefactModel artefact, uint editionId)
        {
            return new ArtefactDTO
            {
                id = artefact.ArtefactId,
                editionId = editionId,
                imageId = artefact.ImageId,
                artefactDataEditorId = artefact.ArtefactDataEditorId,
                mask = new PolygonDTO
                {
                    mask = artefact.Mask,
                    maskEditorId = artefact.MaskEditorId,
                    transformation = new TransformationDTO
                    {
                        scale = artefact.Scale,
                        rotate = artefact.Rotate,
                        translate = artefact.TranslateX.HasValue && artefact.TranslateY.HasValue
                            ? new TranslateDTO
                            {
                                x = artefact.TranslateX.Value,
                                y = artefact.TranslateY.Value
                            }
                            : null
                    },
                    positionEditorId = artefact.PositionEditorId
                },

                imagedObjectId = artefact.ImagedObjectId,
                statusMessage = artefact.WorkStatusMessage,

                name = artefact.Name,
                side = artefact.CatalogSide == 0 ? ArtefactDTO.ArtefactSide.recto : ArtefactDTO.ArtefactSide.verso,
                zOrder = artefact.ZIndex
            };
        }

        public static ArtefactListDTO QueryArtefactListToArtefactListDTO(List<ArtefactModel> artefacts, uint editionId)
        {
            return new ArtefactListDTO
            {
                artefacts = artefacts.Select(x => QueryArtefactToArtefactDTO(x, editionId)).ToList()
            };
        }
    }
}