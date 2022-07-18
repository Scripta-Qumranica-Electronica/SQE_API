using System.Collections.Generic;
using System.Linq;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Serialization
{
	public static partial class ExtensionsDTO
	{
		public static SignInterpretationRoiData
				ToSignInterpretationRoiData(this SetInterpretationRoiDTO x)
			=> new SignInterpretationRoiData
			{
					SignInterpretationId = x.signInterpretationId
					, ArtefactId = x.artefactId
					, Exceptional = x.exceptional
					, TranslateX = x.translate.x
					, TranslateY = x.translate.y
					, Shape = GeometryValidation.ValidatePolygon(x.shape, "roi")
					, ValuesSet = x.valuesSet
					, StanceRotation = x.stanceRotation
					,
			};

		public static SignInterpretationRoiData
				ToSignInterpretationRoiData(this UpdateInterpretationRoiDTO x)
			=> new SignInterpretationRoiData
			{
					SignInterpretationId = x.signInterpretationId
					, SignInterpretationRoiId = x.interpretationRoiId
					, ArtefactId = x.artefactId
					, Exceptional = x.exceptional
					, TranslateX = x.translate.x
					, TranslateY = x.translate.y
					, Shape = GeometryValidation.ValidatePolygon(x.shape, "roi")
					, ValuesSet = x.valuesSet
					, StanceRotation = x.stanceRotation
					,
			};

		// public static SignInterpretationRoiData ToSignInterpRoiData(this InterpretationRoiDTO x)
		// {
		//     return new SignInterpretationRoiData
		//     {
		//         SignInterpretationId = x.signInterpretationId,
		//         SignInterpretationRoiId = x.interpretationRoiId,
		//         ArtefactId = x.artefactId,
		//         Exceptional = x.exceptional,
		//         TranslateX = x.translate.x,
		//         TranslateY = x.translate.y,
		//         Shape = GeometryValidation.ValidatePolygon(x.shape, "roi"),
		//         ValuesSet = x.valuesSet,
		//         StanceRotation = x.stanceRotation,
		//     };
		// }

		public static IEnumerable<SignInterpretationRoiData> ToSignInterpretationRoiData(
				this IEnumerable<SetInterpretationRoiDTO> x)
		{
			return x.Select(x => x.ToSignInterpretationRoiData());
		}

		public static IEnumerable<SignInterpretationRoiData> ToSignInterpretationRoiData(
				this IEnumerable<UpdateInterpretationRoiDTO> x)
		{
			return x.Select(x => x.ToSignInterpretationRoiData());
		}

		public static UpdatedInterpretationRoiDTO ToUpdateDTO(this SignInterpretationRoiData x)
			=> new UpdatedInterpretationRoiDTO
			{
					artefactId = x.ArtefactId.GetValueOrDefault()
					, editorId = x.SignInterpretationRoiEditorId.GetValueOrDefault()
					, creatorId = x.SignInterpretationRoiCreatorId.GetValueOrDefault()
					, exceptional = x.Exceptional.GetValueOrDefault()
					, interpretationRoiId = x.SignInterpretationRoiId.GetValueOrDefault()
					, oldInterpretationRoiId = x.OldSignInterpretationRoiId
					, signInterpretationId = x.SignInterpretationId.GetValueOrDefault()
					, translate =
							new TranslateDTO
							{
									x = x.TranslateX.GetValueOrDefault()
									, y = x.TranslateY.GetValueOrDefault()
									,
							}
					, shape = x.Shape
					, valuesSet = x.ValuesSet.GetValueOrDefault()
					, stanceRotation = x.StanceRotation.GetValueOrDefault()
					,
			};

		public static IEnumerable<UpdatedInterpretationRoiDTO> ToUpdateDTO(
				this IEnumerable<SignInterpretationRoiData> x)
		{
			return x.Select(x => x.ToUpdateDTO());
		}

		// private static SignInterpretationRoiData ToSignInterpretationRoiData(InterpretationRoiDTO x)
		// {
		//     return new SignInterpretationRoiData
		//     {
		//         SignInterpretationRoiId = x.interpretationRoiId,
		//         SignInterpretationId = x.signInterpretationId,
		//         ArtefactId = x.artefactId,
		//         Exceptional = x.exceptional,
		//         TranslateX = x.translate.x,
		//         TranslateY = x.translate.y,
		//         Shape = GeometryValidation.ValidatePolygon(x.shape, "roi"),
		//         ValuesSet = x.valuesSet,
		//         StanceRotation = x.stanceRotation
		//     };
		// }
		//
		// public static IEnumerable<SignInterpretationRoiData> ToSignInterpretationRoiData(this IEnumerable<InterpretationRoiDTO> x)
		// {
		//     return x.Select(x => x.ToSignInterpRoiData());
		// }

		public static UpdateInterpretationRoiDTO
				ToUpdateInterpretationRoiDTO(this SetInterpretationRoiDTO x, uint roiId)
			=> new UpdateInterpretationRoiDTO
			{
					artefactId = x.artefactId
					, interpretationRoiId = roiId
					, signInterpretationId = x.signInterpretationId
					, exceptional = x.exceptional
					, valuesSet = x.valuesSet
					, translate = x.translate
					, shape = GeometryValidation.ValidatePolygon(x.shape, "roi")
					, stanceRotation = x.stanceRotation
					,
			};

		public static Dictionary<uint, ReconstructedRoi> toModel(
				this Dictionary<uint, SetReconstructedInterpretationRoiDTO> dict)
			=> dict.ToDictionary(
					x => x.Key
					, x => new ReconstructedRoi
					{
							Shape = x.Value.shape
							, TranslateX = x.Value.translate.x
							, TranslateY = x.Value.translate.y
							,
					});

		public static Dictionary<uint, SetReconstructedInterpretationRoiDTO> FromDTO(
				this IEnumerable<IndexedReplacementTextRoi> rois)
			=> rois.ToDictionary(x => x.index, x => x.roi);
	}
}
