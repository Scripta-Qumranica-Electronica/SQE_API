namespace SQE.SqeApi.DataAccess.Queries
{
  public static class GetTextChunk
  {

    /// <summary>
    /// Retrieves all textual data for a chunk of text
    /// </summary>
    /// <param name="startId">Id of the first sign</param>
    /// <param name="endId">Id of the last sign</param>
    /// <param name="editionId">Id of the edition the text is to be taken from</param>
    public const string GetQuery = @"
    WITH RECURSIVE sign_interpretation_ids
    AS (
          SELECT @startId AS signInterpretationId, @editionId AS EditionId

        UNION
          SELECT next_sign_interpretation_id, EditionId
          FROM  sign_interpretation_ids, position_in_stream
          WHERE position_in_stream.sign_interpretation_id = signInterpretationId
            AND signInterpretationId != @endId
    )
    
    SELECT manuscript_data.manuscript_id AS manuscriptId,
      manuscript_data.name AS editionName,
      manuscript_author.user_id AS manuscriptAuthor,
      
      edition.copyright_holder AS copyrightHolder,
      edition.collaborators,
      
      text_fragment_data.text_fragment_id AS textFragmentId,
      text_fragment_data.name AS textFragmentName,
      text_fragment_author.user_id AS textFragmentAuthor,
      
      line_data.line_id AS lineId,
      line_data.name AS line,
      line_author.user_id AS lineAuthor,
      
      sign_interpretation.sign_id AS signId,
      next_sign_interpretation.sign_interpretation_id AS nextSignInterpretationId,
      sign_sequence_author.user_id AS signSequenceAuthor,
      
      signInterpretationId,
      sign_interpretation.`character` AS `character`,
      
      sign_interpretation_attribute.sign_interpretation_attribute_id AS interpretationAttributeId,
      sign_interpretation_attribute.attribute_value_id AS attributeValueId,
      sign_interpretation_attribute.sequence AS sequence,
      sign_interpretation_attribute_author.user_id AS signInterpretationAttributeAuthor,
      attribute_numeric.value AS value,

      sign_interpretation_roi.sign_interpretation_id AS SignInterpretationRoiId,
      sign_interpretation_roi_author.user_id AS SignInterpretationRoiAuthor,
      sign_interpretation_roi.values_set AS ValuesSet,
      sign_interpretation_roi.exceptional AS Exceptional,
      ST_ASTEXT(roi_shape.path) AS Shape,
      roi_position.transform_matrix AS Position,
      roi_position.artefact_id AS ArtefactId

    FROM sign_interpretation_ids

      JOIN sign_interpretation ON sign_interpretation.sign_interpretation_id=signInterpretationId
      JOIN sign_interpretation_attribute ON sign_interpretation_attribute.sign_interpretation_id=signInterpretationId
      LEFT JOIN attribute_numeric USING (sign_interpretation_attribute_id)
      JOIN sign_interpretation_attribute_owner ON sign_interpretation_attribute_owner.sign_interpretation_attribute_id = sign_interpretation_attribute.sign_interpretation_attribute_id
          AND sign_interpretation_attribute_owner.edition_id = EditionId
      JOIN edition_editor AS sign_interpretation_attribute_author ON sign_interpretation_attribute_author.edition_editor_id = sign_interpretation_attribute_owner.edition_editor_id

      JOIN line_to_sign ON line_to_sign.sign_id = sign_interpretation.sign_id
      JOIN line_data USING (line_id)
      JOIN line_data_owner ON line_data_owner.line_data_id = line_data.line_data_id
          AND line_data_owner.edition_id = EditionId
      JOIN edition_editor AS line_author ON line_data_owner.edition_editor_id = line_author.edition_editor_id

      JOIN text_fragment_to_line USING (line_id)
      JOIN text_fragment_data USING (text_fragment_id)
      JOIN text_fragment_data_owner ON text_fragment_data_owner.text_fragment_data_id = text_fragment_data.text_fragment_data_id
          AND text_fragment_data_owner.edition_id = EditionId
      JOIN edition_editor AS text_fragment_author ON text_fragment_data_owner.edition_editor_id = text_fragment_author.edition_editor_id

      JOIN manuscript_to_text_fragment USING (text_fragment_id)
      JOIN manuscript_data USING (manuscript_id)
      JOIN manuscript_data_owner ON manuscript_data_owner.manuscript_data_id = manuscript_data.manuscript_data_id
          AND manuscript_data_owner.edition_id = EditionId
      JOIN edition_editor AS manuscript_author ON manuscript_data_owner.edition_editor_id = manuscript_author.edition_editor_id
          
      JOIN position_in_stream ON position_in_stream.sign_interpretation_id = signInterpretationId
      JOIN position_in_stream_owner ON position_in_stream_owner.position_in_stream_id = position_in_stream.position_in_stream_id
          AND position_in_stream_owner.edition_id = EditionId
      JOIN edition_editor AS sign_sequence_author ON position_in_stream_owner.edition_editor_id = sign_sequence_author.edition_editor_id

      LEFT JOIN sign_interpretation_roi ON sign_interpretation_roi.sign_interpretation_id = sign_interpretation.sign_interpretation_id
      LEFT JOIN sign_interpretation_roi_owner ON sign_interpretation_roi_owner.sign_interpretation_roi_id = sign_interpretation_roi.sign_interpretation_roi_id
          AND sign_interpretation_roi_owner.edition_id = EditionId
      LEFT JOIN roi_shape ON roi_shape.roi_shape_id = sign_interpretation_roi.roi_shape_id
      LEFT JOIN roi_position ON roi_position.roi_position_id = sign_interpretation_roi.roi_position_id
      LEFT JOIN edition_editor AS sign_interpretation_roi_author ON sign_interpretation_roi_author.edition_editor_id = sign_interpretation_roi_owner.edition_editor_id
          
      JOIN sign_interpretation AS next_sign_interpretation ON next_sign_interpretation.sign_interpretation_id = next_sign_interpretation_id

      JOIN edition ON edition.edition_id = EditionId
";
  }

  internal static class GetLineTerminators
  {
    /// <summary>
    /// Retrieves the first and last sign of a line
    /// </summary>
    /// <param name="entityId">Id of line</param>
    /// <param name="editionId">d of the edition the line is to be taken from</param>
    public const string GetQuery = @"
      SELECT sign_interpretation.sign_interpretation_id
      FROM  line_to_sign
        JOIN  sign_interpretation USING (sign_id)
        JOIN   sign_interpretation_attribute USING (sign_interpretation_id)
        JOIN sign_interpretation_attribute_owner USING (sign_interpretation_attribute_id)
      WHERE line_id=@entityId
        AND (attribute_value_id=10 OR attribute_value_id = 11)
        AND edition_id=@editionId

";
  }

  internal static class GetFragmentTerminators
  {
    /// <summary>
    /// Retrieves the first and last sign of a textFragmentName
    /// </summary>
    /// <param name="entityId">Id of line</param>
    /// <param name="editionId">d of the edition the line is to be taken from</param>
    public const string GetQuery = @"
      SELECT sign_interpretation.sign_interpretation_id
      FROM text_fragment_to_line
        JOIN line_to_sign USING (line_id)
        JOIN  sign_interpretation USING (sign_id)
        JOIN   sign_interpretation_attribute USING (sign_interpretation_id)
        JOIN sign_interpretation_attribute_owner USING (sign_interpretation_attribute_id)
        JOIN edition_editor ON edition_editor.edition_editor_id = @EditionId
      WHERE (attribute_value_id = 12 OR attribute_value_id = 13)
        AND text_fragment_id=@EntityId
        AND sign_interpretation_attribute_owner.edition_id=@EditionId
        AND (edition_editor.user_id = @UserId OR edition_editor.user_id = 1)
      ORDER BY attribute_value_id
";
  }

  internal static class GetLineData
  {
    public const string Query = @"
      WITH RECURSIVE lineIds
      AS (
        SELECT text_fragment_to_line.text_fragment_id AS fragmentId, text_fragment_to_line.line_id AS lineId, line_data.name AS lineName, sign_interpretation_attribute_owner.edition_id AS editionId
        FROM text_fragment_to_line
          JOIN line_to_sign USING (line_id)
          JOIN sign_interpretation USING (sign_id)
          JOIN sign_interpretation_attribute USING (sign_interpretation_id)
          JOIN sign_interpretation_attribute_owner USING (sign_interpretation_attribute_id)
          JOIN line_data USING(line_id)
          JOIN line_data_owner USING(line_data_id)
          JOIN edition_editor ON edition_editor.edition_id = @EditionId
        WHERE text_fragment_id = @TextFragmentId
          AND sign_interpretation_attribute_owner.edition_id = @EditionId
          AND line_data_owner.edition_id = @EditionId
          AND (edition_editor.user_id = @UserId OR edition_editor.user_id = 1)
          AND attribute_value_id = 12
        
       UNION
        
        SELECT fragmentId, lts2.line_id AS lineId, line_data.name AS lineName, editionId
        FROM lineIds
          JOIN line_to_sign AS lts1 ON lts1.line_id =lineId
          JOIN sign_interpretation USING (sign_id)
          JOIN sign_interpretation_attribute USING (sign_interpretation_id)
          JOIN sign_interpretation_attribute_owner ON sign_interpretation_attribute_owner.sign_interpretation_attribute_id = sign_interpretation_attribute.sign_interpretation_attribute_id
            AND sign_interpretation_attribute_owner.edition_id = editionId
          JOIN position_in_stream USING (sign_interpretation_id)
          JOIN sign_interpretation AS si2 ON si2.sign_interpretation_id = position_in_stream.next_sign_interpretation_id
          JOIN line_to_sign as lts2 on lts2.sign_id=si2.sign_id
          JOIN text_fragment_to_line ON lts2.line_id=text_fragment_to_line.line_id
          JOIN text_fragment_to_line_owner ON text_fragment_to_line_owner.text_fragment_to_line_id = text_fragment_to_line.text_fragment_to_line_id
            AND text_fragment_to_line_owner.edition_id = editionId
          JOIN line_data ON line_data.line_id = lts2.line_id
          JOIN line_data_owner ON line_data_owner.line_data_id = line_data.line_data_id
            AND line_data_owner.edition_id = editionId
        WHERE lts1.line_id = lineId
           AND  attribute_value_id =11
           AND text_fragment_to_line.text_fragment_id=fragmentId
        )
      SELECT lineId, lineName
      FROM lineIds
    
    ";
  }
  internal static class GetFragmentData
  {
    public const string GetQuery = @"
SELECT text_fragment_id AS TextFragmentId, name AS TextFragmentName, text_fragment_sequence.position AS Position, 
       text_fragment_sequence.text_fragment_sequence_id AS TextFragmentSequenceId
FROM text_fragment_data
  JOIN text_fragment_data_owner ON text_fragment_data_owner.text_fragment_data_id = text_fragment_data.text_fragment_data_id
    AND text_fragment_data_owner.edition_id = @EditionId
  JOIN text_fragment_sequence USING(text_fragment_id)
  JOIN text_fragment_sequence_owner ON text_fragment_sequence_owner.text_fragment_sequence_id = text_fragment_sequence.text_fragment_sequence_id
    AND text_fragment_sequence_owner.edition_id = @EditionId
  JOIN edition_editor ON edition_editor.edition_id = @EditionId
WHERE edition_editor.user_id = @UserId OR edition_editor.user_id = 1
ORDER BY text_fragment_sequence.position
      ";
  }

  internal static class CreateTextFragment
  {
    public const string GetQuery = @"
INSERT INTO text_fragment () VALUES()
";
  }

  internal static class ManuscriptOfEdition
  {
    public const string GetQuery = @"
SELECT manuscript_id
FROM edition
WHERE edition_id = @EditionId
";
  }
}