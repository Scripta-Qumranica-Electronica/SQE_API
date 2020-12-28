namespace SQE.DatabaseAccess.Queries
{
	public static class GetTextChunk
	{
		// NOTE I (Ingo) change the query to reflect the integration of the numeric value into the attribute table
		// which became necessary by deleting the table attribute_numeric

		/// <summary>
		///  Retrieves all textual data for a chunk of text
		///  @startId is the Id of the first sign
		///  @endId is the Id of the last sign
		///  @editionId is the Id of the edition the text is to be taken from
		/// </summary>
		public const string GetQuery = @"
WITH RECURSIVE sign_interpretation_ids
	AS (
		SELECT 	position_in_stream.position_in_stream_id,
				position_in_stream.sign_interpretation_id AS signInterpretationId,
				position_in_stream.next_sign_interpretation_id,
				position_in_stream_owner.is_main,
				position_in_stream_owner.edition_editor_id,
				position_in_stream_owner.edition_id,
				position_in_stream.creator_id,
				1 AS sequence
		FROM position_in_stream
			JOIN position_in_stream_owner
				ON position_in_stream_owner.position_in_stream_id = position_in_stream.position_in_stream_id
					AND position_in_stream_owner.edition_id = @EditionId
		WHERE position_in_stream.sign_interpretation_id = @StartId

      UNION

		SELECT 	position_in_stream.position_in_stream_id,
				position_in_stream.sign_interpretation_id AS signInterpretationId,
				position_in_stream.next_sign_interpretation_id,
				position_in_stream_owner.is_main,
				position_in_stream_owner.edition_editor_id,
				sign_interpretation_ids.edition_id,
				position_in_stream.creator_id,
				sequence + 1 AS sequence
		FROM  sign_interpretation_ids
			JOIN position_in_stream
				ON position_in_stream.sign_interpretation_id = sign_interpretation_ids.next_sign_interpretation_id
				AND signInterpretationId != @EndId
			JOIN position_in_stream_owner
				ON position_in_stream_owner.position_in_stream_id = position_in_stream.position_in_stream_id
					AND position_in_stream_owner.edition_id = sign_interpretation_ids.edition_id
	)

SELECT distinctrow 	manuscript_data.manuscript_id AS manuscriptId,
		manuscript_data.name AS editionName,
		manuscript_data_owner.edition_editor_id AS manuscriptAuthor,

		edition.copyright_holder AS copyrightHolder,
		edition.collaborators,

		text_fragment_to_line.text_fragment_id AS textFragmentId,
        text_fragment_data.name AS textFragmentName,
		text_fragment_data_owner.edition_editor_id AS TextFragmentEditorId,

		line_data.line_id AS lineId,
		line_data.name AS lineName,
		line_data_owner.edition_editor_id AS lineAuthor,

		sign_interpretation.sign_id AS signId,
		sign_interpretation_ids.next_sign_interpretation_id AS nextSignInterpretationId,
		sign_interpretation_ids.edition_editor_id AS signSequenceAuthor,
		sign_interpretation_ids.creator_id AS PositionCreatorId,

		signInterpretationId,
		sign_interpretation_character.`character` AS `character`,
		sign_interpretation_character_owner.priority AS IsVariant,
		sign_interpretation.sign_id AS SignId,
		interpretation_commentary.commentary AS InterpretationCommentary,
		interpretation_commentary.creator_id AS InterpretationCommentaryCreator,
		interpretation_commentary.edition_editor_id AS InterpretationCommentaryEditor,

		sign_interpretation_attribute.sign_interpretation_attribute_id AS SignInterpretationAttributeId,
		sign_interpretation_attribute.attribute_value_id AS AttributeValueId,
		sign_interpretation_attribute.creator_id AS SignInterpretationAttributeCreatorId,
		sign_interpretation_attribute.sequence AS Sequence,
		sign_interpretation_attribute_owner.edition_editor_id AS SignInterpretationAttributeEditorId,
		attribute_commentary.commentary AS AttributeCommentary,
		attribute_commentary.creator_id AS AttributeCommentaryCreatorId,
		attribute_commentary.edition_editor_id AS AttributeCommentaryEditorId,

		roi.sign_interpretation_roi_id AS SignInterpretationRoiId,
		roi.sign_interpretation_id AS SignInterpretationId,
		roi.edition_editor_id AS SignInterpretationRoiAuthor,
		roi.values_set AS ValuesSet,
		roi.exceptional AS Exceptional,
		ST_ASTEXT(roi.path) AS Shape,
		roi.translate_x AS TranslateX,
		roi.translate_y AS TranslateY,
		roi.stance_rotation AS StanceRotation,
		roi.artefact_id AS ArtefactId,

		sign_stream_section.sign_stream_section_id AS WordId



FROM sign_interpretation_ids
	JOIN sign_interpretation ON sign_interpretation.sign_interpretation_id = signInterpretationId
	JOIN sign_interpretation_character ON sign_interpretation_character.sign_interpretation_id = signInterpretationId
	JOIN sign_interpretation_character_owner ON sign_interpretation_character_owner.sign_interpretation_character_id = sign_interpretation_character.sign_interpretation_character_id
		AND sign_interpretation_character_owner.edition_id = sign_interpretation_ids.edition_id
	JOIN sign_interpretation_attribute ON sign_interpretation_attribute.sign_interpretation_id = signInterpretationId
	JOIN sign_interpretation_attribute_owner
		ON sign_interpretation_attribute_owner.sign_interpretation_attribute_id = sign_interpretation_attribute.sign_interpretation_attribute_id
		AND sign_interpretation_attribute_owner.edition_id = sign_interpretation_ids.edition_id
	JOIN attribute_value USING(attribute_value_id)

	JOIN line_to_sign ON line_to_sign.sign_id = sign_interpretation.sign_id
	JOIN line_data USING (line_id)
	JOIN line_data_owner ON line_data_owner.line_data_id = line_data.line_data_id
	  AND line_data_owner.edition_id = sign_interpretation_ids.edition_id

	JOIN text_fragment_to_line USING (line_id)
	JOIN text_fragment_data USING (text_fragment_id)
	JOIN text_fragment_data_owner
		ON text_fragment_data_owner.text_fragment_data_id = text_fragment_data.text_fragment_data_id
			AND text_fragment_data_owner.edition_id = sign_interpretation_ids.edition_id

	JOIN manuscript_to_text_fragment USING (text_fragment_id)
	JOIN manuscript_data USING (manuscript_id)
	JOIN manuscript_data_owner ON manuscript_data_owner.manuscript_data_id = manuscript_data.manuscript_data_id
		AND manuscript_data_owner.edition_id = sign_interpretation_ids.edition_id

	LEFT JOIN
		(SELECT	sign_interpretation_roi.sign_interpretation_roi_id,
				sign_interpretation_roi.sign_interpretation_id,
				sign_interpretation_roi_owner.edition_editor_id,
				sign_interpretation_roi.values_set,
				sign_interpretation_roi.exceptional,
				roi_shape.path AS path,
				roi_position.translate_x,
				roi_position.translate_y,
				roi_position.stance_rotation,
				roi_position.artefact_id,
				sign_interpretation_roi_owner.edition_id
		FROM sign_interpretation_roi
			JOIN sign_interpretation_roi_owner
				ON sign_interpretation_roi_owner.sign_interpretation_roi_id = sign_interpretation_roi.sign_interpretation_roi_id
			JOIN roi_shape ON roi_shape.roi_shape_id = sign_interpretation_roi.roi_shape_id
			JOIN roi_position ON roi_position.roi_position_id = sign_interpretation_roi.roi_position_id
		)
		AS roi
			ON roi.sign_interpretation_id = sign_interpretation.sign_interpretation_id
				AND roi.edition_id = sign_interpretation_ids.edition_id

	LEFT JOIN
		(SELECT sign_interpretation_id, commentary, creator_id, edition_id, edition_editor_id
		FROM sign_interpretation_commentary
		JOIN sign_interpretation_commentary_owner USING(sign_interpretation_commentary_id)
		WHERE attribute_id IS NULL
		) AS interpretation_commentary ON interpretation_commentary.sign_interpretation_id = sign_interpretation.sign_interpretation_id
				AND interpretation_commentary.edition_id = sign_interpretation_ids.edition_id

	LEFT JOIN
		(SELECT sign_interpretation_id, attribute_id, commentary, creator_id, edition_id, edition_editor_id
		FROM sign_interpretation_commentary
		JOIN sign_interpretation_commentary_owner USING(sign_interpretation_commentary_id)
		) AS attribute_commentary ON attribute_commentary.sign_interpretation_id = sign_interpretation.sign_interpretation_id
				AND attribute_commentary.attribute_id = attribute_value.attribute_id
				AND attribute_commentary.edition_id = sign_interpretation_ids.edition_id

	JOIN edition ON edition.edition_id = sign_interpretation_ids.edition_id

	LEFT JOIN position_in_stream_to_section_rel USING (position_in_stream_id)
    LEFT JOIN sign_stream_section USING(sign_stream_section_id)
    LEFT JOIN sign_stream_section_owner
        ON sign_stream_section.sign_stream_section_id=sign_stream_section_owner.sign_stream_section_id
        AND sign_stream_section_owner.edition_id = sign_interpretation_ids.edition_id

ORDER BY sign_interpretation_ids.sequence,
	line_to_sign.sign_id,
	sign_interpretation_ids.is_main,
	sign_interpretation_character_owner.priority desc,
	sign_interpretation_ids.signInterpretationId,
	nextSignInterpretationId,
	SignInterpretationAttributeId,
	SignInterpretationRoiId
";
	}

	internal static class GetLineTerminators
	{
		/// <summary>
		///  Retrieves the first and last sign of a line
		///  @entityId is the Id of line
		///  @editionId is the Id of the edition the line is to be searched
		/// </summary>
		public const string GetQuery = @"
SELECT sign_interpretation.sign_interpretation_id
FROM line_to_sign
	JOIN edition ON edition.edition_id = @EditionId
	JOIN edition_editor ON edition_editor.edition_id = edition.edition_id
    JOIN line_to_sign_owner ON line_to_sign_owner.line_to_sign_id = line_to_sign.line_to_sign_id
    	AND line_to_sign_owner.edition_id = edition.edition_id
	JOIN sign_interpretation USING (sign_id)
	JOIN sign_interpretation_attribute USING (sign_interpretation_id)
	JOIN sign_interpretation_attribute_owner
	    ON sign_interpretation_attribute_owner.sign_interpretation_attribute_id = sign_interpretation_attribute.sign_interpretation_attribute_id
		AND sign_interpretation_attribute_owner.edition_id=edition.edition_id

WHERE line_id = @EntityId
	AND (attribute_value_id = 10 OR attribute_value_id = 11)
	AND (edition_editor.user_id = @UserId OR edition.public = 1)
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
          JOIN edition ON edition.edition_id = @EditionId
        WHERE text_fragment_id = @TextFragmentId
          AND sign_interpretation_attribute_owner.edition_id = @EditionId
          AND line_data_owner.edition_id = @EditionId
          AND (edition_editor.user_id = @UserId OR edition.public = 1)
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
WITH RECURSIVE cte_fragment AS (
    SELECT pitfs_1.text_fragment_id, 0 AS sequence
    FROM position_in_text_fragment_stream AS pitfs_1
    JOIN position_in_text_fragment_stream_owner AS pitfso_1
        ON pitfs_1.position_in_text_fragment_stream_id = pitfso_1.position_in_text_fragment_stream_id
        AND pitfso_1.edition_id=@EditionId
    WHERE (
        SELECT count(pitfs_2.position_in_text_fragment_stream_id)
            FROM position_in_text_fragment_stream AS pitfs_2
            JOIN position_in_text_fragment_stream_owner AS pitfso_2
                ON pitfs_2.position_in_text_fragment_stream_id = pitfso_2.position_in_text_fragment_stream_id
                AND pitfso_2.edition_id=@EditionId
            WHERE pitfs_2.next_text_fragment_id=pitfs_1.text_fragment_id
                  ) = 0

    UNION
    SELECT next_text_fragment_id, sequence+1
    FROM cte_fragment, position_in_text_fragment_stream as pitfs
        JOIN position_in_text_fragment_stream_owner AS pitfso
            ON pitfs.position_in_text_fragment_stream_id = pitfso.position_in_text_fragment_stream_id
                AND edition_id=@EditionId
    WHERE pitfs.text_fragment_id = cte_fragment.text_fragment_id
)

SELECT text_fragment_id AS TextFragmentId,
       name AS TextFragmentName,
       cte_fragment.sequence AS Position,
	   tfdo.edition_editor_id AS EditionEditorId
FROM cte_fragment
    JOIN text_fragment_data USING(text_fragment_id)
    JOIN text_fragment_data_owner tfdo USING (text_fragment_data_id)
WHERE edition_id=@EditionId
ORDER BY cte_fragment.sequence
";
	}

	internal static class GetFragmentDataOld
	{
		public const string GetQuery = @"
WITH RECURSIVE text_fragment_ids
	AS (
	    SELECT 	position_in_text_fragment_stream.text_fragment_id AS textFragmentId,
		    position_in_text_fragment_stream.position_in_text_fragment_stream_id AS textFragmentPositionId,
		    position_in_text_fragment_stream.next_text_fragment_id AS nextTextFragmentId,
		    position_in_text_fragment_stream_owner.edition_id AS editionId,
		    @X := 0 AS sequence
	    FROM position_in_text_fragment_stream_owner
			JOIN position_in_text_fragment_stream USING(position_in_text_fragment_stream_id)
		WHERE edition_id = @EditionId
			AND (position_in_text_fragment_stream.text_fragment_id NOT IN (
				SELECT position_in_text_fragment_stream.next_text_fragment_id
				FROM position_in_text_fragment_stream_owner
				JOIN position_in_text_fragment_stream USING(position_in_text_fragment_stream_id)
				WHERE edition_id = @EditionId
				)
			)
		UNION

		SELECT 	position_in_text_fragment_stream.next_text_fragment_id AS textFragmentId,
		    position_in_text_fragment_stream.position_in_text_fragment_stream_id AS textFragmentPositionId,
			position_in_text_fragment_stream.next_text_fragment_id AS nextTextFragmentId,
		    position_in_text_fragment_stream_owner.edition_id AS editionId,
			@X := @X + 1 AS sequence
		FROM text_fragment_ids
		JOIN position_in_text_fragment_stream
			ON position_in_text_fragment_stream.text_fragment_id = text_fragment_ids.textFragmentId
		JOIN position_in_text_fragment_stream_owner
			ON position_in_text_fragment_stream_owner.edition_id = text_fragment_ids.editionId
				AND position_in_text_fragment_stream_owner.position_in_text_fragment_stream_id = text_fragment_ids.textFragmentPositionId
	)
SELECT	text_fragment_id AS TextFragmentId, name AS TextFragmentName,
		text_fragment_ids.sequence AS Position,
		text_fragment_data_owner.edition_editor_id AS EditionEditorId
FROM text_fragment_ids
	JOIN text_fragment_data ON text_fragment_data.text_fragment_id = text_fragment_ids.textFragmentId
	JOIN text_fragment_data_owner ON text_fragment_data_owner.text_fragment_data_id = text_fragment_data.text_fragment_data_id
		AND text_fragment_data_owner.edition_id = text_fragment_ids.editionId
	JOIN edition_editor ON edition_editor.edition_id = text_fragment_ids.editionId
	JOIN edition ON edition.edition_id = text_fragment_ids.editionId
WHERE edition_editor.user_id = @UserId OR edition.public = 1
ORDER BY text_fragment_ids.sequence
      ";
	}

	internal static class GetFragmentNameById
	{
		public const string GetQuery = @"
SELECT text_fragment_data.name
FROM text_fragment_data
JOIN text_fragment_data_owner USING(text_fragment_data_id)
JOIN edition USING(edition_id)
JOIN edition_editor USING(edition_id)
WHERE text_fragment_data.text_fragment_id = @TextFragmentId
  AND edition_id = @EditionId
  AND (edition_editor.user_id = @UserId OR edition.public = 1)
";
	}

	internal static class GetTextFragmentArtefacts
	{
		public const string Query = @"
SELECT DISTINCT artefact_id AS ArtefactId,
       artefact_data.name AS Name
FROM text_fragment_data
JOIN text_fragment_data_owner ON text_fragment_data_owner.text_fragment_data_id = text_fragment_data.text_fragment_data_id
   AND text_fragment_data_owner.edition_id = @EditionId
JOIN text_fragment_to_line USING(text_fragment_id)
JOIN text_fragment_to_line_owner ON text_fragment_to_line_owner.text_fragment_to_line_id = text_fragment_to_line.text_fragment_to_line_id
   AND text_fragment_to_line_owner.edition_id = @EditionId
JOIN line_to_sign USING(line_id)
JOIN line_to_sign_owner ON line_to_sign_owner.line_to_sign_id = line_to_sign.line_to_sign_id
   AND line_to_sign_owner.edition_id = @EditionId
JOIN sign_interpretation USING(sign_id)
JOIN sign_interpretation_roi USING(sign_interpretation_id)
JOIN sign_interpretation_roi_owner ON sign_interpretation_roi_owner.sign_interpretation_roi_id = sign_interpretation_roi.sign_interpretation_roi_id
   AND sign_interpretation_roi_owner.edition_id = @EditionId
JOIN roi_position USING(roi_position_id)
JOIN artefact_data USING(artefact_id)
JOIN artefact_data_owner ON artefact_data_owner.artefact_data_id = artefact_data.artefact_data_id
   AND artefact_data_owner.edition_id = @EditionId
JOIN edition ON edition.edition_id = @EditionId
JOIN edition_editor ON edition_editor.edition_id = @EditionId
WHERE text_fragment_data.text_fragment_id = @TextFragmentId
   AND (edition.public = 1 OR edition_editor.user_id = @UserId)";
	}

	internal static class TextFragmentAttributes
	{
		public const string GetQuery = @"
SELECT DISTINCT attribute_value.attribute_value_id AS attributeValueId,
                attribute_value.string_value AS attributeValueString,
                attribute_value.attribute_id AS attributeId,
                attribute.name AS attributeString
FROM attribute_value
JOIN attribute_value_owner
	ON attribute_value_owner.attribute_value_id = attribute_value.attribute_value_id
	AND attribute_value_owner.edition_id = @EditionId
JOIN attribute USING(attribute_id)
JOIN attribute_owner
	ON attribute_owner.attribute_id = attribute.attribute_id
	AND attribute_owner.edition_id = @EditionId
";
	}

	//TODO Can be deleted, use DatabaseWriter.SimpleInsertAsync (Ingo)
	/*internal static class CreateManuscript
	{
		public const string GetQuery = @"
INSERT INTO manuscript () VALUES()
";
	}*/

	//TODO Can be deleted (Ingo
	/*internal static class CreateTextFragment
	{
	    public const string GetQuery = @"
INSERT INTO text_fragment () VALUES()
";
	}*/

	internal static class GetTextFragmentByName
	{
		public const string GetQuery = @"
SELECT text_fragment_id AS TextFragmentId,
       name AS TextFragmentName
FROM text_fragment_data
JOIN text_fragment_data_owner tfdo USING (text_fragment_data_id)
WHERE name LIKE @Name and edition_id=@EditionId
";
	}

	internal static class GetTextFragmentDataId
	{
		public const string GetQuery = @"
SELECT text_fragment_data_id
FROM text_fragment_data
JOIN text_fragment_data_owner USING (text_fragment_data_id)
WHERE text_fragment_data.text_fragment_id = @TextFragmentId
  AND text_fragment_data_owner.edition_id=@EditionId
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

	internal static class GetSignInterpretationIdsForSignIdQuery
	{
		public const string GetQuery = @"
			SELECT DISTINCT sign_interpretation_id
			FROM sign_interpretation
			JOIN sign_interpretation_attribute USING (sign_interpretation_id)
			JOIN sign_interpretation_attribute_owner USING (sign_interpretation_attribute_id)
			WHERE sign_id = @SignId
			AND edition_id= @EditionId
		";
	}

	internal static class GetSignInterpretationIdQuery
	{
		public const string GetQuery = @"
			SELECT sign_interpretation_id
			FROM sign_interpretation
			JOIN sign_interpretation_character USING(sign_interpretation_id)
			JOIN sign_interpretation_character_owner ON sign_interpretation_character_owner.sign_interpretation_character_id = sign_interpretation_character.sign_interpretation_character_id
			WHERE sign_id = @SignId
			  	AND sign_interpretation_character_owner.edition_id = @EditionId
				AND `character`= @Character
		";
	}

	internal static class AddSignInterpretationQuery
	{
		public const string GetQuery = @"
			INSERT INTO sign_interpretation
				(sign_id)
				VALUES (@SignId)
			ON DUPLICATE KEY UPDATE sign_interpretation_id = LAST_INSERT_ID(sign_interpretation_id)
		";
	}
}
