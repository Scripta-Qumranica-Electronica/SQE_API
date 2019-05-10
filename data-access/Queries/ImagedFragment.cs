using System.Text;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal class ImagedFragemntQueries
    {
        private const string _getFragments = @"
SELECT image_catalog.institution AS institution,
image_catalog.catalog_number_1 AS catalog_1,
image_catalog.catalog_number_2 AS catalog_2
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

        public static string GetFragmentsQuery(bool fragmentId)
        {
            if(!fragmentId)
                return _getFragments;
            var str = new StringBuilder(_getFragments);
            str.Append(" AND image_catalog.institution=@Institution");
            str.Append(" AND image_catalog.catalog_number_1=@Catalog1");
            str.Append(" AND image_catalog.catalog_number_2=@Catalog2");
            return str.ToString();
        }

        internal class Result
        {
            public string institution { get; set; }
            public string catalog_1 { get; set; }
            public string catalog_2 { get; set; }

        }
    }

}
