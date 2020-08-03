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
    public interface IRoiRepository
    {
        Task<List<SignInterpretationRoiData>> CreateRoisAsync(UserInfo editionUser,
            List<SignInterpretationRoiData> newRois);

        Task<List<SignInterpretationRoiData>> UpdateRoisAsync(UserInfo editionUser,
            List<SignInterpretationRoiData> updateRois);

        (Task<List<SignInterpretationRoiData>>, Task<List<SignInterpretationRoiData>>, Task<List<uint>>)
            BatchEditRoisAsync(UserInfo editionUser,
                List<SignInterpretationRoiData> newRois,
                List<SignInterpretationRoiData> updateRois,
                List<uint> deleteRois);

        Task<List<uint>> DeleteRoisAsync(UserInfo editionUser, List<uint> deleteRoiIds);

        Task<List<uint>> DeleteAllRoisForSignInterpretationAsync(UserInfo editionUser,
            uint signInterpretationId);

        Task<SignInterpretationRoiData> GetSignInterpretationRoiByIdAsync(UserInfo editionUser,
            uint signInterpretationRoiId);

        Task<List<SignInterpretationRoiData>> GetSignInterpretationRoisByArtefactIdAsync(
            UserInfo editionUser,
            uint artefactId);

        Task<List<uint>> GetSignInterpretationRoiIdsByDataAsync(UserInfo editionUser,
            SignInterpretationROISearchData searchData);

        Task<List<SignInterpretationRoiData>> GetSignInterpretationRoisByDataAsync(UserInfo editionUser,
            SignInterpretationROISearchData searchData);

        Task<List<uint>> GetSignInterpretationRoisIdsByInterpretationId(UserInfo editionUser,
            uint signInterpretationId);

        Task<List<SignInterpretationRoiData>> ReplaceSignInterpretationRoisAsync(UserInfo editionUser,
            List<SignInterpretationRoiData> rois);
    }

    public class RoiRepository : DbConnectionBase, IRoiRepository
    {
        private readonly IDatabaseWriter _databaseWriter;

        public RoiRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config)
        {
            _databaseWriter = databaseWriter;
        }


        /// <summary>
        ///     Creates a sign interpretation roi from a list.
        /// </summary>
        /// <param name="editionUser">UserInfo object with user details and edition permissions</param>
        /// <param name="newRois">List of rois to be added to the system.</param>
        /// <returns></returns>
        public async Task<List<SignInterpretationRoiData>> CreateRoisAsync(UserInfo editionUser,
            List<SignInterpretationRoiData> newRois)
        {
            return newRois != null && newRois.Any()
                ? (await Task.WhenAll(
                    newRois.Select(
                        async x =>
                        {
                            var roiShapeId = CreateRoiShapeAsync(x.Shape);
                            var roiPositionId = CreateRoiPositionAsync(
                                x.ArtefactId.GetValueOrDefault(),
                                x.TranslateX.GetValueOrDefault(),
                                x.TranslateY.GetValueOrDefault(),
                                x.StanceRotation.GetValueOrDefault()
                            );
                            var signInterpretationRoiId = await CreateSignInterpretationRoiAsync(
                                editionUser,
                                x.SignInterpretationId,
                                await roiShapeId,
                                await roiPositionId,
                                x.ValuesSet.GetValueOrDefault(),
                                x.Exceptional.GetValueOrDefault()
                            );
                            return (SignInterpretationRoiData)await GetSignInterpretationRoiByIdAsync(
                                editionUser,
                                signInterpretationRoiId
                            );
                        }
                    )
                )).ToList()
                : new List<SignInterpretationRoiData>();
        }

        public (Task<List<SignInterpretationRoiData>>, Task<List<SignInterpretationRoiData>>, Task<List<uint>>)
            BatchEditRoisAsync(UserInfo editionUser,
                List<SignInterpretationRoiData> newRois,
                List<SignInterpretationRoiData> updateRois,
                List<uint> deleteRois)
        {
            using (var transactionScope = new TransactionScope())
            using (var connection = OpenConnection())
            {
                var createdRois = CreateRoisAsync(editionUser, newRois);
                var updatedRois = UpdateRoisAsync(editionUser, updateRois);
                var deletedRois = DeleteRoisAsync(editionUser, deleteRois);
                transactionScope.Complete();
                return (createdRois, updatedRois, deletedRois);
            }
        }

        /// <summary>
        ///     Updates each sign interpretation roi in a list.
        /// </summary>
        /// <param name="editionUser">UserInfo object with user details and edition permissions</param>
        /// <param name="updateRois">List of rois to be added to the system.</param>
        /// <returns></returns>
        public async Task<List<SignInterpretationRoiData>> UpdateRoisAsync(UserInfo editionUser,
            List<SignInterpretationRoiData> updateRois)
        {
            return updateRois != null && updateRois.Any()
                ? (await Task.WhenAll(
                    updateRois.Select(
                        async x =>
                        {
                            var originalSignRoiInterpretation =
                                await GetSignInterpretationRoiByIdAsync(editionUser, x.SignInterpretationRoiId.GetValueOrDefault());

                            // TODO: Maybe parse this better, because the strings can be non-equal, but the data may still be the same.
                            var roiShapeId = originalSignRoiInterpretation.Shape == x.Shape
                                ? originalSignRoiInterpretation.RoiShapeId
                                : await CreateRoiShapeAsync(x.Shape);

                            var roiPositionId = originalSignRoiInterpretation.TranslateX == x.TranslateX
                                                && originalSignRoiInterpretation.TranslateY == x.TranslateY
                                                && originalSignRoiInterpretation.StanceRotation == x.StanceRotation
                                                && originalSignRoiInterpretation.ArtefactId == x.ArtefactId
                                ? originalSignRoiInterpretation.RoiPositionId
                                : await CreateRoiPositionAsync(
                                    x.ArtefactId.GetValueOrDefault(),
                                    x.TranslateX.GetValueOrDefault(),
                                    x.TranslateY.GetValueOrDefault(),
                                    x.StanceRotation.GetValueOrDefault()
                                );

                            var signInterpretationRoiUpdate = await UpdateSignInterpretationRoiAsync(
                                editionUser,
                                x.SignInterpretationId,
                                roiShapeId.GetValueOrDefault(),
                                roiPositionId.GetValueOrDefault(),
                                x.ValuesSet.GetValueOrDefault(),
                                x.Exceptional.GetValueOrDefault(),
                                x.SignInterpretationRoiId.GetValueOrDefault()
                            );
                            if (!signInterpretationRoiUpdate.NewId.HasValue
                                || !signInterpretationRoiUpdate.OldId.HasValue)
                                throw new StandardExceptions.DataNotWrittenException("update sign interpretation");

                            var updatedRoi =
                                (SignInterpretationRoiData)await GetSignInterpretationRoiByIdAsync(
                                    editionUser,
                                    signInterpretationRoiUpdate.NewId.Value
                                );

                            updatedRoi.SignInterpretationRoiId = signInterpretationRoiUpdate.OldId;

                            return updatedRoi;
                        }
                    )
                )).ToList()
                : new List<SignInterpretationRoiData>();
        }

        /// <summary>
        ///     Deletes the ROI's with the submitted roiIds from the edition
        /// </summary>
        /// <param name="editionUser">UserInfo object with user details and edition permissions</param>
        /// <param name="deleteRoiIds">ROI ID's to be deleted'</param>
        /// <returns></returns>
        public async Task<List<uint>> DeleteRoisAsync(UserInfo editionUser, List<uint> deleteRoiIds)
        {
            if (deleteRoiIds == null)
                return new List<uint>();

            foreach (var deleteRoiId in deleteRoiIds) await DeleteSignInterpretationRoiAsync(editionUser, deleteRoiId);
            return deleteRoiIds;
        }

        /// <summary>
        /// Deletes all rois for the sign interpretation referred by its id
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="signInterpretationId">Id of sign interpretation</param>
        /// <returns>List of ids of deleted roiss</returns>
        public async Task<List<uint>> DeleteAllRoisForSignInterpretationAsync(UserInfo editionUser, uint signInterpretationId)
        {
            var roiIds = await GetSignInterpretationRoisIdsByInterpretationId(editionUser, signInterpretationId);
            return await DeleteRoisAsync(
                editionUser,
                roiIds);
        }

        public async Task<SignInterpretationRoiData> GetSignInterpretationRoiByIdAsync(UserInfo editionUser,
            uint signInterpretationRoiId)
        {
            using (var connection = OpenConnection())
            {
                var result = (await connection.QueryAsync<SignInterpretationRoiData>(
                    GetSignInterpretationRoiDetailsQuery.GetQuery,
                    new
                    {
                        editionUser.EditionId,
                        SignInterpretationRoiId = signInterpretationRoiId
                    }
                )).ToList();

                if (result.Count != 1)
                    throw new StandardExceptions.DataNotFoundException(
                        "sign interpretation roi",
                        signInterpretationRoiId
                    );
                return result.First();
            }
        }

        public async Task<List<SignInterpretationRoiData>> GetSignInterpretationRoisByArtefactIdAsync(
            UserInfo editionUser,
            uint artefactId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<SignInterpretationRoiData>(
                    GetSignInterpretationRoiDetailsByArtefactIdQuery.GetQuery,
                    new
                    {
                        editionUser.EditionId,
                        ArtefactId = artefactId
                    }
                )).ToList();
            }
        }

        /// <summary>
        /// Retrieves all sign interpretation rois which match the data provided by searchData
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="searchData">Sign interpretation roi search data object</param>
        /// <returns>List of sign interpretation attribute data - if nothing had been found the list is empty.</returns>
        public async Task<List<SignInterpretationRoiData>> GetSignInterpretationRoisByDataAsync(UserInfo editionUser,
            SignInterpretationROISearchData searchData)
        {
            var query = GetSignInterpretationRoiDetailsByDataQuery.GetQuery.Replace(
                "@WhereData",
                searchData.getSearchParameterString());
            using (var connection = OpenConnection())
            {
                var result = await connection.QueryAsync<SignInterpretationRoiData>(
                    query,
                    new { editionUser.EditionId });
                return result == null ? new List<SignInterpretationRoiData>() : result.ToList();
            }
        }

        public async Task<List<uint>> GetSignInterpretationRoisIdsByInterpretationId(UserInfo editionUser,
            uint signInterpretationId)
        {
            var searchData = new SignInterpretationROISearchData()
            {
                SignInterpretationId = signInterpretationId
            };

            return await GetSignInterpretationRoiIdsByDataAsync(
                editionUser,
                searchData);
        }

        public async Task<List<SignInterpretationRoiData>> ReplaceSignInterpretationRoisAsync(
            UserInfo editionUser,
            List<SignInterpretationRoiData> rois)
        {
            foreach (var signInterpretationId in
                rois.Select(r => r.SignInterpretationId).Distinct())
            {
                await DeleteAllRoisForSignInterpretationAsync(editionUser, signInterpretationId.GetValueOrDefault());
            }

            return await CreateRoisAsync(editionUser, rois);


        }

        /// <summary>
        /// Retrieves all sign interpretation roi ids which match the data provided by searchData
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="searchData">Sign interpretation roi search data object</param>
        /// <returns>List of sign interpretation attribute data - if nothing had been found the list is empty.</returns>
        public async Task<List<uint>> GetSignInterpretationRoiIdsByDataAsync(UserInfo editionUser,
            SignInterpretationROISearchData searchData)
        {
            var query = GetRoiIdByData.GetQuery.Replace(
                "@WhereData",
                searchData.getSearchParameterString()).Replace(
                "@JoinString",
                searchData.getJoinsString());
            using (var connection = OpenConnection())
            {
                var result = await connection.QueryAsync<uint>(
                    query,
                    new { editionUser.EditionId });
                return result == null ? new List<uint>() : result.ToList();
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

                return await connection.QuerySingleAsync<uint>(
                    GetRoiShapeIdQuery.GetQuery,
                    new
                    {
                        Path = path
                    }
                );
            }
        }

        private async Task<uint> CreateRoiPositionAsync(uint artefactId,
            int translateX,
            int translateY,
            ushort stanceRotate)
        {
            using (var connection = OpenConnection())
            {
                var insertedShape = await connection.ExecuteAsync(
                    CreateRoiPositionQuery.GetQuery,
                    new
                    {
                        ArtefactId = artefactId,
                        TranslateX = translateX,
                        TranslateY = translateY,
                        StanceRotation = stanceRotate
                    }
                );

                return await connection.QuerySingleAsync<uint>(
                    GetRoiPositionIdQuery.GetQuery,
                    new
                    {
                        ArtefactId = artefactId,
                        TranslateX = translateX,
                        TranslateY = translateY,
                        StanceRotation = stanceRotate
                    }
                );
            }
        }

        private async Task<uint> CreateSignInterpretationRoiAsync(
            UserInfo editionUser,
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

            if (writeResults.Count != 1
                || !writeResults.First().NewId.HasValue)
                throw new StandardExceptions.DataNotWrittenException("create sign interpretation roi");
            return writeResults.First().NewId.Value;
        }

        private async Task<AlteredRecord> UpdateSignInterpretationRoiAsync(
            UserInfo editionUser,
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

            if (writeResults.Count != 1
                || !writeResults.First().NewId.HasValue)
                throw new StandardExceptions.DataNotWrittenException("update sign interpretation roi");
            return writeResults.First();
        }

        private async Task DeleteSignInterpretationRoiAsync(UserInfo editionUser, uint signInterpretationRoiId)
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
                throw new StandardExceptions.DataNotWrittenException("delete sign interpretation roi");
        }

        #endregion Private methods
    }
}