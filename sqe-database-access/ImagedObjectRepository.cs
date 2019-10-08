using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess
{
	public interface IImagedObjectRepository
	{
		Task<IEnumerable<ImagedObject>> GetImagedObjectsAsync(EditionUserInfo editionUser, string imagedObjectId);
	}

	public class ImagedObjectRepository : DbConnectionBase, IImagedObjectRepository
	{
		public ImagedObjectRepository(IConfiguration config) : base(config)
		{
		}

		public async Task<IEnumerable<ImagedObject>> GetImagedObjectsAsync(EditionUserInfo editionUser,
			string imagedObjectId)
		{
			var sql = ImagedObjectQueries.GetQuery(imagedObjectId != null);

			using (var connection = OpenConnection())
			{
				var results = await connection.QueryAsync<ImagedObjectQueries.Result>(
					sql,
					new
					{
						UserId = editionUser.userId,
						editionUser.EditionId,
						ObjectId = imagedObjectId
					}
				);

				var models = results.Select(result => CreateImagedObject(result));
				return models;
			}
		}

		private ImagedObject CreateImagedObject(ImagedObjectQueries.Result imagedFragment)
		{
			var model = new ImagedObject
			{
				Id = imagedFragment.object_id,
				Institution = imagedFragment.Institution,
				Catalog1 = imagedFragment.catalog_1,
				Catalog2 = imagedFragment.catalog_2
			};
			return model;
		}
	}
}