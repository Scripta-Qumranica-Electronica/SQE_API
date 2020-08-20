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
    public class HttpRequestObject<TInput>
    {
        public HttpMethod RequestVerb { get; set; }
        public string RequestString { get; set; }
        public TInput Payload { get; set; }
    }

    public interface IRequestObject<TInput, in TOutput, TListener>
    {
        /// <summary>
        ///     Returns an HttpRequestObject with the information needed to make a request to the HTTP server for this API endpoint
        /// </summary>
        /// <returns>May return null if HTTP requests are not possible with this endpoint</returns>
        HttpRequestObject<TInput> GetHttpRequestObject();

        /// <summary>
        ///     Returns a function that can make a SignalR request to this API endpoint
        ///     The function that is returned requires a HubConnection as its only argument
        /// </summary>
        /// <typeparam name="T">The compiler checks to make sure T == TOutput</typeparam>
        /// <returns></returns>
        Func<HubConnection, Task<T>> SignalrRequest<T>()
            where T : TOutput;

        /// <summary>
        ///     Returns the string name of the endpoint's SignalR broadcast method
        /// </summary>
        /// <returns></returns>
        string GetListenerMethod();

        /// <summary>
        ///     Returns the HTTP request verb of the endpoint
        /// </summary>
        /// <returns></returns>
        HttpMethod GetRequestVerb();

        /// <summary>
        ///     Returns the edition id of the particular request, or null if
        ///     the endpoint is not associated with an edition id
        /// </summary>
        /// <returns></returns>
        uint? GetEditionId();
    }

    /// <summary>
    ///     An class used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
    /// </summary>
    /// <typeparam name="TInput">The type of the request payload</typeparam>
    /// <typeparam name="TOutput">The API endpoint return type</typeparam>
    /// <typeparam name="TListener">The API endpoint signalr broadcast type</typeparam>
    public abstract class RequestObject<TInput, TOutput, TListener> : IRequestObject<TInput, TOutput, TListener>
    {
        private readonly TInput _payload;
        private readonly HttpMethod _requestVerb;
        protected readonly string RequestPath;
        protected string ListenerMethod = null;

        /// <summary>
        ///     Provides a RequestObject used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
        /// </summary>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        protected RequestObject(TInput payload = default)
        {
            _payload = payload;
            var pathElements = GetType().ToString().Split(".").Last().Split('+', '_');
            RequestPath =
                "/" + string.Join("/", pathElements.Skip(1).Select(x => x.ToKebabCase()).Where(x => x != "null"));
            var verb = pathElements.First();
            _requestVerb = verb.ToLowerInvariant() switch
            {
                "get" => HttpMethod.Get,
                "post" => HttpMethod.Post,
                "put" => HttpMethod.Put,
                "delete" => HttpMethod.Delete,
                _ => throw new Exception("The HTTP request verb is incorrect")
            };
        }

        public virtual HttpRequestObject<TInput> GetHttpRequestObject()
        {
            return new HttpRequestObject<TInput>
            {
                RequestVerb = _requestVerb,
                RequestString = HttpPath(),
                Payload = _payload
            };
        }

        public virtual Func<HubConnection, Task<T>> SignalrRequest<T>()
            where T : TOutput
        {
            return signalR => _payload == null
                ? signalR.InvokeAsync<T>(SignalrRequestString())
                : signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
        }

        public string GetListenerMethod()
        {
            return ListenerMethod;
        }

        public HttpMethod GetRequestVerb()
        {
            return _requestVerb;
        }

        public virtual uint? GetEditionId()
        {
            return null;
        }

        /// <summary>
        ///     Returns the HTTP request string with all route and query
        ///     parameters interpolated.
        /// </summary>
        /// <returns></returns>
        protected virtual string HttpPath()
        {
            return RequestPath;
        }

        /// <summary>
        ///     Formats the API endpoint method name for the SignalR server
        /// </summary>
        /// <returns></returns>
        protected string SignalrRequestString()
        {
            return _requestVerb.ToString().First().ToString().ToUpper()
                   + _requestVerb.ToString().Substring(1).ToLower()
                   + RequestPath.Replace("/", "_").ToPascalCase();
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