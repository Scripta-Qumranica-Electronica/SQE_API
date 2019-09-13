using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess
{
	public interface IRoiRepository
	{
		Task<List<SignInterpretationROI>> CreateRoisAsync(EditionUserInfo editionUser,
			List<SetSignInterpretationROI> newRois);

		Task<List<SignInterpretationROI>> UpdateRoisAsync(EditionUserInfo editionUser,
			List<SignInterpretationROI> updateRois);

		Task DeletRoisAsync(EditionUserInfo editionUser, List<uint> deleteRoiIds);

		Task<DetailedSignInterpretationROI> GetSignInterpretationRoiByIdAsync(EditionUserInfo editionUser,
			uint signInterpretationRoiId);
	}

	public class RoiRepository : DbConnectionBase, IRoiRepository
	{
		private readonly IDatabaseWriter _databaseWriter;

		public RoiRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config)
		{
			_databaseWriter = databaseWriter;
		}

		/// <summary>
		/// Creates a sign interpretation roi from a list.
		/// </summary>
		/// <param name="editionUser">UserInfo object with user details and edition permissions</param>
		/// <param name="newRois">List of rois to be added to the system.</param>
		/// <returns></returns>
		public async Task<List<SignInterpretationROI>> CreateRoisAsync(EditionUserInfo editionUser,
			List<SetSignInterpretationROI> newRois)
		{
			return (await Task.WhenAll(newRois.Select(
				async (x) =>
				{
					var roiShapeId = CreateRoiShapeAsync(x.Shape);
					var roiPositionId = CreateRoiPositionAsync(x.ArtefactId, x.Position);
					var signInterpretationRoiId = await CreateSignInterpretationRoiAsync(
						editionUser,
						x.SignInterpretationId,
						await roiShapeId,
						await roiPositionId,
						x.ValuesSet,
						x.Exceptional
					);
					return (SignInterpretationROI)(await GetSignInterpretationRoiByIdAsync(editionUser, signInterpretationRoiId));
				}
			))).ToList();
		}

		/// <summary>
		/// Updates each sign interpretation roi in a list.
		/// </summary>
		/// <param name="editionUser">UserInfo object with user details and edition permissions</param>
		/// <param name="updateRois">List of rois to be added to the system.</param>
		/// <returns></returns>
		public async Task<List<SignInterpretationROI>> UpdateRoisAsync(EditionUserInfo editionUser,
			List<SignInterpretationROI> updateRois)
		{
			return (await Task.WhenAll(updateRois.Select(
				async (x) =>
				{
					if (!x.SignInterpretationId.HasValue)
						throw new StandardErrors.ImproperInputData("signInterpretationId");

					var originalSignRoiInterpretation =
						await GetSignInterpretationRoiByIdAsync(editionUser, x.SignInterpretationRoiId);

					// TODO: Maybe parse this better, because the strings can be non-equal, but the data may still be the same.
					var roiShapeId = originalSignRoiInterpretation.Shape == x.Shape
						? originalSignRoiInterpretation.RoiShapeId
						: await CreateRoiShapeAsync(x.Shape);

					// TODO: Maybe parse this better, because the strings can be non-equal, but the data may still be the same.
					var roiPositionId = originalSignRoiInterpretation.Position == x.Position
						? originalSignRoiInterpretation.RoiPositionId
						: await CreateRoiPositionAsync(x.ArtefactId, x.Position);

					var signInterpretationRoiId = await UpdateSignInterpretationRoiAsync(
						editionUser,
						x.SignInterpretationId,
						roiShapeId,
						roiPositionId,
						x.ValuesSet,
						x.Exceptional,
						x.SignInterpretationRoiId
					);
					return (SignInterpretationROI)(await GetSignInterpretationRoiByIdAsync(editionUser, signInterpretationRoiId));
				}
			))).ToList();
		}

		public async Task DeletRoisAsync(EditionUserInfo editionUser, List<uint> deleteRoiIds)
		{
			foreach (var deleteRoiId in deleteRoiIds)
			{
				await DeleteSignInterpretationRoiAsync(editionUser, deleteRoiId);
			}
		}

		public async Task<DetailedSignInterpretationROI> GetSignInterpretationRoiByIdAsync(EditionUserInfo editionUser,
			uint signInterpretationRoiId)
		{
			using (var connection = OpenConnection())
			{
				return await connection.QuerySingleAsync<DetailedSignInterpretationROI>(
					GetSignInterpretationRoiDetailsQuery.GetQuery,
					new
					{
						EditionId = editionUser.EditionId,
						SignInterpretationRoiId = signInterpretationRoiId
					});
			}
		}

		#region Private methods

		private async Task<uint> CreateRoiShapeAsync(string path)
		{
			using (var connection = OpenConnection())
			{
				var insertedShape = await connection.ExecuteAsync(
					CreateRoiShapeQuery.GetQuery,
					new
					{
						Path = path
					}
				);

				if (insertedShape != 1)
					throw new StandardErrors.DataNotWritten("Create ROI shape");

				return await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);
			}
		}

		private async Task<uint> CreateRoiPositionAsync(uint artefactId, string transformMatrix)
		{
			using (var connection = OpenConnection())
			{
				var insertedShape = await connection.ExecuteAsync(
					CreateRoiPositionQuery.GetQuery,
					new
					{
						ArtefactId = artefactId,
						TransformMatrix = transformMatrix
					}
				);

				if (insertedShape != 1)
					throw new StandardErrors.DataNotWritten("Create ROI position");

				return await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);
			}
		}

		private async Task<uint> CreateSignInterpretationRoiAsync(
			EditionUserInfo editionUser,
			uint? signInterpretationId,
			uint roiShapeId,
			uint roiPositionId,
			bool valuesSet,
			bool exceptional)
		{
			var signInterpretationRoiParameters = new DynamicParameters();
			signInterpretationRoiParameters.Add("@sign_interpretation_id", signInterpretationId);
			signInterpretationRoiParameters.Add("@roi_shape_id", roiShapeId);
			signInterpretationRoiParameters.Add("@roi_position_id", roiPositionId);
			signInterpretationRoiParameters.Add("@values_set", valuesSet);
			signInterpretationRoiParameters.Add("@exceptional", exceptional);
			var signInterpretationRoiRequest = new MutationRequest(
				MutateType.Create,
				signInterpretationRoiParameters,
				"sign_interpretation_roi"
			);

			var writeResults = await _databaseWriter.WriteToDatabaseAsync(
				editionUser,
				new List<MutationRequest> { signInterpretationRoiRequest }
			);

			if (writeResults.Count != 1 || !writeResults.First().NewId.HasValue)
				throw new StandardErrors.DataNotWritten("create sign interpretation roi");
			return writeResults.First().NewId.Value;
		}

		private async Task<uint> UpdateSignInterpretationRoiAsync(
			EditionUserInfo editionUser,
			uint? signInterpretationId,
			uint roiShapeId,
			uint roiPositionId,
			bool valuesSet,
			bool exceptional,
			uint signInterpretationRoiId)
		{
			var signInterpretationRoiParameters = new DynamicParameters();
			signInterpretationRoiParameters.Add("@sign_interpretation_id", signInterpretationId);
			signInterpretationRoiParameters.Add("@roi_shape_id", roiShapeId);
			signInterpretationRoiParameters.Add("@roi_position_id", roiPositionId);
			signInterpretationRoiParameters.Add("@values_set", valuesSet);
			signInterpretationRoiParameters.Add("@exceptional", exceptional);
			var signInterpretationRoiRequest = new MutationRequest(
				MutateType.Update,
				signInterpretationRoiParameters,
				"sign_interpretation_roi",
				signInterpretationRoiId
			);

			var writeResults = await _databaseWriter.WriteToDatabaseAsync(
				editionUser,
				new List<MutationRequest> { signInterpretationRoiRequest }
			);

			if (writeResults.Count != 1 || !writeResults.First().NewId.HasValue)
				throw new StandardErrors.DataNotWritten("update sign interpretation roi");
			return writeResults.First().NewId.Value;
		}

		private async Task DeleteSignInterpretationRoiAsync(EditionUserInfo editionUser, uint signInterpretationRoiId)
		{
			var signInterpretationRoiRequest = new MutationRequest(
				MutateType.Delete,
				new DynamicParameters(),
				"sign_interpretation_roi",
				signInterpretationRoiId
			);

			var writeResults = await _databaseWriter.WriteToDatabaseAsync(
				editionUser,
				new List<MutationRequest> { signInterpretationRoiRequest }
			);

			if (writeResults.Count != 1)
				throw new StandardErrors.DataNotWritten("delete sign interpretation roi");
		}


		#endregion Private methods
	}
}