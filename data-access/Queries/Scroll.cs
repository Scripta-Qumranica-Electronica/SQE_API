﻿using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

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
Values (@MainActionId, @Action, ""scroll_data"", @IdInTable)";

            public static string _addScrollName = @"INSERT INTO scroll_data (scroll_id, name)
VALUES (@ScrollVersionId, @Name)";

            private static string _updateScrollData = @"
update scroll_data
set name =@Name
where scroll_id = @ScrollVersionId;";

            public static string _lastId = @"LAST_INSERTED_ID()";

            public static string _checkIfNameExists = @"SELECT scroll_data_id FROM scroll_data WHERE name =@Name AND scroll_id =@ScrollVersionId";


            public static string _updateScrollDataOwner = @"UPDATE scroll_data_owner
set scroll_data_id = @ScrollDataId
where scroll_version_id = @ScrollVersionId";

            public static string _getScrollDataId = @"SELECT scroll_data_id from scroll_data where scroll_id = @ScrollVersionId";

            public static string UpdateScrollData()
            {
                return _updateScrollData;
            }
            public static string GetScrollDataId ()
            {
                return _getScrollDataId;
            }

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

        internal class CopyScrollVersionQueries
        {
            private static string _checkPermissions = @"select user_id from scroll_version
where scroll_version_id = @ScrollVersionId";

            private static string _insetIntoScrollVersion = @"INSERT INTO scroll_version(user_id, scroll_version_group_id, may_write, may_lock)

VALUES(@UserId, @ScrollVersionGroupId, 1,1)";

            private static string _insertIntoScrollVersionGroup = @"INSERT INTO scroll_version_group(scroll_id, locked)
VALUES(@ScrollID, 0)";

            private static string _insertIntoScrollData = @"INSERT INTO scroll_data(scroll_id, name)
VALUES (@ScrollVersionId, @Name)";

            public string _getScrollId = @"SELECT scroll.scroll_id
	FROM scroll
		 JOIN scroll_data USING (scroll_id)
		JOIN  scroll_data_owner USING (scroll_data_id)
		JOIN scroll_version USING (scroll_version_id)
	WHERE scroll_data.name like @ScrollName
		AND user_id = @UserId
    ORDER BY scroll_data.name;";

            public static List<string> GetOwnerList()
            {
                List<string> ownerList = new List<string>();
                ownerList.Add(@"INSERT INTO area_group_owner (area_group_id, scroll_version_id)
SELECT area_group_id, @SVID FROM area_group_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO artefact_data_owner (artefact_data_id, scroll_version_id)
SELECT artefact_data_id, @SVID FROM artefact_data_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO artefact_position_owner (artefact_position_id, scroll_version_id)
SELECT artefact_position_id, @SVID FROM artefact_position_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO artefact_shape_owner(artefact_shape_id, scroll_version_id)
SELECT artefact_shape_id, @SVID FROM artefact_shape_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO char_of_writing_owner(char_of_writing_id, scroll_version_id)
SELECT char_of_writing_id, @SVID FROM char_of_writing_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO col_data_owner(col_data_id, scroll_version_id)
SELECT col_data_id, @SVID FROM col_data_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO col_sequence_owner(col_sequence_id, scroll_version_id)
SELECT col_sequence_id, @SVID FROM col_sequence_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO col_to_line_owner(col_to_line_id, scroll_version_id)
SELECT col_to_line_id, @SVID FROM col_to_line_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO form_of_writing_owner(form_of_writing_id, scroll_version_id)
SELECT form_of_writing_id, @SVID FROM form_of_writing_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO line_data_owner(line_data_id, scroll_version_id)
SELECT line_data_id, @SVID FROM line_data_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO line_to_sign_owner(line_to_sign_id, scroll_version_id)
SELECT line_to_sign_id, @SVID FROM line_to_sign_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO position_in_stream_owner(position_in_stream_id, scroll_version_id)
SELECT position_in_stream_id, @SVID FROM position_in_stream_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO scribal_font_type_owner(scribal_font_type_id, scroll_version_id)
SELECT scribal_font_type_id, @SVID FROM scribal_font_type_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO scribe_owner(scribe_id, scroll_version_id)
SELECT scribe_id, @SVID FROM scribe_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO scroll_data_owner(scroll_data_id, scroll_version_id)
SELECT scroll_data_id, @SVID FROM scroll_data_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO scroll_to_col_owner(scroll_to_col_id, scroll_version_id)
SELECT scroll_to_col_id, @SVID FROM scroll_to_col_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO sign_char_attribute_owner(sign_char_attribute_id, scroll_version_id)
SELECT sign_char_attribute_id, @SVID FROM sign_char_attribute_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO sign_char_commentary_owner(sign_char_commentary_id, scroll_version_id)
SELECT sign_char_commentary_id, @SVID FROM sign_char_commentary_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO sign_char_roi_owner(sign_char_roi_id, scroll_version_id)
SELECT sign_char_roi_id, @SVID FROM sign_char_roi_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                ownerList.Add(@"INSERT INTO word_owner(word_id, scroll_version_id)
SELECT word_id, @SVID FROM word_owner
JOIN scroll_version USING(scroll_version_id)
WHERE scroll_version_group_id = @OLDSVID");

                return ownerList;
            }


            private static string _getScrollVersionGroupId = @"select scroll_version_group_id from scroll_version_group
where scroll_id = @ScrollVersionId";

            public static string GetScrollVersionGroupId()
            {
                return _getScrollVersionGroupId;
            }

            public static string InsertIntoScrollData()
            {
                return _insertIntoScrollData;
            }

            public static string InsertIntoScrollVersionGroup()
            {
                return _insertIntoScrollVersionGroup;
            }

            public static string InsertIntoScrollVersion()
            {
                return _insetIntoScrollVersion;
            }

            public static string CheckPermission()
            {
                return _checkPermissions;
            }
        }
    }

}
