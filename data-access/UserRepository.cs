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
        Task<DetailedUserWithToken> GetUserByPasswordAsync(string email, string password);
        Task<DetailedUserWithToken> GetDetailedUserByIdAsync(UserInfo userInfo);
        Task<DetailedUser> GetDetailedUserByTokenAsync(string token);
        Task<DetailedUserWithToken> GetUnactivatedUserByEmailAsync(string email);
        Task<UserEditionPermissions> GetUserEditionPermissionsAsync(UserInfo user);
        Task<List<EditorInfo>> GetEditionEditorsAsync(uint editionId);
        
        // Create/update account data
        Task<DetailedUserWithToken> CreateNewUserAsync(string email, string password, string forename = null,
            string surname = null, string organization = null);
        Task ResolveExistingUserConflictAsync(string email);
        Task UpdateUserAsync(UserInfo user, string password, string email, bool resetActivation,
            string forename = null, string surname = null, string organization = null);
        Task<DetailedUserWithToken> CreateUserActivateTokenAsync(string email);
        Task ConfirmAccountCreationAsync(string token);
        Task UpdateUnactivatedUserEmailAsync(string oldEmail, string newEmail);
        Task ChangePasswordAsync(UserInfo user, string oldPassword, string newPassword);
        Task<DetailedUserWithToken> RequestResetForgottenPasswordAsync(string email);
        Task<DetailedUser> ResetForgottenPasswordAsync(string token, string password);
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
        /// <param name="email"></param>
        /// <param name="password">The user's password</param>
        /// <returns></returns>
        public async Task<DetailedUserWithToken> GetUserByPasswordAsync(string email, string password)
        {
            using (var connection = OpenConnection())
            {
                var columns = new List<string>() { "user_id", "email", "activated", "forename", "surname", "organization" };
                var where = new List<string>() { "email", "pw" };
                try
                {
                    return await connection.QuerySingleAsync<DetailedUserWithToken>(UserDetails.GetQuery(columns, where), 
                        new
                        {
                            Email = email,
                            Pw = password
                        });
                }
                catch (InvalidOperationException)
                {
                    throw new StandardErrors.BadLogin(email);
                }
            }
        }

        public async Task<DetailedUserWithToken> GetDetailedUserByIdAsync(UserInfo userInfo)
        {
            using (var connection = OpenConnection())
            {
                var columns = new List<string>()
                    {"user_id", "email", "forename", "surname", "organization", "activated"};
                var where = new List<string>() {"user_id"};
                return await connection.QuerySingleAsync<DetailedUserWithToken>(
                    UserDetails.GetQuery(columns, where), new
                    {
                        UserId = userInfo.userId ?? 0
                    });
            }
        }

        public async Task<DetailedUser> GetDetailedUserByTokenAsync(string token)
        {
            using (var connection = OpenConnection())
            {
                try
                {
                    return await connection.QuerySingleAsync<DetailedUser>(UserByTokenQuery.GetQuery,
                        new {
                            Token = token
                        });
                }
                catch (InvalidOperationException)
                {
                    throw new StandardErrors.DataNotFound("user", token, "token");
                }
                
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
                // Generate our new secret token (if token already exists, this will update that token's date)
                var token = Guid.NewGuid().ToString();
                await connection.ExecuteAsync(CreateUserEmailTokenQuery.GetQuery(emailOnly: true), new
                {
                    Email = email,
                    Token = token,
                    Type = CreateUserEmailTokenQuery.Activate,
                });
                
                // Prepare account details request
                var columns = new List<string>() {"email", "user_id", "forename", "surname", "token"};
                var where = new List<string>() {"email", "activated"};
                
                try
                {
                    return await connection.QuerySingleAsync<DetailedUserWithToken>(
                        UserDetails.GetQuery(columns, where), new
                        {
                            Email = email,
                            Activated = 0
                        });
                }
                catch (InvalidOperationException)
                {
                    throw new StandardErrors.DataNotFound("user", email, email);
                }
            }
        }

        /// <summary>
        /// Retrieves the current users permissions for a specific edition.
        /// </summary>
        /// <param name="user"></param>
        /// <returns>Returns the user's rights to read, write, and admin the edition and the users editor id for the edition</returns>
        public async Task<UserEditionPermissions> GetUserEditionPermissionsAsync(UserInfo user)
        {
            using (var connection = OpenConnection())
            {
                try
                {
                    var results = await connection.QuerySingleAsync<UserEditionPermissions>(UserPermissionQuery.GetQuery, new
                    {
                        EditionId = user.editionId,
                        UserId = user.userId,
                    });
                    return results;
                }
                catch (InvalidOperationException)
                {
                    throw new StandardErrors.NoPermissions(user);
                }
            }
        }

        /// <summary>
        /// Create a new user in the database and create the email token record.  This method checks
        /// for any conflicts with existing emails, and it will respond accordingly.
        /// </summary>
        /// <param name="email">Email address for the new account (it will be verified)</param>
        /// <param name="password">Password for the new account (it is hashed in the database)</param>
        /// <param name="forename">Optional given name</param>
        /// <param name="surname">Optional family name</param>
        /// <param name="organization">Optional organizational affiliation</param>
        /// <returns>Returns a User object with the details of the newly created user. This object contains
        /// the secret confirmation token that should be emailed to the user and then likely stripped
        /// from the User object, which can be returned as a DTO to the HTTP request.</returns>
        public async Task<DetailedUserWithToken> CreateNewUserAsync(string email, string password,
            string forename = null, string surname = null, string organization = null)
        {
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    // Find any users with either the same  email address.
                    await ResolveExistingUserConflictAsync(email);

                    // Ok, the input email is unique so create the record
                    var newUser = await connection.ExecuteAsync(CreateNewUserQuery.GetQuery,
                        new
                        {
                            Email = email,
                            Password = password,
                            Forename = forename,
                            Surname = surname,
                            Organization = organization
                        });
                    if (newUser != 1) // Something strange must have gone wrong
                        throw new StandardErrors.DataNotWritten("create user");

                    // Everything went well, so create the email token so the
                    // calling function can email the new user.
                    var newUserObject = await CreateUserActivateTokenAsync(email);
                    transactionScope.Complete();
                    return newUserObject;
                }
            }
        }

        /// <summary>
        /// Note that this method may be destructive!  This method resolves any uniqueness constraints
        /// on an email. It will throw if an activated user account already exists with the
        /// email.  If an unactivated user account exists with the email, it will be overwritten.
        /// </summary>
        /// <param name="email">Email to check for uniqueness</param>
        /// <returns></returns>
        public async Task ResolveExistingUserConflictAsync(string email)
        {
            using (var connection = OpenConnection())
            {
                // Find any users with either the same email address.
                var columns = new List<string>() { "user_id", "activated", "email" };
                var where = new List<string>() { "email"};
                var existingUser = (await connection.QueryAsync<DetailedUserWithToken>(
                    UserDetails.GetQuery(columns, where),
                    new
                    {
                        Email = email
                    })).ToList();

                // Check if we need to send error because email already exist.
                if (existingUser.Any())
                {
                    foreach (var record in existingUser)
                    {
                        if (record.Activated) // If this user record has been authenticated throw a conflict error
                            throw new StandardErrors.ConflictingData("email");
                            
                        await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetUserIdQuery,
                            new {UserId = record.UserId});
                        await connection.ExecuteAsync(DeleteUserQuery.GetQuery,
                            new {UserId = record.UserId});
                    }
                }
            }
        }

        /// <summary>
        /// Updates the info for an existing user.  This cannot be used to reset a password, use ChangePasswordAsync
        /// instead. You should probably have run ResolveExistingUserConflict before attempting this.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="email">Email address for the new account (it will be verified)</param>
        /// <param name="resetActivation"></param>
        /// <param name="forename">Optional given name</param>
        /// <param name="surname">Optional family name</param>
        /// <param name="organization">Optional organizational affiliation</param>
        /// <returns>Returns a User object with the details of the newly created user. This object contains
        /// the secret confirmation token that should be emailed to the user and then likely stripped
        /// from the User object, which can be returned as a DTO to the HTTP request.</returns>
        public async Task UpdateUserAsync(UserInfo user, string password, string email, bool resetActivation,
            string forename = null, string surname = null, string organization = null)
        {
            using (var connection = OpenConnection())
            {
                if (resetActivation) // If email is new, make sure it is unique
                    await ResolveExistingUserConflictAsync(email);
                
                var userUpdate = await connection.ExecuteAsync(UpdateUserInfo.GetQuery(resetActivation), new
                { 
                    Pw = password,
                    Email = email, 
                    Forename = forename, 
                    Surname = surname, 
                    Organization = organization,
                    UserId = user.userId
                });

                if (userUpdate != 1) // The password was wrong
                    throw new StandardErrors.WrongPassword();
            }
        }

        /// <summary>
        /// Generates an activation token for the user account in the database.  This only works
        /// if the account is not yet activated.
        /// </summary>
        /// <param name="email">Email address of the unactivated account</param>
        /// <returns>User details for the account with the activation token</returns>
        public async Task<DetailedUserWithToken> CreateUserActivateTokenAsync(string email)
        {
            using (var connection = OpenConnection())
            {
                // Confirm creation by getting the User object for the new user
                var columns = new List<string>() {"user_id", "email", "forename", "surname", "organization"};
                var where = new List<string>() {"email"};
                var userObject = await connection.QuerySingleAsync<DetailedUserWithToken>(
                    UserDetails.GetQuery(columns, where),
                    new
                    {
                        Email = email,
                    });
                
                // Generate our secret token
                var token = Guid.NewGuid().ToString();
                // Add the secret token to the database
                var userEmailConfirmation = await connection.ExecuteAsync(
                    CreateUserEmailTokenQuery.GetQuery(),
                    new
                    {
                        UserId = userObject.UserId,
                        Token = token,
                        Type = CreateUserEmailTokenQuery.Activate
                    });
                if (userEmailConfirmation != 1) // Something strange must have gone wrong
                    throw new StandardErrors.DataNotWritten("create confirmation token");

                // Everything went well, so add the token to the User object so the calling function
                // can email the new user.
                userObject.Token = token;
                return userObject;
            }
        }

        /// <summary>
        /// Activates user account based on the secret token sent to the new user's email address.
        /// </summary>
        /// <param name="token">Secret token for user authentication</param>
        /// <returns></returns>
        public async Task ConfirmAccountCreationAsync(string token)
        {
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    var confirmRegistration = await connection.ExecuteAsync(ConfirmNewUserAccount.GetQuery, new
                        { Token = token});
                    if (confirmRegistration != 1)
                        throw new StandardErrors.ImproperInputData("user account activation token");
                    await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetTokenQuery, new
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
        public async Task UpdateUnactivatedUserEmailAsync(string oldEmail, string newEmail)
        {
            using (var connection = OpenConnection())
            {
                await ResolveExistingUserConflictAsync(newEmail);
                var newEmailEntry = await connection.ExecuteAsync(ChangeUnactivatedUserEmail.GetQuery, new
                    {
                        OldEmail = oldEmail,
                        NewEmail = newEmail
                    });
                if (newEmailEntry != 1)
                    throw new StandardErrors.DataNotWritten("update email");
            }
        }

        /// <summary>
        /// Change password from old password to new password for the user's account.
        /// </summary>
        /// <param name="user">User object with all information for the user requesting a password change</param>
        /// <param name="oldPassword">The old password for the user's account</param>
        /// <param name="newPassword">The new password for the user's account</param>
        /// <returns></returns>
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
                    throw new StandardErrors.WrongPassword();
            }
        }
        
        /// <summary>
        /// Creates a request in the database for a reset password token.
        /// </summary>
        /// <param name="email">Email address of the user who has forgotten the password</param>
        /// <returns>Returns user information and secret token that are used to format the reset password email</returns>
        public async Task<DetailedUserWithToken> RequestResetForgottenPasswordAsync(string email)
        {
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    try
                    {
                        // Get the user's details via the submitted email address
                        var columns = new List<string>() { "email", "user_id", "forename", "surname", "organization" };
                        var where = new List<string>() { "email" };
                        var userInfo = await connection.QuerySingleAsync<DetailedUserWithToken>(
                            UserDetails.GetQuery(columns, where), 
                            new { Email = email});
                        
                        // Generate our secret token
                        var token = Guid.NewGuid().ToString();
                        
                        // Write the token to the database
                        var tokenEntry = await connection.ExecuteAsync(CreateUserEmailTokenQuery.GetQuery(), new
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
        public async Task<DetailedUser> ResetForgottenPasswordAsync(string token, string password)
        {
            using (var transactionScope = new TransactionScope())
            {
                using (var connection = OpenConnection())
                {
                    DetailedUser detailedUserInfo = await GetDetailedUserByTokenAsync(token);

                    var resetPassword = await connection.ExecuteAsync(UpdatePasswordByToken.GetQuery, new
                        {
                            Token = token,
                            Password = password
                        });
                    if (resetPassword != 1)
                        throw new StandardErrors.DataNotWritten("reset password");

                    await connection.ExecuteAsync(DeleteUserEmailTokenQuery.GetTokenQuery, new
                    {
                        Token = token
                    });
                    
                    transactionScope.Complete(); // Close the transaction
                    return detailedUserInfo;
                }
            }
        }

        public async Task<List<EditorInfo>> GetEditionEditorsAsync(uint editionId)
        {
            using (var connection = OpenConnection())
            {
                return (await connection.QueryAsync<EditorInfo>(GetEditorInfo.GetQuery, 
                    new {EditionId = editionId})).ToList();
            }
        }
    }
}
