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
                        /// <summary>
                        /// Request a listing of all editions available to the user
                        /// </summary>
                        public Blank() : base(null)
                        {
                            requestVerb = HttpMethod.Get;
                            requestPath = "/v1/Editions";
                        }
                    }
                    public class EditionId : EditionRequestObject<EmptyInput, EditionGroupDTO>
                    {
                        /// <summary>
                        /// Request information about a specific edition
                        /// </summary>
                        /// <param name="editionId">The editionId for the desired edition</param>
                        public EditionId(uint editionId) : base(editionId, null)
                        {
                            requestVerb = HttpMethod.Get;
                            requestPath = "/v1/Editions/EditionId";
                            httpPath = requestPath.Replace("EditionId", base._editionId.ToString());
                        }

                        public override Func<HubConnection, Task<T>> signalrRequest<T>()
                        {
                            return (signalR) => signalR.InvokeAsync<T>(this.signalrRequestString(), _editionId);
                        }
                    }
                }
            }
        }
    }
}