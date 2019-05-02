using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Dapper;
using System.Linq;
using System.Transactions;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;
using Wkx;
using Polygon = Wkx.Polygon;

namespace SQE.SqeHttpApi.DataAccess
{

    public interface IArtefactRepository
    {
        Task<Artefact> GetArtefact(uint? userId, uint artefactId);
        Task<IEnumerable<ArtefactNamesOfEditionQuery.Result>> GetEditionArtefactNameListAsync(uint? userId, uint editionId);
        Task<IEnumerable<ArtefactNamesOfEditionQuery.Result>> GetEditionArtefactListAsync(uint? userId, uint editionId, bool withMask = false);
        Task<List<AlteredRecord>> UpdateArtefactShape(UserInfo user, uint editionId, uint artefactId, string shape);
        Task<List<AlteredRecord>> UpdateArtefactName(UserInfo user, uint editionId, uint artefactId, string name);

        Task<List<AlteredRecord>> UpdateArtefactPosition(UserInfo user, uint editionId, uint artefactId,
            string position);
        Task<uint> CreateNewArtefact(UserInfo user, uint editionId, uint masterImageId, string shape, string artefactName, string position = null);
    }

    public class ArtefactRepository : DbConnectionBase, IArtefactRepository
    {
        private readonly IDatabaseWriter _databaseWriter;

        public ArtefactRepository(IConfiguration config, IDatabaseWriter databaseWriter) : base(config)
        {
            _databaseWriter = databaseWriter;
        }

        public async Task<IEnumerable<ArtefactNamesOfEditionQuery.Result>> GetEditionArtefactListAsync(uint? userId, uint editionId, bool withMask = false)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<ArtefactNamesOfEditionQuery.Result>(ArtefactNamesOfEditionQuery.GetQuery(userId, withMask),
                    new
                    {
                        EditionId = editionId,
                        UserId = userId ?? 0
                    });
            }
        }
        
        public Task<Artefact> GetArtefact(uint? userId, uint artfactId)
        {

            /**  using (var connection = OpenConnection() as MySqlConnection)
              {
               
                  //update scroll data owner
                  var cmd = new MySqlCommand(UpdateScrollNameQueries.UpdateScrollDataOwner(), connection);
                  cmd.Parameters.AddWithValue("@ScrollVersionId", sv.Id);
                  cmd.Parameters.AddWithValue("@ScrollDataId", scrollDataId);
                  await cmd.ExecuteNonQueryAsync();

                  //update main_action table
                  cmd = new MySqlCommand(UpdateScrollNameQueries.AddMainAction(), connection);
                  cmd.Parameters.AddWithValue("@ScrollVersionId", sv.Id);
                  await cmd.ExecuteNonQueryAsync();
                  var mainActionId = Convert.ToInt32(cmd.LastInsertedId);

                  //update single_action table

                  cmd = new MySqlCommand(UpdateScrollNameQueries.AddSingleAction(), connection);
                  cmd.Parameters.AddWithValue("@MainActionId", mainActionId);
                  cmd.Parameters.AddWithValue("@IdInTable", scrollDataId);
                  cmd.Parameters.AddWithValue("@Action", "add");



                  await cmd.ExecuteNonQueryAsync();

                  transactionScope.Complete();
                  await connection.CloseAsync();
              }

          }
                  return sv;**/
            return null;
        }

        public async Task<IEnumerable<ArtefactNamesOfEditionQuery.Result>> GetEditionArtefactNameListAsync(uint? userId, uint editionId)
        {
            using (var connection = OpenConnection())
            {
                return await connection.QueryAsync<ArtefactNamesOfEditionQuery.Result>(ArtefactNamesOfEditionQuery.GetQuery(userId),
                    new
                    {
                        EditionId = editionId,
                        UserId = userId ?? 0
                    });
            }
        }

        // TODO: the following six functions really should be written with a better DRY approach (add a single func they all call).
        public async Task<List<AlteredRecord>> UpdateArtefactShape(UserInfo user, uint editionId, uint artefactId, string shape)
        {
            /* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
             write the value directly into the database, but it does not seem to work right yet.  Thus we currently 
             use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().
             
            var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
            var res = string.Join("", binaryMask);
            var mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
            var artefactShapeId = GetArtefactPk(user, editionId, artefactId, "artefact_shape");
            var sqeImageId = GetArtefactShapeSqeImageId(user, editionId, artefactId);
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@region_in_sqe_image", shape);
            artefactChangeParams.Add("@artefact_id", artefactId);
            artefactChangeParams.Add("@sqe_image_id", await sqeImageId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Update,
                artefactChangeParams,
                "artefact_shape",
                await artefactShapeId
            );
                    
            // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
            // care of main_action and single_action.
            var response = await _databaseWriter.WriteToDatabaseAsync(editionId, user, new List<MutationRequest>() { artefactChangeRequest });
                    
            if (response.Count == 0 || !response[0].NewId.HasValue)
            {
                throw new ImproperRequestException("change artefact shape",
                    "no change or failed to write to DB");
            }

            return response;
        }
        
        public async Task<List<AlteredRecord>> UpdateArtefactName(UserInfo user, uint editionId, uint artefactId, string name)
        {
            /* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
             write the value directly into the database, but it does not seem to work right yet.  Thus we currently 
             use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().
             
            var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
            var res = string.Join("", binaryMask);
            var mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
           
            
            var artefactNameId = GetArtefactPk(user, editionId, artefactId, "artefact_data");
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@name", name);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Update,
                artefactChangeParams,
                "artefact_data",
                await artefactNameId
            );
                    
            // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
            // care of main_action and single_action.
            var response = await _databaseWriter.WriteToDatabaseAsync(editionId, user, new List<MutationRequest>() { artefactChangeRequest });
                    
            if (response.Count == 0 || !response[0].NewId.HasValue)
            {
                throw new ImproperRequestException("change artefact name",
                    "no change or failed to write to DB");
            }

            return response;
        }
        
        public async Task<List<AlteredRecord>> UpdateArtefactPosition(UserInfo user, uint editionId, uint artefactId, string position)
        {
            /* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
             write the value directly into the database, but it does not seem to work right yet.  Thus we currently 
             use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().
             
            var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
            var res = string.Join("", binaryMask);
            var mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
           
            var artefactPositionId = GetArtefactPk(user, editionId, artefactId, "artefact_position");
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@transform_matrix", position);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Update,
                artefactChangeParams,
                "artefact_position",
                await artefactPositionId
            );
                    
            // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
            // care of main_action and single_action.
            var response = await _databaseWriter.WriteToDatabaseAsync(editionId, user, new List<MutationRequest>() { artefactChangeRequest });
                    
            if (response.Count == 0 || !response[0].NewId.HasValue)
            {
                throw new ImproperRequestException("change artefact position",
                    "no change or failed to write to DB");
            }

            return response;
        }
        
        public async Task<List<AlteredRecord>> InsertArtefactShape(UserInfo user, uint editionId, uint artefactId, uint masterImageId, string shape)
        {
            /* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
             write the value directly into the database, but it does not seem to work right yet.  Thus we currently 
             use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().
             
            var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
            var res = string.Join("", binaryMask);
            var mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
           
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@region_in_sqe_image", shape);
            artefactChangeParams.Add("@sqe_image_id", masterImageId);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Create,
                artefactChangeParams,
                "artefact_shape"
            );
                    
            // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
            // care of main_action and single_action.
            var response = await _databaseWriter.WriteToDatabaseAsync(editionId, user, new List<MutationRequest>() { artefactChangeRequest });
                    
            if (response.Count == 0 || !response[0].NewId.HasValue)
            {
                throw new ImproperRequestException("insert artefact shape",
                    "no change or failed to write to DB");
            }

            return response;
        }
        
        public async Task<List<AlteredRecord>> InsertArtefactName(UserInfo user, uint editionId, uint artefactId, string name)
        {
            /* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
             write the value directly into the database, but it does not seem to work right yet.  Thus we currently 
             use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().
             
            var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
            var res = string.Join("", binaryMask);
            var mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
           
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@name", name);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Create,
                artefactChangeParams,
                "artefact_data"
            );
                    
            // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
            // care of main_action and single_action.
            var response = await _databaseWriter.WriteToDatabaseAsync(editionId, user, new List<MutationRequest>() { artefactChangeRequest });
                    
            if (response.Count == 0 || !response[0].NewId.HasValue)
            {
                throw new ImproperRequestException("insert artefact name",
                    "no change or failed to write to DB");
            }

            return response;
        }
        
        public async Task<List<AlteredRecord>> InsertArtefactPosition(UserInfo user, uint editionId, uint artefactId, string position)
        {
            /* NOTE: I thought we could transform the WKT to a binary and prepend the SIMD byte 00000000, then
             write the value directly into the database, but it does not seem to work right yet.  Thus we currently 
             use a workaround in the WriteToDatabaseAsync functionality to wrap the WKT in a ST_GeomFromText().
             
            var binaryMask = Geometry.Deserialize<WktSerializer>(shape).SerializeByteArray<WkbSerializer>();
            var res = string.Join("", binaryMask);
            var mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
           
            var artefactChangeParams = new DynamicParameters();
            artefactChangeParams.Add("@name", position);
            artefactChangeParams.Add("@artefact_id", artefactId);
            var artefactChangeRequest = new MutationRequest(
                MutateType.Create,
                artefactChangeParams,
                "artefact_position"
            );
                    
            // Now TrackMutation will insert the data, make all relevant changes to the owner tables and take
            // care of main_action and single_action.
            var response = await _databaseWriter.WriteToDatabaseAsync(editionId, user, new List<MutationRequest>() { artefactChangeRequest });
                    
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
            var mask = Geometry.Deserialize<WkbSerializer>(binaryMask).SerializeString<WktSerializer>();*/
           
            using (var transactionScope = new TransactionScope())
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
                        var newShape = InsertArtefactShape(user, editionId, artefactId, masterImageId, shape);
                        var newName = InsertArtefactName(user, editionId, artefactId, artefactName);
                        // TODO: check virtual scroll for unused spot to place the artefact instead of putting it at the beginning.
                        var newPosition = InsertArtefactPosition(
                            user, 
                            editionId, 
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

        private async Task<uint> GetArtefactPk(UserInfo user, uint editionId, uint artefactId, string table)
        {
            using (var connection = OpenConnection())
            {
                try
                {
                    return await connection.QuerySingleAsync<uint>(FindArtefactComponentId.GetQuery(table),
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
    }
}
