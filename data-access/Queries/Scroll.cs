using System;
using System.Text;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal class EditionGroupQuery
    {
        private const string _baseQuery = @"
SELECT ed2.edition_id AS EditionId,
       edition_editor.is_admin AS Admin,
	   scroll_data.name AS Name, 
       im.thumbnail_url AS Thumbnail, 
       ed2.locked AS Locked,
       edition_editor.may_lock AS MayLock,
       edition_editor.may_write AS MayWrite, 
       edition_editor.may_read AS MayRead,
       last.last_edit AS LastEdit, 
       user.user_id AS UserId, 
       user.user_name AS UserName 
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
            public uint EditionId { get; set; }
            public bool Admin { get; set; }
            public string Name { get; set; }
            public string Thumbnail { get; set; }
            public bool Locked { get; set; }
            public bool MayLock { get; set; }
            public bool MayWrite { get; set; }
            public bool MayRead { get; set; }
            public DateTime? LastEdit { get; set; }
            public uint UserId { get; set; }
            public string UserName { get; set; }
        }
    }

    internal class ScrollVersionGroupQuery
    {
        private const string _baseQuery = @"
SELECT DISTINCT ed2.edition_id AS EditionId, ed2.scroll_id AS ScrollId
FROM edition AS ed1
JOIN edition AS ed2 ON ed2.scroll_id = ed1.scroll_id
JOIN edition_editor ON ed2.edition_id = edition_editor.edition_id
";
        private const string _where = "WHERE ed1.edition_id = @EditionId \n AND \n";
        private const string _orderBy = "\n ORDER BY ed2.scroll_id, ed2.edition_id";

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
            public uint ScrollId { get; set; }
            public uint EditionId { get; set; }
        }
    }

    internal class EditionNameQuery
    {
        private const string _baseQuery = @"
SELECT scroll_data_id AS ScrollDataId, scroll_id AS ScrollId, name AS Name
FROM scroll_data
JOIN scroll_data_owner USING(scroll_data_id)
WHERE edition_id = @EditionId";

        public static string GetQuery()
        {
            return _baseQuery;
        }

        internal class Result
        {
            public uint ScrollDataId { get; set; }
            public uint ScrollId { get; set; }
            public string Name { get; set; }
        }
    }
    
    internal static class EditionLockQuery
    {
        public const string GetQuery = @"
SELECT locked AS Locked
FROM edition_editor
JOIN edition USING(edition_id)
WHERE edition_id = @EditionId";

        internal class Result
        {
            public bool Locked { get; set; }  // locked is TINYINT, which is 8-bit unsigned like C# bool.  Is it ok/safe?
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
