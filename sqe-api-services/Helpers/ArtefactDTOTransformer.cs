using System.Collections.Generic;
using System.Linq;
using SQE.API.DATA.Models;
using SQE.API.DTO;

namespace SQE.API.SERVICES.Helpers
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
					scale = artefact.Scale,
					rotate = artefact.Rotate,
					translateX = artefact.TranslateX,
					translateY = artefact.TranslateY,
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