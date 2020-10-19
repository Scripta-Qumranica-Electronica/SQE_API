using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;
using Xunit;

namespace SQE.ApiTest.Helpers
{
	public class UserHelpers
	{
		private static uint _userCount;

		/// <summary>
		///  Create a user with random information.
		/// </summary>
		/// <param name="client">The HttpClient</param>
		/// <param name="password">The password for the newly created user.</param>
		/// <returns>Returns a DetailedUserDTO object describing the new user.</returns>
		public static async Task<UserDTO> CreateRandomUserAsync(HttpClient client, string password)
		{
			var user = new NewUserRequestDTO(
					$"sequential.user{_userCount}@fakeEmail.edu"
					, password
					, $"Company {_userCount}"
					, $"forename {_userCount}"
					, $"surname {_userCount}");

			var userAcctMsg = await CreateUserAccountAsync(client, user);

			await ActivateUserAccountAsync(client, userAcctMsg);
			_userCount++;

			return userAcctMsg;
		}

		/// <summary>
		///  Creates a user account with the specified details.
		/// </summary>
		/// <param name="client">The HttpClient</param>
		/// <param name="user">A NewUserRequestDTO object with the details of the new user to be created</param>
		/// <param name="shouldSucceed">Optional, whether the action should succeed</param>
		/// <returns>Returns a DetailedUserDTO for the newly created user.</returns>
		private static async Task<UserDTO> CreateUserAccountAsync(
				HttpClient          client
				, NewUserRequestDTO user
				, bool              shouldSucceed = true)
		{
			var userRequest = new Post.V1_Users(user);
			await userRequest.SendAsync(client);

			var (response, msg) = (userRequest.HttpResponseMessage, userRequest.HttpResponseObject);

			// Assert
			if (shouldSucceed)
			{
				response.EnsureSuccessStatusCode();
				Assert.Equal(user.email, msg.email);

				// Assert.Equal(user.forename, msg.forename);
				// Assert.Equal(user.surname, msg.surname);
				// Assert.Equal(user.organization, msg.organization);
			}
			else
				Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

			return msg;
		}

		/// <summary>
		///  Activates a newly created user account.
		/// </summary>
		/// <param name="client">The HttpClient</param>
		/// <param name="user">The DetailedUserDTO for the user whose account will be activated</param>
		/// <param name="shouldSucceed">Optional, whether the action should succeed</param>
		/// <returns>void</returns>
		private static async Task ActivateUserAccountAsync(
				HttpClient client
				, UserDTO  user
				, bool     shouldSucceed = true)
		{
			var userToken = await GetToken(user.email, "ACTIVATE_ACCOUNT"); // Get  token from DB

			var payload = new AccountActivationRequestDTO { token = userToken.token.ToString() };

			var activationRequest = new Post.V1_Users_ConfirmRegistration(payload);

			await activationRequest.SendAsync(client);
			var response = activationRequest.HttpResponseMessage;

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
				Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		/// <summary>
		///  We don't ever delete users from SQE, this function is used to cleanup testing users.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		private static async Task CleanupUserAccountAsync(UserDTO user, DatabaseQuery db)
		{
			const string deleteNewUserSql = "DELETE FROM user WHERE email = @Email";

			const string deleteUserRoleSql =
					"DELETE FROM users_system_roles WHERE user_id = @UserID";

			const string deleteUserDataSql =
					"DELETE FROM SQE.user_data_store WHERE user_id = @UserID";

			const string deleteEmailTokenSql =
					"DELETE FROM user_email_token WHERE user_id = @UserId";

			var deleteEmailTokenParams = new DynamicParameters();
			deleteEmailTokenParams.Add("@UserId", user.userId);
			deleteEmailTokenParams.Add("@Email", user.email);

			await db.RunExecuteAsync(deleteEmailTokenSql, deleteEmailTokenParams);

			await db.RunExecuteAsync(deleteUserRoleSql, deleteEmailTokenParams);

			await db.RunExecuteAsync(deleteUserDataSql, deleteEmailTokenParams);

			await db.RunExecuteAsync(deleteNewUserSql, deleteEmailTokenParams);
		}

		/// <summary>
		///  Returns the details of the user with the specified email address.
		/// </summary>
		/// <param name="email">The email address of the user to be found</param>
		/// <param name="shouldSucceed">Optional, whether the action should succeed</param>
		/// <returns>Returns a UserObj object with the users account details.</returns>
		private static async Task<UserObj> GetUserByEmail(string email, bool shouldSucceed = true)
		{
			var db = new DatabaseQuery();

			const string checkForUserSql = "SELECT * FROM user WHERE email = @Email";

			var checkForUserSqlParams = new DynamicParameters();
			checkForUserSqlParams.Add("@Email", email);

			if (shouldSucceed)
			{
				return await db.RunQuerySingleAsync<UserObj>(
						checkForUserSql
						, checkForUserSqlParams); // Get user from DB
			}

			var users =
					await db.RunQueryAsync<UserObj>(
							checkForUserSql
							, checkForUserSqlParams); // Get user from DB

			Assert.Empty(users);

			return new UserObj();
		}

		/// <summary>
		///  Returns a single token associated with the user's email address. Don't use this if you
		///  anticipate there will be more than one token
		/// </summary>
		/// <param name="email">Email address of the user whose tokens are being searched for.</param>
		/// <param name="type">The type of token to be searching for</param>
		/// <param name="shouldSucceed">Optional, whether the action should succeed</param>
		/// <returns></returns>
		private static async Task<Token> GetToken(
				string   email
				, string type
				, bool   shouldSucceed = true)
		{
			var db = new DatabaseQuery();

			const string getNewUserTokenSql = @"
SELECT token, date_created, type
FROM user_email_token
  JOIN user USING(user_id)
WHERE email = @Email AND type = @Type";

			var checkForUserSQLParams = new DynamicParameters();
			checkForUserSQLParams.Add("@Email", email);
			checkForUserSQLParams.Add("@Type", type);

			// Get tokens from DB
			var tokens = (await db.RunQueryAsync<Token>(getNewUserTokenSql, checkForUserSQLParams))
					.ToList();

			if (shouldSucceed)
			{
				Assert.NotEmpty(tokens);

				return tokens.First();
			}

			Assert.Empty(tokens);

			return new Token();
		}

		public class UserCreator : IDisposable
		{
			public UserCreator(
					NewUserRequestDTO newUser
					, HttpClient      client
					, DatabaseQuery   db
					, bool            activate = true)
			{
				NewUser = newUser;
				Client = client;
				Db = db;
				Activate = activate;
			}

			private UserDTO           User     { get; set; }
			private NewUserRequestDTO NewUser  { get; }
			private HttpClient        Client   { get; }
			private DatabaseQuery     Db       { get; }
			private bool              Activate { get; }

			public void Dispose()
			{
				// This seems to work properly even though it is an antipattern.
				// There is no async Dispose (Task.Run...Wait() is a hack) and it is supposed to be very short running anyway.
				// Maybe using try/finally in the individual tests would ultimately be safer.
				Task.Run<Task>(async () => await CleanupUserAccountAsync(User, Db)).Wait();
			}

			public async Task<UserDTO> CreateUser()
			{
				User = await CreateUserAccountAsync(Client, NewUser);

				if (Activate)
					await ActivateUserAccountAsync(Client, User);

				return User;
			}

			public void UpdateUserDetails(DetailedUserDTO updatedUser)
			{
				User = updatedUser;
			}
		}

		private class UserObj
		{
			public string email        { get; set; }
			public string password     { get; set; }
			public string forename     { get; set; }
			public string surname      { get; set; }
			public string organization { get; set; }
			public bool   activated    { get; set; }
			public uint   user_id      { get; set; }
		}

		private class Token
		{
			public Guid     token        { get; set; }
			public string   type         { get; set; }
			public DateTime date_created { get; set; }
		}
	}
}
