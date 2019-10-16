using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using CaseExtensions;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class ApiRequests
    {

    }

    public abstract class RequestObject<Tinput, Toutput>
    {
        public Type returnType = typeof(Toutput);
        public Type inputType = typeof(Tinput);
        protected string requestPath;
        public HttpMethod requestVerb;
        protected string httpPath;
        protected Tinput payload;

        protected RequestObject(Tinput _payload)
        {
            httpPath = httpPath ?? requestPath;
            payload = _payload;
        }

        public HttpRequestObject GetHttpResponseObject()
        {
            return new HttpRequestObject()
            {
                requestVerb = this.requestVerb,
                requestString = this.requestPath,
                payload = this.payload
            };
        }

        public class HttpRequestObject
        {
            public HttpMethod requestVerb { get; set; }
            public string requestString { get; set; }
            public Tinput payload { get; set; }
        }

        public Func<HubConnection, Task<Toutput>> signalrRequest()
        {
            return (signalR) => signalR.InvokeAsync<Toutput>(this.signalrRequestString());
        }

        protected string signalrRequestString()
        {
            return this.requestVerb.ToString() + this.requestPath.Replace("/", "_").ToPascalCase();
        }
    }
}