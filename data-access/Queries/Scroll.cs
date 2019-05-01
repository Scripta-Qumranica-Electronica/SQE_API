using System;
using System.Text;
using System.Linq;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal class ScrollVersionQuery
    {
        private static string _baseQuery = @"
SELECT sv.scroll_version_id as id, 
	   sd.name as name, 
       im.thumbnail_url as thumbnail, 
       svg.locked as locked, 
       last.last_edit as last_edit, 
       user_id as user_id, 
       user_name as user_name 
FROM scroll_version AS sv
JOIN scroll_version_group svg USING (scroll_version_group_id)
JOIN scroll_data_owner sdw USING(scroll_version_id)
JOIN scroll_data sd USING(scroll_data_id)
JOIN user USING(user_id)
LEFT JOIN (SELECT scroll_version_id, MAX(time) AS last_edit FROM main_action GROUP BY time) AS last USING (scroll_version_id)
<<<<<<< HEAD
LEFT JOIN (SELECT ec.scroll_id as scroll_id, MIN(CONCAT(proxy, url, SQE_image.filename)) as thumbnail_url 
=======
LEFT JOIN (SELECT ec.scroll_id as scroll_id, MIN(CONCAT(proxy, url, filename)) as thumbnail_url 
>>>>>>> 2e4abdd34cc531700dfdb472530c3e40d4e02e11
		   FROM edition_catalog ec
           JOIN image_to_edition_catalog USING (edition_catalog_id)
		   JOIN SQE_image ON SQE_image.image_catalog_id = image_to_edition_catalog.image_catalog_id AND SQE_image.type = 0 
           JOIN image_urls USING(image_urls_id)
           WHERE ec.edition_side=0
           GROUP BY scroll_id) im ON (im.scroll_id=sd.scroll_id)
";

        public static string GetQuery(bool limitUser, bool limitScrolls)
        {
            // Build the WHERE clause
            var where = new StringBuilder(" WHERE (user_name='SQE_API'");
            if (limitUser)
            {
                where.Append(" OR user_id=@UserId");
            }
            where.Append(")");

            if (limitScrolls)
                where.Append(" AND sv.scroll_version_id IN @ScrollVersionIds");

            return _baseQuery + where.ToString();
        }


        internal class Result
        {
            
            public uint id { get; set; }
            public string name { get; set; }
            public string thumbnail { get; set; }
            public bool locked { get; set; }
            public DateTime? last_edit { get; set; }
            public uint user_id { get; set; }
            public string user_name { get; set; }
        }
    }

    internal class ScrollVersionGroupQuery
    {
        private static string _baseQuery = @"
SELECT DISTINCT sv2.scroll_version_id as scroll_version_id, svg2.scroll_id as group_id
FROM scroll_version sv 
JOIN scroll_version_group svg USING (scroll_version_group_id)
JOIN scroll_version_group svg2 USING (scroll_id)
JOIN scroll_version sv2 ON (svg2.scroll_version_group_id=sv2.scroll_version_group_id)
";
        private static string _where = "WHERE sv.scroll_version_id=@ScrollVersionId\n";
        private static string _orderBy = "ORDER BY group_id, scroll_version_id\n";

        public static string GetQuery(bool limitScrollVersion)
        {
            var sql = new StringBuilder(_baseQuery);
            if (limitScrollVersion)
            {
                sql.Append(_where);
            }
            sql.Append(_orderBy);

            return sql.ToString();
        }

        internal class Result
        {
            public uint group_id { get; set; }
            public uint scroll_version_id { get; set; }
        }
    }

    internal class ScrollNameQuery
    {
        private static string _baseQuery = @"
SELECT scroll_data_id, scroll_id, name
FROM scroll_data
JOIN scroll_data_owner USING(scroll_data_id)
JOIN scroll_version USING(scroll_version_id)
WHERE " + ScrollVersionGroupLimitQuery.LimitToScrollVersionGroupAndUser;

        public static string GetQuery()
        {
            return _baseQuery;
        }

        internal class Result
        {
            public uint scroll_data_id { get; set; }
            public uint scroll_id { get; set; }
            public string name { get; set; }
        }
    }
    
    internal static class ScrollLockQuery
    {
        public static string GetQuery { get; } = @"
SELECT locked
FROM scroll_version
JOIN scroll_version_group USING(scroll_version_group_id)
WHERE scroll_version_id = @ScrollVersionId";

        internal class Result
        {
            public bool locked { get; set; }  // locked is TINYINT, which is 8-bit unsigned like C# bool.  Is it ok/safe?
        }
    }

    internal static class ScrollVersionGroupLimitQuery
    {
        private const string DefaultLimit = " sv1.user_id = 1 ";

        private const string UserLimit = " sv1.user_id = @UserId ";
        
        private const string CoalesceScrollVersions = @"scroll_version_id IN 
            (SELECT sv2.scroll_version_id
            FROM scroll_version sv1
            JOIN scroll_version_group USING(scroll_version_group_id)
            JOIN scroll_version sv2 ON sv2.scroll_version_group_id = scroll_version_group.scroll_version_group_id
            WHERE sv1.scroll_version_id = @ScrollVersionId";
        
        // You must add a parameter `@ScrollVersionId` to any query using this.
        public const string LimitToScrollVersionGroup = CoalesceScrollVersions + ")";
        
        // You must add a parameter `@ScrollVersionId` to any query using this.
        public const string LimitToScrollVersionGroupNoAuth = CoalesceScrollVersions + " AND " + DefaultLimit + ")";
        
        // You must add the parameters `@ScrollVersionId` and `@UserId` to any query using this.
        public const string LimitToScrollVersionGroupAndUser = CoalesceScrollVersions + " AND (" + DefaultLimit + " OR " + UserLimit + "))";
    }

    internal static class CreateScrollVersionQuery
    {
        // You must add a parameter `@UserId`, `@ScrollVersionId`, and `@MayLock` (0 = false, 1 = true) to use this.
        public const string GetQuery = 
            @"INSERT INTO scroll_version (user_id, scroll_version_group_id, may_write, may_lock) 
            VALUES (@UserId, @ScrollVersionGroupId, 1, @MayLock)";
    }
    
    internal static class CreateScrollVersionGroupQuery
    {
        // You must add a parameter `@ScrollVersionId` to use this.
        public const string GetQuery = 
            @"INSERT INTO scroll_version_group (scroll_id, locked)  
            (SELECT scroll_id, 0
            FROM scroll_version
            JOIN scroll_version_group USING(scroll_version_group_id)
            WHERE scroll_version_id = @ScrollVersionId)";
    }
    
    internal static class CreateScrollVersionGroupAdminQuery
    {
        // You must add a parameter `@ScrollVersionId` and `@UserID` to use this.
        public const string GetQuery = 
            @"INSERT INTO scroll_version_group_admin (scroll_version_group_id, user_id)   
            VALUES (@ScrollVersionGroupId, @UserId)";
    }
    
    internal static class CopyScrollVersionDataForTableQuery
    {
        // You must add a parameter `@ScrollVersionId` and `@CopyToScrollVersionId` to use this.
        public static string GetQuery(string tableName, string tableIdColumn)
        {
            return $@"INSERT IGNORE INTO {tableName} ({tableIdColumn}, scroll_version_id) 
            SELECT {tableIdColumn}, @CopyToScrollVersionId 
            FROM {tableName} 
            WHERE " + ScrollVersionGroupLimitQuery.LimitToScrollVersionGroup;
        }
    }
}
