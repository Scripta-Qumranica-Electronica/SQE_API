namespace SQE.DatabaseAccess.Queries
{
	internal static class GetSignInterpretationCommentaryByData
	{
		public const string GetQuery = @"
				SELECT sign_interpretation_commentary_id AS SignInterpretationCommentaryId,
				       sign_interpretation_id AS SignInterpretationId,
				       commentary AS Commentary,
				       attribute_id AS AttributeId,
				       sign_interpretation_commentary_owner.edition_editor_id AS SignInterpretationCommentaryEditorId,
				       sign_interpretation_commentary.creator_id AS SignInterpretationCommentaryCreatorId
				FROM sign_interpretation_commentary
				JOIN sign_interpretation_commentary_owner USING (sign_interpretation_commentary_id)
				WHERE @WhereData
					AND edition_id=@EditionId
				";
	}
}
