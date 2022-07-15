namespace SQE.DatabaseAccess.Queries
{
	internal static class GetManuscriptMetadataQuery
	{
		// Added here an ad-hoc uniqueness constraint, we may need an index on `path` for better performance
		public const string GetQuery = @"
SELECT 	material,
				publication_number AS publicationNumber,
				plate,
				frag,
				site,
				period,
				composition,
				copy,
				manuscript,
				other_identifications AS otherIdentifications,
				abbreviation,
				manuscript_type AS manuscriptType,
				composition_type AS compositionType,
				language,
				script,
				publication
FROM  edition_iaa_manifest
WHERE edition_id = @EditionId
LIMIT 1
";
	}
}
