using System.Collections.Generic;
using System.Linq;
using SQE.API.DTO;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Serialization
{
    public static partial class ExtensionsDTO
    {
        public static AttributeListDTO ToDTO(this IEnumerable<SignInterpretationAttributeEntry> attributes)
        {
            return new AttributeListDTO()
            {
                attributes = attributes.GroupBy(attr => (
                        attr.AttributeId,
                        attr.AttributeName,
                        attr.AttributeDescription,
                        attr.AttributeCreator,
                        attr.AttributeEditor),
                    attr => new AttributeValueDTO()
                    {
                        id = attr.AttributeValueId,
                        value = attr.AttributeStringValue,
                        cssDirectives = attr.Css,
                        description = attr.AttributeStringValueDescription,
                        creatorId = attr.AttributeValueCreator,
                        editorId = attr.AttributeValueEditor,
                    },
                    (attrId, attrValues) => new AttributeDTO()
                    {
                        attributeId = attrId.AttributeId,
                        attributeName = attrId.AttributeName,
                        description = attrId.AttributeDescription,
                        creatorId = attrId.AttributeCreator,
                        editorId = attrId.AttributeEditor,
                        values = attrValues.ToArray(),
                    }).ToArray(),
            };
        }

        public static InterpretationAttributeDTO ToDTO(this SignInterpretationAttributeData sia, IEnumerable<SignInterpretationCommentaryData> comms)
        {
            return new InterpretationAttributeDTO()
            {
                attributeId = sia.AttributeId ?? 0,
                attributeValueId = sia.AttributeValueId ?? 0,
                attributeValueString = sia.AttributeValueString,
                creatorId = sia.SignInterpretationAttributeCreatorId ?? 0,
                editorId = sia.SignInterpretationAttributeEditorId ?? 0,
                interpretationAttributeId = sia.SignInterpretationAttributeId ?? 0,
                sequence = sia.Sequence ?? 0,
                value = sia.NumericValue ?? 0,
                commentary = comms.Where(y =>
                        y.AttributeId.HasValue && y.AttributeId.Value == sia.AttributeId.Value)
                    .Select(y => y.ToDTO())
                    .FirstOrDefault(),
            };
        }

        public static InterpretationRoiDTO ToDTO(this SignInterpretationRoiData sr)
        {
            return new InterpretationRoiDTO()
            {
                artefactId = sr.ArtefactId ?? 0,
                creatorId = sr.SignInterpretationRoiCreatorId ?? 0,
                editorId = sr.SignInterpretationRoiEditorId ?? 0,
                exceptional = sr.Exceptional ?? false,
                interpretationRoiId = sr.SignInterpretationRoiId ?? 0,
                shape = sr.Shape,
                signInterpretationId = sr.SignInterpretationId,
                stanceRotation = sr.StanceRotation ?? 0,
                translate = sr.TranslateX.HasValue && sr.TranslateY.HasValue
                    ? new TranslateDTO()
                    {
                        x = sr.TranslateX.Value,
                        y = sr.TranslateY.Value,
                    }
                    : null,
                valuesSet = sr.ValuesSet ?? false,
            };
        }

        public static NextSignInterpretationDTO ToDTO(this NextSignInterpretation nsi)
        {
            return new NextSignInterpretationDTO()
            {
                editorId = nsi.PositionEditorId,
                creatorId = nsi.PositionCreatorId,
                nextSignInterpretationId = nsi.NextSignInterpretationId,
            };
        }

        public static CommentaryDTO ToDTO(this SignInterpretationCommentaryData sic)
        {
            return new CommentaryDTO()
            {
                commentary = sic.Commentary,
                creatorId = sic.SignInterpretationCommentaryCreatorId ?? 0,
                editorId = sic.SignInterpretationCommentaryEditorId ?? 0,
            };
        }

        public static SignInterpretationDTO ToDTO(this SignInterpretationData si)
        {
            return new SignInterpretationDTO()
            {
                character = si.Character,
                signInterpretationId = si.SignInterpretationId ?? 0,
                attributes = si.Attributes.Select(x => x.ToDTO(si.Commentaries)).ToArray(),
                rois = si.SignInterpretationRois.Select(x => x.ToDTO()).ToArray(),
                nextSignInterpretations = si.NextSignInterpretations.Select(x => x.ToDTO()).ToArray(),
                commentary = si.Commentaries.Where(x => !x.AttributeId.HasValue).Select(x => x.ToDTO()).FirstOrDefault(),
                isVariant = si.IsVariant,
            };
        }
    }
}