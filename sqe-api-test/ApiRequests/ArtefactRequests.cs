/*
 * Example:
var arts = new ApiRequests.ApiRequests.Get.V1.Artefacts.EditionId(894, null);
var (res1, mssg1) = await Request.Send(arts, http: _client, realtime: StartConnectionAsync);
 */
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class ApiRequests
    {
        public static partial class Get
        {
            public static partial class V1
            {
                public static class Artefacts
                {
                    public class EditionId : EditionRequestObject<EmptyInput, ArtefactListDTO>
                    {
                        private List<string> _optional;

                        /// <summary>
                        /// Request a list of artefacts by their editionId
                        /// </summary>
                        /// <param name="_editionId">Id of the edition to search for artefacts</param>
                        /// <param name="optional">List of optional parameters: "masks", "images"</param>
                        public EditionId(uint editionId, List<string> optional) : base(editionId, null)
                        {
                            _optional = optional;
                            requestVerb = HttpMethod.Get;
                            requestPath = "/v1/Editions/EditionId/Artefacts";
                            httpPath = requestPath.Replace("EditionId", base._editionId.ToString());
                        }

                        public override Func<HubConnection, Task<T>> signalrRequest<T>()
                        {
                            return (signalR) => signalR.InvokeAsync<T>(this.signalrRequestString(), _editionId, _optional);
                        }
                    }
                }
            }
        }
    }
}