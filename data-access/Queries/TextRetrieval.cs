namespace SQE.SqeHttpApi.DataAccess.Queries
{
  public static class TextRetrieval
  {

    /// <summary>
    /// Retrieves all textual data for a chunk of text
    /// </summary>
    /// <param name="startId">Id of the first sign</param>
    /// <param name="endId">Id of the last sign</param>
    /// <param name="editionId">Id of the edition the text is to be taken from</param>
    public const string GetTextChunkQuery = @"
    WITH RECURSIVE sign_ids
    AS (
          SELECT @startId AS signId, @editionId AS EditionId

        UNION
          SELECT next_sign_id, EditionId
          FROM  sign_ids, position_in_stream
          WHERE position_in_stream.sign_id = signId
            AND signId != @endId
    )
    
    SELECT scroll_data.scroll_id AS scrollId,
       scroll_data.name AS scroll,
       col_data.col_id AS fragmentId,
       col_data.name AS fragment,
       line_data.line_id AS lineId,
       line_data.name AS line,
       signId,
       sign_char.sign_char_id as signCharId,
       sign_char.sign as signChar,
       sign_char_attribute.sign_char_attribute_id AS charAttributeId,
       sign_char_attribute.attribute_value_id as attributeValueId,
       attribute_numeric.value as value

    FROM sign_ids
      JOIN sign_char ON sign_char.sign_id=signId
      JOIN sign_char_attribute USING (sign_char_id)
      LEFT JOIN attribute_numeric USING (sign_char_attribute_id)
      JOIN sign_char_attribute_owner USING (sign_char_attribute_id)

      JOIN line_to_sign USING (sign_id)
      JOIN line_data USING (line_id)
      JOIN line_data_owner USING (line_data_id)

      JOIN col_to_line USING (line_id)
      JOIN col_data USING (col_id)
      JOIN col_data_owner USING (col_data_id)

      JOIN scroll_to_col USING (col_id)
      JOIN scroll_data USING (scroll_id)
      JOIN scroll_data_owner USING (scroll_data_id)

      WHERE sign_char_attribute_owner.edition_id
              = line_data_owner.edition_id
              = col_data_owner.edition_id
              =scroll_data_owner.edition_id
              = EditionId
";

    /// <summary>
    /// Retrieves the first and last sign of a line
    /// </summary>
    /// <param name="entityId">Id of line</param>
    /// <param name="editionId">d of the edition the line is to be taken from</param>
    public const string GetLineTerminatorsQuery = @"
      SELECT sign_char.sign_id
      FROM  line_to_sign
        JOIN  sign_char USING (sign_id)
        JOIN   sign_char_attribute USING (sign_char_id)
        JOIN sign_char_attribute_owner USING (sign_char_attribute_id)
      WHERE line_id=@entityId
        AND (attribute_value_id=10 OR attribute_value_id = 11)
        AND edition_id=@editionId

";

    /// <summary>
    /// Retrieves the first and last sign of a fragment
    /// </summary>
    /// <param name="entityId">Id of line</param>
    /// <param name="editionId">d of the edition the line is to be taken from</param>
    public const string GetFragmentTerminatorsQuery = @"
      SELECT sign_char.sign_id
      FROM col_to_line
        JOIN line_to_sign USING (line_id)
        JOIN  sign_char USING (sign_id)
        JOIN   sign_char_attribute USING (sign_char_id)
        JOIN sign_char_attribute_owner USING (sign_char_attribute_id)
      WHERE col_id=@entityId
        AND (attribute_value_id=12 OR attribute_value_id = 13)
        AND edition_id=@editionId
";

    public const string GetLineIdsQuery = @"
      WITH RECURSIVE lineIds
      AS (
        SELECT col_to_line.col_id AS fragmentId, col_to_line.line_id AS lineId, edition_id AS editionId
        FROM col_to_line
          JOIN line_to_sign USING (line_id)
          JOIN sign_char USING (sign_id)
          JOIN sign_char_attribute USING (sign_char_id)
          JOIN sign_char_attribute_owner USING (sign_char_attribute_id)
        WHERE col_id = @fragmentId
          AND edition_id=@editionId
          AND attribute_value_id =12
        
       UNION
        
        SELECT fragmentId, lts2.line_id as lineId, editionId
        FROM lineIds     
          JOIN line_to_sign AS lts1 ON lts1.line_id =lineId
          JOIN sign_char USING (sign_id)
          JOIN sign_char_attribute USING (sign_char_id)
          JOIN sign_char_attribute_owner USING (sign_char_attribute_id)
          JOIN position_in_stream USING (sign_id)
          JOIN line_to_sign as lts2 on lts2.sign_id=next_sign_id
          JOIN col_to_line ON lts2.line_id=col_to_line.line_id
          JOIN col_to_line_owner USING (col_to_line_id)
        WHERE lts1.line_id = lineId
           AND  attribute_value_id =11
           AND col_to_line.col_id=fragmentId
           AND sign_char_attribute_owner.edition_id
                = col_to_line_owner.edition_id
                = editionId
        )
      SELECT lineId
      FROM lineIds

";

    // TODO We have still to connect the fagments in a scroll and to adjust the query accordingly
    public const string GetFragmentIdsQuery = @"
      SELECT col_id
      FROM col_data
        JOIN col_data_owner USING (col_data_id)
      WHERE edition_id =1     
";

    
  }
}