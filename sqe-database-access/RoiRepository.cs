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
        Task<List<SignInterpretationROI>> CreateRoisAsync(EditionUserInfo editionUser,
            List<SetSignInterpretationROI> newRois);

        Task<List<UpdatedSignInterpretationROI>> UpdateRoisAsync(EditionUserInfo editionUser,
            List<SignInterpretationROI> updateRois);

        (Task<List<SignInterpretationROI>>, Task<List<UpdatedSignInterpretationROI>>, Task<List<uint>>)
            BatchEditRoisAsync(EditionUserInfo editionUser,
                List<SetSignInterpretationROI> newRois,
                List<SignInterpretationROI> updateRois,
                List<uint> deleteRois);

        Task<List<uint>> DeletRoisAsync(EditionUserInfo editionUser, List<uint> deleteRoiIds);

        Task<DetailedSignInterpretationROI> GetSignInterpretationRoiByIdAsync(EditionUserInfo editionUser,
            uint signInterpretationRoiId);

        Task<List<DetailedSignInterpretationROI>> GetSignInterpretationRoisByArtefactIdAsync(
            EditionUserInfo editionUser,
            uint artefactId);
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
        public async Task<List<SignInterpretationROI>> CreateRoisAsync(EditionUserInfo editionUser,
            List<SetSignInterpretationROI> newRois)
        {
            return newRois != null && newRois.Any()
                ? (await Task.WhenAll(
                    newRois.Select(
                        async x =>
                        {
                            var roiShapeId = CreateRoiShapeAsync(x.Shape);
                            var roiPositionId = CreateRoiPositionAsync(
                                x.ArtefactId,
                                x.TranslateX,
                                x.TranslateY,
                                x.StanceRotation
                            );
                            var signInterpretationRoiId = await CreateSignInterpretationRoiAsync(
                                editionUser,
                                x.SignInterpretationId,
                                await roiShapeId,
                                await roiPositionId,
                                x.ValuesSet,
                                x.Exceptional
                            );
                            return (SignInterpretationROI)await GetSignInterpretationRoiByIdAsync(
                                editionUser,
                                signInterpretationRoiId
                            );
                        }
                    )
                )).ToList()
                : new List<SignInterpretationROI>();
        }

        public (Task<List<SignInterpretationROI>>, Task<List<UpdatedSignInterpretationROI>>, Task<List<uint>>)
            BatchEditRoisAsync(EditionUserInfo editionUser,
                List<SetSignInterpretationROI> newRois,
                List<SignInterpretationROI> updateRois,
                List<uint> deleteRois)
        {
            using (var transactionScope = new TransactionScope())
            using (var connection = OpenConnection())
            {
                var createdRois = CreateRoisAsync(editionUser, newRois);
                var updatedRois = UpdateRoisAsync(editionUser, updateRois);
                var deletedRois = DeletRoisAsync(editionUser, deleteRois);
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
        public async Task<List<UpdatedSignInterpretationROI>> UpdateRoisAsync(EditionUserInfo editionUser,
            List<SignInterpretationROI> updateRois)
        {
            return updateRois != null && updateRois.Any()
                ? (await Task.WhenAll(
                    updateRois.Select(
                        async x =>
                        {
                            var originalSignRoiInterpretation =
                                await GetSignInterpretationRoiByIdAsync(editionUser, x.SignInterpretationRoiId);

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
                                    x.ArtefactId,
                                    x.TranslateX,
                                    x.TranslateY,
                                    x.StanceRotation
                                );

                            var signInterpretationRoiUpdate = await UpdateSignInterpretationRoiAsync(
                                editionUser,
                                x.SignInterpretationId,
                                roiShapeId,
                                roiPositionId,
                                x.ValuesSet,
                                x.Exceptional,
                                x.SignInterpretationRoiId
                            );
                            if (!signInterpretationRoiUpdate.NewId.HasValue
                                || !signInterpretationRoiUpdate.OldId.HasValue)
                                throw new StandardExceptions.DataNotWrittenException("update sign interpretation");

                            var updatedRoi =
                                (SignInterpretationROI)await GetSignInterpretationRoiByIdAsync(
                                    editionUser,
                                    signInterpretationRoiUpdate.NewId.Value
                                );
                            return new UpdatedSignInterpretationROI
                            {
                                ArtefactId = updatedRoi.ArtefactId,
                                Exceptional = updatedRoi.Exceptional,
                                OldSignInterpretationRoiId = signInterpretationRoiUpdate.OldId.Value,
                                Position = updatedRoi.Position,
                                Shape = updatedRoi.Shape,
                                SignInterpretationId = updatedRoi.SignInterpretationId,
                                SignInterpretationRoiAuthor = updatedRoi.SignInterpretationRoiAuthor,
                                SignInterpretationRoiId = updatedRoi.SignInterpretationRoiId,
                                ValuesSet = updatedRoi.ValuesSet
                            };
                        }
                    )
                )).ToList()
                : new List<UpdatedSignInterpretationROI>();
        }

        /// <summary>
        ///     Deletes the ROI's with the submitted roiIds from the edition
        /// </summary>
        /// <param name="editionUser">UserInfo object with user details and edition permissions</param>
        /// <param name="deleteRoiIds">ROI ID's to be deleted'</param>
        /// <returns></returns>
        public async Task<List<uint>> DeletRoisAsync(EditionUserInfo editionUser, List<uint> deleteRoiIds)
        {
            if (deleteRoiIds == null)
                return new List<uint>();

            foreach (var deleteRoiId in deleteRoiIds) await DeleteSignInterpretationRoiAsync(editionUser, deleteRoiId);
            return deleteRoiIds;
        }

        public async Task<DetailedSignInterpretationROI> GetSignInterpretationRoiByIdAsync(EditionUserInfo editionUser,
            uint signInterpretationRoiId)
        {
            using (var connection = OpenConnection())
            {
                var result = (await connection.QueryAsync<DetailedSignInterpretationROI>(
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

        public async Task<List<DetailedSignInterpretationROI>> GetSignInterpretationRoisByArtefactIdAsync(
            EditionUserInfo editionUser,
            uint artefactId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<DetailedSignInterpretationROI>(
                    GetSignInterpretationRoiDetailsByArtefactIdQuery.GetQuery,
                    new
                    {
                        editionUser.EditionId,
                        ArtefactId = artefactId
                    }
                )).ToList();
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
            uint translateX,
            uint translateY,
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

            if (writeResults.Count != 1
                || !writeResults.First().NewId.HasValue)
                throw new StandardExceptions.DataNotWrittenException("create sign interpretation roi");
            return writeResults.First().NewId.Value;
        }

        private async Task<AlteredRecord> UpdateSignInterpretationRoiAsync(
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

            if (writeResults.Count != 1
                || !writeResults.First().NewId.HasValue)
                throw new StandardExceptions.DataNotWrittenException("update sign interpretation roi");
            return writeResults.First();
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
                throw new StandardExceptions.DataNotWrittenException("delete sign interpretation roi");
        }

        #endregion Private methods
    }
}