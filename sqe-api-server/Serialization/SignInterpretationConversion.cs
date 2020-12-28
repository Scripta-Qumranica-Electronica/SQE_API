using System.Collections.Generic;
using System.Linq;
using SQE.API.DTO;
using SQE.DatabaseAccess.Models;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.API.Server.Serialization
{
	public static partial class ExtensionsDTO
	{
		public static AttributeListDTO ToDTO(
				this IEnumerable<SignInterpretationAttributeEntry> attributes)
		{
			return new AttributeListDTO
			{
					attributes = attributes.GroupBy(
												   attr => (attr.AttributeId, attr.AttributeName
															, attr.AttributeDescription
															, attr.AttributeCreator
															, attr.AttributeEditor, attr.Editable
															, attr.Removable
															, attr.Repeatable, attr.BatchEditable)
												   , attr => new AttributeValueDTO
												   {
														   id = attr.AttributeValueId
														   , value = attr.AttributeStringValue
														   , cssDirectives = attr.Css
														   , description =
																   attr
																		   .AttributeStringValueDescription
														   , creatorId = attr.AttributeValueCreator
														   , editorId = attr.AttributeValueEditor
														   ,
												   }
												   , (attrId, attrValues) => new AttributeDTO
												   {
														   attributeId = attrId.AttributeId
														   , attributeName = attrId.AttributeName
														   , description =
																   attrId.AttributeDescription
														   , creatorId = attrId.AttributeCreator
														   , editorId = attrId.AttributeEditor
														   , values = attrValues.ToArray()
														   , editable = attrId.Editable
														   , removable = attrId.Removable
														   , repeatable = attrId.Repeatable
														   , batchEditable = attrId.BatchEditable
														   ,
												   })
										   .ToArray()
					,
			};
		}

		public static InterpretationAttributeDTO ToDTO(
				this SignInterpretationAttributeData            sia
				, IEnumerable<SignInterpretationCommentaryData> comms)
		{
			return new InterpretationAttributeDTO
			{
					attributeId = sia.AttributeId ?? 0
					, attributeString = sia.AttributeString
					, attributeValueId = sia.AttributeValueId ?? 0
					, attributeValueString = sia.AttributeValueString
					, creatorId = sia.SignInterpretationAttributeCreatorId ?? 0
					, editorId = sia.SignInterpretationAttributeEditorId ?? 0
					, interpretationAttributeId = sia.SignInterpretationAttributeId ?? 0
					, sequence = sia.Sequence ?? 0
					, commentary = comms
								   .Where(
										   y => y.AttributeId.HasValue
												&& (y.AttributeId.Value
													== sia.AttributeId.Value))
								   .Select(y => y.ToDTO())
								   .FirstOrDefault()
					,
			};
		}

		public static InterpretationRoiDTO ToDTO(this SignInterpretationRoiData sr)
			=> new InterpretationRoiDTO
			{
					artefactId = sr.ArtefactId.GetValueOrDefault()
					, creatorId = sr.SignInterpretationRoiCreatorId.GetValueOrDefault()
					, editorId = sr.SignInterpretationRoiEditorId.GetValueOrDefault()
					, exceptional = sr.Exceptional.GetValueOrDefault()
					, interpretationRoiId = sr.SignInterpretationRoiId.GetValueOrDefault()
					, shape = sr.Shape
					, signInterpretationId = sr.SignInterpretationId.GetValueOrDefault()
					, stanceRotation = sr.StanceRotation.GetValueOrDefault()
					, translate = sr.TranslateX.HasValue && sr.TranslateY.HasValue
							? new TranslateDTO
							{
									x = sr.TranslateX.Value
									, y = sr.TranslateY.Value
									,
							}
							: null
					, valuesSet = sr.ValuesSet.GetValueOrDefault()
					,
			};

		public static IEnumerable<InterpretationRoiDTO> ToDTO(
				this IEnumerable<SignInterpretationRoiData> sr)
		{
			return sr.Select(x => x.ToDTO());
		}

		public static NextSignInterpretationDTO ToDTO(this NextSignInterpretation nsi)
			=> new NextSignInterpretationDTO
			{
					editorId = nsi.PositionEditorId
					, creatorId = nsi.PositionCreatorId
					, nextSignInterpretationId = nsi.NextSignInterpretationId
					,
			};

		public static CommentaryDTO ToDTO(this SignInterpretationCommentaryData sic)
			=> new CommentaryDTO
			{
					commentary = sic.Commentary
					, creatorId = sic.SignInterpretationCommentaryCreatorId ?? 0
					, editorId = sic.SignInterpretationCommentaryEditorId ?? 0
					,
			};

		public static SignInterpretationDTO ToDTO(this SignInterpretationData si)
		{
			return new SignInterpretationDTO
			{
					signId = si.SignId ?? 0
					, character = si.Character
					, signInterpretationId = si.SignInterpretationId ?? 0
					, attributes = si.Attributes.Select(x => x.ToDTO(si.Commentaries)).ToArray()
					, rois = si.SignInterpretationRois.ToDTO().ToArray()
					, nextSignInterpretations =
							si.NextSignInterpretations.Where(x => x != null)
							  .Select(x => x.ToDTO())
							  .ToArray()
					, commentary =
							si.Commentaries.Where(x => !x.AttributeId.HasValue)
							  .Select(x => x.ToDTO())
							  .FirstOrDefault()
					, isVariant = si.IsVariant
					,
			};
		}

		public static SignInterpretationAttributeValueInput FromDTO(
				this CreateAttributeValueDTO cav) => new SignInterpretationAttributeValueInput
		{
				AttributeStringValue = cav.value
				, AttributeStringValueDescription = cav.description
				, Css = cav.cssDirectives
				,
		};

		public static SignInterpretationAttributeValue FromDTO(this UpdateAttributeValueDTO uav)
			=> new SignInterpretationAttributeValue
			{
					AttributeValueId = uav.id
					, AttributeStringValue = uav.value
					, AttributeStringValueDescription = uav.description
					, Css = uav.cssDirectives
					,
			};

		public static List<SignInterpretationAttributeData> ToSignInterpretationAttributeDatas(
				this IEnumerable<InterpretationAttributeCreateDTO> attr)
		{
			return attr.Select(
							   x => new SignInterpretationAttributeData
							   {
									   AttributeId = x.attributeId
									   , AttributeValueId = x.attributeValueId
									   , Sequence = x.sequence
									   ,
							   })
					   .ToList();
		}

		public static List<SignInterpretationCommentaryData> ToSignInterpretationCommentaryDatas(
				this SignInterpretationCreateDTO sicd)
		{
			var response = sicd.attributes == null
					? new List<SignInterpretationCommentaryData>()
					: sicd.attributes.Select(
								  x => new SignInterpretationCommentaryData
								  {
										  AttributeId = x.attributeId
										  , Commentary = sicd.commentary?.commentary
										  ,
								  })
						  .ToList();

			if (sicd.commentary != null)
			{
				response.Add(
						new SignInterpretationCommentaryData
						{
								Commentary = sicd.commentary?.commentary,
						});
			}

			return response;
		}

		public static List<SignInterpretationRoiData> ToSignInterpretationRoiDatas(
				this IEnumerable<SetInterpretationRoiDTO> rois)
		{
			return rois.Select(
							   x => new SignInterpretationRoiData
							   {
									   ArtefactId = x.artefactId
									   , Exceptional = x.exceptional
									   , Shape = x.shape
									   , TranslateX = x.translate.x
									   , TranslateY = x.translate.y
									   , StanceRotation = x.stanceRotation
									   , ValuesSet = x.valuesSet
									   ,
							   })
					   .ToList();
		}

		public static SignInterpretationData
				ToSignInterpretationData(this SignInterpretationCreateDTO sicd)
			=> new SignInterpretationData
			{
					Attributes = sicd.attributes?.ToSignInterpretationAttributeDatas()
					, Character = sicd.character
					, Commentaries = sicd.ToSignInterpretationCommentaryDatas()
					, IsVariant = sicd.isVariant
					, NextSignInterpretations = null
					, SignInterpretationRois = sicd.rois?.ToSignInterpretationRoiDatas()
					,
			};

		public static SignData ToSignData(this SignInterpretationCreateDTO sicd) => new SignData
		{
				SignId = null
				, SignInterpretations =
						new List<SignInterpretationData>
						{
								sicd.ToSignInterpretationData(),
						}
				,
		};
	}
}
