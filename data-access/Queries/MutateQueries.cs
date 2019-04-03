using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;

namespace SQE.Backend.DataAccess.Queries
{
    internal static class OwnedTableInsert
    {
        public static string GetQuery { get; } = @"
INSERT INTO @TableName (@Columns)
VALUES(@Values)
ON DUPLICATE KEY UPDATE @PrimaryKeyName = LAST_INSERT_ID(@PrimaryKeyName)
";
    }
    
    internal static class OwnerTableInsert
    {
        public static string GetQuery { get; } = @"
INSERT INTO @OwnerTableName (@OwnedTablePkName, scroll_version_id)
SELECT t.@OwnedTablePkName, COALESCE(sda.scroll_version_id, t.scroll_version_id)
FROM (SELECT @OwnedTableId AS @OwnedTablePkName, @ScrollVersionId AS scroll_version_id) AS t
LEFT JOIN @OwnerTableName AS sda
  ON sda.@OwnedTablePkName = t.@OwnedTablePkName
  AND sda." + ScrollVersionGroupLimit.LimitToScrollVersionGroup;
        
        
    }
    
    internal static class OwnerTableDelete
    {
        public static string GetQuery { get; } = @"
DELETE FROM @OwnerTableName 
WHERE @OwnedTablePkName = @OwnedTableId
  AND " + ScrollVersionGroupLimit.LimitToScrollVersionGroup;
    }
    
    internal static class MainActionInsert
    {
        public static string GetQuery { get; } = @"INSERT INTO main_action (scroll_version_id) VALUES(@ScrollVersionId)";
    }
    
    internal static class SingleActionInsert
    {
        public static string GetQuery { get; } = @"
INSERT INTO single_action (`main_action_id`, `action`, `table`, `id_in_table`) 
VALUES(@MainActionId, @Action, @TableName, @OwnedTableId)";
    }
    
    internal static class PermissionCheck
    {
        public static string GetQuery { get; } = @"
SELECT may_write, locked 
FROM scroll_version_group 
JOIN scroll_version USING(scroll_version_group_id) 
WHERE scroll_version_id = @ScrollVersionId AND user_id = @UserId";
    }
}