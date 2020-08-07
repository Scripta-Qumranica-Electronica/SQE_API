namespace SQE.DatabaseAccess.Queries
{
    internal static class SignInterpretationQuery
    {
        public const string GetQuery = @"
SELECT DISTINCT sign_interpretation.sign_interpretation_id AS SignInterpretationId,
                sign_interpretation.`character` AS `Character`,
                sign_interpretation.is_variant AS IsVariant,
                pos.next_sign_interpretation_id AS NextSignInterpretationId,
                pos.is_main AS IsMainNextSignInterpretation,
                pos.creator_id AS PositionCreatorId,
                pos.edition_editor_id AS PositionEditorId,
                pos.sign_stream_section_id AS SignStreamSectionId
FROM sign_interpretation
JOIN sign_interpretation_attribute USING(sign_interpretation_id)
JOIN sign_interpretation_attribute_owner USING(sign_interpretation_attribute_id)
LEFT JOIN (
    SELECT position_in_stream.sign_interpretation_id,
           position_in_stream.next_sign_interpretation_id,
           position_in_stream_owner.is_main,
           position_in_stream.creator_id,
           position_in_stream_owner.edition_id,
           position_in_stream_owner.edition_editor_id,
           section.sign_stream_section_id
    FROM position_in_stream
    JOIN position_in_stream_owner ON position_in_stream.position_in_stream_id = position_in_stream_owner.position_in_stream_id
    LEFT JOIN (
        SELECT position_in_stream_to_section_rel.position_in_stream_id,
               position_in_stream_to_section_rel.sign_stream_section_id,
               sign_stream_section_owner.edition_id
        FROM position_in_stream_to_section_rel
        JOIN sign_stream_section USING(sign_stream_section_id)
        JOIN sign_stream_section_owner USING(sign_stream_section_id)
    ) AS section ON position_in_stream.position_in_stream_id = section.position_in_stream_id
        AND section.edition_id = position_in_stream_owner.edition_id
) AS pos ON pos.sign_interpretation_id = sign_interpretation.sign_interpretation_id
    AND pos.edition_id = sign_interpretation_attribute_owner.edition_id
WHERE sign_interpretation.sign_interpretation_id = @SignInterpretationId
    AND sign_interpretation_attribute_owner.edition_id = @EditionId
ORDER BY pos.next_sign_interpretation_id, pos.sign_stream_section_id
";
    }
}