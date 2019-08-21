using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using SQE.ApiTest.Helpers;
using SQE.SqeHttpApi.Server;
using SQE.SqeHttpApi.Server.DTOs;
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

		private readonly Faker _faker = new Faker();

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

		private async Task<(HttpResponseMessage response, DetailedUserDTO msg)> CreateUserAccountAsync(
			NewUserRequestDTO user,
			bool shouldSucceed = true)
		{
			var (response, msg) = await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(
				_client,
				HttpMethod.Post,
				baseUrl,
				user
			);

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
			var userToken = await GetToken(user.email); // Get  token from DB
			var payload = new AccountActivationRequestDTO {token = userToken.token};

			var (response, msg) =
				await HttpRequest.SendAsync<AccountActivationRequestDTO, UserDTO>(
					_client,
					HttpMethod.Post,
					confirmRegistration,
					payload
				);

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

		private async Task<DetailedUserTokenDTO> Login(NewUserRequestDTO user, bool shouldSucceed = true)
		{
			var (response, userLogin) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
				_client,
				HttpMethod.Post,
				login,
				new LoginRequestDTO {email = user.email, password = user.password}
			);
			if (shouldSucceed)
			{
				response.EnsureSuccessStatusCode();
				return userLogin;
			}

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
			var user = RandomUser();
			await CreateActivatedUserAsync(user, shouldSucceed); // Asserts already in sub functions.
			return user;
		}

		/// <summary>
		///     We don't ever delete users from SQE, this function is used to cleanup testing users.
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

		private NewUserRequestDTO RandomUser(bool empty = false, string locale = null)
		{
			var faker = string.IsNullOrEmpty(locale) ? _faker : new Faker(locale);
			return new NewUserRequestDTO(
				faker.Internet.Email(),
				faker.Internet.Password(),
				empty ? null : faker.Company.CompanyName(),
				empty ? null : faker.Name.FirstName(),
				empty ? null : faker.Name.LastName()
			);
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

		/// <summary>
		///     Changing user account details should fail when password is incorrect.
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task AccountDetailsUpdateFailsWithWrongPassword()
		{
			// Arrange
			var user = await CreateRandomActivatedUserAsync();
			var newDetails = RandomUser();
			var loginDetails = await Login(user);

			// Act 
			var (response, msg) = await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(
				_client,
				HttpMethod.Put,
				baseUrl,
				newDetails,
				loginDetails.token
			);

			// Assert
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

			// Cleanup
			await CleanupUserAccountAsync(loginDetails);
		}

		/// <summary>
		///     Changing password should fail when the incorrect current password is submitted.
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task AccountPasswordUpdateFailsWithWrongEnteredPassword()
		{
			// Arrange
			var user = await CreateRandomActivatedUserAsync();
			var newDetails = RandomUser();
			var loginDetails = await Login(user);
			var newPwd = new ResetLoggedInUserPasswordRequestDTO
			{
				oldPassword = _faker.Internet.Password(),
				newPassword = _faker.Internet.Password()
			};

			// Act 
			var (response, msg) = await HttpRequest.SendAsync<ResetLoggedInUserPasswordRequestDTO, string>(
				_client,
				HttpMethod.Post,
				changePassword,
				newPwd,
				loginDetails.token
			);

			// Assert
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

			// Cleanup
			await CleanupUserAccountAsync(loginDetails);
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
			var user = RandomUser(locale: "zh_CN"); // Let's try chinese to ensure unicode multibyte success.
			var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function

			// Assert
			var createdUser = await GetUserByEmail(user.email); // Get user from DB
			Assert.False(createdUser.activated);

			// Act (activate user)
			await ActivatUserAccountAsync(newUser); // Asserts already in this function

			// Act (login)
			var userForLogin = new LoginRequestDTO {email = user.email, password = user.password};
			var (response, loggedInUser) = await HttpRequest.SendAsync<LoginRequestDTO, DetailedUserTokenDTO>(
				_client,
				HttpMethod.Post,
				login,
				userForLogin
			);

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
		///     Register for a new user account and resend the activation email.
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task CanResendAccountActivation()
		{
			// ARRANGE
			var user = RandomUser();
			var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function

			var tokenCreationTime = await GetToken(user.email);

			var payload = new ResendUserAccountActivationRequestDTO {email = newUser.email};

			// Act (resend activation)
			Thread.Sleep(2000); // The time resolution of the database date_created field is 1 second.
			var (response, msg) =
				await HttpRequest.SendAsync<ResendUserAccountActivationRequestDTO, string>(
					_client,
					HttpMethod.Post,
					resendActivationEmail,
					payload
				);

			// Assert (resend activation)
			response.EnsureSuccessStatusCode();
			Assert.Null(msg);
			var newTokenCreationTime = await GetToken(user.email);
			Assert.True(
				newTokenCreationTime.date_created >= tokenCreationTime.date_created
			); // Make sure the token date the same or higher

			// Cleanup (remove user)
			await CleanupUserAccountAsync(newUser);
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
			// Arrange
			var user = RandomUser(true);
			var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
			var tokenCreationTime = await GetToken(user.email);
			// Update user details
			var updateUser = new NewUserRequestDTO(user.email, user.password, "ACME", "Joe", "Nobody");

			// Act (update details)
			Thread.Sleep(2000); // The time resolution of the database date_created field is 1 second.
			var (response, updatedUser) = await CreateUserAccountAsync(updateUser); // Asserts already in this function

			// Assert (resend activation)
			response.EnsureSuccessStatusCode();
			var newTokenCreationTime = await GetToken(user.email);
			Assert.True(newTokenCreationTime.date_created > tokenCreationTime.date_created);
			Assert.NotEqual(tokenCreationTime.token, newTokenCreationTime.token);
			Assert.NotEqual(user.forename, updatedUser.forename);
			Assert.NotEqual(user.surname, updatedUser.surname);
			Assert.NotEqual(user.organization, updatedUser.organization);
			Assert.Equal(user.email, updatedUser.email);

			// Cleanup (remove user)
			await CleanupUserAccountAsync(updatedUser);
		}

		/// <summary>
		///     Make sure that authenticated users can change their password when logged in.
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task ChangeActivatedAccountPassword()
		{
			// ARRANGE
			var user = RandomUser();
			var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
			await ActivatUserAccountAsync(newUser); // Asserts already in this function

			var loggedInUser = await Login(user);
			Assert.NotNull(loggedInUser.token);

			var newPassword = _faker.Internet.Password();

			// Act (change password)
			var (cpResponse, cpMsg) = await HttpRequest.SendAsync<ResetLoggedInUserPasswordRequestDTO, string>(
				_client,
				HttpMethod.Post,
				changePassword,
				new ResetLoggedInUserPasswordRequestDTO {oldPassword = user.password, newPassword = newPassword},
				loggedInUser.token
			);

			// Assert (change password)
			cpResponse.EnsureSuccessStatusCode();
			Assert.Null(cpMsg);
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
		[Fact]
		public async Task ChangeActivatedAccountUserDetails()
		{
			// ARRANGE
			var user = RandomUser();
			var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
			await ActivatUserAccountAsync(newUser); // Asserts already in this function

			var loggedInUser = await Login(user);
			Assert.NotNull(loggedInUser.token);

			var updatedUser = RandomUser();
			updatedUser.password = user.password;

			// Act (change user details)
			var (cdResponse, cdMsg) = await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(
				_client,
				HttpMethod.Put,
				baseUrl,
				updatedUser,
				loggedInUser.token
			);

			// Assert (change user details)
			cdResponse.EnsureSuccessStatusCode();
			Assert.Equal(updatedUser.email, cdMsg.email);
			Assert.Equal(updatedUser.forename, cdMsg.forename);
			Assert.Equal(updatedUser.surname, cdMsg.surname);
			Assert.Equal(updatedUser.organization, cdMsg.organization);

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
		[Fact]
		public async Task ChangeActivatedAccountUserDetailsWithoutEmail()
		{
			// ARRANGE
			var user = RandomUser();
			var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
			await ActivatUserAccountAsync(newUser); // Asserts already in this function

			var loggedInUser = await Login(user);
			Assert.NotNull(loggedInUser.token);

			var updatedUser = RandomUser();
			updatedUser.email = null;
			updatedUser.password = user.password;

			// Act (change user details)
			var (cdResponse, cdMsg) = await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(
				_client,
				HttpMethod.Put,
				baseUrl,
				updatedUser,
				loggedInUser.token
			);

			// Assert (change user details)
			cdResponse.EnsureSuccessStatusCode();
			Assert.Equal(updatedUser.forename, cdMsg.forename);
			Assert.Equal(updatedUser.surname, cdMsg.surname);
			Assert.Equal(updatedUser.organization, cdMsg.organization);

			// Act (attempt login for new details)
			updatedUser.email = user.email;
			var upMsg = await Login(updatedUser);

			// Assert (attempt login for new details)
			Assert.Equal(updatedUser.email, upMsg.email);
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
		[Fact]
		public async Task ChangeUnactivatedAccountEmail()
		{
			// Arrange
			var user = RandomUser();
			await CreateUserAccountAsync(user); // Asserts already in this function
			var newEmail = _faker.Internet.Email();

			// Act
			Thread.Sleep(2000); // The time resolution of the database date_created field is 1 second.
			var (response, updatedUser) = await HttpRequest.SendAsync<UnactivatedEmailUpdateRequestDTO, string>(
				_client,
				HttpMethod.Post,
				changeUnactivatedEmail,
				new UnactivatedEmailUpdateRequestDTO {email = user.email, newEmail = newEmail}
			);

			// Assert
			response.EnsureSuccessStatusCode();
			Assert.Null(updatedUser);

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
			Assert.NotNull(token.token);

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
			var user = RandomUser();
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
			var user = RandomUser(locale: "vi");

			// Act
			var loggedInUser = await Login(user, false); // Asserts already in method.
		}

		/// <summary>
		///     Make sure that activated accounts can request a password reset.
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task ResetActivatedAccountPassword()
		{
			// ARRANGE
			var user = RandomUser();
			var (_, newUser) = await CreateUserAccountAsync(user); // Asserts already in this function
			await ActivatUserAccountAsync(newUser); // Asserts already in this function

			var loggedInUser = await Login(user);
			Assert.NotNull(loggedInUser.token);

			// Act (request password reset)
			var (rpResponse, rpMsg) = await HttpRequest.SendAsync<ResetUserPasswordRequestDTO, string>(
				_client,
				HttpMethod.Post,
				forgotPassword,
				new ResetUserPasswordRequestDTO {email = user.email}
			);

			// Assert (request password reset)
			rpResponse.EnsureSuccessStatusCode();
			Assert.Null(rpMsg);
			var userToken = await GetToken(user.email);
			Assert.NotNull(userToken);

			// Act (attempt to reset password with token)
			var newPassword = _faker.Internet.Password();
			var (cpResponse, cpMsg) = await HttpRequest.SendAsync<ResetForgottenUserPasswordRequestDTO, string>(
				_client,
				HttpMethod.Post,
				changeForgottenPassword,
				new ResetForgottenUserPasswordRequestDTO {token = userToken.token, password = newPassword}
			);

			// Assert (attempt to reset password with token)
			cpResponse.EnsureSuccessStatusCode();
			Assert.Null(cpMsg);

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

		// Note: I had implemented an email tester
		// https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format,
		// which rejected patterns that it should not have.
		// Unfortunately, this made some false rejections, so I removed it. Maybe we add something special
		// when we go to production, then we could use these tests again (perhaps https://emailregex.com/).
		/* 
        /// <summary>
        /// Make sure that the API immediately rejects improperly formatted email addresses.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ShouldNotCreateAccountWithBadEmail()
        {
            // Arrange
            var user = RandomUser();
            user.email = _faker.Random.Word(); // This will not be a valid email
            
            // Act
            await CreateUserAccountAsync(user, shouldSucceed: false); // Asserts in method
        }
        
        /// <summary>
        /// Make sure that the API immediately rejects improperly formatted email addresses when
        /// requesting update of unactivated account details.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ShouldNotUpdateUnactivatedAccountWithBadEmail()
        {
            // Arrange
            var user = RandomUser();
            var (_, newUser) = await CreateUserAccountAsync(user);
            var newEmail = _faker.Random.Word(); // This will not be a valid email
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<UnactivatedEmailUpdateRequestDTO, string>(_client,
                HttpMethod.Post, changeUnactivatedEmail, new UnactivatedEmailUpdateRequestDTO()
                {
                    email = user.email,
                    newEmail = newEmail
                });
            
            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            
            // Cleanup
            await CleanupUserAccountAsync(newUser);
        }
        
        /// <summary>
        /// Make sure that the API immediately rejects improperly formatted email addresses when
        /// requesting update of activated account details.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ShouldNotUpdateActivatedAccountWithBadEmail()
        {
            // Arrange
            var user = RandomUser();
            await CreateActivatedUserAsync(user);
            var newUser = await Login(user);
            var newEmail = _faker.Random.Word(); // This will not be a valid email
            
            // Act
            var (response, msg) = await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(_client,
                HttpMethod.Put, baseUrl, new NewUserRequestDTO(newEmail, user.password, user.organization, user.forename, user.surname), 
                newUser.token);
            
            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            
            // Cleanup
            await CleanupUserAccountAsync(newUser);
        }*/
	}
}