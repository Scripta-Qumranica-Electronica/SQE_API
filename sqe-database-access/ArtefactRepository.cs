using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess
{
    public interface IArtefactRepository
    {
        Task<ArtefactModel> GetEditionArtefactAsync(EditionUserInfo editionUser,
            uint artefactId,
            bool withMask = false);

        // Bronson: Don't return query results, create a DataModel object and return that. The query results are internal
        // Itay: I would prefer not to go through three levels of serialization: query Result -> intermediary -> DTO.  The
        // service serializes the DTO and the repo serializes the queried data (two serialization operations, two object classes).
        // I would be ok with having some external model, and letting the repo tell Dapper serialize to that model instead
        // of the query result object, and using that as the function returns.
        Task<IEnumerable<ArtefactModel>> GetEditionArtefactListAsync(EditionUserInfo editionUser,
            bool withMask = false);

        Task<List<AlteredRecord>> UpdateArtefactShapeAsync(EditionUserInfo editionUser,
            uint artefactId,
            string shape);

        Task<List<AlteredRecord>> UpdateArtefactStatusAsync(EditionUserInfo editionUser,
            uint artefactId,
            string workStatus);

        Task<List<AlteredRecord>> UpdateArtefactNameAsync(EditionUserInfo editionUser, uint artefactId, string name);

        Task<List<AlteredRecord>> UpdateArtefactPositionAsync(EditionUserInfo editionUser,
            uint artefactId,
            float? scale,
            float? rotate,
            uint? translateX,
            uint? translateY,
            sbyte? zIndex);

        Task<uint> CreateNewArtefactAsync(EditionUserInfo editionUser,
            uint masterImageId,
            string shape,
            string artefactName,
            float? scale,
            float? rotate,
            uint? translateX,
            uint? translateY,
            sbyte? zIndex,
            string workStatus);

        Task DeleteArtefactAsync(EditionUserInfo editionUser, uint artefactId);
        Task<List<TextFragmentData>> ArtefactTextFragmentsAsync(EditionUserInfo editionUser, uint artefactId);
        Task<List<TextFragmentData>> ArtefactSuggestedTextFragmentsAsync(EditionUserInfo editionUser, uint artefactId);
    }

    public class ArtefactRepository : DbConnectionBase, IArtefactRepository
    {
        private readonly IDatabaseWriter _databaseWriter;

        public ArtefactRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config)
        {
            _databaseWriter = databaseWriter;
        }

        public async Task<ArtefactModel> GetEditionArtefactAsync(EditionUserInfo editionUser,
            uint artefactId,
            bool withMask = false)
        {
            using (var connection = OpenConnection())
            {
                var artefacts = await connection.QueryAsync<ArtefactModel>(
                    ArtefactOfEditionQuery.GetQuery(editionUser.userId, withMask),
                    new
                    {
                        editionUser.EditionId,
                        UserId = editionUser.userId,
                        ArtefactId = artefactId
                    }
                );
                if (artefacts.Count() != 1)
                    throw new StandardExceptions.DataNotFoundException("artefact", artefactId, "artefact_id");
                return artefacts.First();
            }
        }

        /// <summary>
        ///     Returns artefact details for all artefacts belonging to the specified edition.
        /// </summary>
        /// <param name="editionUser"></param>
        /// <param name="withMask">Optionally include the mask data for the artefacts</param>
        /// <returns></returns>
        public async Task<IEnumerable<ArtefactModel>> GetEditionArtefactListAsync(EditionUserInfo editionUser,
            bool withMask = false)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<ArtefactModel>(
                    ArtefactsOfEditionQuery.GetQuery(editionUser.userId, withMask),
                    new
                    {
                        editionUser.EditionId,
                        UserId = editionUser.userId
                    }
                );
            }
        }

        public async Task<List<AlteredRecord>> UpdateArtefactShapeAsync(EditionUserInfo editionUser,
            uint artefactId,
            string shape)
        {
            /* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
			 write the value directly into the database, but it does not seem to work right yet.  Thus we currently 
			 use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().
			 
			var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
			var res = string.Join("", binaryMask);
			var Mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
            const string tableName = "artefact_shape";
            var artefactShapeId = await GetArtefactPkAsync(editionUser, artefactId, tableName);
            if (artefactShapeId == 0)
                throw new StandardExceptions.DataNotFoundException("artefact mask", artefactId, "artefact_id");
            var sqeImageId = GetArtefactShapeSqeImageIdAsync(editionUser, artefactId);

            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@region_in_sqe_image", shape);
            artefactChangeParams.Add("@artefact_id", artefactId);
            artefactChangeParams.Add("@sqe_image_id", await sqeImageId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Update,
                artefactChangeParams,
                tableName,
                artefactShapeId
            );
            try
            {
                return await WriteArtefactAsync(editionUser, artefactChangeRequest);
            }
            catch (MySqlException e)
            {
                // Capture any errors caused by improperly formatted WKT shapes, which become null in this query.
                if (e.Message.IndexOf("Column 'region_in_sqe_image' cannot be null", StringComparison.Ordinal) > -1)
                    throw new StandardExceptions.ImproperInputDataException("mask");

                throw;
            }
        }

        public async Task<List<AlteredRecord>> UpdateArtefactStatusAsync(EditionUserInfo editionUser,
            uint artefactId,
            string workStatus)
        {
            const string tableName = "artefact_status";
            var artefactStatusId = await GetArtefactPkAsync(editionUser, artefactId, tableName);
            if (artefactStatusId == 0)
                return await InsertArtefactStatusAsync(editionUser, artefactId, workStatus);

            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@artefact_id", artefactId);
            if (!string.IsNullOrEmpty(workStatus))
                artefactChangeParams.Add("@work_status_id", await SetWorkStatusAsync(workStatus));
            var artefactChangeRequest = new MutationRequest(
                MutateType.Update,
                artefactChangeParams,
                tableName,
                artefactStatusId
            );

            return await WriteArtefactAsync(editionUser, artefactChangeRequest);
        }

        public async Task<List<AlteredRecord>> UpdateArtefactNameAsync(EditionUserInfo editionUser,
            uint artefactId,
            string name)
        {
            const string tableName = "artefact_data";
            var artefactDataId = await GetArtefactPkAsync(editionUser, artefactId, tableName);
            if (artefactDataId == 0)
                throw new StandardExceptions.DataNotFoundException("artefact name", artefactId, "artefact_id");
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@Name", name);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Update,
                artefactChangeParams,
                tableName,
                artefactDataId
            );

            return await WriteArtefactAsync(editionUser, artefactChangeRequest);
        }

        public async Task<List<AlteredRecord>> UpdateArtefactPositionAsync(EditionUserInfo editionUser,
            uint artefactId,
            float? scale,
            float? rotate,
            uint? translateX,
            uint? translateY,
            sbyte? zIndex)
        {
            const string tableName = "artefact_position";
            var artefactPositionId = await GetArtefactPkAsync(editionUser, artefactId, tableName);
            // It is not necessary for every artefact to have a position (they may get positioning via artefact stack).
            // If no artefact_position already exists we need to create a new entry here.
            if (artefactPositionId == 0)
                return await InsertArtefactPositionAsync(
                    editionUser,
                    artefactId,
                    scale,
                    rotate,
                    translateX,
                    translateY,
                    zIndex
                );

            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@scale", scale);
            artefactChangeParams.Add("@rotate", rotate);
            artefactChangeParams.Add("@translate_x", translateX);
            artefactChangeParams.Add("@translate_y", translateY);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Update,
                artefactChangeParams,
                tableName,
                artefactPositionId
            );

            return await WriteArtefactAsync(editionUser, artefactChangeRequest);
        }

        public async Task<uint> CreateNewArtefactAsync(EditionUserInfo editionUser,
            uint masterImageId,
            string shape,
            string artefactName,
            float? scale,
            float? rotate,
            uint? translateX,
            uint? translateY,
            sbyte? zIndex,
            string workStatus)
        {
            /* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
			 write the value directly into the database, but it does not seem to work right yet.  Thus we currently 
			 use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().
			 
			var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
			var res = string.Join("", binaryMask);
			var Mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
            return await DatabaseCommunicationRetryPolicy.ExecuteRetry(
                async () =>
                {
                    using (var transactionScope = new TransactionScope())
                    using (var connection = OpenConnection())
                    {
                        // Create a new edition
                        await connection.ExecuteAsync("INSERT INTO artefact (artefact_id) VALUES(NULL)");

                        var artefactId = await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);
                        if (artefactId == 0)
                            throw new StandardExceptions.DataNotWrittenException("create artefact");

                        shape = string.IsNullOrEmpty(shape) ? "POLYGON((0 0))" : shape;
                        var newShape = InsertArtefactShapeAsync(editionUser, artefactId, masterImageId, shape);
                        var newArtefact = InsertArtefactStatusAsync(editionUser, artefactId, workStatus);
                        var newName = InsertArtefactNameAsync(editionUser, artefactId, artefactName ?? "");
                        if (scale.HasValue
                            || rotate.HasValue
                            || translateX.HasValue
                            || translateY.HasValue
                            || zIndex.HasValue)
                            await InsertArtefactPositionAsync(
                                editionUser,
                                artefactId,
                                scale,
                                rotate,
                                translateX,
                                translateY,
                                zIndex
                            );

                        await newShape;
                        await newArtefact;
                        await newName;
                        //Cleanup
                        transactionScope.Complete();

                        return artefactId;
                    }
                }
            );
        }

        public async Task DeleteArtefactAsync(EditionUserInfo editionUser, uint artefactId)
        {
            var mutations = new List<MutationRequest>();
            foreach (var table in artefactTableNames.All())
                if (table != artefactTableNames.stack)
                {
                    var pk = await GetArtefactPkAsync(editionUser, artefactId, table);
                    if (pk != 0)
                        mutations.Add(new MutationRequest(MutateType.Delete, new DynamicParameters(), table, pk));
                }
                else
                {
                    var pks = await GetArtefactStackPksAsync(editionUser, artefactId, table);
                    mutations.AddRange(
                        pks.Select(pk => new MutationRequest(
                            MutateType.Delete,
                            new DynamicParameters(),
                            table,
                            pk)));
                }

            var _ = await _databaseWriter.WriteToDatabaseAsync(editionUser, mutations);
        }

        public async Task<List<TextFragmentData>> ArtefactTextFragmentsAsync(EditionUserInfo editionUser,
            uint artefactId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<TextFragmentData>(
                    FindArtefactTextFragments.GetQuery,
                    new
                    {
                        editionUser.EditionId,
                        UserId = editionUser.userId,
                        ArtefactId = artefactId
                    }
                )).ToList();
            }
        }

        public async Task<List<TextFragmentData>> ArtefactSuggestedTextFragmentsAsync(EditionUserInfo editionUser,
            uint artefactId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<TextFragmentData>(
                    FindSuggestedArtefactTextFragments.GetQuery,
                    new
                    {
                        editionUser.EditionId,
                        UserId = editionUser.userId,
                        ArtefactId = artefactId
                    }
                )).ToList();
            }
        }

        public async Task<List<AlteredRecord>> InsertArtefactShapeAsync(EditionUserInfo editionUser,
            uint artefactId,
            uint masterImageId,
            string shape)
        {
            /* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
			 write the value directly into the database, but it does not seem to work right yet.  Thus we currently 
			 use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().
			 
			var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
			var res = string.Join("", binaryMask);
			var Mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/

            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@region_in_sqe_image", shape);
            artefactChangeParams.Add("@sqe_image_id", masterImageId);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Create,
                artefactChangeParams,
                "artefact_shape"
            );

            return await WriteArtefactAsync(editionUser, artefactChangeRequest);
        }

        public async Task<List<AlteredRecord>> InsertArtefactStatusAsync(EditionUserInfo editionUser,
            uint artefactId,
            string workStatus)
        {
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@artefact_id", artefactId);
            if (!string.IsNullOrEmpty(workStatus))
                artefactChangeParams.Add("@work_status_id", await SetWorkStatusAsync(workStatus));
            var artefactChangeRequest = new MutationRequest(
                MutateType.Create,
                artefactChangeParams,
                "artefact_status"
            );

            return await WriteArtefactAsync(editionUser, artefactChangeRequest);
        }

        public async Task<List<AlteredRecord>> InsertArtefactNameAsync(EditionUserInfo editionUser,
            uint artefactId,
            string name)
        {
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@name", name);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Create,
                artefactChangeParams,
                "artefact_data"
            );

            return await WriteArtefactAsync(editionUser, artefactChangeRequest);
        }

        public async Task<List<AlteredRecord>> InsertArtefactPositionAsync(EditionUserInfo editionUser,
            uint artefactId,
            float? scale,
            float? rotate,
            uint? translateX,
            uint? translateY,
            sbyte? zIndex)
        {
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@scale", scale);
            artefactChangeParams.Add("@rotate", rotate);
            artefactChangeParams.Add("@z_index", zIndex ?? 0);
            artefactChangeParams.Add("@translate_x", translateX);
            artefactChangeParams.Add("@translate_y", translateY);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Create,
                artefactChangeParams,
                "artefact_position"
            );

            return await WriteArtefactAsync(editionUser, artefactChangeRequest);
        }

        public async Task<List<AlteredRecord>> WriteArtefactAsync(EditionUserInfo editionUser,
            MutationRequest artefactChangeRequest)
        {
            // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
            // care of main_action and single_action.
            return await _databaseWriter.WriteToDatabaseAsync(
                editionUser,
                new List<MutationRequest> { artefactChangeRequest }
            );
        }

        private async Task<uint> GetArtefactPkAsync(EditionUserInfo editionUser, uint artefactId, string table)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryFirstOrDefaultAsync<uint>(
                    FindArtefactComponentId.GetQuery(table),
                    new
                    {
                        editionUser.EditionId,
                        ArtefactId = artefactId
                    }
                );
            }
        }

        private async Task<List<uint>> GetArtefactStackPksAsync(EditionUserInfo editionUser,
            uint artefactId,
            string table)
        {
            using (var connection = OpenConnection())
            {
                var stacks = (await connection.QueryAsync<uint>(
                    FindArtefactComponentId.GetQuery(table, true),
                    new
                    {
                        editionUser.EditionId,
                        ArtefactId = artefactId
                    }
                )).ToList();

                return stacks;
            }
        }

        private async Task<uint> GetArtefactShapeSqeImageIdAsync(EditionUserInfo editionUser,
            uint artefactId)
        {
            using (var connection = OpenConnection())
            {
                try
                {
                    return await connection.QuerySingleAsync<uint>(
                        FindArtefactShapeSqeImageId.GetQuery,
                        new
                        {
                            editionUser.EditionId,
                            ArtefactId = artefactId
                        }
                    );
                }
                catch (InvalidOperationException)
                {
                    throw new StandardExceptions.DataNotFoundException("SQE_image", artefactId, "artefact_id");
                }
            }
        }

        private async Task<uint?> SetWorkStatusAsync(string workStatus)
        {
            if (string.IsNullOrEmpty(workStatus))
                return null;
            using (var connection = OpenConnection())
            {
                await connection.ExecuteAsync(
                    SetWorkStatus.GetQuery,
                    new { WorkStatus = workStatus }
                );

                return await connection.QuerySingleAsync<uint>(
                    GetWorkStatus.GetQuery,
                    new { WorkStatus = workStatus }
                );
            }
        }

        private static class artefactTableNames
        {
            public const string data = "artefact_data";
            public const string shape = "artefact_shape";
            public const string position = "artefact_position";
            public const string stack = "artefact_stack";
            public const string status = "artefact_status";

            public static List<string> All()
            {
                return new List<string> { data, shape, position, stack, status };
            }
        }
    }
}