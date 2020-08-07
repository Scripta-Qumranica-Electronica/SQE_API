namespace SQE.DatabaseAccess.Queries
{
    internal static class GetSignInterpretationAttributesByDataQuery
    {
        public const string GetQuery = @"
				SELECT sign_interpretation_attribute_id AS SignInterpretationAttributeId,
				       sign_interpretation_id as SignInterpretationId,
				       attribute_value.attribute_id AS AttributeId,
				       attribute_value_id AS AttributeValueId,
				       sequence as Sequence,
				       sign_interpretation_attribute.creator_id AS SignInterpretationAttributeCreatorId,
				       sign_interpretation_attribute_owner.edition_editor_id AS SignInterpretationAttributeEditorId,
				       string_value as AttributeValueString,
				       numeric_value as NumericValue
				FROM sign_interpretation_attribute
				JOIN sign_interpretation_attribute_owner USING (sign_interpretation_attribute_id)
				JOIN attribute_value USING (attribute_value_id)
				WHERE @WhereData
					AND edition_id=@EditionId
				";
    }

    internal static class GetSignInterpretationAttributeIdsByDataQuery
    {
        public const string GetQuery = @"
				SELECT sign_interpretation_attribute_id AS SignInterpretationAttributeId
				FROM sign_interpretation_attribute
				JOIN sign_interpretation_attribute_owner USING (sign_interpretation_attribute_id)
				WHERE @WhereData
					AND edition_id=@EditionId
				";
    }

    internal static class GetAllEditionSignInterpretationAttributesQuery
    {
        public const string GetQuery = @"
SELECT attribute.attribute_id AS AttributeId,
       attribute.name AS AttributeName,
       attribute.description AS AttributeDescription,
       attribute.creator_id AS AttributeCreator,
       attribute_owner.edition_editor_id AS AttributeEditor,
       attribute_value.attribute_value_id AS AttributeValueId,
       attribute_value.string_value AS AttributeStringValue,
       attribute_value.description AS AttributeStringValueDescription,
       attribute_value.creator_id AS AttributeValueCreator,
       attribute_value_owner.edition_editor_id AS AttributeValueEditor,
       attr_css.css AS Css
FROM attribute 
JOIN attribute_owner USING(attribute_id)
JOIN attribute_value USING(attribute_id)
JOIN attribute_value_owner ON attribute_value.attribute_value_id = attribute_value_owner.attribute_value_id
	AND attribute_value_owner.edition_id = attribute_owner.edition_id
LEFT JOIN (
    SELECT css, edition_id, attribute_value_id
    FROM attribute_value_css
    JOIN attribute_value_css_owner USING(attribute_value_css_id)
    ) AS attr_css ON attr_css.attribute_value_id = attribute_value.attribute_value_id
		AND attr_css.edition_id = attribute_owner.edition_id
WHERE attribute_owner.edition_id = @EditionId
";
    }
}