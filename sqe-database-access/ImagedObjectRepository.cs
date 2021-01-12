using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess
{
	public interface IImagedObjectRepository
	{
		Task<IEnumerable<ImagedObject>> GetEditionImagedObjectsAsync(
				UserInfo editionUser
				, string imagedObjectId);

		Task<IEnumerable<ImagedObjectImage>> GetImagedObjectImagesAsync(string imagedObjectId);
		Task<IEnumerable<uint>> GetImagedObjectEditionsAsync(uint? userId, string imagedObjectId);
	}

	public class ImagedObjectRepository : DbConnectionBase
										  , IImagedObjectRepository
	{
		public ImagedObjectRepository(IConfiguration config) : base(config) { }

		public async Task<IEnumerable<ImagedObject>> GetEditionImagedObjectsAsync(
				UserInfo editionUser
				, string imagedObjectId)
		{
			var sql = EditionImagedObjectQueries.GetQuery(imagedObjectId != null);

			using (var connection = OpenConnection())
			{
				var results = await connection.QueryAsync<ImagedObject>(
						sql
						, new
						{
								UserId = editionUser.userId
								, editionUser.EditionId
								, ObjectId = imagedObjectId
								,
						});

				return results;
			}
		}

		public async Task<IEnumerable<ImagedObjectImage>> GetImagedObjectImagesAsync(
				string imagedObjectId)
		{
			using (var connection = OpenConnection())
			{
				return await connection.QueryAsync<ImagedObjectImage>(
						ImagedObjectImageQuery.GetQuery
						, new { ImagedObjectId = imagedObjectId });
			}
		}

		public Task<IEnumerable<uint>> GetImagedObjectEditionsAsync(
				uint?    userId
				, string imagedObjectId)
		{
			using (var conn = OpenConnection())
			{
				const string sql = @"
SELECT DISTINCT edition_editor.edition_id
FROM image_catalog
JOIN SQE_image USING(image_catalog_id)
JOIN artefact_shape USING(SQE_image_id)
JOIN artefact_shape_owner USING(artefact_shape_id)
JOIN edition USING(edition_id)
JOIN edition_editor USING(edition_id)
WHERE image_catalog.object_id = @ImagedObjectId
	AND (edition.public = 1 OR edition_editor.user_id = @UserId)";

				return conn.QueryAsync<uint>(
						sql
						, new { ImagedObjectId = imagedObjectId, UserId = userId ?? 0 });
			}
		}
	}
}
