using System;
using System.Text;

namespace SQE.Backend.DataAccess.Queries
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
LEFT JOIN (SELECT ec.scroll_id as scroll_id, MIN(CONCAT(proxy, url)) as thumbnail_url 
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
            public int id { get; set; }
            public string name { get; set; }
            public string thumbnail { get; set; }
            public bool locked { get; set; }
            public DateTime? last_edit { get; set; }
            public int user_id { get; set; }
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
            public int group_id { get; set; }
            public int scroll_version_id { get; set; }
        }

        internal class UpdateScrollNameQueries
        {
            private static string _updateMainAction = @"INSERT INTO main_action 
          (scroll_version_id) VALUES (@ScrollVersionId)";

            private static string _updateSingleAction = @"insert into single_action (main_action_id, action, `table`,id_in_table)
Values (@MainActionId, ""add"", ""scroll_data"", @IdInTable)";

            public static string _addScrollName = @"INSERT INTO scroll_data (scroll_id, name)
VALUES (@ScrollVersionId, @Name)";

            public static string _lastId = @"LAST_INSERTED_ID()";

            public static string _checkIfNameExists = @"SELECT scroll_data_id FROM scroll_data WHERE name =@Name AND scroll_id =@ScrollVersionId";

            public static string _updateScrollDataOwner = @"UPDATE scroll_data_owner
set scroll_data_id = @ScrollDataId
where scroll_version_id = @ScrollVersionId";

            public static string AddScrollName()
            {
                return _addScrollName;
            } 

            public static string CheckIfNameExists()
            {
                return _checkIfNameExists;
            }

            public static string UpdateScrollDataOwner()
            {
                return _updateScrollDataOwner;
            }

            public static string AddMainAction()
            {
                return _updateMainAction;
            }

            public static string AddSingleAction()
            {
                return _updateSingleAction;
            }

            public static string LastId()
            {
                return _lastId;
            }
        }
    }


}
