using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CaseExtensions;
using Microsoft.AspNetCore.SignalR.Client;

namespace SQE.ApiTest.ApiRequests
{
    /// <summary>
    ///     An object containing the necessary data to make an HTTP request to this API endpoint
    /// </summary>
    public class HttpRequestObject<Tinput>
    {
        public HttpMethod requestVerb { get; set; }
        public string requestString { get; set; }
        public Tinput payload { get; set; }
    }

    public interface IRequestObject<Tinput, Toutput, TListener>
    {
        /// <summary>
        ///     Returns an HttpRequestObject with the information needed to make a request to the HTTP server for this API endpoint
        /// </summary>
        /// <returns>May return null if HTTP requests are not possible with this endpoint</returns>
        HttpRequestObject<Tinput> GetHttpRequestObject();

        /// <summary>
        ///     Returns a function that can make a SignalR request to this API endpoint
        ///     The function that is returned requires a HubConnection as its only argument
        /// </summary>
        /// <typeparam name="T">The compiler checks to make sure T == Toutput</typeparam>
        /// <returns></returns>
        Func<HubConnection, Task<T>> SignalrRequest<T>()
            where T : Toutput;

        string GetListenerMethod();

        HttpMethod GetRequestVerb();

        uint? GetEditionId();
    }

    /// <summary>
    ///     An class used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public abstract class RequestObject<Tinput, Toutput, TListener> : IRequestObject<Tinput, Toutput, TListener>
    {
        protected string listenerMethod = null;
        protected readonly Tinput payload;
        protected readonly string requestPath;
        private readonly HttpMethod requestVerb;

        /// <summary>
        ///     Provides a RequestObject used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
        /// </summary>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        protected RequestObject(Tinput payload = default(Tinput))
        {
            this.payload = payload;
            var pathElements = GetType().ToString().Split(".").Last().Split('+', '_');
            requestPath =
                "/" + string.Join("/", pathElements.Skip(1).Select(x => x.ToKebabCase()).Where(x => x != "null"));
            var verb = pathElements.First();
            requestVerb = verb.ToLowerInvariant() switch
            {
                "get" => HttpMethod.Get,
                "post" => HttpMethod.Post,
                "put" => HttpMethod.Put,
                "delete" => HttpMethod.Delete,
                _ => throw new Exception("The HTTP request verb is incorrect")
            };
        }

        /// <summary>
        ///     Returns an HttpRequestObject with the information needed to make a request to the HTTP server for this API endpoint
        /// </summary>
        /// <returns>May return null if HTTP requests are not possible with this endpoint</returns>
        public virtual HttpRequestObject<Tinput> GetHttpRequestObject()
        {
            return new HttpRequestObject<Tinput>
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
            return signalR => payload == null
                ? signalR.InvokeAsync<T>(SignalrRequestString())
                : signalR.InvokeAsync<T>(SignalrRequestString(), payload);
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

        public string GetListenerMethod()
        {
            return listenerMethod;
        }

        public HttpMethod GetRequestVerb()
        {
            return requestVerb;
        }

        public virtual uint? GetEditionId()
        {
            return null;
        }
    }

    /// <summary>
    ///     An empty request payload object
    /// </summary>
    public class EmptyInput
    {
    }

    /// <summary>
    ///     An empty request response object
    /// </summary>
    public class EmptyOutput
    {
    }
}