using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CaseExtensions;
using Microsoft.AspNetCore.SignalR.Client;

namespace SQE.ApiTest.ApiRequests
{
    /// <summary>
    ///     An class used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public abstract class RequestObject<Tinput, Toutput>
    {
        public string listenerMethod = null;
        protected Tinput payload;
        protected string requestPath;
        public HttpMethod requestVerb;

        /// <summary>
        ///     Provides a RequestObject used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
        /// </summary>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        protected RequestObject(Tinput payload)
        {
            this.payload = payload;
            var pathElements = this.GetType().ToString().Split(".").Last().Split("+");
            this.requestPath = "/" + string.Join("/", pathElements.Skip(1).Select(x => x.ToKebabCase()).Where(x => x != "null"));
            var verb = pathElements.First();
            switch (verb)
            {
                case "Get":
                    this.requestVerb = HttpMethod.Get;
                    break;
                case "Post":
                    this.requestVerb = HttpMethod.Post;
                    break;
                case "Put":
                    this.requestVerb = HttpMethod.Put;
                    break;
                case "Delete":
                    this.requestVerb = HttpMethod.Get;
                    break;
            }
        }

        /// <summary>
        ///     Returns an HttpRequestObject with the information needed to make a request to the HTTP server for this API endpoint
        /// </summary>
        /// <returns>May return null if HTTP requests are not possible with this endpoint</returns>
        public virtual HttpRequestObject GetHttpRequestObject()
        {
            return new HttpRequestObject
            {
                requestVerb = requestVerb,
                requestString = HttpPath(),
                payload = payload
            };
        }

        protected virtual string HttpPath()
        {
            return requestPath;
        }

        /// <summary>
        ///     Returns a function that can make a SignalR request to this API endpoint
        ///     The function that is returned requires a HubConnection as its only argument
        /// </summary>
        /// <typeparam name="T">The compiler checks to make sure T == Toutput</typeparam>
        /// <returns></returns>
        public virtual Func<HubConnection, Task<T>> SignalrRequest<T>()
            where T : Toutput
        {
            return signalR => signalR.InvokeAsync<T>(SignalrRequestString());
        }

        /// <summary>
        ///     Formats the API endpoint method name for the SignalR server
        /// </summary>
        /// <returns></returns>
        protected string SignalrRequestString()
        {
            return requestVerb.ToString().First().ToString().ToUpper()
                   + requestVerb.ToString().Substring(1).ToLower()
                   + requestPath.Replace("/", "_").ToPascalCase();
        }

        /// <summary>
        ///     An object containing the necessary data to make an HTTP request to this API endpoint
        /// </summary>
        public class HttpRequestObject
        {
            public HttpMethod requestVerb { get; set; }
            public string requestString { get; set; }
            public Tinput payload { get; set; }
        }
    }

    /// <summary>
    ///     Subclass of RequestObject for all requests made on an edition
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class EditionRequestObject<Tinput, Toutput> : RequestObject<Tinput, Toutput>
    {
        public readonly uint editionId;

        /// <summary>
        ///     Provides an EditionRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public EditionRequestObject(uint editionId, Tinput payload) : base(payload)
        {
            this.editionId = editionId;
        }

        protected override string HttpPath()
        {
            return requestPath.Replace("/edition-id", $"/{editionId.ToString()}");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => payload == null
                ? signalR.InvokeAsync<T>(SignalrRequestString(), editionId)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, payload);
        }
    }

    /// <summary>
    ///     Subclass of EditionRequestObject for all requests made on an text fragment
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class TextFragmentRequestObject<Tinput, Toutput> : EditionRequestObject<Tinput, Toutput>
    {
        public readonly uint textFragmentId;

        /// <summary>
        ///     Provides an TextFragmentRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="textFragmentId">The id of the text fragment to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public TextFragmentRequestObject(uint editionId, uint textFragmentId, Tinput payload) : base(editionId, payload)
        {
            this.textFragmentId = textFragmentId;
        }

        protected override string HttpPath()
        {
            return base.HttpPath().Replace("/text-fragment-id", $"/{textFragmentId.ToString()}");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => payload == null
                ? signalR.InvokeAsync<T>(SignalrRequestString(), editionId, textFragmentId)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, textFragmentId, payload);
        }
    }

    /// <summary>
    ///     Subclass of EditionRequestObject for all requests made on a line
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class LineRequestObject<Tinput, Toutput> : EditionRequestObject<Tinput, Toutput>
    {
        public readonly uint lineId;

        /// <summary>
        ///     Provides an TextFragmentRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="lineId">The id of the line to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public LineRequestObject(uint editionId, uint lineId, Tinput payload) : base(editionId, payload)
        {
            this.lineId = lineId;
        }

        protected override string HttpPath()
        {
            return base.HttpPath().Replace("{textFragmentId}", lineId.ToString());
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => payload == null
                ? signalR.InvokeAsync<T>(SignalrRequestString(), editionId, lineId)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, lineId, payload);
        }
    }

    /// <summary>
    ///     An empty request payload object
    /// </summary>
    public abstract class EmptyInput
    {
    }

    /// <summary>
    ///     An empty request response object
    /// </summary>
    public class EmptyOutput
    {
    }
}