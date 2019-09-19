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
						translate = artefact.TranslateX != null && artefact.TranslateY != null
							? new TranslateDTO
							{
								// These are stored as unsigned ints in the DB, convert them to signed ints here
								translateX = (int)artefact.TranslateX - 2147483647,
								translateY = (int)artefact.TranslateY - 2147483647
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