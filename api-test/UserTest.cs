using System;
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

        private readonly NewUserRequestDTO normalUser = new NewUserRequestDTO("test@mymail.com",
            "test-pw", "My Organization", "Testing", "Tester");
        private readonly NewUserRequestDTO nullUser = new NewUserRequestDTO("nulltest@mymail.com",
            "test-pw", null, null, null);
        private readonly NewUserRequestDTO emptyUser = new NewUserRequestDTO("emptytest@mymail.com",
            "test-pw", "", "", "");
        private readonly NewUserRequestDTO updateUser = new NewUserRequestDTO("emptytest@mymail.com",
            "test-pw", "EmptyOrg", "EmptyFirst", "EmptyLast");
        private readonly NewUserRequestDTO conflictingUser = new NewUserRequestDTO("test@mymail.com",
            "test-pw", "My Organization", "Testing", "Tester");
        
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
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", normalUser.email);
            
            // Act (register user)
            var (_, newUser) = await CreateUserAcountAsync(normalUser); // Asserts already in this function
            
            // Assert
            var createdUser = await _db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
            Assert.False(createdUser.activated);
            
            // Act (activate user)
            await ActivatUserAccountAsync(newUser); // Asserts already in this function
            
            // Act (login)
            var userForLogin = new LoginRequestDTO() {email = normalUser.email, password = normalUser.password};
            var (response, loggedInUser) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(_client, HttpMethod.Post, "/v1/users/login", userForLogin);
            
            // Assert (login)
            response.EnsureSuccessStatusCode();
            Assert.NotNull(loggedInUser.token);
            Assert.Equal(createdUser.user_id, loggedInUser.userId);
            Assert.Equal(createdUser.email, loggedInUser.email);
            Assert.Equal(createdUser.forename, loggedInUser.forename);
            Assert.Equal(createdUser.surname, loggedInUser.surname);
            Assert.Equal(createdUser.organization, loggedInUser.organization);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(newUser);
        }
        
        [Fact]
        public async Task CanResendAccountActivation()
        {
            // ARRANGE
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", normalUser.email);
            
            // Act (register user)
            var (_, newUser) = await CreateUserAcountAsync(normalUser); // Asserts already in this function
            
            // Assert
            var createdUser = await _db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
            Assert.False(createdUser.activated);
            
            
            // Arrange (resend activation)
            const string getTokenCreationSQL = "SELECT date_created FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            var tokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForUserSQLParams);
            const string resendUrl = "/v1/users/resend-activation-email";
            var payload = new ResendUserAccountActivationRequestDTO() {email = newUser.email};
            
            // Act (resend activation)
            var (response, msg) =
                await HttpRequest.SendAsync<ResendUserAccountActivationRequestDTO, string>(_client, HttpMethod.Post, resendUrl,
                    payload);
            
            // Assert (resend activation)
            response.EnsureSuccessStatusCode();
            Assert.Null(msg);
            var newTokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForUserSQLParams);
            Assert.True(newTokenCreationTime.date_created > tokenCreationTime.date_created);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(newUser);
        }

        [Fact]
        public async Task CanUpdateDetailsBeforeActivation()
        {
            // Arrange
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", emptyUser.email);
            const string getTokenCreationSQL = "SELECT date_created, token FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            
            // Act
            var (_, newUser) = await CreateUserAcountAsync(emptyUser); // Asserts already in this function
            var tokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForUserSQLParams);
            
            // Assert
            var createdUser = await _db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
            Assert.False(createdUser.activated);
            
            
            // Act (update details)
            var (response, updatedUser) = await CreateUserAcountAsync(updateUser); // Asserts already in this function
            
            // Assert (resend activation)
            response.EnsureSuccessStatusCode();
            var newTokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForUserSQLParams);
            Assert.True(newTokenCreationTime.date_created > tokenCreationTime.date_created);
            Assert.NotEqual(tokenCreationTime.token, newTokenCreationTime.token);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(updatedUser);
        }

        private async Task<(HttpResponseMessage response, DetailedUserDTO msg)> CreateUserAcountAsync(NewUserRequestDTO user)
        {
            const string url = "/v1/users";
            var (response, msg) = await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(_client, 
                HttpMethod.Post, url, user);
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(user.email, msg.email);
            Assert.Equal(user.forename, msg.forename);
            Assert.Equal(user.surname, msg.surname);
            Assert.Equal(user.organization, msg.organization);
            return (response, msg);
        }

        private async Task ActivatUserAccountAsync(DetailedUserDTO user)
        {
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";
            const string getNewUserTokenSQL = "SELECT token FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            const string confirmRegistrationUrl = "/v1/users/confirm-registration";
            
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", user.email);
            
            var userToken = await _db.RunQuerySingleAsync<Token>(getNewUserTokenSQL, checkForUserSQLParams); // Get  token from DB
            var payload = new AccountActivationRequestDTO() {token = userToken.token};
            
            var (response, msg) =
                await HttpRequest.SendAsync<AccountActivationRequestDTO, UserDTO>(_client, HttpMethod.Post, confirmRegistrationUrl,
                    payload);
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var confirmedUser = await _db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams);
            Assert.Equal(confirmedUser.email, user.email);
            Assert.True(confirmedUser.activated);
        }

        private async Task<DetailedUserDTO> CreateActivatedUserAsync(NewUserRequestDTO user)
        {
            var (_, newUser) = await CreateUserAcountAsync(normalUser); // Asserts already in this function
            await ActivatUserAccountAsync(newUser); // Asserts already in this function
            return newUser;
        }

        /// <summary>
        /// We don't ever delete users from SQE, this function is used to cleanup testing users.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task CleanupUserAccountAsync(DetailedUserDTO user)
        {
            const string deleteNewUserSQL = "DELETE FROM user WHERE email = @Email";
            const string deleteEmailTokenSQL = "DELETE FROM user_email_token WHERE user_id = @UserId";
            var deleteEmailTokenParams = new DynamicParameters();
            deleteEmailTokenParams.Add("@UserId", user.userId);
            deleteEmailTokenParams.Add("@Email", user.email);
            await _db.RunExecuteAsync(deleteEmailTokenSQL, deleteEmailTokenParams);
            await _db.RunExecuteAsync(deleteNewUserSQL, deleteEmailTokenParams);
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
            public DateTime date_created { get; set; }
        }
    }
}