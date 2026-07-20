using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess;

public interface IImagedObjectRepository
{
	Task<IEnumerable<ImagedObject>> GetEditionImagedObjectsAsync(
			UserInfo editionUser
			, string imagedObjectId);

	Task<ImagedObject> CreateEditionImagedObjectsAsync(UserInfo editionUser, string imagedObjectId);

	Task DeleteEditionImagedObjectsAsync(UserInfo editionUser, string imagedObjectId);

	Task<IEnumerable<ImagedObjectImage>> GetImagedObjectImagesAsync(string imagedObjectId);

	Task<IEnumerable<uint>> GetImagedObjectEditionsAsync(uint? userId, string imagedObjectId);

	Task<IEnumerable<AlteredRecord>> CreateEditionImagedObjectsBySqeImageIdAsync(
			UserInfo editionUser
			, uint   sqeImageId);
}

public class ImagedObjectRepository(IDatabaseAccessor dba) : IImagedObjectRepository
{
	public async Task<IEnumerable<ImagedObject>> GetEditionImagedObjectsAsync(
			UserInfo editionUser
			, string imagedObjectId)
	{
		var sql = EditionImagedObjectQueries.GetQuery(!string.IsNullOrEmpty(imagedObjectId));

		var results = await dba.QueryAsync<ImagedObject>(
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

	public async Task<IEnumerable<ImagedObjectImage>> GetImagedObjectImagesAsync(
			string imagedObjectId) => await dba.QueryAsync<ImagedObjectImage>(
			ImagedObjectImageQuery.GetQuery
			, new { ImagedObjectId = imagedObjectId });

	public async Task<IEnumerable<uint>> GetImagedObjectEditionsAsync(
			uint?    userId
			, string imagedObjectId)
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

		return await dba.QueryAsync<uint>(
				sql
				, new { ImagedObjectId = imagedObjectId, UserId = userId ?? 0 });
	}

	public async Task<ImagedObject> CreateEditionImagedObjectsAsync(
			UserInfo editionUser
			, string imagedObjectId)
	{
		await dba.BeginTransactionAsync();

		var imageCatalogueIds = (await _getImageCatalogId(imagedObjectId)).ToList();

		if (imageCatalogueIds.Count == 0)
			throw new StandardExceptions.DataNotFoundException("imaged object", imagedObjectId);

		var createRequests = imageCatalogueIds.Select(x =>
													  {
														  var parameters = new DynamicParameters();

														  parameters.Add("image_catalog_id", x);

														  return new MutationRequest(
																  MutateType.Create
																  , parameters
																  , "image_catalog"
																  , x);
													  });

		var a = await dba.WriteToDatabaseAsync(editionUser, createRequests.AsList());

		dba.CommitTransaction();

		return (await GetEditionImagedObjectsAsync(editionUser, imagedObjectId)).FirstOrDefault();
	}

	public async Task<IEnumerable<AlteredRecord>> CreateEditionImagedObjectsBySqeImageIdAsync(
			UserInfo editionUser
			, uint   sqeImageId)
	{
		await dba.BeginTransactionAsync();
		var imageCatalogueIds = await _getRelatedImageCatalogIdsForSqeImage(sqeImageId);

		// if (!imageCatalogueIds.Any())
		// {
		// 	throw new StandardExceptions.DataNotFoundException(
		// 			"imaged object from SQE image"
		// 			, sqeImageId);
		// }

		var createRequests = imageCatalogueIds.Select(x =>
													  {
														  var parameters = new DynamicParameters();

														  parameters.Add("image_catalog_id", x);

														  return new MutationRequest(
																  MutateType.Create
																  , parameters
																  , "image_catalog"
																  , x);
													  });

		var result = await dba.WriteToDatabaseAsync(editionUser, createRequests.AsList());

		dba.CommitTransaction();

		return result;
	}

	public async Task DeleteEditionImagedObjectsAsync(UserInfo editionUser, string imagedObjectId)
	{
		await dba.BeginTransactionAsync();
		var imageCatalogueIds = await _getImageCatalogId(imagedObjectId);

		if (!imageCatalogueIds.Any())
			throw new StandardExceptions.DataNotFoundException("imaged object", imagedObjectId);

		var deleteRequests = imageCatalogueIds.Select(x => new MutationRequest(
															  MutateType.Delete
															  , new DynamicParameters()
															  , "image_catalog"
															  , x));

		await dba.WriteToDatabaseAsync(editionUser, deleteRequests.AsList());
		dba.CommitTransaction();
	}

	private async Task<IEnumerable<uint>> _getImageCatalogId(string imagedObjectId)
	{
		const string sql = "SELECT image_catalog_id FROM image_catalog WHERE object_id = @ObjectId";

		return await dba.QueryAsync<uint>(sql, new { ObjectId = imagedObjectId });
	}

	private async Task<IEnumerable<uint>> _getRelatedImageCatalogIdsForSqeImage(uint sqeImageId)
	{
		const string sql = @"
select im2.image_catalog_id
from SQE_image
join image_catalog
	on SQE_image.image_catalog_id = image_catalog.image_catalog_id
join image_catalog im2
	on image_catalog.object_id = im2.object_id
where SQE_image.sqe_image_id = @SqeImageId";

		return await dba.QueryAsync<uint>(sql, new { SqeImageId = sqeImageId });
	}
}
