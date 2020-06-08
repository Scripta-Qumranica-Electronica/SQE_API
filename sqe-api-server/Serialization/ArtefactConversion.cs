using System.Collections.Generic;
using System.Linq;
using SQE.API.DTO;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Serialization
{
    public static partial class ExtensionsDTO
    {
        public static ArtefactDTO ToDTO(this ArtefactModel artefact, uint editionId)
        {
            return new ArtefactDTO
            {
                id = artefact.ArtefactId,
                editionId = editionId,
                imageId = artefact.ImageId,
                artefactDataEditorId = artefact.ArtefactDataEditorId,
                mask = artefact.Mask,
                artefactMaskEditorId = artefact.MaskEditorId,
                isPlaced = artefact.TranslateX.HasValue && artefact.TranslateY.HasValue,
                placement = new PlacementDTO
                {
                    // Always provide a PlacementDTO, but if the values were null, substitute the defaults
                    scale = artefact.Scale ?? 1,
                    rotate = artefact.Rotate ?? 0,
                    zIndex = artefact.ZIndex ?? 0,

                    // Always include a TranslateDTO (x and y are nullable uints as in the database)
                    translate = new TranslateDTO
                    {
                        x = artefact.TranslateX,
                        y = artefact.TranslateY
                    }
                },
                artefactPlacementEditorId = artefact.PositionEditorId,

                imagedObjectId = artefact.ImagedObjectId,
                statusMessage = artefact.WorkStatusMessage,

                name = artefact.Name,
                side = artefact.CatalogSide == 0 ? ArtefactDTO.ArtefactSide.recto : ArtefactDTO.ArtefactSide.verso,
            };
        }


    }

    public static class ArtefactListSerializationDTO
    {
        public static ArtefactListDTO QueryArtefactListToArtefactListDTO(List<ArtefactModel> artefacts, uint editionId)
        {
            return new ArtefactListDTO()
            {
                artefacts = artefacts.Select(x => x.ToDTO(editionId)).ToList()
            };
        }
    }
}