/*
 * This file is automatically generated by the GenerateTestRequestObjects
 * project in the Utilities folder. Do not edit this file directly as
 * its contents may be overwritten at any point.
 *
 * Should a class here need to be altered for any reason, you should look
 * first to the auto generation program for possible updating to include
 * the needed special case. Otherwise, it is possible to create your own
 * manually written ApiRequest object, though this is generally discouraged.
 */

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
	public static partial class Post
	{
		public class V1_Utils_RepairWktPolygon : RequestObject<WktPolygonDTO, WktPolygonDTO>
		{
			private readonly WktPolygonDTO _payload;

			/// <summary>
			///  Checks a WKT polygon to ensure validity. If the polygon is invalid,
			///  it attempts to construct a valid polygon that matches the original
			///  as closely as possible.
			/// </summary>
			/// <param name="payload">JSON object with the WKT polygon to validate</param>
			public V1_Utils_RepairWktPolygon(WktPolygonDTO payload) : base(payload)
				=> _payload = payload;

			protected override string HttpPath() => RequestPath;

			public override Func<HubConnection, Task<T>> SignalrRequest<T>()
			{
				return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
			}
		}
	}
}
