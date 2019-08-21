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
					transformMatrix = artefact.TransformMatrix,
					transformMatrixEditorId = artefact.TransformMatrixEditorId
				},

				imagedObjectId = artefact.ImagedObjectId,

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