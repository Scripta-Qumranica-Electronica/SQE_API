using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using CaseExtensions;

namespace SQE.ApiTest.ApiRequests
{
    /// <summary>
    /// An class used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public abstract class RequestObject<Tinput, Toutput>
    {
        public Type returnType = typeof(Toutput);
        public Type inputType = typeof(Tinput);
        protected string requestPath;
        public HttpMethod requestVerb;
        protected string httpPath;
        public string listenerMethod = null;
        protected Tinput payload;

        /// <summary>
        /// Provides a RequestObject used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
        /// </summary>
        /// <param name="_payload">Payload to be sent to the API endpoint</param>
        protected RequestObject(Tinput _payload)
        {
            payload = _payload;
        }

        /// <summary>
        /// Returns an HttpRequestObject with the information needed to make a request to the HTTP server for this API endpoint
        /// </summary>
        /// <returns>May return null if HTTP requests are not possible with this endpoint</returns>
        public virtual HttpRequestObject GetHttpResponseObject()
        {
            return new HttpRequestObject()
            {
                requestVerb = this.requestVerb,
                requestString = this.httpPath ?? this.requestPath,
                payload = this.payload
            };
        }

        /// <summary>
        /// An object containing the necessary data to make an HTTP request to this API endpoint
        /// </summary>
        public class HttpRequestObject
        {
            public HttpMethod requestVerb { get; set; }
            public string requestString { get; set; }
            public Tinput payload { get; set; }
        }

        /// <summary>
        /// Returns a function that can make a SignalR request to this API endpoint
        /// The function that is returned requires a HubConnection as its only argument
        /// </summary>
        /// <typeparam name="T">The compiler checks to make sure T == Toutput</typeparam>
        /// <returns></returns>
        public virtual Func<HubConnection, Task<T>> signalrRequest<T>()
            where T : Toutput
        {
            return (signalR) => signalR.InvokeAsync<T>(this.signalrRequestString());
        }

        /// <summary>
        /// Formats the API endpoint method name for the SignalR server
        /// </summary>
        /// <returns></returns>
        protected string signalrRequestString()
        {
            return this.requestVerb.ToString() + this.requestPath.Replace("/", "_").ToPascalCase();
        }
    }

    /// <summary>
    /// Subclass of RequestObject for all requests made on an edition
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class EditionRequestObject<Tinput, Toutput> : RequestObject<Tinput, Toutput>
    {
        public readonly uint _editionId;

        /// <summary>
        /// Provides an EditionRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="_payload">Payload to be sent to the API endpoint</param>
        public EditionRequestObject(uint editionId, Tinput _payload) : base(_payload)
        {
            this._editionId = editionId;
        }

    }

    // The RequestObject class may take either an empty input object or empty output object for its generic classes.
    /// <summary>
    /// An empty request payload object
    /// </summary>
    public class EmptyInput { }

    /// <summary>
    /// An empty request response object
    /// </summary>
    public class EmptyOutput { }
}