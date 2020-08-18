using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using Xunit;

namespace SQE.ApiTest.Helpers
{
    public static class Request
    {
        /// <summary>
        ///     Send a request to the API and receive the response.
        ///     At least `http` or `realtime` must be provided.
        /// </summary>
        /// <param name="request">A subclass of RequestObject with the details necessary to make the request.</param>
        /// <param name="http">
        ///     An HttpClient to run the request on;
        ///     may be null if no request should be made to the HTTP server.
        /// </param>
        /// <param name="realtime">
        ///     A function to acquire a SignalR hub connection;
        ///     may be null if no request should be made to the SignalR server.
        /// </param>
        /// <param name="addRealtimeListener">
        ///     Whether to run a test with a listener on the request
        ///     (only works for Post, Put, and Delete requests; default is false).
        ///     The default user for the listener is user2 (if that is null user1 is used).
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
        ///     If no User is provided, user2 = user1.
        /// </param>
        /// <param name="shouldSucceed">Whether the request is expected to succeed.</param>
        /// <param name="deterministic">
        ///     Whether the request is expected to return the same response from multiple requests.
        ///     This method will throw an error if the request is deterministic but the http and realtime responses differ.
        /// </param>
        /// <typeparam name="Tinput">Type of the request payload (this is automatically inferred from the request argument).</typeparam>
        /// <typeparam name="Toutput">Type of the API response (this is automatically inferred from the request argument).</typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async
            Task<(HttpResponseMessage httpResponseMessage, Toutput httpResponse, Toutput signalrResponse, Tlistener
                listenerResponse)> Send<Tinput, Toutput, Tlistener>(
                IRequestObject<Tinput, Toutput, Tlistener> request,
                HttpClient http = null,
                Func<string, Task<HubConnection>> realtime = null,
                bool auth = false,
                UserAuthDetails requestUser = null,
                UserAuthDetails listenerUser = null,
                bool shouldSucceed = true,
                bool deterministic = true,
                bool requestRealtime = true,
                bool listenToEdition = true)
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
            var httpResponse = default(Toutput);
            var signalrResponse = default(Toutput);
            var listenerResponse = default(Tlistener);
            HubConnection signalrListener = null;
            string jwt1 = null;
            string jwt2 = null;

            // Generate any necessary JWT's
            if (auth)
                jwt1 = http != null
                    ? await GetJwtViaHttpAsync(http, requestUser ?? null)
                    : await GetJwtViaRealtimeAsync(realtime, requestUser ?? null);

            if (auth && listenerUser != null)
                jwt2 = await GetJwtViaRealtimeAsync(realtime, listenerUser);

            // Set up a SignalR listener if desired (this hub connection must be different than the one used to make
            // the API request.
            if (listenerUser != null
                && request.GetRequestVerb() != HttpMethod.Get
                && realtime != null
                && request.GetListenerMethods().Any()
                && request.GetEditionId().HasValue)
            {
                signalrListener = await realtime(jwt2);
                // Subscribe to messages on the edition
                if (listenToEdition)
                    await signalrListener.InvokeAsync("SubscribeToEdition", request.GetEditionId().Value);
                // Register a listener for messages returned by this API request
                foreach (var listener in request.GetListenerMethods())
                    signalrListener.On<Tlistener>(listener, receivedData => listenerResponse = receivedData);

                // Reload the listener if connection is lost
                signalrListener.Closed += async error =>
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await signalrListener.StartAsync();
                    // Subscribe to messages on the edition
                    await signalrListener.InvokeAsync("SubscribeToEdition", request.GetEditionId().Value);
                    // Register a listener for messages returned by this API request
                    foreach (var listener in request.GetListenerMethods())
                        signalrListener.On<Tlistener>(listener, receivedData => listenerResponse = receivedData);
                };
            }

            // Run the HTTP request if desired and available
            var httpObj = request.GetHttpRequestObject();
            if (http != null
                && httpObj != null)
            {
                (httpResponseMessage, httpResponse) = await SendHttpRequestAsync<Tinput, Toutput>(
                    http,
                    httpObj.requestVerb,
                    httpObj.requestString,
                    httpObj.payload,
                    jwt1
                );

                if (shouldSucceed)
                    httpResponseMessage.EnsureSuccessStatusCode();
            }

            // Run the SignalR request if desired
            if (realtime != null && requestRealtime)
            {
                HubConnection signalR = null;
                try
                {
                    signalR = await realtime(jwt1);
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
            if (listenerUser == null
                || request.GetRequestVerb() == HttpMethod.Get)
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
                Assert.NotNull(listenerResponse);
            return (httpResponseMessage, httpResponse, signalrResponse, listenerResponse);
        }

        /// <summary>
        ///     Wrapper to make HTTP requests (with authorization) and return the response with the required class.
        /// </summary>
        /// <param name="client">The HttpClient to make the request</param>
        /// <param name="httpMethod">The type of requests: GET/POST/PUT/DELETE</param>
        /// <param name="url">The requested url (should start with a /), the SQE_API address is automatically prepended</param>
        /// <param name="bearer">The current bearer token of the requesting client.</param>
        /// <param name="payload">Optional class T1 to be sent as a stringified JSON object.</param>
        /// <returns>Returns an HttpStatusCode for the request and a parsed object T2 with the response.</returns>
        public static async Task<(HttpResponseMessage response, T2 msg)> SendHttpRequestAsync<T1, T2>(
            HttpClient client,
            HttpMethod httpMethod,
            string url,
            T1 payload,
            string bearer = null)
        {
            // Initialize the response
            var parsedClass = default(T2);
            var response = new HttpResponseMessage();

            // Create the request message.  Automatically disposed after the using block ends.
            using (var requestMessage = new HttpRequestMessage(httpMethod, url))
            {
                try
                {
                    StringContent jsonPayload = null;
                    if (!string.IsNullOrEmpty(bearer)) // Add the bearer token
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

                    if (payload != null) // Add payload if it exists.
                    {
                        var json = JsonConvert.SerializeObject(payload);
                        jsonPayload = new StringContent(json, Encoding.UTF8, "application/json");
                        requestMessage.Content = jsonPayload;
                    }

                    // Make the request and capture the response and http status message.
                    response = await client.SendAsync(requestMessage);
                    if (typeof(T2) != typeof(string))
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        if (response.StatusCode == HttpStatusCode.OK)
                            parsedClass = JsonConvert.DeserializeObject<T2>(responseBody);
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine(e);
                }
            }

            return (response, msg: parsedClass);
        }

        /// <summary>
        ///     Returns the JWT for a user account. If no username/password is provided
        ///     this will return a JWT for the "test" account.
        /// </summary>
        /// <param name="client">Http Client</param>
        /// <param name="username">email address of the user</param>
        /// <param name="pwd">the user's password</param>
        /// <returns>A valid JWT</returns>
        public static async Task<string> GetJwtViaHttpAsync(HttpClient client, UserAuthDetails userAuthDetails = null)
        {
            if (userAuthDetails == null)
                userAuthDetails = DefaultUsers.User1;

            var login = new LoginRequestDTO { email = userAuthDetails.email, password = userAuthDetails.password };
            var (response, msg) = await SendHttpRequestAsync<LoginRequestDTO, DetailedUserTokenDTO>(
                client,
                HttpMethod.Post,
                "/v1/users/login",
                login
            );
            response.EnsureSuccessStatusCode();
            return msg.token;
        }

        /// <summary>
        ///     Returns the JWT for a user account. If no username/password is provided
        ///     this will return a JWT for the "test" account.
        /// </summary>
        /// <param name="client">Http Client</param>
        /// <param name="userAuthDetails">A User object with the desired login credentials</param>
        /// <returns>A valid JWT</returns>
        public static async Task<string> GetJwtViaRealtimeAsync(Func<string, Task<HubConnection>> realtime,
            UserAuthDetails userAuthDetails = null)
        {
            if (userAuthDetails == null)
                userAuthDetails = DefaultUsers.User1;
            var login = new LoginRequestDTO { email = userAuthDetails.email, password = userAuthDetails.password };

            var signalR = await realtime(null);
            var msg = await signalR.InvokeAsync<DetailedUserTokenDTO>("PostV1UsersLogin", login);

            return msg.token;
        }

        public static class DefaultUsers
        {
            public static readonly UserAuthDetails User1 = new UserAuthDetails
            { email = "test@1.com", password = "test" };

            public static readonly UserAuthDetails User2 = new UserAuthDetails
            { email = "test@2.com", password = "test" };
        }

        public class UserAuthDetails
        {
            public string email { get; set; }
            public string password { get; set; }
        }
    }
}