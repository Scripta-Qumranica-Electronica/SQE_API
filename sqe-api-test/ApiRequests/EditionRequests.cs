/*
 * Example Usage:
var editions = new ApiRequests.ApiRequests.Get.V1.Editions.Blank();
var (res, mssg) = await Request.Send(editions, http: _client, realtime: StartConnectionAsync);

var editions1 = new ApiRequests.ApiRequests.Get.V1.Editions.EditionId(894);
var (res1, mssg1) = await Request.Send(editions1, http: _client, realtime: StartConnectionAsync);
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
                    public class Blank : RequestObject<EmptyInput, EditionListDTO>
                    {
                        public Blank() : base(null)
                        {
                            requestVerb = HttpMethod.Get;
                            requestPath = "/v1/Editions";
                        }
                    }
                    public class EditionId : EditionRequestObject<EmptyInput, EditionGroupDTO>
                    {
                        public EditionId(uint _editionId) : base(_editionId, null)
                        {
                            requestVerb = HttpMethod.Get;
                            requestPath = "/v1/Editions/EditionId";
                            httpPath = requestPath.Replace("EditionId", editionId.ToString());
                        }

                        public override Func<HubConnection, Task<T>> signalrRequest<T>()
                        {
                            return (signalR) => signalR.InvokeAsync<T>(this.signalrRequestString(), editionId);
                        }
                    }
                }
            }
        }
    }
}