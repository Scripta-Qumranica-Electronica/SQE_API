using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

/*using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
*/


namespace ingo_test
{

    struct test
    {
        public int a;
        public int b;
    }
    class Program
    {
        static async Task Main(string[] args)
        {

            
         

            var configuration = new ConfigurationBuilder().
                SetBasePath(Directory.GetCurrentDirectory())
                // TODO: when we know the deployment details we will probably need to change the logging settings
                .AddJsonFile("appsettings.json", true)
                .Build();
            var dbWriter = new DatabaseWriter(configuration);
            var userRep = new UserRepository(configuration);
            EditionUserInfo editionInfo = new EditionUserInfo(1,1,userRep);
            editionInfo.ReadPermissions();




            try
            {



                var tr = new TextRepository(configuration, dbWriter);
                var result = tr.GetFragmentDataAsync(editionInfo).Result;
                var ids = result.Select(x => x.TextFragmentName);
                Console.WriteLine(string.Join(", ", ids));

                using (var conn = tr.Connection)
                {
                    var fact = await PositionDataRequestFactory.CreateInstanceAsync(
                        conn, StreamType.SignInterpretationStream, 
                        new List<uint>(){10},
                     1, true);
                fact.AddAction(PositionAction.DeleteAndClose);
                var requests = await fact.CreateRequestsAsync();
                
                Console.WriteLine(requests);

            }

            /*
              var fact = new PositionDataRequestFactory(conn, StreamType.TextFragmentStream, 5
                  ,1);
              fact.AddAction(PositionAction.MoveTo);
             fact.AddAnchorBefore(4);
              fact.AddAnchorBehind(6);
              var requests = fact.CreateRequests();

              var task = dbWriter.WriteToDatabaseAsync(editionInfo,requests);
               Console.WriteLine(task.Exception); 

               result = tr.GetFragmentDataAsync(editionInfo).Result;
               ids = result.Select(x => x.TextFragmentName);
               Console.WriteLine(string.Join(", ", ids));
               
               */
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            } 
            
        }
        }
    }

        
