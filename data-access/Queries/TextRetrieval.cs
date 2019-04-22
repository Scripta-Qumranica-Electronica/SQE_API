namespace SQE.SqeHttpApi.DataAccess.Queries
{
    public static class TextRetrieval
    {

      private const string _dataPart = @"
SELECT  scroll_data.name as scroll, scroll_data.scroll_id as scrollId,
            col_data.col_id as fragmentId,col_data.name as fragment,
            line_data.line_id as lineId, line_data.name as line,
            SignId as signId,
            sign_char.sign_char_id as signCharId,
            sign_char.sign as signChar,
            sign_char_attribute.sign_char_attribute_id AS charAttributeId,
            sign_char_attribute.attribute_value_id as attributeValueId,
            attribute_numeric.value as value
            
    FROM sign_ids
      JOIN line_to_sign ON line_to_sign.sign_id=SignId
      JOIN line_data USING (line_id)
      JOIN line_data_owner USING (line_data_id)
      JOIN scroll_version AS sv1 ON sv1.scroll_version_id=line_data_owner.scroll_version_id
      JOIN col_to_line USING (line_id)
      JOIN col_data USING (col_id)
      JOIN col_data_owner USING (col_data_id)
      JOIN scroll_version AS sv2 ON sv2.scroll_version_id=col_data_owner.scroll_version_id
      JOIN scroll_to_col USING (col_id)
      JOIN scroll_data using (scroll_id)
      JOIN scroll_data_owner USING (scroll_data_id)
      JOIN scroll_version AS sv3 ON sv3.scroll_version_id=scroll_data_owner.scroll_version_id
      JOIN sign_char using (sign_id)
      JOIN sign_char_attribute USING (sign_char_id)
      LEFT JOIN attribute_numeric USING (sign_char_attribute_id)
      JOIN sign_char_attribute_owner USING (sign_char_attribute_id)
      JOIN scroll_version as sv4 ON sv4.scroll_version_id=sign_char_attribute_owner.scroll_version_id
    WHERE sv1.scroll_version_group_id=ScrollVersionGroupId
      AND sv2.scroll_version_group_id=ScrollVersionGroupId
      AND sv3.scroll_version_group_id=ScrollVersionGroupId
      AND sv4.scroll_version_group_id=ScrollVersionGroupId
";
      
        public const string GetLineTextByIdQuery = @"
    WITH RECURSIVE sign_ids
    AS (
        SELECT sign_char.sign_id AS SignId, 
               line_to_sign.line_id AS LineId, 
               scroll_version_group_id AS ScrollVersionGroupId
        FROM sign_char
          JOIN sign_char_attribute USING (sign_char_id)
          JOIN line_to_sign USING (sign_id)
          JOIN sign_char_attribute_owner USING (sign_char_attribute_id)
          JOIN scroll_version USING (scroll_version_id)
        WHERE attribute_value_id = 10
          AND line_id = @EntityId
          AND scroll_version_group_id = @ScrollVersionGroupId

        UNION
          SELECT next_sign_id, LineId, ScrollVersionGroupId
          FROM  position_in_stream,
                sign_ids,
                position_in_stream_owner,
                scroll_version AS sv1,
                line_to_sign,
                line_to_sign_owner,
                scroll_version AS sv2
          WHERE position_in_stream.sign_id = SignId
            AND position_in_stream_owner.position_in_stream_id=position_in_stream.position_in_stream_id
            AND sv1.scroll_version_id = position_in_stream_owner.scroll_version_id
            AND sv1.scroll_version_group_id=ScrollVersionGroupId
            AND line_to_sign.sign_id=position_in_stream.next_sign_id
            AND line_to_sign_owner.line_to_sign_id=line_to_sign.line_to_sign_id
            AND sv2.scroll_version_id=line_to_sign_owner.scroll_version_id
            AND sv2.scroll_version_group_id=ScrollVersionGroupId
            AND line_to_sign.line_id=LineId

    )" + _dataPart;

        public const string GetFragmentTextByIdQuery = @"
    WITH RECURSIVE sign_ids
    AS (
        SELECT sign_char.sign_id AS SignId, 
               line_to_sign.line_id AS LineId, 
               col_to_line.col_id AS ColId,
               scroll_version_group_id AS ScrollVersionGroupId
        FROM sign_char
          JOIN sign_char_attribute USING (sign_char_id)
          JOIN line_to_sign USING (sign_id)
          JOIN col_to_line USING (line_id)
          JOIN sign_char_attribute_owner USING (sign_char_attribute_id)
          JOIN scroll_version USING (scroll_version_id)
        WHERE attribute_value_id = 14
          AND col_id =  @EntityId
          AND scroll_version_group_id = @ScrollVersionGroupId

        UNION
          SELECT next_sign_id, LineId, ColId, ScrollVersionGroupId
          FROM  position_in_stream,
                sign_ids,
                position_in_stream_owner,
                scroll_version AS sv1,
                line_to_sign,
                col_to_line,
                col_to_line_owner,
                scroll_version AS sv2
          WHERE position_in_stream.sign_id = SignId
            AND position_in_stream_owner.position_in_stream_id=position_in_stream.position_in_stream_id
            AND sv1.scroll_version_id = position_in_stream_owner.scroll_version_id
            AND sv1.scroll_version_group_id=ScrollVersionGroupId
            AND line_to_sign.sign_id=position_in_stream.next_sign_id
            AND col_to_line.line_id=line_to_sign.line_id
            AND col_to_line_owner.col_to_line_id=col_to_line.col_to_line_id
            AND sv2.scroll_version_id=col_to_line_owner.scroll_version_id
            AND sv2.scroll_version_group_id=ScrollVersionGroupId
            AND col_to_line.col_id=ColId

    )" + _dataPart;
        
    }
}