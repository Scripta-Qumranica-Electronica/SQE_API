using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess
{
	public interface IImagedObjectRepository
	{
		Task<IEnumerable<ImagedObject>> GetEditionImagedObjectsAsync(
				UserInfo editionUser
				, string imagedObjectId);

		Task<ImagedObject> CreateEditionImagedObjectsAsync(
				UserInfo editionUser
				, string imagedObjectId);

		Task DeleteEditionImagedObjectsAsync(UserInfo editionUser, string imagedObjectId);

		Task<IEnumerable<ImagedObjectImage>> GetImagedObjectImagesAsync(string imagedObjectId);
		Task<IEnumerable<uint>> GetImagedObjectEditionsAsync(uint? userId, string imagedObjectId);
	}

	public class ImagedObjectRepository : DbConnectionBase
										  , IImagedObjectRepository
	{
		private readonly IDatabaseWriter _databaseWriter;

		public ImagedObjectRepository(IConfiguration config, IDatabaseWriter databaseWriter) :
				base(config) => _databaseWriter = databaseWriter;

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

		public async Task<ImagedObject> CreateEditionImagedObjectsAsync(
				UserInfo editionUser
				, string imagedObjectId)
		{
			using (var transaction = new TransactionScope())
			{
				var imageCatalogueIds = await _getImageCatalogId(imagedObjectId);

				if (!imageCatalogueIds.Any())
				{
					throw new StandardExceptions.DataNotFoundException(
							"imaged object"
							, imagedObjectId);
				}

				var createRequests = imageCatalogueIds.Select(
						x =>
						{
							var parameters = new DynamicParameters();
							parameters.Add("image_catalog_id", x);

							return new MutationRequest(
									MutateType.Create
									, parameters
									, "image_catalog"
									, x);
						});

				await _databaseWriter.WriteToDatabaseAsync(editionUser, createRequests.AsList());
				transaction.Complete();
			}

			return (await GetEditionImagedObjectsAsync(editionUser, imagedObjectId))
					.FirstOrDefault();
		}

		public async Task DeleteEditionImagedObjectsAsync(
				UserInfo editionUser
				, string imagedObjectId)
		{
			using (var transaction = new TransactionScope())
			{
				var imageCatalogueIds = await _getImageCatalogId(imagedObjectId);

				if (!imageCatalogueIds.Any())
				{
					throw new StandardExceptions.DataNotFoundException(
							"imaged object"
							, imagedObjectId);
				}

				var deleteRequests = imageCatalogueIds.Select(
						x => new MutationRequest(
								MutateType.Delete
								, new DynamicParameters()
								, "image_catalog"
								, x));

				await _databaseWriter.WriteToDatabaseAsync(editionUser, deleteRequests.AsList());
				transaction.Complete();
			}
		}

		private Task<IEnumerable<uint>> _getImageCatalogId(string imagedObjectId)
		{
			const string sql =
					"SELECT image_catalog_id FROM image_catalog WHERE object_id = @ObjectId";

			using (var conn = OpenConnection())
				return conn.QueryAsync<uint>(sql, new { ObjectId = imagedObjectId });
		}
	}
}
