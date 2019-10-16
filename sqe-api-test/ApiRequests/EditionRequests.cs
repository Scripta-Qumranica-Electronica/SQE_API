/*
 * Example Usage:
var signalR = await StartConnectionAsync();
var editions = new ApiRequests.ApiRequests.Get.V1.Editions.Blank();
var (res, mssg) = await Request.Send<string, EditionListDTO>(editions, _client, signalR);
 */
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class ApiRequests
    {
        public static int editionCount = 0;

        public static partial class Get
        {
            public static partial class V1
            {
                public static class Editions
                {
                    public class Blank : RequestObject<string, EditionListDTO>
                    {
                        public Blank() : base(null)
                        {
                            requestVerb = HttpMethod.Get;
                            requestPath = "/v1/Editions";
                        }
                    }
                    public class EditionId : RequestObject<string, EditionGroupDTO>
                    {
                        private uint editionId;

                        public EditionId(uint _editionId) : base(null)
                        {
                            editionId = _editionId;
                            requestVerb = HttpMethod.Get;
                            requestPath = "/v1/Editions/EditionId";
                            httpPath = requestPath.Replace("EditionId", editionId.ToString());
                        }

                        public Func<HubConnection, Task<EditionGroupDTO>> signalrRequest()
                        {
                            return (signalR) => signalR.InvokeAsync<EditionGroupDTO>(this.signalrRequestString(), editionId);
                        }
                    }
                }
            }
        }
    }
}