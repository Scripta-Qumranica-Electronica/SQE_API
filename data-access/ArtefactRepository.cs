using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Transactions;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess
{

    public interface IArtefactRepository
    {
        Task<ArtefactModel> GetEditionArtefactAsync(UserInfo user, uint artfactId, bool withMask = false);

        // Bronson: Don't return query results, create a DataModel object and return that. The query results are internal
        // Itay: I would prefer not to go through three levels of serialization: query Result -> intermediary -> DTO.  The
        // service serializes the DTO and the repo serializes the queried data (two serialization operations, two object classes).
        // I would be ok with having some external model, and letting the repo tell Dapper serialize to that model instead
        // of the query result object, and using that as the function returns.
        Task<IEnumerable<ArtefactModel>> GetEditionArtefactListAsync(uint? userId, uint editionId, bool withMask = false);

        Task<List<AlteredRecord>> UpdateArtefactShape(UserInfo user, uint artefactId, string shape);
        Task<List<AlteredRecord>> UpdateArtefactName(UserInfo user, uint artefactId, string name);

        Task<List<AlteredRecord>> UpdateArtefactPosition(UserInfo user, uint artefactId,
            string position);
        Task<uint> CreateNewArtefact(UserInfo user, uint editionId, uint masterImageId, string shape, string artefactName, string position = null);
        Task DeleteArtefact(UserInfo user, uint artefactId);
    }

    public class ArtefactRepository : DbConnectionBase, IArtefactRepository
    {
        private readonly IDatabaseWriter _databaseWriter;

        public ArtefactRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config)
        {
            _databaseWriter = databaseWriter;
        }

        public async Task<ArtefactModel> GetEditionArtefactAsync(UserInfo user, uint artfactId, bool withMask = false)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QuerySingleAsync<ArtefactModel>(ArtefactOfEditionQuery.GetQuery(user.userId, withMask),
                    new
                    {
                        EditionId = user.editionId ?? 0,
                        UserId = user.userId ?? 0,
                        ArtefactId = artfactId
                    });
            }
        }

        public async Task<IEnumerable<ArtefactModel>> GetEditionArtefactListAsync(uint? userId, uint editionId, bool withMask = false)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<ArtefactModel>(ArtefactsOfEditionQuery.GetQuery(userId, withMask),
                    new
                    {
                        EditionId = editionId,
                        UserId = userId ?? 0
                    });
            }
        }

        public async Task<List<AlteredRecord>> UpdateArtefactShape(UserInfo user, uint artefactId, string shape)
        {
            /* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
             write the value directly into the database, but it does not seem to work right yet.  Thus we currently 
             use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().
             
            var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
            var res = string.Join("", binaryMask);
            var Mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
            const string tableName = "artefact_shape";
            var artefactShapeId = GetArtefactPk(user, artefactId, tableName);
            var sqeImageId = GetArtefactShapeSqeImageId(user, user.editionId.Value, artefactId);
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@region_in_sqe_image", shape);
            artefactChangeParams.Add("@artefact_id", artefactId);
            artefactChangeParams.Add("@sqe_image_id", await sqeImageId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Update,
                artefactChangeParams,
                tableName,
                await artefactShapeId
            );
                    
            return await WriteArtefact(user, artefactChangeRequest);
        }
        
        public async Task<List<AlteredRecord>> UpdateArtefactName(UserInfo user, uint artefactId, string name)
        {
            const string tableName = "artefact_data";
            var artefactDataId = GetArtefactPk(user, artefactId, tableName);
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@Name", name);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Update,
                artefactChangeParams,
                tableName,
                await artefactDataId
            );
                    
            return await WriteArtefact(user, artefactChangeRequest);
        }
        
        public async Task<List<AlteredRecord>> UpdateArtefactPosition(UserInfo user, uint artefactId, string position)
        {
            const string tableName = "artefact_position";
            var artefactPositionId = GetArtefactPk(user, artefactId, tableName);
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@transform_matrix", position);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Update,
                artefactChangeParams,
                tableName,
                await artefactPositionId
            );
                    
            return await WriteArtefact(user, artefactChangeRequest);
        }
        
        public async Task<List<AlteredRecord>> InsertArtefactShape(UserInfo user, uint artefactId, uint masterImageId,
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
                    
            return await WriteArtefact(user, artefactChangeRequest);
        }
        
        public async Task<List<AlteredRecord>> InsertArtefactName(UserInfo user, uint artefactId, string name)
        {
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@name", name);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Create,
                artefactChangeParams,
                "artefact_data"
            );
                    
            return await WriteArtefact(user, artefactChangeRequest);
        }
        
        public async Task<List<AlteredRecord>> InsertArtefactPosition(UserInfo user, uint artefactId, string position)
        {
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@transform_matrix", position);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Create,
                artefactChangeParams,
                "artefact_position"
            );

            return await WriteArtefact(user, artefactChangeRequest);
        }

        public async Task<List<AlteredRecord>> WriteArtefact(UserInfo user, MutationRequest artefactChangeRequest)
        {
            // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
            // care of main_action and single_action.
            var response = await _databaseWriter.WriteToDatabaseAsync(user, new List<MutationRequest>() { artefactChangeRequest });
                    
            if (response.Count == 0 || !response[0].NewId.HasValue)
            {
                throw new ImproperRequestException("insert artefact position",
                    "no change or failed to write to DB");
            }

            return response;
        }
        
        public async Task<uint> CreateNewArtefact(UserInfo user, uint editionId, uint masterImageId, string shape, string artefactName, string position = null)
        {
            /* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
             write the value directly into the database, but it does not seem to work right yet.  Thus we currently 
             use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().
             
            var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
            var res = string.Join("", binaryMask);
            var Mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
           
            using (var transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted }
            ))
            {
                using (var connection = OpenConnection())
                {
                    try
                    {
                        // Create a new edition
                        var results = await connection.ExecuteAsync("INSERT INTO artefact () VALUES()");
                        if (results != 1)
                            throw new DbFailedWrite();
                        
                        var artefactId = await connection.QuerySingleAsync<uint>(LastInsertId.GetQuery);
                        var newShape = InsertArtefactShape(user, artefactId, masterImageId, shape);
                        var newName = InsertArtefactName(user, artefactId, artefactName);
                        // TODO: check virtual scroll for unused spot to place the artefact instead of putting it at the beginning.
                        var newPosition = InsertArtefactPosition(
                            user, 
                            artefactId, 
                            string.IsNullOrEmpty(position) ? "{\"matrix\": [[1,0,0],[0,1,0]]}" : position);
                        await newShape;
                        await newName;
                        await newPosition;
                        //Cleanup
                        transactionScope.Complete();
                        
                        return artefactId;
                    }
                    catch
                    {
                        //Maybe we do something special with the errors?
                        throw new DbFailedWrite();
                    }
                }
            }
        }

        public async Task DeleteArtefact(UserInfo user, uint artefactId)
        {
            List<MutationRequest> mutations = new List<MutationRequest>();
            foreach (var table in artefactTableNames.All())
            {
                if (table != artefactTableNames.stack)
                {
                    var pk = await GetArtefactPk(user, artefactId, table);
                    mutations.Add(new MutationRequest(MutateType.Delete, new DynamicParameters(), table, pk));
                }
                else
                {
                    var pks = await GetArtefactStackPks(user, artefactId, table);
                    foreach (var pk in pks)
                    {
                        mutations.Add(new MutationRequest(MutateType.Delete, new DynamicParameters(), table, pk));
                    }
                }
            }

            var status = _databaseWriter.WriteToDatabaseAsync(user, mutations);
        }

        private async Task<uint> GetArtefactPk(UserInfo user, uint artefactId, string table)
        {
            using (var connection = OpenConnection())
            {
                try
                {
                    return await connection.QuerySingleAsync<uint>(
                        FindArtefactComponentId.GetQuery(table),
                        new
                        {
                            EditionId = user.editionId.Value,
                            ArtefactId = artefactId
                        });
                }
                catch
                {
                    throw new ImproperRequestException("artefact mutate",
                        "could not find existing record in DB");
                }
            }
        }
        private async Task<List<uint>> GetArtefactStackPks(UserInfo user, uint artefactId, string table)
        {
            using (var connection = OpenConnection())
            {
                try
                {
                    return (await connection.QueryAsync<uint>(
                        FindArtefactComponentId.GetQuery(table, stack: true),
                        new
                        {
                            EditionId = user.editionId.Value,
                            ArtefactId = artefactId
                        })).ToList();
                }
                catch
                {
                    throw new ImproperRequestException("artefact mutate",
                        "could not find existing record in DB");
                }
            }
        }
        
        private async Task<uint> GetArtefactShapeSqeImageId(UserInfo user, uint editionId, uint artefactId)
        {
            using (var connection = OpenConnection())
            {
                try
                {
                    return await connection.QuerySingleAsync<uint>(FindArtefactShapeSqeImageId.GetQuery,
                        new
                        {
                            EditionId = editionId,
                            ArtefactId = artefactId
                        });
                }
                catch
                {
                    throw new ImproperRequestException("artefact update",
                        "could not find existing record in DB");
                }
            }
        }

        private static class artefactTableNames
        {
            public const string data = "artefact_data";
            public const string shape = "artefact_shape";
            public const string position = "artefact_position";
            public const string stack = "artefact_stack";

            public static List<string> All()
            {
                return new List<string>(){data, shape, position, stack};
            }
        }
    }
}
