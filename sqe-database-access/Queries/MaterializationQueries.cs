namespace SQE.DatabaseAccess.Queries
{
	internal static class QueuedMaterializationsQuery
	{
		public const string GetQuery = @"
SELECT edition_id AS EditionId,
       initial_sign_interpretation_id AS SignInterpretationId,
       time_initiated AS CreatedDate,
       CURRENT_DATE() AS CurrentTime
FROM materialized_sign_stream_schedule";
	}

	internal static class CreateQueuedMaterializationsQuery
	{
		public const string GetQuery = @"
INSERT INTO materialized_sign_stream_schedule (edition_id, initial_sign_interpretation_id)
VALUES (@EditionId, @SignInterpretationId)";
	}

	internal static class DeleteQueuedMaterializationQuery
	{
		public const string GetQuery = @"
DELETE FROM materialized_sign_stream_schedule
WHERE initial_sign_interpretation_id = @SignInterpretationId
	AND edition_id = @EditionId";
	}

	internal static class DeleteQueuedMaterializationsQuery
	{
		public const string GetQuery = @"
DELETE FROM materialized_sign_stream_schedule
WHERE initial_sign_interpretation_id in @SignStreamInterpretationIds
	AND edition_id = @EditionId";
	}

	internal static class CreateMaterializationsQuery
	{
		public const string GetQuery = @"
INSERT INTO materialized_sign_stream (edition_id, initial_sign_interpretation_id, materialized_text)
VALUES (@EditionId, @SignInterpretationId, @MaterializedText)";
	}

	internal static class DeleteMaterializationsQuery
	{
		public const string GetQuery = @"
DELETE FROM materialized_sign_stream
WHERE initial_sign_interpretation_id = @SignInterpretationId
	AND edition_id = @EditionId";
	}

	internal static class InitialStreamSignInterpretationForEdition
	{
		public const string GetQuery = @"
SELECT position_in_stream.sign_interpretation_id
FROM position_in_stream
JOIN position_in_stream_owner USING(position_in_stream_id)
LEFT JOIN position_in_stream AS pos2 ON pos2.next_sign_interpretation_id = position_in_stream.sign_interpretation_id
LEFT JOIN position_in_stream_owner AS poso2 ON poso2.position_in_stream_id = pos2.position_in_stream_id
    AND poso2.edition_id = position_in_stream_owner.edition_id
WHERE position_in_stream_owner.edition_id = @EditionId
    AND pos2.sign_interpretation_id IS NULL";
	}

	/// <summary>
	///  This query is very fast, but can result in false positives,
	///  but it will not give a false negative.
	///  It will return [0] for no paths found.
	/// </summary>
	internal static class QuickConfirmExistingPath
	{
		public const string GetQuery = @"
SELECT COUNT(linkid)
FROM sign_stream
WHERE latch = 'dijkstra'
	AND origid = @SignInterpretationId
	AND destid = @NextSignInterpretationId
";
	}

	/// <summary>
	///  This search can be (much) slower than QuickConfirmExistingPath,
	///  but it is precise and will not return false negatives or positives.
	///  It will return [0] for no paths found.
	/// </summary>
	internal static class PreciseConfirmExistingPath
	{
		public const string GetQuery = @"
WITH RECURSIVE sign_interpretation_ids
                   AS (
        SELECT position_in_stream.position_in_stream_id,
               position_in_stream.sign_interpretation_id AS signInterpretationId,
               position_in_stream.next_sign_interpretation_id,
               position_in_stream_owner.edition_id
        FROM position_in_stream
                 JOIN position_in_stream_owner
                      ON position_in_stream_owner.position_in_stream_id = position_in_stream.position_in_stream_id
                          AND position_in_stream_owner.edition_id = @EditionId
        WHERE position_in_stream.sign_interpretation_id = @SignInterpretationId

        UNION

        SELECT     position_in_stream.position_in_stream_id,
                   position_in_stream.sign_interpretation_id AS signInterpretationId,
                   position_in_stream.next_sign_interpretation_id,
                   sign_interpretation_ids.edition_id
        FROM  sign_interpretation_ids
                  JOIN position_in_stream
                       ON position_in_stream.sign_interpretation_id = sign_interpretation_ids.next_sign_interpretation_id
                        AND position_in_stream.sign_interpretation_id != @NextSignInterpretationId
                  JOIN position_in_stream_owner
                       ON position_in_stream_owner.position_in_stream_id = position_in_stream.position_in_stream_id
                           AND position_in_stream_owner.edition_id = sign_interpretation_ids.edition_id
    )

SELECT COUNT(signInterpretationId)
FROM sign_interpretation_ids
WHERE next_sign_interpretation_id = @NextSignInterpretationId
";
	}

	// 	internal static class AllSignStreamPossibilities1
	// 	{
	// 		public const string GetQuery = @"
	// SELECT DISTINCT sign_interpretation.sign_interpretation_id AS SignInterpretationId,
	//        sign_interpretation.character AS `Character`,
	//        sign_interpretation.is_variant AS IsVariant,
	//        position_in_stream.next_sign_interpretation_id AS NextSignInterpretationId,
	//        attribute_value.attribute_id AS AttributeId,
	//        sign_interpretation_attribute.attribute_value_id AS AttributeValueId
	// FROM sign_stream
	// JOIN sign_interpretation ON sign_interpretation.sign_interpretation_id = linkid
	// JOIN sign_interpretation_attribute USING(sign_interpretation_id)
	// JOIN sign_interpretation_attribute_owner USING(sign_interpretation_attribute_id)
	// JOIN position_in_stream ON position_in_stream.sign_interpretation_id = linkid
	// JOIN position_in_stream_owner ON position_in_stream_owner.position_in_stream_id = position_in_stream.position_in_stream_id
	//     AND position_in_stream_owner.edition_id = sign_interpretation_attribute_owner.edition_id
	// JOIN attribute_value USING(attribute_value_id)
	// JOIN attribute_value_owner ON attribute_value.attribute_value_id = attribute_value_owner.attribute_value_id
	//     AND attribute_value_owner.edition_id = sign_interpretation_attribute_owner.edition_id
	// WHERE latch='dijkstras'
	//     AND origid = @SignInterpretationId
	//     AND sign_interpretation_attribute_owner.edition_id = @EditionId
	// ORDER BY seq ASC";
	// 	}

	internal static class AllSignStreamPossibilities
	{
		public const string GetQuery = @"
SELECT DISTINCT sign_interpretation.sign_interpretation_id AS SignInterpretationId,
                position_in_stream.next_sign_interpretation_id AS NextSignInterpretationId,
                sign_interpretation_character.character AS `Character`,
                sign_interpretation_character_owner.priority AS IsVariant,
                position_in_stream.next_sign_interpretation_id AS NextSignInterpretationId,
                attribute_value.attribute_id AS AttributeId,
                sign_interpretation_attribute.attribute_value_id AS AttributeValueId
FROM position_in_stream
JOIN position_in_stream_owner
    ON position_in_stream_owner.position_in_stream_id = position_in_stream.position_in_stream_id
JOIN sign_interpretation
    ON sign_interpretation.sign_interpretation_id = position_in_stream.sign_interpretation_id
JOIN sign_interpretation_character ON sign_interpretation_character.sign_interpretation_id = position_in_stream.sign_interpretation_id
JOIN sign_interpretation_character_owner ON sign_interpretation_character_owner.sign_interpretation_character_id = sign_interpretation_character.sign_interpretation_character_id
    AND sign_interpretation_character_owner.edition_id = position_in_stream_owner.edition_id
JOIN sign_interpretation_attribute
    ON sign_interpretation_attribute.sign_interpretation_id = position_in_stream.sign_interpretation_id
JOIN sign_interpretation_attribute_owner
    ON sign_interpretation_attribute_owner.sign_interpretation_attribute_id = sign_interpretation_attribute.sign_interpretation_attribute_id
    AND position_in_stream_owner.edition_id = sign_interpretation_attribute_owner.edition_id
JOIN attribute_value USING(attribute_value_id)
JOIN attribute_value_owner ON attribute_value.attribute_value_id = attribute_value_owner.attribute_value_id
    AND attribute_value_owner.edition_id = sign_interpretation_attribute_owner.edition_id
WHERE position_in_stream_owner.edition_id = @EditionId";
	}

	/// <summary>
	///  This Query will take a previous sign interpretation id (a) and a
	///  next sign interpretation id (c) for an edition and search to see if
	///  there is a single sign interpretation already linking the two:
	///  a -> b -> c, for which it returns the `sign_id` of "b".
	/// </summary>
	internal static class PossibleIntermediarySignInSignStream
	{
		public const string GetQuery = @"
SELECT DISTINCT sign_interpretation.sign_id
FROM sign_stream

JOIN position_in_stream piso ON piso.sign_interpretation_id = sign_stream.origid
    AND piso.next_sign_interpretation_id = sign_stream.linkid
JOIN position_in_stream_owner pisoo ON pisoo.position_in_stream_id = piso.position_in_stream_id

JOIN position_in_stream pisa ON pisa.sign_interpretation_id = sign_stream.linkid
    AND pisa.next_sign_interpretation_id = sign_stream.destid
JOIN position_in_stream_owner pisao ON pisao.position_in_stream_id = piso.position_in_stream_id
    AND pisao.edition_id = pisoo.edition_id

JOIN position_in_stream pisd ON pisd.sign_interpretation_id = sign_stream.destid
JOIN position_in_stream_owner pisdo ON pisdo.position_in_stream_id = pisd.position_in_stream_id
    AND pisdo.edition_id = pisoo.edition_id

JOIN sign_interpretation ON sign_interpretation.sign_interpretation_id = pisa.sign_interpretation_id
WHERE latch='dijkstra'
    AND origid = @PreviousSignInterpretationId
    AND destid = @NextSignInterpretationId
    AND linkid != origid
    AND linkid != destid
    AND pisoo.edition_id = @EditionId";
	}
}
