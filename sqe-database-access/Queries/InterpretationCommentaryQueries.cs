namespace SQE.DatabaseAccess.Queries
{
    internal static class GetSignInterpretationCommentaryByData
    {
        public const string GetQuery = @"
				SELECT sign_interpretation_commentary_id AS SignInterpretationCommentaryId,
				       sign_interpretation_id AS SignInterpretationId,
				       commentary,
				       attribute_id AS attributeId,
				       sign_interpretation_commentary_owner.edition_editor_id AS signInterpretationAttributeAuthor	
				FROM sign_interpretation_commentary
				JOIN sign_interpretation_commentary_owner USING (sign_interpretation_commentary_id)
				WHERE @WhereData
					AND edition_id=@EditionId
				";
    }
}