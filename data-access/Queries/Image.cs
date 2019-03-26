using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.Backend.DataAccess.Queries
{
    internal class ImageQueries
    {
        private static string _getImageQuery = @"select image_urls.url as url,
image_urls.proxy as proxy,
image_catalog.image_catalog_id,
SQE_image.type as type,
image_catalog.catalog_side as side,
SQE_image.is_master as master,
SQE_image.wavelength_start as wave_start,
SQE_image.wavelength_end as wave_end,
image_catalog.institution as institution,
image_catalog.catalog_number_1 as catalog_1,
image_catalog.catalog_number_2 as catalog_2
from edition_catalog
join scroll_version_group using(scroll_id)
join scroll_version using (scroll_version_group_id)
join image_to_edition_catalog using (edition_catalog_id)
JOIN image_catalog USING(image_catalog_id)
join SQE_image using (image_catalog_id)
join image_urls using (image_urls_id)
where scroll_version.scroll_version_id = @ScrollVersionId
and (scroll_version.user_id =1 OR scroll_version.user_id =@UserId)
and edition_catalog.edition_side =0

";
        public static string GetImageQuery(bool filterFragment)
        {
            if (!filterFragment)
                return _getImageQuery;
            StringBuilder str = new StringBuilder(_getImageQuery);
            str.Append(" and image_catalog.institution=@Institution");
            str.Append(" and image_catalog.catalog_number_1=@Catalog1" );
            str.Append(" and image_catalog.catalog_number_2=@Catalog2");
            return str.ToString();
        }

        internal class Result
        {
            public string url { get; set; }
            public string proxy { get; set; }
            public int image_catalog_id { get; set; }
            public int img_type { get; set; }
            public int side { get; set; }
            public int master { get; set; }
            public int wave_start { get; set; }
            public int wave_end { get; set; }
            //public string transformMatrix { get; set; }
            public string institution { get; set; }
            public string catalog_1 { get; set; }
            public string catalog_2 { get; set; }
        }
    }
}
