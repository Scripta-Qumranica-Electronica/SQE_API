
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


    public static partial class Post
    {


        public class V1_Utils_RepairWktPolygon
        : RequestObject<WktPolygonDTO, WktPolygonDTO, EmptyOutput>
        {
            public readonly WktPolygonDTO Payload;

            /// <summary>
            ///     Checks a WKT polygon to ensure validity. If the polygon is invalid,
            ///     it attempts to construct a valid polygon that matches the original
            ///     as closely as possible.
            /// </summary>
            /// <param name="payload">JSON object with the WKT polygon to validate</param>
            public V1_Utils_RepairWktPolygon(WktPolygonDTO payload)
                : base(payload)
            {
                this.Payload = payload;

            }

            protected override string HttpPath()
            {
                return requestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), Payload);
            }


        }
    }

}
