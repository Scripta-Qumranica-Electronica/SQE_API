using System.Text;

namespace SQE.DatabaseAccess.Queries
{
	internal abstract class EditionImagedObjectQueries
	{
		private const string _getFragments = @"
SELECT DISTINCT image_catalog.institution AS Institution,
    image_catalog.catalog_number_1 AS Catalog1,
    image_catalog.catalog_number_2 AS Catalog2,
    image_catalog.object_id AS Id
FROM artefact_shape_owner
JOIN artefact_shape
USING (artefact_shape_id)
JOIN SQE_image
USING (sqe_image_id)
JOIN image_catalog
USING (image_catalog_id)
WHERE artefact_shape_owner.edition_id = @EditionId
";

		public static string GetQuery(bool fragmentId)
		{
			if (!fragmentId)
				return _getFragments;

			var str = new StringBuilder(_getFragments);
			str.Append(" AND image_catalog.object_id=@ObjectId");

			return str.ToString();
		}
	}
}
