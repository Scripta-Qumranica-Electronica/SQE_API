namespace SQE.SqeHttpApi.DataAccess.Queries
{
  public static class TextRetrieval
  {

    /// <summary>
    /// Retrieves all textual data for a chunk of text
    /// </summary>
    /// <param Name="startId">Id of the first sign</param>
    /// <param Name="endId">Id of the last sign</param>
    /// <param Name="editionId">Id of the edition the text is to be taken from</param>
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
       scroll_data.Name AS editionNAme,
       edition.copyright_holder AS copyrightHolder,
       edition.collaborators,
       col_data.col_id AS textFragmentId,
       col_data.Name AS fragment,
       line_data.line_id AS lineId,
       line_data.Name AS line,
       signId,
       position_in_stream.next_sign_id AS nextSignId,
       sign_char.sign_char_id as signCharId,
       sign_char.sign as signChar,
       sign_char_attribute.sign_char_attribute_id AS charAttributeId,
       sign_char_attribute.attribute_value_id as attributeValueId,
       attribute_numeric.value as value

    FROM sign_ids

      JOIN sign_char ON sign_char.sign_id=signId
      JOIN sign_char_attribute USING (sign_char_id)
      LEFT JOIN attribute_numeric USING (sign_char_attribute_id)
      JOIN sign_char_attribute_owner ON sign_char_attribute_owner.sign_char_attribute_id = sign_char_attribute.sign_char_attribute_id
          AND sign_char_attribute_owner.edition_id = EditionId

      JOIN line_to_sign USING (sign_id)
      JOIN line_data USING (line_id)
      JOIN line_data_owner ON line_data_owner.line_data_id = line_data.line_data_id
          AND line_data_owner.edition_id = EditionId

      JOIN col_to_line USING (line_id)
      JOIN col_data USING (col_id)
      JOIN col_data_owner ON col_data_owner.col_data_id = col_data.col_data_id
          AND col_data_owner.edition_id = EditionId

      JOIN scroll_to_col USING (col_id)
      JOIN scroll_data USING (scroll_id)
      JOIN scroll_data_owner ON scroll_data_owner.scroll_data_id = scroll_data.scroll_data_id
          AND scroll_data_owner.edition_id = EditionId
          
      JOIN position_in_stream ON position_in_stream.sign_id = signId
      JOIN position_in_stream_owner ON position_in_stream_owner.position_in_stream_id = position_in_stream.position_in_stream_id
          AND position_in_stream_owner.edition_id = EditionId
          
      JOIN edition ON edition.edition_id = EditionId
";

    /// <summary>
    /// Retrieves the first and last sign of a line
    /// </summary>
    /// <param Name="entityId">Id of line</param>
    /// <param Name="editionId">d of the edition the line is to be taken from</param>
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
    /// <param Name="entityId">Id of line</param>
    /// <param Name="editionId">d of the edition the line is to be taken from</param>
    public const string GetFragmentTerminatorsQuery = @"
      SELECT sign_char.sign_id
      FROM col_to_line
        JOIN line_to_sign USING (line_id)
        JOIN  sign_char USING (sign_id)
        JOIN   sign_char_attribute USING (sign_char_id)
        JOIN sign_char_attribute_owner USING (sign_char_attribute_id)
      WHERE col_id=@EntityId
        AND (attribute_value_id=12 OR attribute_value_id = 13)
        AND edition_id=@EditionId
      ORDER BY attribute_value_id
";

    public const string GetLineIdsQuery = @"
      WITH RECURSIVE lineIds
      AS (
        SELECT col_to_line.col_id AS fragmentId, col_to_line.line_id AS lineId, line_data.name AS lineName, sign_char_attribute_owner.edition_id AS editionId
        FROM col_to_line
          JOIN line_to_sign USING (line_id)
          JOIN sign_char USING (sign_id)
          JOIN sign_char_attribute USING (sign_char_id)
          JOIN sign_char_attribute_owner USING (sign_char_attribute_id)
          JOIN line_data USING(line_id)
          JOIN line_data_owner USING(line_data_id)
        WHERE col_id = @fragmentId
          AND sign_char_attribute_owner.edition_id = @editionId
          AND line_data_owner.edition_id = @editionId
          AND attribute_value_id = 12
        
       UNION
        
        SELECT fragmentId, lts2.line_id AS lineId, line_data.name AS lineName, editionId
        FROM lineIds
          JOIN line_to_sign AS lts1 ON lts1.line_id =lineId
          JOIN sign_char USING (sign_id)
          JOIN sign_char_attribute USING (sign_char_id)
          JOIN sign_char_attribute_owner ON sign_char_attribute_owner.sign_char_attribute_id = sign_char_attribute.sign_char_attribute_id
            AND sign_char_attribute_owner.edition_id = editionId
          JOIN position_in_stream USING (sign_id)
          JOIN line_to_sign as lts2 on lts2.sign_id=next_sign_id
          JOIN col_to_line ON lts2.line_id=col_to_line.line_id
          JOIN col_to_line_owner ON col_to_line_owner.col_to_line_id = col_to_line.col_to_line_id
            AND col_to_line_owner.edition_id = editionId
          JOIN line_data ON line_data.line_id = lts2.line_id
          JOIN line_data_owner ON line_data_owner.line_data_id = line_data.line_data_id
            AND line_data_owner.edition_id = editionId
        WHERE lts1.line_id = lineId
           AND  attribute_value_id =11
           AND col_to_line.col_id=fragmentId
        )
      SELECT lineId, lineName
      FROM lineIds

";

    // TODO We have still to connect the fragments in a scroll and to adjust the query accordingly
    public const string GetFragmentIdsQuery = @"
      SELECT col_id AS ColId, name AS ColName
      FROM col_data
        JOIN col_data_owner USING (col_data_id)
        JOIN col_sequence USING(col_id)
      WHERE edition_id = @EditionId
      ORDER BY col_sequence.position
";

    
  }
}