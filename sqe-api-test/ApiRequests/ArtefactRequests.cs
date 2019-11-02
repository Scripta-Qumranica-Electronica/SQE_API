/*
 * Example:
var arts = new ApiRequests.ApiRequests.Get.V1.Artefacts.EditionId(894, null);
var (res1, mssg1) = await Request.Send(arts, http: _client, realtime: StartConnectionAsync);
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_Editions_EditionId_Artefacts : EditionRequestObject<EmptyInput, ArtefactListDTO>
        {
            private readonly List<string> _optional;

            /// <summary>
            ///     Request a list of artefacts by their editionId
            /// </summary>
            /// <param name="editionId">Id of the edition to search for artefacts</param>
            /// <param name="optional">List of optional parameters: "masks", "images"</param>
            public V1_Editions_EditionId_Artefacts(uint editionId, List<string> optional) : base(editionId)
            {
                _optional = optional;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), editionId, _optional);
            }
        }
    }
}