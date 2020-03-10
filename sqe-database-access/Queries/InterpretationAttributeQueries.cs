namespace SQE.DatabaseAccess.Queries
{

	internal static class GetSignInterpretationAttributesByDataQuery
	{
		public const string GetQuery = @"
				SELECT sign_interpretation_attribute_id AS SignInterpretationAttributeId,
				       sign_interpretation_id as SignInterpretationId,
				       attribute_value_id AS AttributeValueId,
				       sequence as Sequence,
				       sign_interpretation_attribute_owner.edition_editor_id AS SignInterpretationAttributeAuthor,
				       string_value as AttributeString,
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


}
