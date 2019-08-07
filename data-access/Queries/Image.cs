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
    SQE_image.sqe_image_id AS sqe_image_id,
    SQE_image.filename AS filename,
    SQE_image.is_master AS master,
    SQE_image.wavelength_start AS wave_start,
    SQE_image.wavelength_end AS wave_end,
    image_catalog.Institution AS Institution,
    image_catalog.catalog_number_1 AS catalog_1,
    image_catalog.catalog_number_2 AS catalog_2,
    image_catalog.object_id AS object_id,
    ASTEXT(image_to_image_map.region_on_image1) AS region_on_image1,
    ASTEXT(image_to_image_map.region_on_image2) AS region_on_image2,
    image_to_image_map.transform_matrix AS transform_matrix
FROM iaa_edition_catalog
JOIN edition USING(manuscript_id)
JOIN edition_editor USING(edition_id)
JOIN image_to_iaa_edition_catalog USING(iaa_edition_catalog_id)
JOIN image_catalog USING(image_catalog_id)
JOIN SQE_image USING(image_catalog_id)
JOIN SQE_image AS master_image ON image_catalog.image_catalog_id = master_image.image_catalog_id 
    AND master_image.is_master = 1
LEFT JOIN image_to_image_map ON SQE_image.sqe_image_id = image_to_image_map.image2_id
    AND image_to_image_map.image1_id = master_image.sqe_image_id 
JOIN image_urls ON SQE_image.image_urls_id = image_urls.image_urls_id
WHERE edition.edition_id = @EditionId
    AND (edition.public = 1 OR edition_editor.user_id = @UserId)
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
            public string sqe_image_id { get; set; }
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
            public string region_on_image1 { get; set; }
            public string region_on_image2 { get; set; }
            public string transform_matrix { get; set; }
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
JOIN edition USING(manuscript_id)
WHERE edition.edition_id = @EditionId
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
