using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace sqe_signalr_api.Services
{
    public interface IHttpService
    {
        Task<(HttpStatusCode status, string msg)> GetPath(string path);
        Task<(HttpStatusCode status, string msg)> GetPath(string path, string bearer);
        Task<(HttpStatusCode status, string msg)> PostPath(string path, string payload, string bearer);
        Task<(HttpStatusCode status, string msg)> PutPath(string path, string payload, string bearer);
        Task<(HttpStatusCode status, string msg)> DeletePath(string path, string bearer);
        Task<(HttpStatusCode status, string msg)> Authenticate(string payload);
    } 
    /// <summary>
    /// This gets injected into the Hub class.
    /// </summary>
    public class HttpService : IHttpService
    {
        
        private static HttpClient _httpClient;
        private readonly string _httpApi;

        public HttpService()
        {
            // HttpClient should not be disposed for the life of the program.  We use only threadsafe functions and
            // the client is always reused.
            _httpClient = new HttpClient();
            
            // Grab the SQE_API web address from an environment variable, or assume it is localhost.
            var httpApi = Environment.GetEnvironmentVariable("SQE_HTTP_API");
            _httpApi = httpApi ?? "localhost";
        }
        
        ~HttpService()
        {
            _httpClient.Dispose();
        }

        /// <summary>
        /// Get data without authentication.
        /// </summary>
        /// <param name="path">Path of the HTTP API</param>
        /// <returns>Returns an HttpStatusCode for the request and a string with the response.</returns>
        public async Task<(HttpStatusCode status, string msg)> GetPath(string path)
        {
            return await MakeHttpRequestAsync(httpMethod: HttpMethod.Get, url: path);
        }
        
        /// <summary>
        /// Get data with authorization
        /// </summary>
        /// <param name="path">Path of the HTTP API</param>
        /// <param name="bearer">Bearer token for authorization</param>
        /// <returns>Returns an HttpStatusCode for the request and a string with the response.</returns>
        public async Task<(HttpStatusCode status, string msg)> GetPath(string path, string bearer)
        {
            return await MakeHttpRequestAsync(httpMethod: HttpMethod.Get, url: path, bearer: bearer);
        }
        
        /// <summary>
        /// Post data with authorization
        /// </summary>
        /// <param name="path">Path of the HTTP API</param>
        /// <param name="payload">Stringified JSON object for request body.</param>
        /// <param name="bearer">Bearer token for authorization</param>
        /// <returns>Returns an HttpStatusCode for the request and a string with the response.</returns>
        public async Task<(HttpStatusCode status, string msg)> PostPath(string path, string payload, string bearer)
        {
            return await MakeHttpRequestAsync(httpMethod: HttpMethod.Post, url: path, bearer: bearer, payload: payload);
        }
        
        /// <summary>
        /// Put data with authorization
        /// </summary>
        /// <param name="path">Path of the HTTP API</param>
        /// <param name="payload">Stringified JSON object for request body.</param>
        /// <param name="bearer">Bearer token for authorization</param>
        /// <returns>Returns an HttpStatusCode for the request and a string with the response.</returns>
        public async Task<(HttpStatusCode status, string msg)> PutPath(string path, string payload, string bearer)
        {
            return await MakeHttpRequestAsync(httpMethod: HttpMethod.Put, url: path, bearer: bearer, payload: payload);
        }
        
        /// <summary>
        /// Delete data with authorization
        /// </summary>
        /// <param name="path">Path of the HTTP API</param>
        /// <param name="bearer">Bearer token for authorization</param>
        /// <returns>Returns an HttpStatusCode for the request and a string with the response.</returns>
        public async Task<(HttpStatusCode status, string msg)> DeletePath(string path, string bearer)
        {
            
            return await MakeHttpRequestAsync(httpMethod: HttpMethod.Delete, url: path, bearer: bearer);
        }
        
        /// <summary>
        /// Perform authentication with the HTTP API.
        /// </summary>
        /// <param name="payload">Stringified JSON object: {username: string, password: string}</param>
        /// <returns>Returns an HttpStatusCode for the request and a string with the response.</returns>
        public async Task<(HttpStatusCode status, string msg)> Authenticate(string payload)
        {
            var msg = "";
            
            var status = HttpStatusCode.BadRequest;
            
            try 
            {
                var response = await _httpClient.PostAsync(
                    $"http://{_httpApi}/v1/user/login", 
                    new StringContent(payload, Encoding.UTF8, "application/json")
                    );
                response.EnsureSuccessStatusCode();
                status = response.StatusCode;
                msg = await response.Content.ReadAsStringAsync();
            }  
            catch (HttpRequestException e)
            {
                msg = e.ToString();
            }

            return (status: status, msg: msg);
        }

        /// <summary>
        /// Wrapper to make authorized HTTP requests and return the response.
        /// </summary>
        /// <param name="httpMethod">The type of requests: GET/POST/PUT/DELETE</param>
        /// <param name="url">The requested url (should start with a /), the SQE_API address is automatically prepended</param>
        /// <param name="bearer">The current bearer token of the requesting client.</param>
        /// <param name="payload">Optional stringified JSON object for request body.</param>
        /// <returns>Returns an HttpStatusCode for the request and a string with the response.</returns>
        private async Task<(HttpStatusCode status, string msg)> MakeHttpRequestAsync(
            HttpMethod httpMethod, 
            string url,
            string bearer = null,
            string payload = null)
        {
            // Initialize the response
            var msg = "";
            var status = HttpStatusCode.BadRequest;
            
            // Create the request message.  Automatically disposed after the using block ends.
            using(var requestMessage 
                = new HttpRequestMessage(httpMethod, $"http://{_httpApi}{url}"))
            {
                try
                {
                    if (!string.IsNullOrEmpty(bearer))
                    {
                        // Add the bearer token
                        requestMessage.Headers.Authorization 
                            = new AuthenticationHeaderValue("Bearer", bearer);
                    
                        if (!string.IsNullOrEmpty(payload)) // Add payload if it exists.
                            requestMessage.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                    }
                    
                    // Make the request and capture the response and http status message.
                    var response = await _httpClient.SendAsync(requestMessage);
                    response.EnsureSuccessStatusCode();
                    status = response.StatusCode;
                    msg = await response.Content.ReadAsStringAsync();
                }  
                catch (HttpRequestException e)
                {
                    Console.WriteLine(e);
                    // Send the error back as the response message.
                    msg = e.ToString();
                }
            }
            
            return (status: status, msg: msg);
        }
    }
}