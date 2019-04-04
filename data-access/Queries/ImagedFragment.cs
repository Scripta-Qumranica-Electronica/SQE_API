using System;
using System.Collections.Generic;
using System.Text;
using SQE.Backend.DataAccess.Models;

namespace SQE.Backend.DataAccess.Queries
{
    internal class ImagedFragemntQueries
    {
        private static string _getFragments = @"select image_catalog.institution as institution,
image_catalog.catalog_number_1 as catalog_1,
image_catalog.catalog_number_2 as catalog_2
from edition_catalog
join scroll_version_group using(scroll_id)
join scroll_version using (scroll_version_group_id)
join image_to_edition_catalog using (edition_catalog_id)
JOIN image_catalog USING(image_catalog_id)
where scroll_version.scroll_version_id = @ScrollVersionId
and (scroll_version.user_id =1 OR scroll_version.user_id =@UserId) and edition_catalog.edition_side =0
"; 

        public static string GetFragmentsQuery(bool fragmentId)
        {
            if(!fragmentId)
                return _getFragments;
            var str = new StringBuilder(_getFragments);
            str.Append(" and image_catalog.institution=@Institution");
            str.Append(" and image_catalog.catalog_number_1=@Catalog1");
            str.Append(" and image_catalog.catalog_number_2=@Catalog2");
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
