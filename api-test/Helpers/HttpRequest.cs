using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SQE.SqeHttpApi.Server.DTOs;

namespace api_test.Helpers
{ 
    public static class HttpRequest
    {
        /// <summary>
        /// Wrapper to make HTTP requests (with authorization) and return the response with the required class.
        /// </summary>
        /// <param name="client">The HttpClient to make the request</param>
        /// <param name="httpMethod">The type of requests: GET/POST/PUT/DELETE</param>
        /// <param name="url">The requested url (should start with a /), the SQE_API address is automatically prepended</param>
        /// <param name="bearer">The current bearer token of the requesting client.</param>
        /// <param name="payload">Optional class T1 to be sent as a stringified JSON object.</param>
        /// <returns>Returns an HttpStatusCode for the request and a parsed object T2 with the response.</returns>
        public static async Task<(HttpResponseMessage response, T2 msg)> SendAsync<T1,T2>(
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
            using(var requestMessage = new HttpRequestMessage(httpMethod, url))
            {
                try
                {
                    StringContent jsonPayload = null;
                    if (!string.IsNullOrEmpty(bearer)) // Add the bearer token
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                    
                    if (payload != null) // Add payload if it exists.
                    {
                        var json = JsonConvert.SerializeObject(payload);
                        jsonPayload = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
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
            return (response: response, msg: parsedClass);
        }
        
        public static async Task<string> GetJWTAsync(HttpClient client)
        {
            const string name = "test";
            const string password = "asdf";
            var login = new LoginRequestDTO(){ email = name, password = password};
            var (response, msg) = await SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(client, HttpMethod.Post, "/v1/users/login", login);
            response.EnsureSuccessStatusCode();
            return msg.token;
        }

        public static async Task<uint> CreateNewEdition(HttpClient client, uint editionId = 1)
        {
            var newScrollRequest = new EditionUpdateRequestDTO("test-name", null, null);
            var (response, msg) = await SendAsync<EditionUpdateRequestDTO, EditionDTO>(client, HttpMethod.Post, $"/v1/editions/{editionId}", newScrollRequest, await GetJWTAsync(client));
            response.EnsureSuccessStatusCode();
            return msg.id;
        }
    }
}