namespace SQE.DatabaseAccess.Queries
{
    public static class EditionScriptLines
    {
        public static string GetQuery = @"
SELECT DISTINCT text_fragment_data.text_fragment_id AS TextFragmentId, 
                text_fragment_data.name AS TextFragmentName, 
                
                line_to_sign.line_id AS LineId,
                line_data.name AS LineName,
                
                roi_position.artefact_id AS ArtefactId,
                ap.name AS ArtefactName,
                ap.scale AS ArtefactScale,
                ap.rotate AS ArtefactRotate,
                ap.translate_x AS ArtefactTranslateX,
                ap.translate_y AS ArtefactTranslateY,
                ap.z_index AS ArtefactZIndex,
                
                sign_interpretation.sign_interpretation_id AS SignInterpretationId, 
                sign_interpretation.character AS SignInterpretationCharacter, 
                
                sign_interpretation_roi.sign_interpretation_roi_id AS SignInterpretationRoiId,
                AsWKB(roi_shape.path) AS RoiShape, 
                roi_position.translate_x AS RoiTranslateX, 
                roi_position.translate_y AS RoiTranslateY, 
                roi_position.stance_rotation AS RoiRotate,
                
                sign_interpretation_attribute.sign_interpretation_attribute_id AS SignInterpretationAttributeId,
                attribute.name AS AttributeName,
                attribute_value.string_value AS AttributeValue,
                
                position_in_stream.position_in_stream_id AS PositionInStreamId,
                position_in_stream.next_sign_interpretation_id AS NextSignInterpretationId
FROM line_to_sign_owner
JOIN line_to_sign USING(line_to_sign_id)
JOIN line_data USING(line_id)
JOIN line_data_owner ON line_data_owner.line_data_id = line_data.line_data_id
    AND line_data_owner.edition_id = line_to_sign_owner.edition_id
JOIN text_fragment_to_line USING(line_id)
JOIN text_fragment_to_line_owner ON text_fragment_to_line_owner.text_fragment_to_line_id = text_fragment_to_line.text_fragment_to_line_id
    AND text_fragment_to_line_owner.edition_id = line_to_sign_owner.edition_id
JOIN text_fragment_data USING(text_fragment_id)
JOIN text_fragment_data_owner ON text_fragment_data_owner.text_fragment_data_id = text_fragment_data.text_fragment_data_id
    AND text_fragment_data_owner.edition_id = line_to_sign_owner.edition_id
JOIN sign_interpretation ON sign_interpretation.sign_id = line_to_sign.sign_id
    AND sign_interpretation.character != '' 
    AND sign_interpretation.character IS NOT NULL
JOIN sign_interpretation_roi USING(sign_interpretation_id)
JOIN sign_interpretation_roi_owner ON sign_interpretation_roi_owner.sign_interpretation_roi_id = sign_interpretation_roi.sign_interpretation_roi_id
JOIN roi_position USING(roi_position_id)
JOIN roi_shape USING(roi_shape_id)
    
## The related artefact may not have a position, so left join it for null fields instead of filtering
LEFT JOIN 
    (SELECT artefact_position.scale,
            artefact_position.rotate,
            artefact_position.translate_x,
            artefact_position.translate_y,
            artefact_position.z_index,
            artefact_position.artefact_id,
            artefact_data.name,
            artefact_position_owner.edition_id
    FROM artefact_position
    JOIN artefact_position_owner ON artefact_position_owner.artefact_position_id = artefact_position.artefact_position_id
    JOIN artefact_data ON artefact_data.artefact_id = artefact_position.artefact_id
    JOIN artefact_data_owner ON artefact_data_owner.artefact_data_id = artefact_data.artefact_data_id
        AND artefact_data_owner.edition_id = artefact_position_owner.edition_id
    ) AS ap ON ap.artefact_id = roi_position.artefact_id
        AND ap.edition_id = line_to_sign_owner.edition_id

JOIN sign_interpretation_attribute ON sign_interpretation_attribute.sign_interpretation_id = sign_interpretation.sign_interpretation_id
JOIN sign_interpretation_attribute_owner ON sign_interpretation_attribute_owner.sign_interpretation_attribute_id = sign_interpretation_attribute.sign_interpretation_attribute_id
    AND sign_interpretation_attribute_owner.edition_id = line_to_sign_owner.edition_id
JOIN attribute_value USING(attribute_value_id)
JOIN attribute USING(attribute_id)
JOIN position_in_stream ON position_in_stream.sign_interpretation_id = sign_interpretation.sign_interpretation_id
JOIN position_in_stream_owner ON position_in_stream_owner.position_in_stream_id = position_in_stream.position_in_stream_id
    AND position_in_stream_owner.edition_id = line_to_sign_owner.edition_id
JOIN edition ON edition.edition_id = line_to_sign_owner.edition_id
JOIN edition_editor ON edition_editor.edition_id = line_to_sign_owner.edition_id
WHERE line_to_sign_owner.edition_id = @EditionId
    AND (edition.public = 1 OR edition_editor.user_id = @UserId)
ORDER BY text_fragment_to_line.text_fragment_id, 
         line_to_sign.line_id, 
         roi_position.artefact_id, 
         position_in_stream.sign_interpretation_id,
         sign_interpretation_roi.sign_interpretation_roi_id,
         sign_interpretation_attribute.sign_interpretation_attribute_id,
         position_in_stream.position_in_stream_id
";
    }
}