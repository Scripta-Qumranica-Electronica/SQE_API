using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using sqe_signalr_api.Services;
using Newtonsoft.Json;

namespace sqe_signalr_api.Hubs
{
    public class MainHub : Hub
    {
        private readonly IHttpService _http;
        private readonly Regex _svgRegex;

        public MainHub(IHttpService http)
        {
            _http = http;
            
            // Use a precompiled Regex for the function `GetEditionId`.
            _svgRegex = new Regex(@"^.*\/edition\/(\d{1,32})", 
                RegexOptions.Singleline | RegexOptions.Compiled);
        }
        
        /// <summary>
        /// This is the main function to access the database.  You will want to call Auth first to add
        /// a bearer token to the client before making any requests that rely on authorization.
        /// </summary>
        /// <param name="rest">The HTTP verb matching the SQE HTTP API</param>
        /// <param name="path">The path matching the SQE HTTP API. It should start with a / and should
        /// not contain the HTTP server address (that is added automatically).</param>
        /// <param name="payload">Stringified JSON payload, if needed.</param>
        /// <returns></returns>
        public async Task Request(string rest, string path, string payload = null)
        {
            (HttpStatusCode status, string msg) response;
            
            // Check if client has already validated
            if (Context.Items.TryGetValue("Bearer", out var bearer))
            {
                // Get the editionIdId of the current request
                var editionId = GetEditionId(path);
                // If request has a editionIdId check client registration to that group
                if (editionId.HasValue)
                {
                    // If client is already registered with a editionIdId
                    if (Context.Items.TryGetValue("editionId", out var clientEditionGroup))
                    {
                        // Check if it is the same editionIdId as this request
                        if ((uint) clientEditionGroup != editionId.Value)
                        {
                            // If not, remove client from that editionIdId
                            var r = Groups.RemoveFromGroupAsync(Context.ConnectionId, clientEditionGroup.ToString());
                            // And add it to the editionIdId of this request
                            var a = Groups.AddToGroupAsync(Context.ConnectionId, editionId.Value.ToString());
                            await Task.WhenAll(r, a);
                        } 
                    }
                    else // If the client is not already registered with an editionId
                    {
                        // Add it to the editionIdId of this request
                        var a = Groups.AddToGroupAsync(Context.ConnectionId, editionId.Value.ToString());
                        await a;
                    }
                    Context.Items["editionId"] = editionId.Value;
                }
                
                // Route the request (and assume it is a mutate request)
                var mutate = true;
                switch (rest)
                {
                    case "GET":
                        mutate = false; // A GET does not mutate the database
                        response = _http.GetPath(path, bearer.ToString()).Result;
                        break;
                    case "POST":
                        response = _http.PostPath(path, payload, bearer.ToString()).Result;
                        break;
                    case "PUT":
                        response = _http.PutPath(path, payload, bearer.ToString()).Result;
                        break;
                    case "DELETE":
                        response = _http.DeletePath(path, bearer.ToString()).Result;
                        break;
                    default:
                        response = (HttpStatusCode.BadRequest, $"Error: Invalid HTTP verb {rest}.");
                        break;
                }

                
                // If it is a mutate request and the mutate was successful
                if (mutate && response.status == HttpStatusCode.OK)
                {
                    Console.WriteLine("Sending mutate message...");
                    Console.WriteLine(editionId.Value.ToString());
                    if (Context.Items.TryGetValue("editionId", out var clientEditionGroup))
                    {
                        Console.WriteLine(clientEditionGroup);
                    }
                    // Broadcast the mutate to the whole group (this includes the current client)
                    if (editionId.HasValue)
                        await Clients.Group(editionId.Value.ToString()).SendAsync("mutateResponse", response.msg);
                }
            }
            // If not validated, clients can only make get requests.
            else
            {
                response = rest == "GET" ? _http.GetPath(path).Result : (HttpStatusCode.BadRequest, "Must be logged in for POST/PUT/DELETE access.");
            }
            
            // For a GET request, send the full response back to the caller.  For POST/PUT/DELETE send
            // only the status of the request.  The contents of the response will come via the group broadcast message.
            await Clients.Caller.SendAsync("returnedRequest", rest == "GET" ? response.msg : response.status.ToString(), path);
        }
        
        /// <summary>
        /// This is used to authorize the client.  The bearer token stays with the client for the life of the connection.
        /// I believe it is even remains over a reconnect (probably the load balancer needs sticky sessions for that
        /// to work).
        /// </summary>
        /// <param name="payload">Stringified JSON with credentials: {username: string, password: string}</param>
        /// <returns></returns>
        public Task Auth(string payload)
        {
            var response = _http.Authenticate(payload).Result;

            // If we recieved a correct response get the bearer token and add it to the client's context.
            if (response.status == HttpStatusCode.OK)
            {
                var user = JsonConvert.DeserializeObject<User>(response.msg);
                Context.Items["Bearer"] = user.token;
            }
            
            // Call the broadcastMessage method to update clients.
            return Clients.Caller.SendAsync("returnedRequest", response.status.ToString(), "auth");
        }
        
        /// <summary>
        /// TODO: unfinished (should return username, not the bearer token).
        /// It now just sends the bearer token if your user has authenticated.
        /// </summary>
        /// <returns></returns>
        public async Task Identity()
        {
            // Grab the keys and values of the client's context.
            var info = Context.Items.Aggregate("", (current, item) => current + (item.Key + " = " + item.Value + "; "));

            // Call the broadcastMessage method to update clients.
            await Clients.Caller.SendAsync("returnedRequest", info, "identity");
        }
        
        /// <summary>
        /// The standard user information received from the SQE HTTP API.
        /// TODO: Remove bearer token from SQE HTTP API.
        /// </summary>
        private class User
        {
            public string token { get; set; }
            public int userId { get; set; }
            public string userName { get; set; }
        }
        
        /// <summary>
        /// Only the sqe-http-api can use this. It broadcasts a mutation response to all connected SignalR servers.
        /// </summary>
        /// <param name="editionId">editionId for the group that will receive the broadcast</param>
        /// <param name="secret">shared secret so only sqe-http-api can call this</param>
        /// <param name="payload">Stringified JSON with the mutation data</param>
        /// <returns></returns>
        public async Task HttpBroadcast(uint editionId, string secret, string payload)
        {
            if (secret == Environment.GetEnvironmentVariable("HTTP_SIGNALR_SECRET"))
            {
                await Clients.Group(editionId.ToString()).SendAsync("mutateResponse", payload);
            }

            return;
        }
        /// <summary>
        /// Finds the edition_id from the path request.
        /// </summary>
        /// <param name="path">The path matching the SQE HTTP API.</param>
        /// <returns></returns>
        private uint? GetEditionId(string path)
        {
            uint? editionId = null;
            var match = _svgRegex.Match(path);
            if (match.Groups.Count == 2)
                editionId = uint.TryParse(match.Groups[1].Value, out var i) ? (uint?) i : null;
            return editionId;
        }
    }
}
