using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess
{
    public interface IUserRepository
    {
        Task<User> GetUserByPasswordAsync(string userName, string password);
        Task<UserEditionPermissions> GetUserEditionPermissionsAsync(uint userId, uint editionId);
        Task<User> CreateNewUserAsync(string username, string email, string password, string forename = null,
            string surname = null, string organization = null);

        Task ConfirmAccountCreationAsync(string token);
        Task ChangePasswordAsync(UserInfo user, string oldPassword, string newPassword);
        Task<User> RequestResetForgottenPasswordAsync(string email);
        Task ResetForgottenPasswordAsync(string token, string password);
    }
    public class UserRepository: DbConnectionBase , IUserRepository
    {
        private readonly int _tokenValidDays;

        public UserRepository(IConfiguration config) : base(config)
        {
            if (!int.TryParse(config.GetSection("AppSettings")?["EmailTokenDaysValid"], out _tokenValidDays))
                _tokenValidDays = 2; // Set a default of 2 days to expiration if no explicit setting found in AppSettings.
        }

        /// <summary>
        /// Returns user information based on the submitted credentials.
        /// </summary>
        /// <param name="userName">The user's username</param>
        /// <param name="password">The user's password</param>
        /// <returns></returns>
        public async Task<User> GetUserByPasswordAsync(string userName, string password)
        {
            var sql = @"SELECT user_name, user_id FROM user WHERE user_name = @UserName AND pw = SHA2(@Password, 224) AND authenticated = 1";
            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<UserQueryResponse>(sql, new
                {
                    UserName = userName,
                    Password = password
                });

                var firstUser = results.FirstOrDefault();
                return firstUser?.CreateModel();
            }
        }
        
        /// <summary>
        /// Retrieves the current users permissions for a specific edition.
        /// </summary>
        /// <param name="userId">The users id</param>
        /// <param name="editionId">The id of the edition the user is accessing</param>
        /// <returns>Returns the user's rights to read, write, and admin the edition and the users editor id for the edition</returns>
        public async Task<UserEditionPermissions> GetUserEditionPermissionsAsync(uint userId, uint editionId)
        {
            using (var connection = OpenConnection())
            {
                var results = await connection.QuerySingleAsync<UserEditionPermissions>(UserPermissionQuery.GetQuery, new
                {
                    EditionId = editionId,
                    UserId = userId,
                });
                return results;
            }
        }

        /// <summary>
        /// Create a new user in the database and create the email token record.  This method checks
        /// for any conflicts with existing usernames and emails, and it will respond accordingly.
        /// </summary>
        /// <param name="username">Username for the new account</param>
        /// <param name="email">Email address for the new account (it will be verified)</param>
        /// <param name="password">Password for the new account (it is hashed in the database)</param>
        /// <param name="forename">Optional given name</param>
        /// <param name="surname">Optional family name</param>
        /// <param name="organization">Optional organizational affiliation</param>
        /// <returns>Returns a User object with the details of the newly created user. This object contains
        /// the secret confirmation token that should be emailed to the user and then likely stripped
        /// from the User object, which can be returned as a DTO to the HTTP request.</returns>
        /// <exception cref="DbDetailedFailedWrite">The username or email is already in use by an authenticated user.</exception>
        /// <exception cref="DbFailedWrite">Some unexpected database write error has occured.</exception>
        public async Task<User> CreateNewUserAsync(string username, string email, string password, string forename = null, 
            string surname = null, string organization = null)
        {
            await CleanOldTokens(); // Always flush the expired tokens from the database first
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    // Find any users with either the same username of email address.
                    var existingUser = (await connection.QueryAsync<CheckUserAuthentication.Return>(
                        CheckUserAuthentication.GetQuery,
                        new
                        {
                            Username = username,
                            Email = email
                        })).ToList();

                    // Check if we need to send error because username and/or email already exist.
                    if (existingUser.Any())
                    {
                        var errors = new List<string>();
                        foreach (var record in existingUser)
                        {
                            if (record.user_name == username) // First check for duplicate username
                            {
                                if (record.authenticated) // If this user record has been authenticated add info to error
                                    errors.Add("username");
                                else // else delete the unauthenticated user record (that user should have been faster to authenticate!)
                                {
                                    await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetUserIdQuery,
                                        new {UserId = record.user_id});
                                    await connection.ExecuteAsync(DeleteUserQuery.GetQuery,
                                        new {UserId = record.user_id});
                                }
                            }

                            if (record.email == email) // Then check for duplicate email
                            {
                                if (record.authenticated) // If this user record has been authenticated add info to error
                                    errors.Add("email");
                                else // else delete the unauthenticated user record (that user should have been faster to authenticate!)
                                {
                                    await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetUserIdQuery,
                                        new {UserId = record.user_id});
                                    await connection.ExecuteAsync(DeleteUserQuery.GetQuery,
                                        new {UserId = record.user_id});
                                }
                            }
                        }

                        if (errors.Any()) // if we have any errors here, throw them back to the request
                            throw new DbDetailedFailedWrite(
                                $"The {string.Join(" and ", errors)} already exist{(errors.Count() == 1 ? "s" : "")}.");
                    }

                    // Ok, the input username and email are unique so create the record
                    var newUser = await connection.ExecuteAsync(CreateNewUserQuery.GetQuery,
                        new
                        {
                            Username = username,
                            Email = email,
                            Password = password,
                            Forename = forename,
                            Surname = surname,
                            Organization = organization
                        });
                    if (newUser != 1) // Something strange must have gone wrong
                        throw new DbFailedWrite();

                    // Confirm creation by getting the User object for the new user
                    var newUserObject = await connection.QuerySingleAsync<User>(
                        ConfirmUserCreateQuery.GetQuery,
                        new
                        {
                            Username = username,
                            Email = email,
                            Password = password,
                        });

                    // Generate our secret token
                    var token = Guid.NewGuid().ToString();
                    // Add the secret token to the database
                    var userEmailConfirmation = await connection.ExecuteAsync(
                        CreateUserEmailTokenQuery.GetQuery,
                        new
                        {
                            UserId = newUserObject.UserId,
                            Token = token,
                            Type = CreateUserEmailTokenQuery.Activate
                        });
                    if (userEmailConfirmation != 1) // Something strange must have gone wrong
                        throw new DbFailedWrite();

                    // Everything went well, so add the token to the User object so the calling function
                    // can email the new user.
                    newUserObject.Token = token;
                    transactionScope.Complete();
                    return newUserObject;
                }
            }
        }

        /// <summary>
        /// Writes confirmation of user account based on the secret token sent to the new user's email address.
        /// </summary>
        /// <param name="token">Secret token for user authentication</param>
        /// <returns></returns>
        /// <exception cref="DbDetailedFailedWrite"></exception>
        /// <exception cref="DbFailedWrite"></exception>
        public async Task ConfirmAccountCreationAsync(string token)
        {
            await CleanOldTokens(); // Always flush the expired tokens from the database first
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    var confirmRegistration = await connection.ExecuteAsync(ConfirmNewUserAccount.GetQuery, new
                        { Token = token});
                    if (confirmRegistration != 1)
                        throw new DbDetailedFailedWrite("Could not authenticate account.");
                    var deleteToken = await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetTokenQuery, new
                        { Token = token});
                    if (deleteToken != 1)
                        throw new DbDetailedFailedWrite("Could not delete token.");
                    transactionScope.Complete();
                }
            }
        }

        /// <summary>
        /// Change password from old password to new password for the user's account.
        /// </summary>
        /// <param name="user">User object with all information for the user requesting a password change</param>
        /// <param name="oldPassword">The old password for the user's account</param>
        /// <param name="newPassword">The new password for the user's account</param>
        /// <returns></returns>
        /// <exception cref="DbFailedWrite"></exception>
        public async Task ChangePasswordAsync(UserInfo user, string oldPassword, string newPassword)
        {
            using (var connection = OpenConnection())
            {
                var changePassword = await connection.ExecuteAsync(ChangePasswordQuery.GetQuery, new
                {
                    UserId = user.userId,
                    OldPassword = oldPassword,
                    NewPassword = newPassword
                });
                
                if (changePassword != 1)
                    throw new DbFailedWrite();
            }
        }
        
        /// <summary>
        /// Creates a request in the database for a reset password token.
        /// </summary>
        /// <param name="email">Email address of the user who has forgotten the password</param>
        /// <returns>Returns user information and secret token that are used to format the reset password email</returns>
        public async Task<User> RequestResetForgottenPasswordAsync(string email)
        {
            await CleanOldTokens(); // Always flush the expired tokens from the database first
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    try
                    {
                        // Get the user's details via the submitted email address
                        var userInfo = await connection.QuerySingleAsync<User>(UserByEmailQuery.GetQuery, new
                            { Email = email});
                        
                        // Generate our secret token
                        var token = Guid.NewGuid().ToString();
                        
                        // Write the token to the database
                        var tokenEntry = await connection.ExecuteAsync(CreateUserEmailTokenQuery.GetQuery, new
                        {
                            Token = token,
                            UserId = userInfo.UserId,
                            Type = CreateUserEmailTokenQuery.ResetPassword
                        });
                        if (tokenEntry != 1)
                            return null;
                        
                        // Cleanup
                        transactionScope.Complete();
                        
                        // Pass the token back in the user info object
                        userInfo.Token = token;
                        return userInfo;
                    }
                    catch{} // Suppress errors here. We don't want to risk people fishing for valid email addresses,
                            // though any errors are suppressed in the controller too.
                }
            }
            return null;
        }
        
        /// <summary>
        /// Resets a user's password based on the secret reset password token sent to the user's email address
        /// </summary>
        /// <param name="token">Secret token for resetting password</param>
        /// <param name="password">New password for the user's account</param>
        /// <returns></returns>
        /// <exception cref="DbFailedWrite"></exception>
        public async Task ResetForgottenPasswordAsync(string token, string password)
        {
            await CleanOldTokens(); // Always flush the expired tokens from the database first
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    var resetPassword = await connection.ExecuteAsync(UpdatePasswordByToken.GetQuery, new
                    {
                        Token = token,
                        Password = password
                    });
                    if (resetPassword != 1)
                        throw new DbFailedWrite();
                    var cleanTokenTable = await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetTokenQuery, new
                    {
                        Token = token
                    });
                    if (cleanTokenTable != 1)
                        throw new DbFailedWrite();
                    transactionScope.Complete(); // Close the transaction
                }
            }
        }

        /// <summary>
        /// Clears out old tokens, but see the comments to DeleteOldTokens.
        /// </summary>
        /// <returns></returns>
        private async Task CleanOldTokens()
        {
            using (var connection = OpenConnection())
            {
                await connection.ExecuteAsync(DeleteOldTokens.GetQuery, new { Days = _tokenValidDays });
            }
        }
    }
}
