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
        ///     Send a request to the API and receive the response
        /// </summary>
        /// <param name="request">A subclass of RequestObject with the details necessary to make the request</param>
        /// <param name="http">
        ///     An HttpClient to run the request on;
        ///     may be null if no request should be made to the HTTP server
        /// </param>
        /// <param name="realtime">
        ///     A function to acquire a SignalR hub connection;
        ///     may be null if no request should be made to the SignalR server
        /// </param>
        /// <param name="listener">
        ///     Whether to run a test with a listener on the request
        ///     (only works for Post, Put, and Delete requests; default is false)
        /// </param>
        /// <param name="auth">Whether to use authentication (default false)</param>
        /// <param name="jwt">Token used for authentication</param>
        /// <param name="shouldSucceed">Whether the request is expected to succeed</param>
        /// <typeparam name="Tinput">Type of the request payload (this is automatically inferred from the request argument)</typeparam>
        /// <typeparam name="Toutput">Type of the API response (this is automatically inferred from the request argument)</typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async
            Task<(HttpResponseMessage httpResponseMessage, Toutput httpResponse, Toutput signalrResponse, Toutput
                listenerResponse)> Send<Tinput, Toutput>(
                RequestObject<Tinput, Toutput> request,
                HttpClient http = null,
                Func<string, Task<HubConnection>> realtime = null,
                bool listener = false,
                bool auth = false,
                string jwt = null,
                bool shouldSucceed = true,
                bool deterministic = true)
        {
            // Set up the initial variables and their values
            HttpResponseMessage httpResponseMessage = null;
            var httpResponse = default(Toutput);
            var signalrResponse = default(Toutput);
            var listenerResponse = default(Toutput);
            HubConnection signalrListener = null;

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
                await signalrListener.InvokeAsync("SubscribeToEdition", editionRequest?.editionId);
                // Register a listener for messages returned by this API request
                signalrListener.On<Toutput>(request.listenerMethod, receivedData => listenerResponse = receivedData);

                // Reload the listener if connection is lost
                signalrListener.Closed += async error =>
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await signalrListener.StartAsync();
                    // Subscribe to messages on the edition
                    await signalrListener.InvokeAsync("SubscribeToEdition", editionRequest?.editionId);
                    // Register a istener for messages returned by this API request
                    signalrListener.On<Toutput>(
                        request.listenerMethod,
                        receivedData => listenerResponse = receivedData
                    );
                };
            }

            // Run the HTTP request if desired and available
            var httpObj = request.GetHttpResponseObject();
            if (http != null
                && httpObj != null)
            {
                (httpResponseMessage, httpResponse) = await HttpRequest.SendAsync<Tinput, Toutput>(
                    http,
                    httpObj.requestVerb,
                    httpObj.requestString,
                    httpObj.payload,
                    auth ? jwt : null
                );

                if (shouldSucceed)
                    httpResponseMessage.EnsureSuccessStatusCode();
            }

            // Run the SignalR request if desired
            if (realtime != null)
            {
                HubConnection signalR = null;
                try
                {
                    signalR = await realtime(auth ? jwt : null);
                    signalrResponse = await request.SignalrRequest<Toutput>()(signalR);
                }
                catch (Exception)
                {
                    if (shouldSucceed)
                        throw;
                }

                // If the request should succeed and an HTTP request was also made, check that they are the same
                if (shouldSucceed
                    && deterministic
                    && http != null)
                    signalrResponse.ShouldDeepEqual(httpResponse);

                // Cleanup
                signalR?.DisposeAsync();
            }

            // If no listener is running, return the response from the request
            if (!listener
                || request.requestVerb == HttpMethod.Get)
                return (httpResponseMessage, httpResponse, signalrResponse, listenerResponse);

            // Otherwise, wait up to 20 seconds for the listener to receive the message before giving up
            var waitTime = 0;
            while (listenerResponse == null && waitTime < 20)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                waitTime += 1;
            }

            signalrListener?.DisposeAsync(); // Cleanup
            if (shouldSucceed)
                Assert.NotNull(listenerResponse); // Do not try to listen with requests that return void!
            return (httpResponseMessage, httpResponse, signalrResponse, listenerResponse);
        }
    }
}