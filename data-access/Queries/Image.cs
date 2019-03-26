using System;
using System.Text;

namespace SQE.Backend.DataAccess.Queries
{
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
            public int image_catalog_id { get; set; }
            public string institution { get; set; }
            public string catalog_number_1 { get; set; }
            public string catalog_number_2 { get; set; }
            public int catalog_side { get; set; }
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
            public string institution { get; set; }
        }
    }


}
