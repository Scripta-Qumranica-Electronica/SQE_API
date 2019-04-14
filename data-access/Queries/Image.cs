using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal class ImageQueries
    {
        private static string _getImageQuery = @"select image_urls.url as url,
image_urls.proxy as proxy,
image_catalog.image_catalog_id,
SQE_image.type as img_type,
image_catalog.catalog_side as side,
SQE_image.filename as filename,
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
            var str = new StringBuilder(_getImageQuery);
            str.Append(" and image_catalog.institution=@Institution");
            str.Append(" and image_catalog.catalog_number_1=@Catalog1" );
            str.Append(" and image_catalog.catalog_number_2=@Catalog2");
            return str.ToString();
        }

        internal class Result
        {
            public string url { get; set; }
            public string proxy { get; set; }
            public string filename { get; set; }
            public uint image_catalog_id { get; set; }
            public byte img_type { get; set; }
            public byte side { get; set; }
            public bool master { get; set; }
            public ushort wave_start { get; set; }
            public ushort wave_end { get; set; }
            //public string transformMatrix { get; set; }
            public string institution { get; set; }
            public string catalog_1 { get; set; }
            public string catalog_2 { get; set; }
        }
    }

    internal class ImageGroupQuery
    {
        private static readonly string _baseQuery = @"
SELECT  image_catalog.image_catalog_id, 
        image_catalog.institution, 
        image_catalog.catalog_number_1, 
        image_catalog.catalog_number_2, 
        image_catalog.catalog_side
FROM image_catalog
";
        private static readonly string _scrollLimit = @"JOIN image_to_edition_catalog USING(image_catalog_id)
JOIN edition_catalog USING(edition_catalog_id)
JOIN scroll_version_group USING(scroll_id)
JOIN scroll_version USING(scroll_version_group_id)
WHERE scroll_version.scroll_version_id IN @ScrollVersionIds
";

        public static string GetQuery(bool limitScrolls)
        {
            return limitScrolls ? _baseQuery + _scrollLimit : _baseQuery;
        }

        internal class Result
        {
            public uint image_catalog_id { get; set; }
            public string institution { get; set; }
            public string catalog_number_1 { get; set; }
            public string catalog_number_2 { get; set; }
            public byte catalog_side { get; set; }
        }
    }

    internal class UserImageGroupQuery
    {
        private static readonly string _baseQuery = @"
SELECT  image_catalog.image_catalog_id, 
        image_catalog.institution, 
        image_catalog.catalog_number_1, 
        image_catalog.catalog_number_2, 
        image_catalog.catalog_side
FROM artefact_position
JOIN artefact_position_owner USING(artefact_position_id)
JOIN artefact_shape USING(artefact_id)
JOIN artefact_shape_owner USING(artefact_shape_id)
JOIN SQE_image ON SQE_image.sqe_image_id = artefact_shape.id_of_sqe_image
JOIN image_catalog USING(image_catalog_id)
JOIN scroll_version ON scroll_version.scroll_version_id = artefact_position_owner.scroll_version_id
    AND scroll_version.scroll_version_id = artefact_shape_owner.scroll_version_id
WHERE (scroll_version.user_id = @UserId OR scroll_version.user_id = (SELECT user_id FROM user WHERE user_name = 'sqe_api'))
";
       
        private static readonly string _scrollLimit = @"AND scroll_version.scroll_version_id IN @ScrollVersionIds
";

        public static string GetQuery(bool limitScrolls)
        {
            return limitScrolls ? _baseQuery + _scrollLimit : _baseQuery;
        }
    }

    internal static class ImageInstitutionQuery
    {
        public static string GetQuery()
        {
            return $"SELECT DISTINCT institution FROM image_catalog";
        }

        internal class Result
        {
            public string Institution { get; set; }
        }
    }
}
