namespace SQE.DatabaseAccess.Queries
{
	// Bronson - I'm not sure whether all the classes here need to have Query added to their names,
	// as they are not  queries but rather update statements. What do you think?
	// Itay - Sure, let's add Query, it makes things clearer and maybe helps with possible collisions.

	// No more ON DUPLICATE KEY UPDATE.  The following INSERTs use subqueries to verify whether something should
	// be inserted or not.  The only possible problem is if two transactions from different edition_editors in the
	// same edition hit the x_owner table at the same time. The subquery on OwnerTableInsertQuery checks against
	// cases where one x_id is linked twice to the same edition_id.  Without a uniqueness constraint on x_id + edition_id
	// in the database this query would still allow the (however unlikely) situation that two simultaneous transactions
	// could write the same x_id + edition_id, since the query in each transaction would not see the uncommitted
	// mutation from the other (we do not use ReadUncommitted).
	// Note that the NULL-safe equal to operator must be used here if we try to insert any null values, because we don't
	// want duplicate entries due to null values (also, this overcomes the limitations of the unique constraints, since
	// nulls are not unique). This must be checked upon usage of this query. You will have severe performance problems
	// if you have tables with geometry and nullable items. Don't do that!

	// The SQL was just updated here for performance reasons. It turns out the the previous approach,
	// (list of column names) <=> (list of values) in the WHERE NOT EXISTS sub select performs very poorly,
	// like taking over a second in some cases. Now we replace $Where with a more traditional matching pattern:
	// WHERE col1 <=> val1 AND col2 <=> val2 AND ... . This is incredibly faster.
	internal static class OwnedTableInsertQuery
	{
		public static string GetQuery() => @"
INSERT INTO $TableName ($Columns, creator_id)
SELECT $Values, @UserId
FROM dual
WHERE NOT EXISTS
  ( SELECT $Columns     # This is basically an adhoc uniqueness constraint, which Itay wants to protect
    FROM $TableName     # against any database schema updates that fail to set a proper uniqueness
    WHERE $Where        # constraint.  It is very fast if the proper uniqueness constraint already exists.
    LIMIT 1
  ) LIMIT 1
";
	}

	// I wanted to do something really clever, which was to use SELECT LAST_INSERT_ID(@PrimaryKeyName) in the subquery
	// above, but that would mysteriously set the last insert id to something incorrect every other time I ran the
	// query.  Thus, I need to run this after OwnedTableInsertQuery in order to get the correct primary key id.
	// So the cost for not trusting ON DUPLICATE KEY UPDATE is two extra queries (the subquery above and this one here).
	// Note that the NULL-safe equal to operator must be used here if we try to insert any null values, because we don't
	// want duplicate entries due to null values (also, this overcomes the limitations of the unique constraints, since
	// nulls are not unique). This must be checked upon usage of this query. You will have severe performance problems
	// if you have tables with geometry and nullable items. Don't do that!

	// The SQL was just updated here for performance reasons. It turns out the the previous approach,
	// (list of column names) <=> (list of values) in the WHERE NOT EXISTS sub select performs very poorly,
	// like taking over a second in some cases. Now we replace $Where with a more traditional matching pattern:
	// WHERE col1 <=> val1 AND col2 <=> val2 AND ... . This is incredibly faster.
	internal static class OwnedTableIdQuery
	{
		public static string GetQuery() => @"
SELECT $PrimaryKeyName
FROM $TableName
WHERE $Where
LIMIT 1
";
	}

	internal static class OwnerTableInsertQuery
	{
		public const string GetQuery = @"
INSERT INTO $OwnerTableName ($OwnedTablePkName, edition_editor_id, edition_id)
SELECT @OwnedTableId, @EditionEditorId, @EditionId
FROM dual
WHERE NOT EXISTS (
    SELECT $OwnedTablePkName, edition_editor_id, edition_id
    FROM $OwnerTableName
    WHERE $OwnedTablePkName = @OwnedTableId
        AND edition_id = @EditionId
) LIMIT 1";
	}

	// TODO: Check if we really need to be using the @OwnedTableId, maybe we can use a JOIN with the actual record too.
	internal static class OwnerTableDeleteQuery
	{
		public const string GetQuery = @"
DELETE FROM $OwnerTableName
WHERE $OwnedTablePkName = @OwnedTableId
  AND edition_id = @EditionId";
	}

	internal static class MainActionInsertQuery
	{
		public const string GetQuery = @"
          INSERT INTO main_action (edition_id, edition_editor_id)
          VALUES(@EditionId, @EditionEditorId)";
	}

	internal static class SingleActionInsertQuery
	{
		public static string GetQuery { get; } = @"
INSERT INTO single_action (`main_action_id`, `action`, `table`, `id_in_table`)
VALUES(@MainActionId, @Action, @TableName, @OwnedTableId)";
	}
}
