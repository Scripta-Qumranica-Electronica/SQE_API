using System.Text;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal class ImageQueries
    {
        private const string _getImageQuery = @"
SELECT image_urls.url AS url,
    image_urls.proxy AS proxy,
    image_catalog.image_catalog_id,
    SQE_image.type AS img_type,
    image_catalog.catalog_side AS side,
    SQE_image.filename AS filename,
    SQE_image.is_master AS master,
    SQE_image.wavelength_start AS wave_start,
    SQE_image.wavelength_end AS wave_end,
    image_catalog.Institution AS Institution,
    image_catalog.catalog_number_1 AS catalog_1,
    image_catalog.catalog_number_2 AS catalog_2,
    image_catalog.object_id AS object_id
FROM iaa_edition_catalog
JOIN edition USING(scroll_id)
JOIN edition_editor USING(edition_id)
JOIN image_to_iaa_edition_catalog USING(iaa_edition_catalog_id)
JOIN image_catalog USING(image_catalog_id)
JOIN SQE_image USING(image_catalog_id)
JOIN image_urls USING(image_urls_id)
WHERE edition.edition_id = @EditionId
    AND (edition_editor.user_id = 1 OR edition_editor.user_id = @UserId)
    AND iaa_edition_catalog.edition_side =0
";
        public static string GetImageQuery(bool filterFragment)
        {
            if (!filterFragment)
                return _getImageQuery;
            var str = new StringBuilder(_getImageQuery);
            str.Append(" AND image_catalog.object_id=@ObjectId");
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
            //public string TransformMatrix { get; set; }
            public string institution { get; set; }
            public string catalog_1 { get; set; }
            public string catalog_2 { get; set; }
            public string object_id { get; set; }
        }
    }

    internal class ImageGroupQuery
    {
        private const string _baseQuery = @"
SELECT  image_catalog.image_catalog_id, 
        image_catalog.Institution, 
        image_catalog.catalog_number_1, 
        image_catalog.catalog_number_2, 
        image_catalog.catalog_side
FROM image_catalog
";
        private const string _scrollLimit = @"
JOIN image_to_iaa_edition_catalog USING(ImageCatalogId)
JOIN iaa_edition_catalog USING(iaa_edition_catalog_id)
JOIN edition USING(scroll_id)
WHERE edition.edition_is = @EditionId
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
        private const string _baseQuery = @"
SELECT  image_catalog.image_catalog_id, 
        image_catalog.Institution, 
        image_catalog.catalog_number_1, 
        image_catalog.catalog_number_2, 
        image_catalog.catalog_side
FROM artefact_position
JOIN artefact_position_owner USING(artefact_position_id)
JOIN artefact_shape USING(artefact_id)
JOIN artefact_shape_owner USING(artefact_shape_id)
JOIN SQE_image ON SQE_image.sqe_image_id = artefact_shape.sqe_image_id
JOIN image_catalog USING(image_catalog_id)
JOIN edition ON edition.edition_id = artefact_position_owner.edition_id
    AND edition.edition_id = artefact_shape_owner.edition_id
JOIN edition_editor ON edition_editor.edition_id = edition.edition_id
WHERE (edition_editor.user_id = @UserId OR edition_editor.user_id = 1)
";
       
        private const string _scrollLimit = @"AND edition.edition_id = @EditionId";

        public static string GetQuery(bool limitScrolls)
        {
            return limitScrolls ? _baseQuery + _scrollLimit : _baseQuery;
        }
    }

    internal static class ImageInstitutionQuery
    {
        public static string GetQuery()
        {
            return $"SELECT DISTINCT Institution FROM image_catalog";
        }

        internal class Result
        {
            public string Institution { get; set; }
        }
    }
}
