using System;
using System.Net.Http;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.ApiTest.ApiRequests;
using Xunit;

namespace SQE.ApiTest.Helpers
{
    public static class Request
    {
        /// <summary>
        /// Send a request to the API and receive the response
        /// </summary>
        /// <param name="request">A subclass of RequestObject with the details necessary to make the request</param>
        /// <param name="http">An HttpClient to run the request on;
        /// may be null if no request should be made to the HTTP server</param>
        /// <param name="realtime">A function to acquire a signalr hub connection;
        /// may be null if no request should be made to the SignalR server</param>
        /// <param name="listener">Whether to run a test with a listener on the request
        /// (only works for Post, Put, and Delete requests; default is false)</param>
        /// <param name="auth">Whether to use authentication (default false)</param>
        /// <param name="jwt">Token used for authentication</param>
        /// <param name="shouldSucceed">Whether the request is expected to succeed</param>
        /// <typeparam name="Tinput">Type of the request payload (this is automatically inferred from the request argument)</typeparam>
        /// <typeparam name="Toutput">Type of the API response (this is automatically inferred from the request argument)</typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<(HttpResponseMessage, Toutput)> Send<Tinput, Toutput>(
            RequestObject<Tinput, Toutput> request,
            HttpClient http = null,
            Func<string, Task<HubConnection>> realtime = null,
            bool listener = false,
            bool auth = false,
            string jwt = null,
            bool shouldSucceed = true)
        {
            // Set up the initial variables and their values
            HttpResponseMessage resp = null;
            Toutput msg = default(Toutput);
            Toutput rt = default(Toutput);
            Toutput lt = default(Toutput);
            HubConnection signalrListener;

            // Set up a SignalR listener if desired (this hub connection must be different than the one used to make
            // the API request.
            if (listener
                && request.requestVerb != HttpMethod.Get
                && realtime != null
                && !string.IsNullOrEmpty(request.listenerMethod)
                && request.GetType().IsSubclassOf(typeof(EditionRequestObject<Tinput, Toutput>)))
            {
                var editionRequest = request as EditionRequestObject<Tinput, Toutput>;
                signalrListener = await realtime(auth ? jwt : null);
                // Subscribe to messages on the edition
                await signalrListener.InvokeAsync("SubscribeToEdition", editionRequest?._editionId);
                // Register a istener for messages returned by this API request
                signalrListener.On<Toutput>(request.listenerMethod, (receivedData) => lt = receivedData);
            }

            // Run the HTTP request if desired and available
            var httpObj = request.GetHttpResponseObject();
            if (http != null && httpObj != null)
            {
                (resp, msg) = await HttpRequest.SendAsync<Tinput, Toutput>(
                    http,
                    httpObj.requestVerb,
                    httpObj.requestString,
                    httpObj.payload,
                    auth ? jwt : null
                );

                if (shouldSucceed)
                    resp.EnsureSuccessStatusCode();
            }

            // Run the SignalR request if desired
            if (realtime != null)
            {
                try
                {
                    var signalr = await realtime(auth ? jwt : null);
                    rt = await request.signalrRequest<Toutput>()(signalr);
                }
                catch (Exception e)
                {
                    if (shouldSucceed)
                        throw e;
                }

                // If the request should succeed and an HTTP request was also made, check that they are the same
                if (shouldSucceed && http != null)
                    rt.ShouldDeepEqual(msg);
            }

            // If no listener is running, return the response from the request
            if (!listener || request.requestVerb == HttpMethod.Get)
            {
                return (resp, http != null ? msg : rt);
            }

            // Otherwise, wait up to 20 seconds for the listener to receive the message
            var waitTime = 0;
            while (lt == null && waitTime < 20)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                waitTime += 1;
            }
            Assert.NotNull(lt); // Do not try to listen with requests that return void!
            return (resp, lt);
        }
    }
}