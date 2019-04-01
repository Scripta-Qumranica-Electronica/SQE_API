using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace SQE.Backend.DataAccess.Queries
{
    internal static class OwnerTables
    {
        public const string GetQuery = @"
SELECT `TABLE_NAME` as tableName
FROM `information_schema`.`TABLES`
WHERE `TABLE_NAME` LIKE '%_owner'
";

        internal class Result
        {
            public string TableName { get; set; }
        }
    }
    
    // The Docs tell me that this is guaranteed to give the expected last_insert_id
    // when working with a connection.  The connection has its own thread and won't
    // be affected by other DB connections.
    internal static class LastInsertId
    {
        public const string GetQuery = @"SELECT LAST_INSERT_ID()";
    }
}
