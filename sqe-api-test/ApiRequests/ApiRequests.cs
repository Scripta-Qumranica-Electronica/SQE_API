using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CaseExtensions;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.ApiTest.Helpers;
using Xunit;

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
        public HttpResponseMessage HttpResponseMessage { get; protected set; }
        public TOutput HttpResponseObject { get; protected set; }
        public TOutput SignalrResponseObject { get; protected set; }
        protected readonly Dictionary<ListenerMethods, (Func<bool> IsNull, Action<HubConnection> StartListener)> _listenerDict;

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
            _listenerDict = new Dictionary<ListenerMethods, (Func<bool>, Action<HubConnection>)>();
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

        /// <summary>
        ///     Issues the request to the API and stores the response.
        ///     At least `http` or `realtime` must be provided.  If using a listener, you
        ///     will receive a HubConnection as a response from this method. You must
        ///     wait some appropriate amount of time for the desired listener message
        ///     to be received and close the connection yourself on success/fail.
        /// </summary>
        /// <param name="http">
        ///     An HttpClient to run the request on;
        ///     may be null if no request should be made to the HTTP server.
        /// </param>
        /// <param name="realtime">
        ///     A function to acquire a SignalR hub connection;
        ///     may be null if no request should be made to the SignalR server.
        /// </param>
        /// <param name="auth">
        ///     Whether to use authentication (default false).
        ///     If no user1 is provided the default user "test" will be used.
        /// </param>
        /// <param name="requestUser">
        ///     User object for authentication.
        ///     If no User is provided, the default "test" user is used.
        /// </param>
        /// <param name="listenerUser">
        ///     User object for authentication of the listener.
        ///     If no User is provided, listenerUser = user1.
        /// </param>
        /// <param name="shouldSucceed">Whether the request is expected to succeed.</param>
        /// <param name="deterministic">
        ///     Whether the request is expected to return the same response from multiple requests.
        ///     This method will throw an error if the request is deterministic but the http and realtime responses differ.
        /// </param>
        /// <returns>HubConnection</returns>
        public async Task Send(
                HttpClient http = null,
                Func<string, Task<HubConnection>> realtime = null,
                bool auth = false,
                Request.UserAuthDetails requestUser = null,
                Request.UserAuthDetails listenerUser = null,
                bool shouldSucceed = true,
                bool deterministic = true,
                bool requestRealtime = true,
                bool listenToEdition = true,
                IEnumerable<ListenerMethods> listeningFor = null)
        {
            // Throw an error if no transport protocol has been provided
            if (http == null
                && realtime == null)
                throw new Exception(
                    "You must choose at least one transport protocol for the request (http or realtime)."
                );

            // Throw an error is a listener is requested but auth has been rejected
            if (listenerUser != null && !auth)
                throw new Exception("Setting up a listener requires auth");

            // Set up the initial variables and their values
            HttpResponseMessage httpResponseMessage = null;
            HubConnection signalrListener = null;
            string jwt1 = null;
            string jwt2 = null;

            // Generate any necessary JWT's
            if (auth)
                jwt1 = http != null
                    ? await Request.GetJwtViaHttpAsync(http, requestUser ?? null)
                    : await Request.GetJwtViaRealtimeAsync(realtime, requestUser ?? null);

            if (auth && listenerUser != null)
                jwt2 = await Request.GetJwtViaRealtimeAsync(realtime, listenerUser);

            // Set up a SignalR listener if desired (this hub connection must be different than the one used to make
            // the API request.
            if (listenerUser != null
                && GetRequestVerb() != HttpMethod.Get
                && realtime != null
                && !string.IsNullOrEmpty(GetListenerMethod()))
            {
                signalrListener = await realtime(jwt2);
                // Subscribe to messages on the edition
                listenToEdition &= GetEditionId().HasValue;
                if (listenToEdition)
                    await signalrListener.InvokeAsync("SubscribeToEdition", GetEditionId().Value);
                // Register listeners for messages returned by this API request
                foreach (var listener in listeningFor)
                {
                    if (_listenerDict.TryGetValue(listener, out var val))
                        val.StartListener(signalrListener);
                }

                // Reload the listener if connection is lost
                signalrListener.Closed += async error =>
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await signalrListener.StartAsync();
                    // Subscribe to messages on the edition
                    if (listenToEdition)
                        await signalrListener.InvokeAsync("SubscribeToEdition", GetEditionId().Value);
                    // Register listeners for messages returned by this API request
                    foreach (var listener in listeningFor)
                    {
                        if (_listenerDict.TryGetValue(listener, out var val))
                            val.StartListener(signalrListener);
                    }
                };
            }

            // Run the HTTP request if desired and available
            var httpObj = GetHttpRequestObject();
            if (http != null
                && httpObj != null)
            {
                (HttpResponseMessage, HttpResponseObject) = await Request.SendHttpRequestAsync<TInput, TOutput>(
                    http,
                    httpObj.RequestVerb,
                    httpObj.RequestString,
                    httpObj.Payload,
                    jwt1
                );

                if (shouldSucceed)
                    HttpResponseMessage.EnsureSuccessStatusCode();
            }

            // Run the SignalR request if desired
            if (realtime != null && requestRealtime)
            {
                HubConnection signalR = null;
                try
                {
                    signalR = await realtime(jwt1);
                    SignalrResponseObject = await SignalrRequest<TOutput>()(signalR);
                }
                catch (Exception)
                {
                    if (shouldSucceed)
                        throw;
                }

                // If the request should succeed and an HTTP request was also made, check that they are the same
                if (shouldSucceed
                    && deterministic
                    && http != null
                    && typeof(TOutput) == typeof(TListener))
                    SignalrResponseObject.ShouldDeepEqual(HttpResponseObject);

                // Cleanup
                signalR?.DisposeAsync();
            }

            // If no listener is running, return the response from the request
            if (listenerUser == null
                || GetRequestVerb() == HttpMethod.Get)
                return;

            // Otherwise, wait up to 20 seconds for the listener to receive the message before giving up
            var waitTime = 0;

            while (
                listeningFor.Any(x =>
                    !_listenerDict.TryGetValue(x, out var listeners) || listeners.IsNull())
                && waitTime < 20)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                waitTime += 1;
            }

            signalrListener?.DisposeAsync(); // Cleanup
            if (shouldSucceed)
                Assert.Empty(listeningFor.Where(x =>
                    !_listenerDict.TryGetValue(x, out var listeners) || listeners.IsNull()));
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