using System;
using System.Text;
using System.Linq;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal class EditionGroupQuery
    {
<<<<<<< HEAD
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
=======
        private const string _baseQuery = @"
SELECT ed2.edition_id AS edition_id,
       edition_editor.is_admin AS admin,
	   scroll_data.name AS name, 
       im.thumbnail_url AS thumbnail, 
       ed2.locked AS locked,
       edition_editor.may_lock AS may_lock,
       edition_editor.may_lock AS may_write, 
       last.last_edit AS last_edit, 
       user.user_id AS user_id, 
       user.user_name AS user_name 
FROM edition AS ed1
JOIN edition AS ed2 ON ed1.scroll_id = ed2.scroll_id
JOIN edition_editor ON edition_editor.edition_id = ed2.edition_id
JOIN scroll_data_owner ON scroll_data_owner.edition_id = ed2.edition_id
JOIN scroll_data USING(scroll_data_id)
JOIN user ON user.user_id = edition_editor.user_id
LEFT JOIN (SELECT edition_id, MAX(time) AS last_edit 
           FROM edition_editor
           JOIN main_action USING(edition_id) 
           GROUP BY time) AS last ON last.edition_id = ed2.edition_id
LEFT JOIN (SELECT iaa_edition_catalog.scroll_id, MIN(CONCAT(proxy, url, SQE_image.filename)) AS thumbnail_url 
		   FROM edition
		   JOIN iaa_edition_catalog USING(scroll_id)
           JOIN image_to_iaa_edition_catalog USING (iaa_edition_catalog_id)
		   JOIN SQE_image ON SQE_image.image_catalog_id = image_to_iaa_edition_catalog.image_catalog_id AND SQE_image.type = 0 
>>>>>>> 6cc19a4187d1bfe5c70efc913e4adf5b324c1a4e
           JOIN image_urls USING(image_urls_id)
           WHERE iaa_edition_catalog.edition_side = 0
           GROUP BY scroll_id) AS im ON im.scroll_id = ed2.scroll_id
";

        public static string GetQuery(bool limitUser, bool limitScrolls)
        {
            // Build the WHERE clause
            var where = new StringBuilder(" WHERE (user.user_id = 1");
            if (limitUser)
            {
                where.Append(" OR user.user_id = @UserId");
            }
            where.Append(")");

            if (limitScrolls)
                where.Append(" AND ed1.edition_id = @EditionId");

            return _baseQuery + where.ToString();
        }


        internal class Result
        {
            public uint edition_id { get; set; }
            public bool admin { get; set; }
            public string name { get; set; }
            public string thumbnail { get; set; }
            public bool locked { get; set; }
            public bool may_lock { get; set; }
            public bool may_write { get; set; }
            public DateTime? last_edit { get; set; }
            public uint user_id { get; set; }
            public string user_name { get; set; }
        }
    }

    internal class ScrollVersionGroupQuery
    {
        private const string _baseQuery = @"
SELECT DISTINCT ed2.edition_id, ed2.scroll_id
FROM edition AS ed1
JOIN edition AS ed2 ON ed2.scroll_id = ed1.scroll_id
JOIN edition_editor ON ed2.edition_id = edition_editor.edition_id
";
        private const string _where = "WHERE ed1.edition_id = @EditionId \n AND \n";
        private const string _orderBy = "\n ORDER BY scroll_id, edition_id";

        public static string GetQuery(bool limitScrollVersion, bool limitUser)
        {
            var sql = new StringBuilder(_baseQuery);
            if (limitScrollVersion)
            {
                sql.Append(_where);
            }
            else
            {
                sql.Append(" WHERE ");
            }
            
            sql.Append($@" ({(limitUser 
                ? "edition_editor.user_id = @UserId" 
                :  "edition_editor.user_id = @UserId OR edition_editor.user_id = 1")}) " + _orderBy);

            return sql.ToString();
        }

        internal class Result
        {
            public uint scroll_id { get; set; }
            public uint edition_id { get; set; }
        }
    }

    internal class EditionNameQuery
    {
        private const string _baseQuery = @"
SELECT scroll_data_id, scroll_id, name
FROM scroll_data
JOIN scroll_data_owner USING(scroll_data_id)
WHERE edition_id = @EditionId";

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
    
    internal static class EditionLockQuery
    {
        public const string GetQuery = @"
SELECT locked
FROM edition_editor
JOIN edition USING(edition_id)
WHERE edition_id = @EditionId";

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
            JOIN scroll_version_group USING(edition_id)
            JOIN scroll_version sv2 ON sv2.edition_id = scroll_version_group.edition_id
            WHERE sv1.scroll_version_id = @ScrollVersionId";
        
        // You must add a parameter `@ScrollVersionId` to any query using this.
        public const string LimitToScrollVersionGroup = CoalesceScrollVersions + ")";
        
        // You must add a parameter `@ScrollVersionId` to any query using this.
        public const string LimitToScrollVersionGroupNoAuth = CoalesceScrollVersions + " AND " + DefaultLimit + ")";
        
        // You must add the parameters `@ScrollVersionId` and `@UserId` to any query using this.
        public const string LimitToScrollVersionGroupAndUser = CoalesceScrollVersions + " AND (" + DefaultLimit + " OR " + UserLimit + "))";

        public const string LimitScrollVersionGroupToDefaultUser = @"
            scroll_version.user_id = 1 ";
        
        public const string LimitScrollVersionGroupToUser = 
            LimitScrollVersionGroupToDefaultUser + " OR scroll_version.user_id = @UserId ";
    }

    internal static class CreateEditionEditorQuery
    {
        // You must add a parameter `@UserId`, `@EditionId`, `@MayLock` (0 = false, 1 = true),
        // and `@Admin` (0 = false, 1 = true) to use this.
        public const string GetQuery = @"
            INSERT INTO edition_editor (user_id, edition_id, may_write, may_lock, is_admin) 
            VALUES (@UserId, @EditionId, 1, @MayLock, @IsAdmin)";
    }
    
    internal static class CreateEditionQuery
    {
        // You must add a parameter `@EditionEditorId` to use this.
        public const string GetQuery = 
            @"INSERT INTO edition (scroll_id, locked)  
            (SELECT scroll_id, 0
            FROM edition
            WHERE edition_id = @EditionId)";
    }

    internal static class CopyEditionDataForTableQuery
    {
        // You must add a parameter `@ScrollVersionId` and `@CopyToScrollVersionId` to use this.
        public static string GetQuery(string tableName, string tableIdColumn)
        {
            return $@"INSERT IGNORE INTO {tableName} ({tableIdColumn}, edition_editor_id, edition_id) 
            SELECT {tableIdColumn}, @EditionEditorId, @CopyToEditionId 
            FROM {tableName} 
            WHERE edition_id = @EditionId";
        }
    }
}
