using System.Collections.Generic;
using System.Linq;
using SQE.API.DTO;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Serialization
{
    public static partial class ExtensionsDTO
    {
        public static AttributeListDTO ToDTO(this IEnumerable<SignEnterpretationAttributeEntry> attributes)
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
                        editorId = attr.AttributeValueEditor
                    },
                    (attrId, attrValues) => new AttributeDTO()
                    {
                        attributeId = attrId.AttributeId,
                        attributeName = attrId.AttributeName,
                        description = attrId.AttributeDescription,
                        creatorId = attrId.AttributeCreator,
                        editorId = attrId.AttributeEditor,
                        values = attrValues.ToArray()
                    }).ToArray()
            };
        }
    }
}