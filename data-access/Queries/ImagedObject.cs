using System.Text;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal abstract class ImagedObjectQueries
    {
        private const string _getFragments = @"
SELECT image_catalog.institution AS Institution,
    image_catalog.catalog_number_1 AS catalog_1,
    image_catalog.catalog_number_2 AS catalog_2,
    image_catalog.object_id AS object_id
FROM iaa_edition_catalog
JOIN edition USING(scroll_id)
JOIN edition_editor USING(edition_id)
JOIN image_to_iaa_edition_catalog USING(iaa_edition_catalog_id)
JOIN image_catalog USING(image_catalog_id)
WHERE edition.edition_id = @EditionId
  AND (edition_editor.user_id = 1 
         OR edition_editor.user_id = @UserId) 
  AND iaa_edition_catalog.edition_side =0
"; 

        public static string GetQuery(bool fragmentId)
        {
            if(!fragmentId)
                return _getFragments;
            var str = new StringBuilder(_getFragments);
            str.Append(" AND image_catalog.object_id=@ObjectId");
            return str.ToString();
        }

        internal class Result
        {
            public string Institution { get; set; }
            public string catalog_1 { get; set; }
            public string catalog_2 { get; set; }
            public string object_id { get; set; }

        }
    }

}
