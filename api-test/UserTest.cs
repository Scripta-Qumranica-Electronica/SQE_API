using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using api_test.Helpers;
using Bogus;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.SqeHttpApi.Server;
using SQE.SqeHttpApi.Server.DTOs;
using Xunit;

// TODO: write the "should fail" tests
namespace api_test
{
    /// <summary>
    /// This a suite of integration tests for the users controller.
    /// </summary>
    public class UserTest : WebControllerTest
    {
        #region Setup and constructor
        
        private readonly DatabaseQuery _db;
        
        private readonly Faker _faker = new Faker("en");
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

        private readonly string version = "v1";
        private readonly string controller = "users";
        
        public UserTest(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();
        }
        #endregion Setup and constructor
        
        #region Batch theory tests
        
        [Theory]
        [InlineData("/v1/users")]
        public async Task RejectUnauthenticatedGetRequest(string url)
        {
            // Act
            var (response, msg) =
                await HttpRequest.SendAsync<string, DetailedUserDTO>(_client, HttpMethod.Get, url, null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/v1/users/change-password")]
        public async Task RejectUnauthenticatedPostRequests(string url)
        {
            // Act
            var (response, msg) =
                await HttpRequest.SendAsync<string, DetailedUserDTO>(_client, HttpMethod.Post, url, null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        
        [Theory]
        [InlineData("/v1/users")]
        public async Task RejectUnauthenticatedPutRequests(string url)
        {
            // Act
            var (response, msg) =
                await HttpRequest.SendAsync<string, DetailedUserDTO>(_client, HttpMethod.Put, url, null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        
        #endregion Batch theory tests

        #region Registration/activation should succeed
        /// <summary>
        /// Register for the account and activate it. Also tests that the activated user account receives
        /// a bearer token upon login.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanRegisterUserAccount()
        {
            // ARRANGE
            
            // Act (register user)
            var (_, newUser) = await CreateUserAccountAsync(normalUser); // Asserts already in this function
            
            // Assert
            var createdUser = await GetUserByEmail(normalUser.email); // Get user from DB
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
        
        /// <summary>
        /// Register for a new user account and resend the activation email.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanResendAccountActivation()
        {
            // ARRANGE
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", normalUser.email);
            
            // Act (register user)
            var (_, newUser) = await CreateUserAccountAsync(normalUser); // Asserts already in this function
            
            // Assert
            var createdUser = await _db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
            Assert.False(createdUser.activated);
            
            
            // Arrange (resend activation)
            const string getTokenCreationSQL = "SELECT date_created FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            var tokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForUserSQLParams);
            const string resendUrl = "/v1/users/resend-activation-email";
            var payload = new ResendUserAccountActivationRequestDTO() {email = newUser.email};
            
            // Act (resend activation)
            System.Threading.Thread.Sleep(1000); // The time resolution of the database date_created field is 1 second.
            var (response, msg) =
                await HttpRequest.SendAsync<ResendUserAccountActivationRequestDTO, string>(_client, HttpMethod.Post, resendUrl,
                    payload);
            
            // Assert (resend activation)
            response.EnsureSuccessStatusCode();
            Assert.Null(msg);
            var newTokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForUserSQLParams);
            // Make sure the token date was updated
            Assert.True(newTokenCreationTime.date_created > tokenCreationTime.date_created);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(newUser);
        }

        /// <summary>
        /// Register for a new user account, and before activating the account try registering again
        /// with the same email, but different credentials.
        /// The first account will be overwritten with the new details and the old activation token
        /// will be invalidated.  A new token is sent for the new account registration.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUpdateDetailsBeforeActivation()
        {
            // Arrange
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", emptyUser.email);
            const string getTokenCreationSQL = "SELECT date_created, token FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            
            // Act
            var (_, newUser) = await CreateUserAccountAsync(emptyUser); // Asserts already in this function
            var tokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForUserSQLParams);
            
            // Assert
            var createdUser = await _db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
            Assert.False(createdUser.activated);
            
            
            // Act (update details)
            System.Threading.Thread.Sleep(1000); // The time resolution of the database date_created field is 1 second.
            var (response, updatedUser) = await CreateUserAccountAsync(updateUser); // Asserts already in this function
            
            // Assert (resend activation)
            response.EnsureSuccessStatusCode();
            var newTokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForUserSQLParams);
            Assert.True(newTokenCreationTime.date_created > tokenCreationTime.date_created);
            Assert.NotEqual(tokenCreationTime.token, newTokenCreationTime.token);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(updatedUser);
        }

        /// <summary>
        /// After registering for a new account, attempt to change email address before activating the account.
        /// Email should be updated, the token should remain the same but with updated creation time.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ChangeUnactivatedAccountEmail()
        {
            // Arrange
            var originalUser = RandomUser();
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";
            var checkForOriginalUserSQLParams = new DynamicParameters();
            checkForOriginalUserSQLParams.Add("@Email", originalUser.email);
            const string getTokenCreationSQL = "SELECT date_created, token FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            
            var (_, newUser) = await CreateUserAccountAsync(originalUser); // Asserts already in this function
            var tokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForOriginalUserSQLParams);

            var newEmail = _faker.Internet.Email();
            var checkForUpdatedUserSQLParams = new DynamicParameters();
            checkForUpdatedUserSQLParams.Add("@Email", newEmail);
            
            // Act
            System.Threading.Thread.Sleep(1000); // The time resolution of the database date_created field is 1 second.
            var (response, msg) = await HttpRequest.SendAsync<UnactivatedEmailUpdateRequestDTO, string>(_client, 
                HttpMethod.Post, $"/{version}/{controller}/change-unactivated-email", 
                new UnactivatedEmailUpdateRequestDTO() {email = originalUser.email, newEmail = newEmail});
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Null(msg);
            
            // Verify the new email is in the DB
            var (lgResponse, lgMsg) =
                await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(_client, HttpMethod.Post, 
                    $"/{version}/{controller}/login", new LoginRequestDTO() {email = newEmail, password = originalUser.password});
            lgResponse.EnsureSuccessStatusCode();
            Assert.Equal(originalUser.forename, lgMsg.forename);
            Assert.Equal(originalUser.surname, lgMsg.surname);
            Assert.Equal(originalUser.organization, lgMsg.organization);
            Assert.Null(lgMsg.token); // The account is not activated when the token is null.
            
            // Verify that the old email is not in DB
            var origUserRecord = await _db.RunQueryAsync<UserObj>(checkForUserSQL, checkForOriginalUserSQLParams);
            Assert.Empty(origUserRecord);
            
            // Verify we have a new token
            var newTokenCreationTime = await _db.RunQuerySingleAsync<Token>(getTokenCreationSQL, checkForUpdatedUserSQLParams);
            Assert.True(newTokenCreationTime.date_created > tokenCreationTime.date_created);
            Assert.Equal(tokenCreationTime.token, newTokenCreationTime.token);
            
            // Cleanup (remove user)
            await CleanupUserAccountAsync(lgMsg);
        }
        #endregion Registration/activation should succeed

        #region Registration/activation should fail
        
        #endregion Registration/activation should fail
        
        #region Activated account interaction should succeed

        /// <summary>
        /// Make sure that authenticated users can change their password when logged in.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ChangeActivatedAccountPassword()
        {
            // ARRANGE
            var user = RandomUser();
            var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
            await ActivatUserAccountAsync(newUser); // Asserts already in this function
            
            var userForLogin = new LoginRequestDTO() {email = user.email, password = user.password};
            var (response, loggedInUser) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
                _client, HttpMethod.Post, "/v1/users/login", userForLogin);
            response.EnsureSuccessStatusCode();
            Assert.NotNull(loggedInUser.token);
            
            var newPassword = _faker.Internet.Password();
            
            // Act (change password)
            var (cpResponse, cpMsg) = await HttpRequest.SendAsync<ResetLoggedInUserPasswordRequestDTO, string>(
                _client, HttpMethod.Post, $"/{version}/{controller}/change-password", 
                new ResetLoggedInUserPasswordRequestDTO(){oldPassword = user.password, newPassword = newPassword},
                loggedInUser.token);
            
            // Assert (change password)
            cpResponse.EnsureSuccessStatusCode();
            Assert.Null(cpMsg);
            
            // Act (attempt login with old password)
            var (opResponse, opMsg) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
                _client, HttpMethod.Post, "/v1/users/login", userForLogin);
            
            // Assert (attempt login with old password)
            Assert.Equal(HttpStatusCode.Unauthorized, opResponse.StatusCode);
            
            // Act (attempt login with new password)
            userForLogin.password = newPassword;
            var (npResponse, npLoggedInUser) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
                _client, HttpMethod.Post, "/v1/users/login", userForLogin);
            
            // Assert (attempt login with new password)
            npResponse.EnsureSuccessStatusCode();
            Assert.Equal(loggedInUser.email, npLoggedInUser.email);
            Assert.Equal(loggedInUser.forename, npLoggedInUser.forename);
            Assert.Equal(loggedInUser.surname, npLoggedInUser.surname);
            Assert.Equal(loggedInUser.organization, npLoggedInUser.organization);
            Assert.NotNull(npLoggedInUser.token);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(npLoggedInUser);
        }
        
        /// <summary>
        /// Make sure that activated accounts can request a password reset.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ResetActivatedAccountPassword()
        {
            // ARRANGE
            var user = RandomUser();
            var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
            await ActivatUserAccountAsync(newUser); // Asserts already in this function
            
            var userForLogin = new LoginRequestDTO() {email = user.email, password = user.password};
            var (response, loggedInUser) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
                _client, HttpMethod.Post, "/v1/users/login", userForLogin);
            response.EnsureSuccessStatusCode();
            Assert.NotNull(loggedInUser.token);
            
            // Act (request password reset)
            var (rpResponse, rpMsg) = await HttpRequest.SendAsync<ResetUserPasswordRequestDTO, string>(
                _client, HttpMethod.Post, $"/{version}/{controller}/forgot-password", 
                new ResetUserPasswordRequestDTO(){email = user.email});
            
            // Assert (request password reset)
            rpResponse.EnsureSuccessStatusCode();
            Assert.Null(rpMsg);
            const string getNewUserTokenSQL = "SELECT token FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", user.email);
            var userToken = await _db.RunQuerySingleAsync<Token>(getNewUserTokenSQL, checkForUserSQLParams); // Get token from DB
            Assert.NotNull(userToken);
            
            // Act (attempt to reset password with token)
            var newPassword = _faker.Internet.Password();
            var (cpResponse, cpMsg) = await HttpRequest.SendAsync<ResetForgottenUserPasswordRequestDto, string>(
                _client, HttpMethod.Post, $"/{version}/{controller}/change-forgotten-password", 
                new ResetForgottenUserPasswordRequestDto(){token = userToken.token, password = newPassword});
            
            // Assert (attempt to reset password with token)
            cpResponse.EnsureSuccessStatusCode();
            Assert.Null(cpMsg);
            
            // Act (attempt login with new password)
            userForLogin.password = newPassword;
            var (npResponse, npLoggedInUser) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
                _client, HttpMethod.Post, "/v1/users/login", userForLogin);
            
            // Assert (attempt login with new password)
            npResponse.EnsureSuccessStatusCode();
            Assert.Equal(loggedInUser.email, npLoggedInUser.email);
            Assert.Equal(loggedInUser.forename, npLoggedInUser.forename);
            Assert.Equal(loggedInUser.surname, npLoggedInUser.surname);
            Assert.Equal(loggedInUser.organization, npLoggedInUser.organization);
            Assert.NotNull(npLoggedInUser.token);
            // Make sure the token was not left behind in the database.
            var emailTokens = await _db.RunQueryAsync<Token>(getNewUserTokenSQL, checkForUserSQLParams); // Get token from DB
            Assert.Empty(emailTokens);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(npLoggedInUser);
        }
        
        /// <summary>
        /// Make sure that authenticated users can change their account details,including changing the email.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ChangeActivatedAccountUserDetails()
        {
            // ARRANGE
            var user = RandomUser();
            var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
            await ActivatUserAccountAsync(newUser); // Asserts already in this function
            
            var userForLogin = new LoginRequestDTO() {email = user.email, password = user.password};
            var (response, loggedInUser) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
                _client, HttpMethod.Post, "/v1/users/login", userForLogin);
            response.EnsureSuccessStatusCode();
            Assert.NotNull(loggedInUser.token);
            
            var updatedUser = RandomUser();
            updatedUser.password = user.password;
            
            // Act (change user details)
            var (cdResponse, cdMsg) = await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(
                _client, HttpMethod.Put, $"/{version}/{controller}", updatedUser,
                loggedInUser.token);
            
            // Assert (change user details)
            cdResponse.EnsureSuccessStatusCode();
            Assert.Equal(updatedUser.email, cdMsg.email);
            Assert.Equal(updatedUser.forename, cdMsg.forename);
            Assert.Equal(updatedUser.surname, cdMsg.surname);
            Assert.Equal(updatedUser.organization, cdMsg.organization);
            
            // Act (attempt login for new details)
            userForLogin.email = updatedUser.email;
            var (upResponse, upMsg) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
                _client, HttpMethod.Post, "/v1/users/login", userForLogin);
            
            // Assert (attempt login for new details)
            upResponse.EnsureSuccessStatusCode();
            Assert.Equal(updatedUser.email, upMsg.email);
            Assert.Equal(updatedUser.forename, upMsg.forename);
            Assert.Equal(updatedUser.surname, upMsg.surname);
            Assert.Equal(updatedUser.organization, upMsg.organization);
            Assert.Null(upMsg.token); // The account should not be activated yet.
            
            // ACT (activate account and login)
            await ActivatUserAccountAsync(upMsg);
            var (upActResponse, upActMsg) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
                _client, HttpMethod.Post, "/v1/users/login", userForLogin);
            
            // Assert (activate account and login)
            upActResponse.EnsureSuccessStatusCode();
            Assert.NotNull(upActMsg.token);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(upActMsg);
        }
        
        /// <summary>
        /// Make sure that authenticated users can change their account details, without changing the email.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ChangeActivatedAccountUserDetailsWithoutEmail()
        {
            // ARRANGE
            var user = RandomUser();
            var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
            await ActivatUserAccountAsync(newUser); // Asserts already in this function
            
            var userForLogin = new LoginRequestDTO() {email = user.email, password = user.password};
            var (response, loggedInUser) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
                _client, HttpMethod.Post, "/v1/users/login", userForLogin);
            response.EnsureSuccessStatusCode();
            Assert.NotNull(loggedInUser.token);
            
            var updatedUser = RandomUser();
            updatedUser.email = null;
            updatedUser.password = user.password;
            
            // Act (change user details)
            var (cdResponse, cdMsg) = await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(
                _client, HttpMethod.Put, $"/{version}/{controller}", updatedUser,
                loggedInUser.token);
            
            // Assert (change user details)
            cdResponse.EnsureSuccessStatusCode();
            Assert.Equal(updatedUser.forename, cdMsg.forename);
            Assert.Equal(updatedUser.surname, cdMsg.surname);
            Assert.Equal(updatedUser.organization, cdMsg.organization);
            
            // Act (attempt login for new details)
            updatedUser.email = user.email;
            var (upResponse, upMsg) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
                _client, HttpMethod.Post, "/v1/users/login", userForLogin);
            
            // Assert (attempt login for new details)
            upResponse.EnsureSuccessStatusCode();
            Assert.Equal(updatedUser.email, upMsg.email);
            Assert.Equal(updatedUser.forename, upMsg.forename);
            Assert.Equal(updatedUser.surname, upMsg.surname);
            Assert.Equal(updatedUser.organization, upMsg.organization);
            Assert.NotNull(upMsg.token);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(upMsg);
        }
        #endregion Activated account interaction should succeed
        
        #region Activated account interaction should fail
        #endregion Activated account interaction should fail
        
        #region Helper functions
        private async Task<(HttpResponseMessage response, DetailedUserDTO msg)> CreateUserAccountAsync(
            NewUserRequestDTO user, bool shouldSucceed = true)
        {
            var (response, msg) = await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(_client, 
                HttpMethod.Post, $"/{version}/{controller}", user);

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

        private async Task ActivatUserAccountAsync(DetailedUserDTO user, bool shouldSucceed = true)
        {
            var confirmRegistrationUrl = $"/{version}/{controller}/confirm-registration";
            var userToken = await GetToken(user.email); // Get  token from DB
            var payload = new AccountActivationRequestDTO() {token = userToken.token};
            
            var (response, msg) =
                await HttpRequest.SendAsync<AccountActivationRequestDTO, UserDTO>(_client, HttpMethod.Post, confirmRegistrationUrl,
                    payload);
            
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

        private async Task<Token> GetToken(string email)
        {
            const string getNewUserTokenSQL = "SELECT token FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";
            
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", email);
            
            return await _db.RunQuerySingleAsync<Token>(getNewUserTokenSQL, checkForUserSQLParams); // Get  token from DB
        }
        
        private async Task<UserObj> GetUserByEmail(string email)
        {
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";
            
            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", email);
            
            return await _db.RunQuerySingleAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
        }

        private async Task CreateActivatedUserAsync(NewUserRequestDTO user, bool shouldSucceed = true)
        {
            var (_, newUser) = await CreateUserAccountAsync(user, shouldSucceed); // Asserts already in this function
            await ActivatUserAccountAsync(newUser, shouldSucceed); // Asserts already in this function
            return;
        }
        
        private async Task<NewUserRequestDTO> CreateRandomActivatedUserAsync(bool shouldSucceed = true)
        {
            var user = RandomUser(); 
            await CreateActivatedUserAsync(user, shouldSucceed); // Asserts already in sub functions.
            return user;
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

        private NewUserRequestDTO RandomUser(string locale = null)
        {
            var faker = string.IsNullOrEmpty(locale) ? _faker : new Faker(locale);
            return new NewUserRequestDTO(
                faker.Internet.Email(),
                faker.Internet.Password(), 
                faker.Company.CompanyName(), 
                faker.Name.FirstName(),
                faker.Name.LastName());
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
        #endregion Helper functions
    }
}