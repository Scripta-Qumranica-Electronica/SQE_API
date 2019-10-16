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
        public string listenerMethod = null;
        protected Tinput payload;

        protected RequestObject(Tinput _payload)
        {
            payload = _payload;
        }

        public HttpRequestObject GetHttpResponseObject()
        {
            return new HttpRequestObject()
            {
                requestVerb = this.requestVerb,
                requestString = this.httpPath ?? this.requestPath,
                payload = this.payload
            };
        }

        public class HttpRequestObject
        {
            public HttpMethod requestVerb { get; set; }
            public string requestString { get; set; }
            public Tinput payload { get; set; }
        }

        public virtual Func<HubConnection, Task<T>> signalrRequest<T>()
            where T : Toutput
        {
            return (signalR) => signalR.InvokeAsync<T>(this.signalrRequestString());
        }

        protected string signalrRequestString()
        {
            return this.requestVerb.ToString() + this.requestPath.Replace("/", "_").ToPascalCase();
        }
    }

    public class EditionRequestObject<Tinput, Toutput> : RequestObject<Tinput, Toutput>
    {
        public readonly uint editionId;

        public EditionRequestObject(uint _editionId, Tinput _payload) : base(_payload)
        {
            editionId = _editionId;
        }

    }

    // The RequestObject class may take either an empty input object or empty output object for its generic classes.
    public class EmptyInput { }
    public class EmptyOutput { }
}