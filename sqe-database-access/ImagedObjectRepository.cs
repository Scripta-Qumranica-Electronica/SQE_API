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
	}
}
