using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using SQE.API.DTO;
using Xunit;

namespace SQE.ApiTest.Helpers
{
    public class UserHelpers
    {
        private static uint userCount;

        /// <summary>
        ///     Create a user with random information.
        /// </summary>
        /// <param name="client">The HttpClient</param>
        /// <param name="password">The password for the newly created user.</param>
        /// <returns>Returns a DetailedUserDTO object describing the new user.</returns>
        public static async Task<DetailedUserDTO> CreateRandomUserAsync(HttpClient client, string password)
        {
            var user = new NewUserRequestDTO(
                $"sequential.user{userCount}@fakeEmail.edu",
                password,
                $"Company {userCount}",
                $"forename {userCount}",
                $"surname {userCount}"
            );

            var userAcctMsg = await CreateUserAccountAsync(client, user);

            await ActivateUserAccountAsync(client, userAcctMsg);
            userCount++;
            return userAcctMsg;
        }

        /// <summary>
        ///     Creates a user account with the specified details.
        /// </summary>
        /// <param name="client">The HttpClient</param>
        /// <param name="user">A NewUserRequestDTO object with the details of the new user to be created</param>
        /// <param name="shouldSucceed">Optional, whether the action should succeed</param>
        /// <returns>Returns a DetailedUserDTO for the newly created user.</returns>
        private static async Task<DetailedUserDTO> CreateUserAccountAsync(HttpClient client,
            NewUserRequestDTO user,
            bool shouldSucceed = true)
        {
            var (response, msg) = await HttpRequest.SendAsync<NewUserRequestDTO, DetailedUserDTO>(
                client,
                HttpMethod.Post,
                "/v1/users",
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

            return msg;
        }

        /// <summary>
        ///     Activates a newly created user account.
        /// </summary>
        /// <param name="client">The HttpClient</param>
        /// <param name="user">The DetailedUserDTO for the user whose account will be activated</param>
        /// <param name="shouldSucceed">Optional, whether the action should succeed</param>
        /// <returns>void</returns>
        private static async Task ActivateUserAccountAsync(HttpClient client,
            DetailedUserDTO user,
            bool shouldSucceed = true)
        {
            var userToken = await GetToken(user.email, "ACTIVATE_ACCOUNT"); // Get  token from DB
            var payload = new AccountActivationRequestDTO { token = userToken.token };

            var (response, msg) =
                await HttpRequest.SendAsync<AccountActivationRequestDTO, UserDTO>(
                    client,
                    HttpMethod.Post,
                    "/v1/users/confirm-registration",
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

        /// <summary>
        ///     Returns the details of the user with the specified email address.
        /// </summary>
        /// <param name="email">The email address of the user to be found</param>
        /// <param name="shouldSucceed">Optional, whether the action should succeed</param>
        /// <returns>Returns a UserObj object with the users account details.</returns>
        private static async Task<UserObj> GetUserByEmail(string email, bool shouldSucceed = true)
        {
            var db = new DatabaseQuery();
            const string checkForUserSQL = "SELECT * FROM user WHERE email = @Email";

            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", email);
            if (shouldSucceed)
                return await db.RunQuerySingleAsync<UserObj>(
                    checkForUserSQL,
                    checkForUserSQLParams
                ); // Get user from DB

            var users = await db.RunQueryAsync<UserObj>(checkForUserSQL, checkForUserSQLParams); // Get user from DB
            Assert.Empty(users);
            return new UserObj();
        }

        /// <summary>
        ///     Returns a single token associated with the user's email address. Don't use this if you
        ///     anticipate there will be more than one token
        /// </summary>
        /// <param name="email">Email address of the user whose tokens are being searched for.</param>
        /// <param name="type">The type of token to be searching for</param>
        /// <param name="shouldSucceed">Optional, whether the action should succeed</param>
        /// <returns></returns>
        private static async Task<Token> GetToken(string email, string type, bool shouldSucceed = true)
        {
            var db = new DatabaseQuery();
            const string getNewUserTokenSQL = @"
SELECT token, date_created, type 
FROM user_email_token 
  JOIN user USING(user_id) 
WHERE email = @Email AND type = @Type";

            var checkForUserSQLParams = new DynamicParameters();
            checkForUserSQLParams.Add("@Email", email);
            checkForUserSQLParams.Add("@Type", type);

            // Get tokens from DB
            var tokens = (await db.RunQueryAsync<Token>(getNewUserTokenSQL, checkForUserSQLParams)).ToList();

            if (shouldSucceed)
            {
                Assert.NotEmpty(tokens);
                return tokens.First();
            }

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
    }
}