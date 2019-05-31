using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using api_test.Helpers;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.SqeHttpApi.Server;
using SQE.SqeHttpApi.Server.DTOs;
using Xunit;

namespace api_test
{
    /// <summary>
    /// This a suite of integration tests for the users controller.
    /// </summary>
    public class UserTest : WebControllerTest
    {
        private readonly DatabaseQuery _db;
        public UserTest(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();
        }
        
        /// <summary>
        /// Make sure we cannot get user information unless authenticated
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RejectUnauthenticatedUserRequest()
        {
            // ARRANGE
            const string url = "/v1/users";
            
            // Act
            var (response, msg) =
                await HttpRequest.SendAsync<string, DetailedUserDTO>(_client, HttpMethod.Get, url, null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CanRegisterUserAccount()
        {
            // ARRANGE
            const string url = "/v1/users";
            const string email = "test@mymail.com";
            const string password = "test-pw";
            const string forename = "Testing";
            const string surname = "Tester";
            const string organization = "My Organization";
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";
            const string getNewUserTokenSQL = "SELECT token FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            const string deleteNewUserSQL = "DELETE FROM user WHERE email = @Email";
            const string deleteEmailTokenSQL = "DELETE FROM user_email_token WHERE user_id = @UserId";
            
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", email);
            
            var deleteEmailTokenParams = new DynamicParameters();
            var payload = new NewUserRequestDTO(email, password, organization, forename, surname);
            
            // Act (register user)
            var (response, msg) =
                await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(_client, HttpMethod.Post, url,
                    payload);
            
            // Assert
            var createdUser = await _db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
            Assert.Equal(createdUser.email, email);
            Assert.False(createdUser.activated);
            
            
            // Arrange (activate user)
            var userToken = await _db.RunQuerySingleAsync<Token>(getNewUserTokenSQL, checkForUserSQLParams); // Get  token from DB
            const string confirmRegistrationUrl = "/v1/users/confirm-registration";
            var payload2 = new AccountActivationRequestDTO() {token = userToken.token};
            
            // Act (activate user)
            var (response2, msg2) =
                await HttpRequest.SendAsync<AccountActivationRequestDTO, UserDTO>(_client, HttpMethod.Post, confirmRegistrationUrl,
                    payload2);
            
            // Assert
            response2.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);
            var confirmedUser = await _db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams);
            Assert.Equal(confirmedUser.email, createdUser.email);
            Assert.True(confirmedUser.activated);

            // Cleanup (remove user)
            deleteEmailTokenParams.Add("@UserId", createdUser.user_id);
            await _db.RunExecuteAsync(deleteEmailTokenSQL, deleteEmailTokenParams);
            await _db.RunExecuteAsync(deleteNewUserSQL, checkForUserSQLParams);
        }
        
        [Fact]
        public async Task CanRecoverFromAccountRegistrationProblems()
        {
            // ARRANGE (register user)
            const string url = "/v1/users";
            const string email = "test@mymail.com";
            const string password = "test-pw";
            const string forename = "Testing";
            const string surname = "Tester";
            const string organization = "My Organization";
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";
            const string getNewUserTokenSQL = "SELECT token FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            const string deleteNewUserSQL = "DELETE FROM user WHERE email = @Email";
            const string deleteEmailTokenSQL = "DELETE FROM user_email_token WHERE user_id = @UserId";
            
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", email);
            
            var deleteEmailTokenParams = new DynamicParameters();
            var payload1 = new NewUserRequestDTO(email, password, organization, forename, surname);
            
            // Act (register user)
            var (response1, msg1) =
                await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(_client, HttpMethod.Post, url,
                    payload1);
            
            // Assert (register user)
            var createdUser = await _db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
            Assert.Equal(createdUser.email, email);
            Assert.Equal(createdUser.forename, forename);
            Assert.Equal(createdUser.surname, surname);
            Assert.Equal(createdUser.organization, organization);
            Assert.False(createdUser.activated);
            
            
            // Arrange (resend activation)
            const string getTokenCreationSQL = "SELECT date_created FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            var tokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForUserSQLParams);
            const string resendUrl = "/v1/users/resend-activation-email";
            var payload2 = new ResendUserAccountActivationRequestDTO() {email = email};
            
            // Act (resend activation)
            var (response2, msg2) =
                await HttpRequest.SendAsync<ResendUserAccountActivationRequestDTO, string>(_client, HttpMethod.Post, resendUrl,
                    payload2);
            
            // Assert (resend activation)
            var newTokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForUserSQLParams);
            Assert.True(newTokenCreationTime.date_created > tokenCreationTime.date_created);
            
            
//            // Arrange (activate user)
//            var userToken = await _db.RunQuerySingleAsync<Token>(getNewUserTokenSQL, checkForUserSQLParams); // Get  token from DB
//            const string confirmRegistrationUrl = "/v1/users/confirm-registration";
//            var payload2 = new AccountActivationRequestDTO() {token = userToken.token};
//            
//            // Act (activate user)
//            var (response2, msg2) =
//                await HttpRequest.SendAsync<AccountActivationRequestDTO, UserDTO>(_client, HttpMethod.Post, confirmRegistrationUrl,
//                    payload2);
//            
//            // Assert
//            response2.EnsureSuccessStatusCode();
//            Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);
//            var confirmedUser = await _db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams);
//            Assert.Equal(confirmedUser.email, createdUser.email);
//            Assert.True(confirmedUser.activated);

            // Cleanup (remove user)
            deleteEmailTokenParams.Add("@UserId", createdUser.user_id);
            await _db.RunExecuteAsync(deleteEmailTokenSQL, deleteEmailTokenParams);
            await _db.RunExecuteAsync(deleteNewUserSQL, checkForUserSQLParams);
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
            public uint date_created { get; set; }
        }
    }
}