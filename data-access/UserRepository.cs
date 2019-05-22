using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess
{
    public interface IUserRepository
    {
        // Get user data
        Task<UserLoginResponse> GetUserByPasswordAsync(string userName, string password);
        Task<DetailedUserWithToken> GetDetailedUserById(UserInfo userInfo);
        Task<DetailedUserWithToken> GetUnactivatedUserByEmailAsync(string email);
        Task<UserEditionPermissions> GetUserEditionPermissionsAsync(uint userId, uint editionId);
        
        // Create/update account data
        Task<UserToken> CreateNewUserAsync(string username, string email, string password, string forename = null,
            string surname = null, string organization = null);
        Task ResolveExistingUserConflictAsync(string username, string email);
        Task UpdateUserAsync(UserInfo user, string username, string email, bool resetActivation,
            string forename = null, string surname = null, string organization = null);
        Task<DetailedUserWithToken> CreateUserActivateTokenAsync(string username, string email);
        Task ConfirmAccountCreationAsync(string token);
        Task UpdateUnactivatedUserEmailAsync(string oldEmail, string newEmail);
        Task ChangePasswordAsync(UserInfo user, string oldPassword, string newPassword);
        Task<UserToken> RequestResetForgottenPasswordAsync(string email);
        Task<UserEmail> ResetForgottenPasswordAsync(string token, string password);
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
        public async Task<UserLoginResponse> GetUserByPasswordAsync(string userName, string password)
        {
            using (var connection = OpenConnection())
            {
                var columns = new List<string>() { "user_name", "user_id", "email", "activated" };
                var where = new List<string>() { "user_name", "pw" };
                return await connection.QuerySingleAsync<UserLoginResponse>(UserDetails.GetQuery(columns, where), 
                    new
                    {
                        UserName = userName,
                        Pw = password
                    });
            }
        }

        public async Task<DetailedUserWithToken> GetDetailedUserById(UserInfo userInfo)
        {
            using (var connection = OpenConnection())
            {
                var columns = new List<string>()
                    {"user_id", "user_name", "forename", "surname", "organization", "email"};
                var where = new List<string>() {"user_id"};
                return await connection.QuerySingleAsync<DetailedUserWithToken>(
                    UserDetails.GetQuery(columns, where), new
                    {
                        UserId = userInfo.userId ?? 0
                    });
            }
        }

        /// <summary>
        /// Gets user details for the unactivated account with the provided email address.  Do not use this unless you
        /// know what you are doing!
        /// </summary>
        /// <param name="email">Email address for the account details you want to retrieve</param>
        /// <returns></returns>
        public async Task<DetailedUserWithToken> GetUnactivatedUserByEmailAsync(string email)
        {
            using (var connection = OpenConnection())
            {
                try
                {
                    var columns = new List<string>() {"user_name", "user_id", "forename", "surname", "token", "email"};
                    var where = new List<string>() {"email", "activated"};
                    return await connection.QuerySingleAsync<DetailedUserWithToken>(
                        UserDetails.GetQuery(columns, where), new
                        {
                            Email = email,
                            Activated = 0
                        });
                }
                catch
                {
                    throw new DbDetailedFailedWrite("User account not found for email address.");
                }
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
        public async Task<UserToken> CreateNewUserAsync(string username, string email, string password, string forename = null, 
            string surname = null, string organization = null)
        {
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    // Find any users with either the same username of email address.
                    await ResolveExistingUserConflictAsync(username, email);

                    // Ok, the input username and email are unique so create the record
                    var newUser = await connection.ExecuteAsync(CreateNewUserQuery.GetQuery,
                        new
                        {
                            UserName = username,
                            Email = email,
                            Password = password,
                            Forename = forename,
                            Surname = surname,
                            Organization = organization
                        });
                    if (newUser != 1) // Something strange must have gone wrong
                        throw new DbFailedWrite();

                    // Everything went well, so create the email token so the
                    // calling function can email the new user.
                    var newUserObject = await CreateUserActivateTokenAsync(username, email);
                    transactionScope.Complete();
                    return newUserObject;
                }
            }
        }

        /// <summary>
        /// Note that this method may be destructive!  This method resolves any uniqueness constraints
        /// on a username or email. It will throw if an activated user account already exists with the
        /// username or email.  If an unactivated user account exists with the username or email, it
        /// will be overwritten.
        /// </summary>
        /// <param name="username">Username to check for uniqueness</param>
        /// <param name="email">Email to check for uniqueness</param>
        /// <returns></returns>
        /// <exception cref="DbDetailedFailedWrite">Returns details about whether the username and/or email
        /// is already in use.</exception>
        public async Task ResolveExistingUserConflictAsync(string username, string email)
        {
            using (var connection = OpenConnection())
            {
                // Find any users with either the same username OR email address.
                var columns = new List<string>() { "user_id", "activated", "user_name", "email" };
                var where = new List<string>() { "user_name", "email"};
                var existingUser = (await connection.QueryAsync<UserLoginResponse>(
                    UserDetails.GetQuery(columns, where, false),
                    new
                    {
                        UserName = username,
                        Email = email
                    })).ToList();

                // Check if we need to send error because username and/or email already exist.
                if (existingUser.Any())
                {
                    var errors = new List<string>();
                    foreach (var record in existingUser)
                    {
                        if (record.UserName == username) // First check for duplicate username
                        {
                            if (record.Activated) // If this user record has been authenticated add info to error
                                errors.Add("username");
                            else // else delete the unauthenticated user record (that user should have been faster to authenticate!)
                            {
                                await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetUserIdQuery,
                                    new {UserId = record.UserId});
                                await connection.ExecuteAsync(DeleteUserQuery.GetQuery,
                                    new {UserId = record.UserId});
                            }
                        }

                        if (record.Email == email) // Then check for duplicate email
                        {
                            if (record.Activated) // If this user record has been authenticated add info to error
                                errors.Add("email address");
                            else // else delete the unauthenticated user record (that user should have been faster to authenticate!)
                            {
                                await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetUserIdQuery,
                                    new {UserId = record.UserId});
                                await connection.ExecuteAsync(DeleteUserQuery.GetQuery,
                                    new {UserId = record.UserId});
                            }
                        }
                    }

                    if (errors.Any()) // if we have any errors here, throw them back to the request
                        throw new DbDetailedFailedWrite(
                            $"The {string.Join(" and ", errors)} {(errors.Count() == 1 ? "is" : "are")} in use by another account.");
                }
            }
        }

        /// <summary>
        /// Updates the info for an existing user.  This cannot be used to reset a password, use ChangePasswordAsync
        /// instead. You should probably have run ResolveExistingUserConflict before attempting this.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="username">Username for the new account</param>
        /// <param name="email">Email address for the new account (it will be verified)</param>
        /// <param name="forename">Optional given name</param>
        /// <param name="surname">Optional family name</param>
        /// <param name="organization">Optional organizational affiliation</param>
        /// <returns>Returns a User object with the details of the newly created user. This object contains
        /// the secret confirmation token that should be emailed to the user and then likely stripped
        /// from the User object, which can be returned as a DTO to the HTTP request.</returns>
        /// <exception cref="DbFailedWrite">Some unexpected database write error has occured.</exception>
        public async Task UpdateUserAsync(UserInfo user, string username, string email, bool resetActivation,
            string forename = null, string surname = null, string organization = null)
        {
            using (var connection = OpenConnection())
            {
                var userUpdate = await connection.ExecuteAsync(UpdateUserInfo.GetQuery(resetActivation), new
                {
                    UserName = username, 
                    Email = email, 
                    Forename = forename, 
                    Surname = surname, 
                    Organization = organization,
                    UserId = user.userId
                });
                
                if (userUpdate != 1) // Something went wrong, probably you did not run ResolveExistingUserConflict first
                    throw new DbFailedWrite();
            }
        }

        /// <summary>
        /// Generates an activation token for the user account in the database.  This only works
        /// if the account is not yet activated.
        /// </summary>
        /// <param name="username">Username of the unactivated account</param>
        /// <param name="email">Email address of the unactivated account</param>
        /// <returns>User details for the account with the activation token</returns>
        /// <exception cref="DbDetailedFailedWrite">Reason for token creation failure</exception>
        public async Task<DetailedUserWithToken> CreateUserActivateTokenAsync(string username, string email)
        {
            using (var connection = OpenConnection())
            {
                try
                {
                    // Confirm creation by getting the User object for the new user
                    var columns = new List<string>() {"user_id", "user_name", "forename", "surname", "organization"};
                    var where = new List<string>() {"user_name", "email"};
                    var userObject = await connection.QuerySingleAsync<DetailedUserWithToken>(
                        UserDetails.GetQuery(columns, where),
                        new
                        {
                            UserName = username,
                            Email = email,
                        });
                    
                    // Generate our secret token
                    var token = Guid.NewGuid().ToString();
                    // Add the secret token to the database
                    var userEmailConfirmation = await connection.ExecuteAsync(
                        CreateUserEmailTokenQuery.GetQuery,
                        new
                        {
                            UserId = userObject.UserId,
                            Token = token,
                            Type = CreateUserEmailTokenQuery.Activate
                        });
                    if (userEmailConfirmation != 1) // Something strange must have gone wrong
                        throw new DbDetailedFailedWrite("Could not create activation token for user account.");

                    // Everything went well, so add the token to the User object so the calling function
                    // can email the new user.
                    userObject.Token = token;
                    return userObject;
                }
                catch
                {
                    throw new DbDetailedFailedWrite("Could not confirm that the new account was created.");
                }  
            }
        }

        /// <summary>
        /// Activates user account based on the secret token sent to the new user's email address.
        /// </summary>
        /// <param name="token">Secret token for user authentication</param>
        /// <returns></returns>
        /// <exception cref="DbDetailedFailedWrite"></exception>
        public async Task ConfirmAccountCreationAsync(string token)
        {
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    var confirmRegistration = await connection.ExecuteAsync(ConfirmNewUserAccount.GetQuery, new
                        { Token = token});
                    if (confirmRegistration != 1)
                        throw new DbDetailedFailedWrite("Could not activate the account. The token probably does not exist or is out of date.");
                    var deleteToken = await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetTokenQuery, new
                        { Token = token});
                    transactionScope.Complete();
                }
            }
        }

        /// <summary>
        /// Updates the email address for an account that has not yet been activated.
        /// </summary>
        /// <param name="oldEmail">Email address that was originally entered when creating the account</param>
        /// <param name="newEmail">New email address to use for the account</param>
        /// <returns></returns>
        /// <exception cref="DbDetailedFailedWrite"></exception>
        public async Task UpdateUnactivatedUserEmailAsync(string oldEmail, string newEmail)
        {
            using (var connection = OpenConnection())
            {
                var newEmailEntry = await connection.ExecuteAsync(ChangeUnactivatedUserEmail.GetQuery, new
                {
                    OldEmail = oldEmail,
                    NewEmail = newEmail
                });
                if (newEmailEntry != 1)
                    throw new DbDetailedFailedWrite("Failed to update the email address. The new email address is probably in use by another user.");
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
        public async Task<UserToken> RequestResetForgottenPasswordAsync(string email)
        {
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    try
                    {
                        // Get the user's details via the submitted email address
                        var columns = new List<string>() { "user_name", "user_id" };
                        var where = new List<string>() { "email" };
                        var userInfo = await connection.QuerySingleAsync<UserToken>(
                            UserDetails.GetQuery(columns, where), 
                            new { Email = email});
                        
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
        public async Task<UserEmail> ResetForgottenPasswordAsync(string token, string password)
        {
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    UserEmail userInfo;
                    try
                    {
                        userInfo = await connection.QuerySingleAsync<UserEmail>(UserByTokenQuery.GetQuery, new
                        {
                            Token = token
                        });
                    }
                    catch
                    {
                        throw new DbDetailedFailedWrite("The submitted token is incorrect or out of date.");
                    }

                    var resetPassword = await connection.ExecuteAsync(UpdatePasswordByToken.GetQuery, new
                        {
                            Token = token,
                            Password = password
                        });
                    if (resetPassword != 1)
                        throw new DbDetailedFailedWrite("Failed to change the password. Perhaps the token went out of date.");

                    await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetTokenQuery, new
                    {
                        Token = token
                    });
                    
                    transactionScope.Complete(); // Close the transaction
                    return userInfo;
                }
            }
        }
    }
}
