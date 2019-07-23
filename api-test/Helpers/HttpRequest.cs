using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using SQE.SqeHttpApi.Server.DTOs;
using Xunit;
using Bogus;

namespace SQE.ApiTest.Helpers
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
        
        public static async Task<string> GetJWTAsync(HttpClient client, string username = null, string pwd = null)
        {
            var name = username ?? "test";
            var password = pwd ?? "asdf";
            var login = new LoginRequestDTO(){ email = name, password = password};
            var (response, msg) = await SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(client, HttpMethod.Post, "/v1/users/login", login);
            response.EnsureSuccessStatusCode();
            return msg.token;
        }

        public static async Task<uint> CreateNewEdition(HttpClient client, uint editionId = 1, string username = null, 
            string pwd = null)
        {
            var newScrollRequest = new EditionUpdateRequestDTO("test-name", null, null);
            var (response, msg) = await SendAsync<EditionUpdateRequestDTO, EditionDTO>(client, HttpMethod.Post, 
                $"/v1/editions/{editionId}", newScrollRequest, await GetJWTAsync(client, username, pwd));
            response.EnsureSuccessStatusCode();
            return msg.id;
        }
        
        public static async Task DeleteEdition(HttpClient client, uint editionId, bool authenticated = false, bool shouldSucceed = true)
        {
            var (response, msg) = await SendAsync<EditionUpdateRequestDTO, EditionDTO>(client, HttpMethod.Delete, 
                $"/v1/editions/{editionId}", null, authenticated ? await GetJWTAsync(client) : null);
            if (shouldSucceed)
                response.EnsureSuccessStatusCode();
        }
        
        #region User Account Conveniences

        public static async Task<DetailedUserDTO> CreateRandomUser(HttpClient client, string password)
        {
            var faker = new Faker("en");
            var user = new NewUserRequestDTO(faker.Internet.Email(), password, faker.Company.CompanyName(), 
                faker.Name.FirstName(), faker.Name.LastName());

            var (userAcctResp, userAcctMsg) = await CreateUserAccountAsync(client, user);
            userAcctResp.EnsureSuccessStatusCode();

            await ActivateUserAccountAsync(client, userAcctMsg);
            return userAcctMsg;
        }
        
        private static async Task<(HttpResponseMessage response, DetailedUserDTO msg)> CreateUserAccountAsync(HttpClient client,
            NewUserRequestDTO user, bool shouldSucceed = true)
        {
            var (response, msg) = await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(client, 
                HttpMethod.Post, "/v1/users", user);

            // Assert
            if (shouldSucceed)
            {
                response.EnsureSuccessStatusCode();
                Assert.Equal(user.email, msg.email);
                Assert.Equal(user.forename, msg.forename);
                Assert.Equal(user.surname, msg.surname);
                Assert.Equal(user.organization, msg.organization);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            }
            return (response, msg);
        }

        private static async Task ActivateUserAccountAsync(HttpClient client, DetailedUserDTO user, bool shouldSucceed = true)
        {
            var userToken = await GetToken(user.email); // Get  token from DB
            var payload = new AccountActivationRequestDTO() {token = userToken.token};
            
            var (response, msg) =
                await HttpRequest.SendAsync<AccountActivationRequestDTO, UserDTO>(client, HttpMethod.Post, 
                    "/v1/users/confirm-registration", payload);
            
            // Assert
            if (shouldSucceed)
            {
                response.EnsureSuccessStatusCode();
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                var confirmedUser = await GetUserByEmail(user.email);
                Assert.Equal(user.email, confirmedUser.email);
                Assert.True(confirmedUser.activated);
            }
            else
            {
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }
        
        private static async Task<UserObj> GetUserByEmail(string email, bool shouldSucceed = true)
        {
            var db = new DatabaseQuery();
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";
            
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", email);
            if (shouldSucceed)
                return await db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
            
            var users = await db.RunQueryAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
            Assert.Empty(users);
            return new UserObj();
        }
        
        private static async Task<Token> GetToken(string email, bool shouldSucceed = true)
        {
            var db = new DatabaseQuery();
            const string getNewUserTokenSQL = "SELECT token, date_created, type FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", email);
            
            if (shouldSucceed)
                return await db.RunQuerySingleAsync<Token>(getNewUserTokenSQL, checkForUserSQLParams); // Get  token from DB
            
            var tokens = await db.RunQueryAsync<Token>(getNewUserTokenSQL, checkForUserSQLParams);
            Assert.Empty(tokens);
            return new Token();
        }
        
        private class UserObj
        {
            public string email { get; set; }
            public string password { get; set; }
            public string forename { get; set; }
            public string surname { get; set; }
            public string organization { get; set; }
            public bool activated { get; set; }
            public uint user_id { get; set; }
        }
        
        private class Token
        {
            public string token { get; set; }
            public string type { get; set; }
            public DateTime date_created { get; set; }
        }
        
        #endregion User Account Conveniences
    }
}