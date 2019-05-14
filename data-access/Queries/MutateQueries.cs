namespace SQE.SqeHttpApi.DataAccess.Queries
{
    // Bronson - I'm not sure whether all the classes here need to have Query added to their names,
    // as they are not  queries but rather update statements. What do you think?
    // Itay - Sure, let's add Query, it makes things clearer and maybe helps with possible collisions.
    
    // No more ON DUPLICATE KEY UPDATE.  The following INSERTs use subqueries to verify whether something should
    // be inserted or not.  The only question I have is if two transactions from different scroll_versions in the
    // same scroll_version_group hit the x_owner table at the same time, will two entries be made (one for each
    // scroll_version)?  The subquery on OwnerTableInsertQuery checks against cases where one x_id is linked to two
    // members (scroll_version) of the same group (scroll_version_group).  Since we use ReadUncommitted in the transaction,
    // we should be safe in situations like this when two transactions run at the same time.
    internal static class OwnedTableInsertQuery
    {
        public static string GetQuery { get; } = @"
INSERT INTO $TableName ($Columns)
SELECT $Values
FROM dual
WHERE NOT EXISTS
  ( SELECT $Columns                 # This is basically an adhoc uniqueness constraint, which Itay wants to protect
    FROM $TableName                 # against any database schema updates that fail to set a proper uniqueness
    WHERE ($Columns) = ($Values)    # constraint.  It is very fast if the proper uniqueness constraint already exists.
  ) LIMIT 1
";
    }

    // I wanted to do something really clever, which was to use SELECT LAST_INSERT_ID(@PrimaryKeyName) in the subquery
    // above, but that would mysteriously set the last insert id to something incorrect every other time I ran the
    // query.  Thus, I need to run this after OwnedTableInsertQuery in order to get the correct primary key id.
    // So the cost for not trusting ON DUPLICATE KEY UPDATE is two extra queries (the subquery above and this one here).
    internal static class OwnedTableIdQuery
    {
        public static string GetQuery { get; } = @"
SELECT $PrimaryKeyName
FROM $TableName
WHERE ($Columns) = ($Values)
LIMIT 1
";
    }

    internal static class OwnerTableInsertQuery
    {
        public const string GetQuery = @"
INSERT INTO $OwnerTableName ($OwnedTablePkName, edition_editor_id, edition_id)
SELECT t.$OwnedTablePkName, COALESCE(sda.edition_editor_id, t.edition_editor_id), @EditionId
FROM (SELECT @OwnedTableId AS $OwnedTablePkName, @EditionEditorId AS edition_editor_id) AS t
LEFT JOIN $OwnerTableName AS sda
  ON sda.$OwnedTablePkName = t.$OwnedTablePkName
  AND sda.edition_id = @EditionId";
    }
    
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
    
    internal static class PermissionCheckQuery
    {
        public static string GetQuery { get; } = @"
SELECT may_write AS MayWrite, locked AS Locked
FROM edition 
JOIN edition_editor USING(edition_id) 
WHERE edition_id = @EditionId AND user_id = @UserId";
        
        internal class Result 
        {
            public ushort MayWrite { get; set; }
            public ushort Locked { get; set; }
        }
    }
}