using System.Collections.Generic;
using System.Linq;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    public class AuthorshipQueries
    {
        internal class AuthorByTableId
        {
            private string _query = @"
SELECT DISTINCT email
FROM single_action
JOIN main_action USING(main_action_id)
JOIN scroll_version USING(scroll_version_id)
JOIN user USING(user_id)
WHERE single_action.table = ""@TableName"" AND single_action.action = ""add""
    AND (@IdList)
GROUP BY single_action.id_in_table
    HAVING min(single_action.single_action_id)"; // Presumably a lower single_action_id is equivalent to an earlier date main_action. object this[int index]

            /// <summary>
            /// This returns a query string that provides the original author for a list of
            /// Id's in a user editable table.
            /// </summary>
            /// <param name="tableName">The table containing the Id's being audited.</param>
            /// <param name="idList">The list of Id's to audit</param>
            /// <returns>A query string to be run with the database connector.</returns>
            public string GetQuery(string tableName, List<uint> idList)
            {
                // The direct string injection here should be fine, since this will be run internally.
                // Bronson was worried about performance when using a huge numbers of parameters.
                // The idList for this query may conceivably have 50,000 items.  Should we be doing
                // this some other way?
                return _query.Replace("@TableName", tableName) // Insert the table Name into the query.
                    .Replace("@IdList", string.Join(" || ", 
                        idList.Select(x => "single_action.id_in_table = " + x.ToString()) // Unwind the Id's into an || separated list
                        )                                                                 // and format the search string, e.g., single_action.id_in_table = 234.
                    );
            }
        }
    }
}