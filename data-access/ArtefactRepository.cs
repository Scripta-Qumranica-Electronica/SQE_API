using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using SQE.Backend.DataAccess.Models;



namespace SQE.Backend.DataAccess
{

    public interface IArtefactRepository
    {
        Task<Artefact> GetArtefact(uint? userId, uint artefactId);

    }

    class ArtefactRepository : DBConnectionBase, IArtefactRepository
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

    }
}
