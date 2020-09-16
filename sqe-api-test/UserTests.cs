using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DeepEqual.Syntax;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.API.DTO;
using SQE.API.Server;
using SQE.ApiTest.ApiRequests;
using SQE.ApiTest.Helpers;
using Xunit;

namespace SQE.ApiTest
{
    /// <summary>
    ///     This a suite of integration tests for the users controller.
    /// </summary>
    public class UserTest : WebControllerTest
    {
        public UserTest(WebApplicationFactory<Startup> factory) : base(factory)
        {
            _db = new DatabaseQuery();
            // Routes
            baseUrl = $"/{version}/{controller}";
            login = $"{baseUrl}/login";
            changePassword = $"{baseUrl}/change-password";
            resendActivationEmail = $"{baseUrl}/resend-activation-email";
            changeUnactivatedEmail = $"{baseUrl}/change-unactivated-email";
            forgotPassword = $"{baseUrl}/forgot-password";
            changeForgottenPassword = $"{baseUrl}/change-forgotten-password";
            confirmRegistration = $"{baseUrl}/confirm-registration";
        }

        private readonly DatabaseQuery _db;

        // Routes
        private const string version = "v1";
        private const string controller = "users";
        private readonly string baseUrl;
        private readonly string login;
        private readonly string changePassword;
        private readonly string resendActivationEmail;
        private readonly string changeUnactivatedEmail;
        private readonly string forgotPassword;
        private readonly string changeForgottenPassword;
        private readonly string confirmRegistration;

        private Request.UserAuthDetails UserUpdateRequestDTOToUserAuthDetails(UserUpdateRequestDTO user)
        {
            return new Request.UserAuthDetails { Email = user.email, Password = user.password };
        }


        [Fact]
        public async Task RejectUnauthenticatedGetRequest()
        {
            // Act
            var request = new Get.V1_Users();
            await request.SendAsync(_client, StartConnectionAsync, requestRealtime: true, shouldSucceed: false);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, request.HttpResponseMessage.StatusCode);
        }

        [Fact]
        public async Task RejectUnauthenticatedPostRequests()
        {
            // Act
            var request = new Post.V1_Users_ChangePassword(null);
            await request.SendAsync(_client, StartConnectionAsync, requestRealtime: true, shouldSucceed: false);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, request.HttpResponseMessage.StatusCode);
        }

        [Fact]
        public async Task RejectUnauthenticatedPutRequests()
        {
            // Act
            var request = new Put.V1_Users(null);
            await request.SendAsync(_client, StartConnectionAsync, requestRealtime: true, shouldSucceed: false);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, request.HttpResponseMessage.StatusCode);
        }

        private async Task<(HttpResponseMessage response, UserDTO msg)> CreateUserAccountAsync(
            NewUserRequestDTO user,
            bool shouldSucceed = true,
            bool realtime = false)
        {
            var request = new Post.V1_Users(user);
            await request.SendAsync(
                realtime ? null : _client,
                StartConnectionAsync,
                requestRealtime: realtime,
                shouldSucceed: shouldSucceed);

            // Assert
            var msg = realtime ? request.SignalrResponseObject : request.HttpResponseObject;
            if (shouldSucceed)
            {
                if (!realtime)
                    request.HttpResponseMessage.EnsureSuccessStatusCode();
                Assert.Equal(user.email, msg.email);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Conflict, request.HttpResponseMessage.StatusCode);
            }

            return (request.HttpResponseMessage, msg);
        }

        private async Task ActivatUserAccountAsync(
            UserDTO user,
            bool shouldSucceed = true,
            bool realtime = false)
        {
            var userToken = await GetToken(user.email); // Get  token from DB
            var payload = new AccountActivationRequestDTO { token = userToken.token.ToString() };

            var request = new Post.V1_Users_ConfirmRegistration(payload);
            await request.SendAsync(
                realtime ? null : _client,
                StartConnectionAsync,
                requestRealtime: realtime,
                shouldSucceed: shouldSucceed);

            // Assert
            if (shouldSucceed)
            {
                if (!realtime)
                {
                    request.HttpResponseMessage.EnsureSuccessStatusCode();
                    Assert.Equal(HttpStatusCode.NoContent, request.HttpResponseMessage.StatusCode);
                }
                var confirmedUser = await GetUserByEmail(user.email);
                Assert.Equal(user.email, confirmedUser.email);
                Assert.True(confirmedUser.activated);
            }
            else if (!realtime)
            {
                Assert.Equal(HttpStatusCode.NotFound, request.HttpResponseMessage.StatusCode);
            }
        }

        private async Task<Token> GetToken(string email, bool shouldSucceed = true)
        {
            const string getNewUserTokenSQL =
                "SELECT token, date_created, type FROM user_email_token JOIN user USING(user_id) WHERE email = @Email";

            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", email);

            if (shouldSucceed)
            {
                var activateTokens =
                    await _db.RunQueryAsync<Token>(getNewUserTokenSQL, checkForUserSQLParams); // Get  token from DB
                Assert.NotEmpty(activateTokens);
                return activateTokens.First();
            }


            var tokens = await _db.RunQueryAsync<Token>(getNewUserTokenSQL, checkForUserSQLParams);
            Assert.Empty(tokens);
            return new Token();
        }

        /// <summary>
        ///     Get the user details for the specified email address
        /// </summary>
        /// <param name="email"></param>
        /// <param name="shouldSucceed"></param>
        /// <returns></returns>
        private async Task<UserObj> GetUserByEmail(string email, bool shouldSucceed = true)
        {
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";

            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", email);
            if (shouldSucceed)
                return await _db.RunQuerySingleAsync<UserObj>(
                    checkForUserSQL,
                    checkForUserSQLParams
                ); // Get user from DB

            var users = await _db.RunQueryAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
            Assert.Empty(users);
            return new UserObj();
        }

        private async Task<DetailedUserTokenDTO> Login(
            NewUserRequestDTO user,
            bool shouldSucceed = true,
            bool realtime = false)
        {
            var request = new Post.V1_Users_Login(new LoginRequestDTO { email = user.email, password = user.password });
            await request.SendAsync(
                realtime ? null : _client,
                StartConnectionAsync,
                requestRealtime: realtime,
                shouldSucceed: shouldSucceed);
            if (shouldSucceed)
            {
                if (!realtime)
                    request.HttpResponseMessage.EnsureSuccessStatusCode();
                return realtime ? request.SignalrResponseObject : request.HttpResponseObject;
            }

            if (!realtime)
                Assert.Equal(HttpStatusCode.Unauthorized, request.HttpResponseMessage.StatusCode);
            return new DetailedUserTokenDTO();
        }

        /// <summary>
        ///     Create an activated user in the system.
        /// </summary>
        /// <param name="user">The user details</param>
        /// <param name="shouldSucceed">Set to false if this request is supposed to fail</param>
        /// <returns></returns>
        private async Task CreateActivatedUserAsync(NewUserRequestDTO user, bool shouldSucceed = true)
        {
            var (_, newUser) = await CreateUserAccountAsync(user, shouldSucceed); // Asserts already in this function
            await ActivatUserAccountAsync(newUser, shouldSucceed); // Asserts already in this function
        }

        /// <summary>
        ///     Create a random activated user in the system.
        /// </summary>
        /// <param name="shouldSucceed">Set to false if this request is supposed to fail</param>
        /// <returns></returns>
        private async Task<NewUserRequestDTO> CreateRandomActivatedUserAsync(bool shouldSucceed = true)
        {
            var user = new NewUserRequestDTO(
                "nobody@fake-email.com",
                "7ryew$$###01-_-Ytiuy",
                null,
                "Joe",
                "Nobody"
            );
            await CreateActivatedUserAsync(user, shouldSucceed); // Asserts already in sub functions.
            return user;
        }

        /// <summary>
        ///     We don't ever delete users from SQE, this function is used to cleanup testing users.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task CleanupUserAccountAsync(UserDTO user)
        {
            const string deleteNewUserSQL = "DELETE FROM user WHERE email = @Email";
            const string deleteEmailTokenSQL = "DELETE FROM user_email_token WHERE user_id = @UserId";
            var deleteEmailTokenParams = new DynamicParameters();
            deleteEmailTokenParams.Add("@UserId", user.userId);
            deleteEmailTokenParams.Add("@Email", user.email);
            await _db.RunExecuteAsync(deleteEmailTokenSQL, deleteEmailTokenParams);
            await _db.RunExecuteAsync(deleteNewUserSQL, deleteEmailTokenParams);
            var x = 30;
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
            public Guid token { get; set; }
            public string type { get; set; }
            public DateTime date_created { get; set; }
        }

        /// <summary>
        ///     Changing user account details should fail when password is incorrect.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AccountDetailsUpdateFailsWithWrongPassword()
        {
            var user = new NewUserRequestDTO(
                "fake1@fake1-email.com",
                "*****&&&&&",
                "𒂍𒁾𒁀",
                "𒁉𒊏𒀭𒊓𒀭",
                "𒇽𒋛𒀀 𒃻 𒄑𒌁"
            );
            using (var userCreator = new UserHelpers.UserCreator(user, _client, _db))
            {
                // Arrange
                await userCreator.CreateUser();
                var loginDetails = await Login(user);
                var newDetails = new UserUpdateRequestDTO(
                    "fake1@fake2-email.com",
                    "WrongPassword",
                    "Normal Org.",
                    "Firstname",
                    "Lastname"
                );

                // Act 
                var updateUserAccountObject = new Put.V1_Users(newDetails);
                var userAuth = UserUpdateRequestDTOToUserAuthDetails(user);
                await updateUserAccountObject.SendAsync(
                    _client,
                    auth: true,
                    requestUser: userAuth,
                    shouldSucceed: false
                );

                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, updateUserAccountObject.HttpResponseMessage.StatusCode);
            }
        }

        /// <summary>
        ///     Changing password should fail when the incorrect current password is submitted.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AccountPasswordUpdateFailsWithWrongEnteredPassword()
        {
            var user = new NewUserRequestDTO(
                "fake2@fake-email.com",
                "*****&&&&&",
                "בת ספר",
                "שלמה",
                "בן ציון"
            );
            using (var userCreator = new UserHelpers.UserCreator(user, _client, _db))
            {
                // Arrange
                await userCreator.CreateUser();
                var loginDetails = await Login(user);
                var authUser = UserUpdateRequestDTOToUserAuthDetails(user);
                var newPwd = new ResetLoggedInUserPasswordRequestDTO
                {
                    oldPassword = "wrongpasswd",
                    newPassword = "newpasswd"
                };

                // Act 
                var passwordUpdateRequest = new Post.V1_Users_ChangePassword(newPwd);
                await passwordUpdateRequest.SendAsync(
                    _client,
                    auth: true,
                    shouldSucceed: false,
                    requestUser: authUser
                );

                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, passwordUpdateRequest.HttpResponseMessage.StatusCode);
            }
        }

        /// <summary>
        ///     Register for the account and activate it. Also tests that the activated user account receives
        ///     a bearer token upon login.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanRegisterUserAccount()
        {
            // ARRANGE

            // Act (register user)
            var user = new NewUserRequestDTO(
                "fake3@fake-email.com",
                "12345",
                "🦊🦆",
                "💩",
                null
            ); // Let's try some modern person with an emoji for a name to ensure unicode multibyte success.
            using (var userCreator = new UserHelpers.UserCreator(user, _client, _db, false))
            {
                var userDetails = await userCreator.CreateUser();

                // Assert
                var createdUser = await GetUserByEmail(user.email); // Get user from DB
                Assert.False(createdUser.activated);

                // Act (activate user)
                await ActivatUserAccountAsync(userDetails, realtime: true); // Asserts already in this function

                // Act (login)
                var userAuth = UserUpdateRequestDTOToUserAuthDetails(user);
                var userLoginRequest =
                    new Post.V1_Users_Login(new LoginRequestDTO { email = user.email, password = user.password });
                await userLoginRequest.SendAsync(
                    _client,
                    auth: true,
                    requestUser: userAuth
                );
                var (response, loggedInUser) =
                    (userLoginRequest.HttpResponseMessage, userLoginRequest.HttpResponseObject);

                // Assert (login)
                response.EnsureSuccessStatusCode();
                Assert.NotNull(loggedInUser.token);
                Assert.Equal(createdUser.user_id, loggedInUser.userId);
                Assert.Equal(createdUser.email, loggedInUser.email);
                Assert.Equal(createdUser.forename, loggedInUser.forename);
                Assert.Equal(createdUser.surname, loggedInUser.surname);
                Assert.Equal(createdUser.organization, loggedInUser.organization);
            }
        }

        /// <summary>
        ///     Register for a new user account and resend the activation email.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanResendAccountActivation()
        {
            // ARRANGE
            var user = new NewUserRequestDTO(
                "fake4@fake-email.com",
                "*****&&&&&",
                "בת ספר",
                "שלמה",
                "בן ציון"
            );
            var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function

            try
            {
                var tokenCreationTime = await GetToken(user.email);

                var payload = new ResendUserAccountActivationRequestDTO { email = newUser.email };

                // Act (resend activation)
                Thread.Sleep(2000); // The time resolution of the database date_created field is 1 second.
                var request = new Post.V1_Users_ResendActivationEmail(payload);
                await request.SendAsync(_client, StartConnectionAsync);

                // Assert (resend activation)
                request.HttpResponseMessage.EnsureSuccessStatusCode();
                Assert.Null(request.HttpResponseObject);
                var newTokenCreationTime = await GetToken(user.email);
                Assert.True(
                    newTokenCreationTime.date_created >= tokenCreationTime.date_created
                ); // Make sure the token date the same or higher
            }
            finally
            {
                // Cleanup (remove user)
                await CleanupUserAccountAsync(newUser);
            }
        }

        /// <summary>
        ///     Register for a new user account, and before activating the account try registering again
        ///     with the same email, but different credentials.
        ///     The first account will be overwritten with the new details and the old activation token
        ///     will be invalidated.  A new token is sent for the new account registration.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CanUpdateDetailsBeforeActivation()
        {
            UserDTO updatedUser = null;

            // Arrange
            var user = new NewUserRequestDTO(
                "fake6@fake-email.com",
                "password",
                "Big Company",
                "Jane",
                "Doe"
            );

            try
            {
                await CreateUserAccountAsync(user, realtime: true); // Asserts already in this function
                var tokenCreationTime = await GetToken(user.email);
                // Update user details
                var updateUser = new NewUserRequestDTO(user.email, user.password, "ACME", "Joe", "Nobody");

                // Act (update details)
                Thread.Sleep(2000); // The time resolution of the database date_created field is 1 second.
                HttpResponseMessage response;
                (response, updatedUser) = await CreateUserAccountAsync(updateUser); // Asserts already in this function

                // Assert (resend activation)
                response.EnsureSuccessStatusCode();
                var newTokenCreationTime = await GetToken(user.email);
                Assert.True(newTokenCreationTime.date_created > tokenCreationTime.date_created);
                Assert.NotEqual(tokenCreationTime.token, newTokenCreationTime.token);
                Assert.Equal(user.email, updatedUser.email);
            }
            finally
            {
                // Cleanup (remove user)
                if (updatedUser != null)
                    await CleanupUserAccountAsync(updatedUser);
            }
        }

        /// <summary>
        ///     Make sure that authenticated users can change their password when logged in.
        /// </summary>
        /// <returns></returns>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ChangeActivatedAccountPassword(bool realtime)
        {
            // ARRANGE
            var user = new NewUserRequestDTO(
                "fake7@fake-email.com",
                "password",
                "Big Company",
                "Jane",
                "Doe"
            );
            var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
            await ActivatUserAccountAsync(newUser); // Asserts already in this function

            var loggedInUser = await Login(user, realtime: realtime);
            Assert.NotNull(loggedInUser.token);

            const string newPassword = "more secure pa$$w0rd";

            // Act (change password)
            var request = new Post.V1_Users_ChangePassword(new ResetLoggedInUserPasswordRequestDTO { oldPassword = user.password, newPassword = newPassword });
            await request.SendAsync(
                realtime ? null : _client,
                StartConnectionAsync,
                requestUser: new Request.UserAuthDetails() { Email = user.email, Password = user.password },
                auth: true,
                requestRealtime: realtime);

            // Assert (change password)
            var msg = realtime ? request.SignalrResponseObject : request.HttpResponseObject;
            if (!realtime)
                request.HttpResponseMessage.EnsureSuccessStatusCode();
            Assert.Null(msg);
            await Login(user, false); // Old credentials should now fail

            // Act (attempt login with new password)
            user.password = newPassword;
            var npLoggedInUser = await Login(user); // Assert checks already in method.

            // Assert (attempt login with new password)
            Assert.Equal(loggedInUser.email, npLoggedInUser.email);
            Assert.Equal(loggedInUser.forename, npLoggedInUser.forename);
            Assert.Equal(loggedInUser.surname, npLoggedInUser.surname);
            Assert.Equal(loggedInUser.organization, npLoggedInUser.organization);
            Assert.NotNull(npLoggedInUser.token);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(npLoggedInUser);
        }

        /// <summary>
        ///     Make sure that authenticated users can change their account details,including changing the email.
        /// </summary>
        /// <returns></returns>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ChangeActivatedAccountUserDetails(bool realtime)
        {
            // ARRANGE
            var user = new NewUserRequestDTO(
                "fake8@fake-email.com",
                "password",
                "Big Company",
                "Jane",
                "Doe"
            );
            var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
            await ActivatUserAccountAsync(newUser); // Asserts already in this function

            var loggedInUser = await Login(user);
            Assert.NotNull(loggedInUser.token);

            var updatedUser = new NewUserRequestDTO(
                "fake88@fake-email.com",
                user.password,
                "Big Company",
                "John",
                "Doe"
            );

            // Act (change user details)
            var request = new Put.V1_Users(updatedUser);
            await request.SendAsync(
                realtime ? null : _client,
                StartConnectionAsync,
                auth: true,
                requestUser: new Request.UserAuthDetails() { Email = user.email, Password = user.password },
                requestRealtime: realtime);

            // Assert (change user details)
            if (!realtime)
                request.HttpResponseMessage.EnsureSuccessStatusCode();
            var cdMsg = realtime ? request.SignalrResponseObject : request.HttpResponseObject;
            Assert.Equal(updatedUser.email, cdMsg.email);

            // Act (attempt login for new details)
            var upMsg = await Login(updatedUser);

            // Assert (attempt login for new details)
            Assert.Equal(updatedUser.email, upMsg.email);
            Assert.Equal(updatedUser.forename, upMsg.forename);
            Assert.Equal(updatedUser.surname, upMsg.surname);
            Assert.Equal(updatedUser.organization, upMsg.organization);
            Assert.Null(upMsg.token); // The account should not be activated yet.

            // ACT (activate account and login)
            await ActivatUserAccountAsync(upMsg);
            var upActMsg = await Login(updatedUser);

            // Assert (activate account and login)
            Assert.NotNull(upActMsg.token);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(upActMsg);
        }

        /// <summary>
        ///     Make sure that authenticated users can change their account details, without changing the email.
        /// </summary>
        /// <returns></returns>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ChangeActivatedAccountUserDetailsWithoutEmail(bool realtime)
        {
            // ARRANGE
            var user = new NewUserRequestDTO(
                "fake9@fake-email.com",
                "password",
                "Big Company",
                "Jane",
                "Doe"
            );
            var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
            await ActivatUserAccountAsync(newUser); // Asserts already in this function

            var loggedInUser = await Login(user);
            Assert.NotNull(loggedInUser.token);

            var updatedUser = new UserUpdateRequestDTO(
                null,
                user.password,
                "Small Company",
                "John",
                "Smith"
            );

            // Act (change user details)
            var request = new Put.V1_Users(updatedUser);
            await request.SendAsync(
                realtime ? null : _client,
                StartConnectionAsync,
                auth: true,
                requestUser: new Request.UserAuthDetails() { Email = user.email, Password = user.password },
                requestRealtime: realtime);

            // Assert (change user details)
            if (!realtime)
                request.HttpResponseMessage.EnsureSuccessStatusCode();
            var cdMsg = realtime ? request.SignalrResponseObject : request.HttpResponseObject;
            Assert.Equal(updatedUser.forename, cdMsg.forename);
            Assert.Equal(updatedUser.surname, cdMsg.surname);
            Assert.Equal(updatedUser.organization, cdMsg.organization);

            // Act (attempt login for new details)
            var upMsg = await Login(user);

            // Assert (attempt login for new details)
            Assert.Equal(user.email, upMsg.email);
            Assert.Equal(updatedUser.forename, upMsg.forename);
            Assert.Equal(updatedUser.surname, upMsg.surname);
            Assert.Equal(updatedUser.organization, upMsg.organization);
            Assert.NotNull(upMsg.token);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(upMsg);
        }

        /// <summary>
        ///     After registering for a new account, attempt to change email address before activating the account.
        ///     Email should be updated, the token should remain the same but with updated creation time.
        /// </summary>
        /// <returns></returns>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ChangeUnactivatedAccountEmail(bool realtime)
        {
            // Arrange
            var user = new NewUserRequestDTO(
                "fake10@fake-email.com",
                "password",
                "Big Company",
                "Jane",
                "Doe"
            );
            await CreateUserAccountAsync(user); // Asserts already in this function
            const string newEmail = "new.user@email.gov";

            // Act
            Thread.Sleep(2000); // The time resolution of the database date_created field is 1 second.
            var request = new Post.V1_Users_ChangeUnactivatedEmail(new UnactivatedEmailUpdateRequestDTO { email = user.email, newEmail = newEmail });
            await request.SendAsync(
                realtime ? null : _client,
                StartConnectionAsync,
                requestRealtime: realtime);

            // Assert
            if (!realtime)
                request.HttpResponseMessage.EnsureSuccessStatusCode();
            Assert.Null(request.HttpResponseObject);
            Assert.Null(request.SignalrResponseObject);

            // Verify the new email is in the DB
            var lgMsg = await Login(new NewUserRequestDTO(newEmail, user.password, null, null, null));
            Assert.Equal(user.forename, lgMsg.forename);
            Assert.Equal(user.surname, lgMsg.surname);
            Assert.Equal(user.organization, lgMsg.organization);
            Assert.Null(lgMsg.token); // The account is not activated when the token is null.

            // Verify that the old email is not in DB
            await GetUserByEmail(user.email, false); // Assert checks already in method

            // Verify we have a new token
            var token = await GetToken(newEmail);
            Assert.NotEqual(Guid.Empty, token.token);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(lgMsg);
        }

        /// <summary>
        ///     Make sure that we do not allow two users with the same email.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task DontAllowDuplicateAccounts()
        {
            // Arrange
            var newUser = await CreateRandomActivatedUserAsync();
            var loggedInUser = await Login(newUser);


            // Act
            var (response, msg) = await CreateUserAccountAsync(newUser, false); // Assert already in method;

            // Cleanup
            await CleanupUserAccountAsync(loggedInUser);
        }

        /// <summary>
        ///     Make sure that unauthenticated users do not get a JWT.  No editing operations are possible without a JWT.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task DontAllowUnauthenticatedUsersToHaveJwt()
        {
            // Arrange
            var user = new NewUserRequestDTO(
                "fake11@fake-email.com",
                "password",
                "Big Company",
                "Jane",
                "Doe"
            );
            var newUser = await CreateUserAccountAsync(user);

            // Act
            var loginResponse = await Login(user);

            // Assert
            Assert.Null(loginResponse.token);

            // Cleanup
            await CleanupUserAccountAsync(loginResponse);
        }

        /// <summary>
        ///     Login should fail for accounts that do not exist.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task NonexistentAccountsShouldNotLogin()
        {
            // Arrange
            var user = new NewUserRequestDTO(
                "fake12@fake-email.com",
                "password",
                "Big Company",
                "Jane",
                "Doe"
            );

            // Act
            var loggedInUser = await Login(user, false); // Asserts already in method.
        }

        /// <summary>
        ///     Make sure that activated accounts can request a password reset.
        /// </summary>
        /// <returns></returns>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResetActivatedAccountPassword(bool realtime)
        {
            // ARRANGE
            var user = new NewUserRequestDTO(
                "fake13@fake-email.com",
                "password",
                "Big Company",
                "Jane",
                "Doe"
            );
            var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
            await ActivatUserAccountAsync(newUser); // Asserts already in this function

            var loggedInUser = await Login(user);
            Assert.NotNull(loggedInUser.token);

            // Act (request password reset)
            var request = new Post.V1_Users_ForgotPassword(new ResetUserPasswordRequestDTO { email = user.email });
            await request.SendAsync(
                realtime ? null : _client,
                StartConnectionAsync,
                requestRealtime: realtime);

            // Assert (request password reset)
            if (!realtime)
                request.HttpResponseMessage.EnsureSuccessStatusCode();
            Assert.Null(request.HttpResponseObject);
            Assert.Null(request.SignalrResponseObject);
            var userToken = await GetToken(user.email);
            Assert.NotNull(userToken);

            // Act (attempt to reset password with token)
            const string newPassword = "unu$u^l..pw@wierdpr0vider.org";
            var tokenRequest = new Post.V1_Users_ChangeForgottenPassword(new ResetForgottenUserPasswordRequestDTO { token = userToken.token.ToString(), password = newPassword });
            await tokenRequest.SendAsync(
                realtime ? null : _client,
                StartConnectionAsync,
                requestRealtime: realtime);

            // Assert (attempt to reset password with token)
            if (!realtime)
                tokenRequest.HttpResponseMessage.EnsureSuccessStatusCode();
            Assert.Null(tokenRequest.HttpResponseObject);
            Assert.Null(tokenRequest.SignalrResponseObject);

            // Act (attempt login with new password)
            user.password = newPassword;
            var npLoggedInUser = await Login(user);

            // Assert (attempt login with new password)
            Assert.Equal(loggedInUser.email, npLoggedInUser.email);
            Assert.Equal(loggedInUser.forename, npLoggedInUser.forename);
            Assert.Equal(loggedInUser.surname, npLoggedInUser.surname);
            Assert.Equal(loggedInUser.organization, npLoggedInUser.organization);
            Assert.NotNull(npLoggedInUser.token);
            // Make sure the token was not left behind in the database.
            await GetToken(user.email, false);

            // Cleanup (remove user)
            await CleanupUserAccountAsync(npLoggedInUser);
        }

        [Fact]
        public async Task CanGetUserDetails()
        {
            // Arrange
            var user = Request.DefaultUsers.User1;

            // Act
            var request = new Get.V1_Users();
            await request.SendAsync(
                _client,
                StartConnectionAsync,
                auth: true,
                requestUser: user);

            // Assert
            request.HttpResponseObject.ShouldDeepEqual(request.SignalrResponseObject);
            Assert.Equal(user.Email, request.HttpResponseObject.email);
        }
    }
}