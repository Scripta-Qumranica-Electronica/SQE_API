using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Dapper;
using System.Linq;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess
{

    public interface IArtefactRepository
    {
        Task<Artefact> GetArtefact(uint? userId, uint artefactId);
        Task<IEnumerable<ArtefactNamesOfEditionQuery.Result>> GetEditionArtefactNameList(uint? userId, uint editionId);
    }

    public class ArtefactRepository : DbConnectionBase, IArtefactRepository
    {
        public ArtefactRepository(IConfiguration config) : base(config) { }

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

        public async Task<IEnumerable<ArtefactNamesOfEditionQuery.Result>> GetEditionArtefactNameList(uint? userId, uint editionId)
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
    }
}
