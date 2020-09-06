using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.Helpers
{
    public static class Request
    {
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
                    if (!string.IsNullOrEmpty(bearer)) // Add the bearer token
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

                    if (payload != null) // Add payload if it exists.
                    {
                        var json = JsonSerializer.Serialize(payload);
                        var jsonPayload = new StringContent(json, Encoding.UTF8, "application/json");
                        requestMessage.Content = jsonPayload;
                    }

                    // Make the request and capture the response and http status message.
                    response = await client.SendAsync(requestMessage);
                    if (typeof(T2) != typeof(string))
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        if (response.StatusCode == HttpStatusCode.OK)
                            parsedClass = JsonSerializer.Deserialize<T2>(responseBody);
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
        /// <param name="userAuthDetails">Information about the user</param>
        /// <returns>A valid JWT</returns>
        public static async Task<string> GetJwtViaHttpAsync(HttpClient client, UserAuthDetails userAuthDetails = null)
        {
            if (userAuthDetails == null)
                userAuthDetails = DefaultUsers.User1;

            var login = new LoginRequestDTO { email = userAuthDetails.Email, password = userAuthDetails.Password };
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
        /// <param name="realtime">The SignalR Client</param>
        /// <param name="userAuthDetails">A User object with the desired login credentials</param>
        /// <returns>A valid JWT</returns>
        public static async Task<string> GetJwtViaRealtimeAsync(Func<string, Task<HubConnection>> realtime,
            UserAuthDetails userAuthDetails = null)
        {
            if (userAuthDetails == null)
                userAuthDetails = DefaultUsers.User1;
            var login = new LoginRequestDTO { email = userAuthDetails.Email, password = userAuthDetails.Password };

            var signalR = await realtime(null);
            var msg = await signalR.InvokeAsync<DetailedUserTokenDTO>("PostV1UsersLogin", login);

            return msg.token;
        }

        public static class DefaultUsers
        {
            public static readonly UserAuthDetails User1 = new UserAuthDetails
            { Email = "test@1.com", Password = "test" };

            public static readonly UserAuthDetails User2 = new UserAuthDetails
            { Email = "test@2.com", Password = "test" };
        }

        public class UserAuthDetails
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }
}