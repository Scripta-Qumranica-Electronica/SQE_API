using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.DatabaseAccess.Queries;

namespace SQE.DatabaseAccess;

public interface IUserRepository
{
	// Get user data
	Task<DetailedUserWithToken> GetUserByPasswordAsync(string email, string password);

	Task<DetailedUserWithToken> GetDetailedUserByIdAsync(uint?     userId);
	Task<DetailedUser>          GetDetailedUserByTokenAsync(string token);

	Task<DetailedUserWithToken> GetUnactivatedUserByEmailAsync(string email);

	Task<UserEditionPermissions> GetUserEditionPermissionsAsync(UserInfo editionUser);

	Task<List<UserSystemRoles>> GetUserSystemRolesAsync(UserInfo editionUser);

	Task<List<EditorInfo>> GetEditionEditorsAsync(uint editionId);

	// Create/update account data
	Task<DetailedUserWithToken> CreateNewUserAsync(
			string          email
			, string        password
			, string        forename     = null
			, string        surname      = null
			, string        organization = null
			, IDbConnection connection   = null);

	Task ResolveExistingUserConflictAsync(string email);

	Task UpdateUserAsync(
			uint            userId
			, string        password
			, string        email
			, bool          resetActivation
			, string        forename     = null
			, string        surname      = null
			, string        organization = null
			, IDbConnection connection   = null);

	Task<DetailedUserWithToken> CreateUserActivateTokenAsync(string    email);
	Task                        ConfirmAccountCreationAsync(string     token);
	Task                        UpdateUnactivatedUserEmailAsync(string oldEmail, string newEmail);

	Task ChangePasswordAsync(uint userId, string oldPassword, string newPassword);

	Task<DetailedUserWithToken> RequestResetForgottenPasswordAsync(string email);

	Task<DetailedUser> ResetForgottenPasswordAsync(string token, string password);

	Task<string>          GetUserDataStoreAsync(uint       userId);
	Task                  SetUserDataStoreAsync(uint       userId, string data);
	Task<DatabaseVersion> GetDatabaseVersion(IDbConnection connection = null);
}

public class UserRepository(IDatabaseAccessor dba) : IUserRepository
{
	/// <summary>
	///  Returns user information based on the submitted credentials.
	/// </summary>
	/// <param name="email"></param>
	/// <param name="password">The user's password</param>
	/// <returns></returns>
	public async Task<DetailedUserWithToken> GetUserByPasswordAsync(string email, string password)
	{
		var columns = new List<string>
		{
				"user_id"
				, "email"
				, "activated"
				, "forename"
				, "surname"
				, "organization"
				,
		};

		var where = new List<string> { "email", "pw" };

		try
		{
			return await dba.QuerySingleAsync<DetailedUserWithToken>(
					UserDetails.GetQuery(columns, where)
					, new { Email = email, Pw = password });
		}
		catch (InvalidOperationException)
		{
			throw new StandardExceptions.BadLoginException(email);
		}
	}

	public async Task<DetailedUserWithToken> GetDetailedUserByIdAsync(uint? userId)
	{
		var columns = new List<string>
		{
				"user_id"
				, "email"
				, "forename"
				, "surname"
				, "organization"
				, "activated"
				,
		};

		var where = new List<string> { "user_id" };

		return await dba.QuerySingleAsync<DetailedUserWithToken>(
				UserDetails.GetQuery(columns, where)
				, new { UserId = userId });
	}

	public async Task<DetailedUser> GetDetailedUserByTokenAsync(string token)
	{
		try
		{
			return await dba.QuerySingleAsync<DetailedUser>(
					UserByTokenQuery.GetQuery
					, new { Token = Guid.Parse(token) });
		}
		catch (InvalidOperationException)
		{
			throw new StandardExceptions.DataNotFoundException("user", token, "token");
		}
	}

	/// <summary>
	///  Gets user details for the unactivated account with the provided email address.  Do not use this unless you
	///  know what you are doing!
	/// </summary>
	/// <param name="email">Email address for the account details you want to retrieve</param>
	/// <returns></returns>
	public async Task<DetailedUserWithToken> GetUnactivatedUserByEmailAsync(string email)
	{
		// Generate our new secret token
		var token = Guid.NewGuid().ToString();

		await dba.ExecuteAsync(
				CreateUserEmailTokenQuery.GetQuery(true)
				, new
				{
						Email = email
						, Token = Guid.Parse(token)
						, Type = CreateUserEmailTokenQuery.Activate
						,
				});

		// Prepare account details request
		var columns = new List<string>
		{
				"email"
				, "user_id"
				, "forename"
				, "surname"
				, "token"
				,
		};

		var where = new List<string>
		{
				"email"
				, "activated"
				, "token"
				,
		};

		try
		{
			return await dba.QuerySingleAsync<DetailedUserWithToken>(
					UserDetails.GetQuery(columns, where)
					, new
					{
							Email = email
							, Activated = 0
							, Token = Guid.Parse(token)
							,
					});
		}
		catch (InvalidOperationException)
		{
			throw new StandardExceptions.DataNotFoundException("user", email, email);
		}
	}

	/// <summary>
	///  Retrieves the current users permissions for a specific edition.
	/// </summary>
	/// <param name="editionUser"></param>
	/// <returns>Returns the user's rights to read, write, and admin the edition and the users editor id for the edition</returns>
	public async Task<UserEditionPermissions> GetUserEditionPermissionsAsync(UserInfo editionUser)
	{
		try
		{
			var results = await dba.QuerySingleAsync<UserEditionPermissions>(
					UserPermissionQuery.GetQuery
					, new
					{
							editionUser.EditionId
							, UserId = editionUser.userId
							,
					});

			return results;
		}
		catch (InvalidOperationException)
		{
			throw new StandardExceptions.NoPermissionsException(editionUser);
		}
	}

	/// <summary>
	///  Retrieves the current users permissions for a specific edition.
	/// </summary>
	/// <param name="editionUser"></param>
	/// <returns>Returns the user's rights to read, write, and admin the edition and the users editor id for the edition</returns>
	public async Task<List<UserSystemRoles>> GetUserSystemRolesAsync(UserInfo editionUser)
	{
		try
		{
			var results = await dba.QueryAsync<string>(
					UserSystemRolesQuery.GetQuery
					, new { UserId = editionUser.userId });

			// Note, I could use Dapper to map the UserSystemRoles by internal database id.
			// To do this make the enum `UserSystemRoles : uint`, but I like the string matching,
			// since that decouples the systems. The system here will break down if the database is
			// altered without matching changes made to the API (or the reverse).
			return results.Select(x => x switch
									   {
											   "REGISTERED_USER" => UserSystemRoles.REGISTERED_USER
											   , "CATALOGUE_CURATOR" => UserSystemRoles
													   .CATALOGUE_CURATOR
											   , "IMAGE_DATA_CURATOR" => UserSystemRoles
													   .IMAGE_DATA_CURATOR
											   , "USER_ADMIN" => UserSystemRoles.USER_ADMIN
											   , _            => UserSystemRoles.REGISTERED_USER
											   ,
									   })
						  .ToList();
		}
		catch (InvalidOperationException)
		{
			throw new StandardExceptions.NoPermissionsException(editionUser);
		}
	}

	/// <summary>
	///  Create a new user in the database and create the email token record.  This method checks
	///  for any conflicts with existing emails, and it will respond accordingly.
	/// </summary>
	/// <param name="email">Email address for the new account (it will be verified)</param>
	/// <param name="password">Password for the new account (it is hashed in the database)</param>
	/// <param name="forename">Optional given name</param>
	/// <param name="surname">Optional family name</param>
	/// <param name="organization">Optional organizational affiliation</param>
	/// <returns>
	///  Returns a User object with the details of the newly created user. This object contains
	///  the secret confirmation token that should be emailed to the user and then likely stripped
	///  from the User object, which can be returned as a DTO to the HTTP request.
	/// </returns>
	public async Task<DetailedUserWithToken> CreateNewUserAsync(
			string          email
			, string        password
			, string        forename     = null
			, string        surname      = null
			, string        organization = null
			, IDbConnection connection   = null)
	{
		await dba.BeginTransactionAsync();

		// Find any users with either the same  email address.
		await ResolveExistingUserConflictAsync(email);

		// Ok, the input email is unique so create the record
		var newUser = await dba.ExecuteAsync(
				CreateNewUserQuery.GetQuery
				, new
				{
						Email = email
						, Password = password
						, Forename = forename
						, Surname = surname
						, Organization = organization
						,
				});

		if (newUser != 1) // Something strange must have gone wrong
			throw new StandardExceptions.DataNotWrittenException("create user");

		// Everything went well, so create the email token so the
		// calling function can email the new user.
		var newUserObject = await CreateUserActivateTokenAsync(email);

		dba.CommitTransaction();

		return newUserObject;
	}

	/// <summary>
	///  Note that this method may be destructive!  This method resolves any uniqueness constraints
	///  on an email. It will throw if an activated user account already exists with the
	///  email.  If an unactivated user account exists with the email, it will be overwritten.
	/// </summary>
	/// <param name="email">Email to check for uniqueness</param>
	/// <returns></returns>
	public async Task ResolveExistingUserConflictAsync(string email)
	{
		await dba.BeginTransactionAsync();

		// Find any users with either the same email address.
		var columns = new List<string>
		{
				"user_id"
				, "activated"
				, "email"
				,
		};

		var where = new List<string> { "email" };

		var existingUser = (await dba.QueryAsync<DetailedUserWithToken>(
				UserDetails.GetQuery(columns, where)
				, new { Email = email })).ToList();

		// Check if we need to send error because email already exist.
		if (existingUser.Any())
		{
			foreach (var record in existingUser)
			{
				if (record.Activated) // If this user record has been authenticated throw a conflict error
					throw new StandardExceptions.ConflictingDataException("email");

				await dba.ExecuteAsync(
						DeleteUserEmailTokenQuery.GetUserIdQuery
						, new { record.UserId });

				// Remove the child rows an unactivated account may hold (system role, profile data)
				// before deleting the user row itself, otherwise the delete violates a foreign key.
				await dba.ExecuteAsync(
						DeleteUnactivatedUserChildrenQuery.SystemRoles
						, new { record.UserId });

				await dba.ExecuteAsync(
						DeleteUnactivatedUserChildrenQuery.DataStore
						, new { record.UserId });

				await dba.ExecuteAsync(DeleteUserQuery.GetQuery, new { record.UserId });
			}
		}

		dba.CommitTransaction();
	}

	/// <summary>
	///  Updates the info for an existing user.  This cannot be used to reset a password, use ChangePasswordAsync
	///  instead. You should probably have run ResolveExistingUserConflict before attempting this.
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="password"></param>
	/// <param name="email">Email address for the new account (it will be verified)</param>
	/// <param name="resetActivation"></param>
	/// <param name="forename">Optional given name</param>
	/// <param name="surname">Optional family name</param>
	/// <param name="organization">Optional organizational affiliation</param>
	/// <returns>
	///  Returns a User object with the details of the newly created user. This object contains
	///  the secret confirmation token that should be emailed to the user and then likely stripped
	///  from the User object, which can be returned as a DTO to the HTTP request.
	/// </returns>
	public async Task UpdateUserAsync(
			uint            userId
			, string        password
			, string        email
			, bool          resetActivation
			, string        forename     = null
			, string        surname      = null
			, string        organization = null
			, IDbConnection connection   = null)
	{
		await dba.BeginTransactionAsync();

		if (resetActivation) // If email is new, make sure it is unique
			await ResolveExistingUserConflictAsync(email);

		var userUpdate = await dba.ExecuteAsync(
				UpdateUserInfo.GetQuery(resetActivation)
				, new
				{
						Pw = password
						, Email = email
						, Forename = forename
						, Surname = surname
						, Organization = organization
						, UserId = userId
						,
				});

		if (userUpdate != 1) // The password was wrong
			throw new StandardExceptions.WrongPasswordException();

		dba.CommitTransaction();
	}

	/// <summary>
	///  Generates an activation token for the user account in the database.  This only works
	///  if the account is not yet activated.
	/// </summary>
	/// <param name="email">Email address of the unactivated account</param>
	/// <returns>User details for the account with the activation token</returns>
	public async Task<DetailedUserWithToken> CreateUserActivateTokenAsync(string email)
	{
		// Confirm creation by getting the User object for the new user
		var columns = new List<string>
		{
				"user_id"
				, "email"
				, "forename"
				, "surname"
				, "organization"
				,
		};

		var where = new List<string> { "email" };

		await dba.BeginTransactionAsync();

		var userObject = await dba.QuerySingleAsync<DetailedUserWithToken>(
				UserDetails.GetQuery(columns, where)
				, new { Email = email });

		// Generate our secret token
		var token = Guid.NewGuid();

		// Add the secret token to the database
		var userEmailConfirmation = await dba.ExecuteAsync(
				CreateUserEmailTokenQuery.GetQuery()
				, new
				{
						userObject.UserId
						, Token = token
						, Type = CreateUserEmailTokenQuery.Activate
						,
				});

		if (userEmailConfirmation != 1) // Something strange must have gone wrong
			throw new StandardExceptions.DataNotWrittenException("create confirmation token");

		dba.CommitTransaction();

		// Everything went well, so add the token to the User object so the calling function
		// can email the new user.
		userObject.Token = token;

		return userObject;
	}

	/// <summary>
	///  Activates user account based on the secret token sent to the new user's email address.
	/// </summary>
	/// <param name="token">Secret token for user authentication</param>
	/// <returns></returns>
	public async Task ConfirmAccountCreationAsync(string token)
	{
		await dba.BeginTransactionAsync();

		var confirmRegistration = await dba.ExecuteAsync(
				ConfirmNewUserAccount.GetQuery
				, new { Token = Guid.Parse(token) });

		if (confirmRegistration != 1)
		{
			throw new StandardExceptions.ImproperInputDataException(
					"user account activation token");
		}

		// Set the user's system role
		await SetNewUserRole(token);

		// Create the user's data store
		await dba.ExecuteAsync(
				CreateUserDataStoreEntry.GetQuery
				, new
				{
						Token = Guid.Parse(token)
						, Data = "{}"
						,
				});

		// Get all Activate tokens for this user
		var tokens = await dba.QueryAsync<Guid>(
				GetTokensQuery.GetQuery
				, new
				{
						Token = Guid.Parse(token)
						, Type = CreateUserEmailTokenQuery.Activate
						,
				});

		// Delete them all
		await dba.ExecuteAsync(
				DeleteUserEmailTokenQuery.GetTokenQuery
				, new
				{
						Tokens = tokens
						, Type = CreateUserEmailTokenQuery.Activate
						,
				});

		dba.CommitTransaction();
	}

	/// <summary>
	///  Updates the email address for an account that has not yet been activated.
	/// </summary>
	/// <param name="oldEmail">Email address that was originally entered when creating the account</param>
	/// <param name="newEmail">New email address to use for the account</param>
	/// <returns></returns>
	public async Task UpdateUnactivatedUserEmailAsync(string oldEmail, string newEmail)
	{
		await dba.BeginTransactionAsync();
		await ResolveExistingUserConflictAsync(newEmail);

		var newEmailEntry = await dba.ExecuteAsync(
				ChangeUnactivatedUserEmail.GetQuery
				, new
				{
						OldEmail = oldEmail
						, NewEmail = newEmail
						,
				});

		if (newEmailEntry != 1)
			throw new StandardExceptions.DataNotWrittenException("update email");

		dba.CommitTransaction();
	}

	/// <summary>
	///  Change password from old password to new password for the user's account.
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="oldPassword">The old password for the user's account</param>
	/// <param name="newPassword">The new password for the user's account</param>
	/// <returns></returns>
	public async Task ChangePasswordAsync(uint userId, string oldPassword, string newPassword)
	{
		var changePassword = await dba.ExecuteAsync(
				ChangePasswordQuery.GetQuery
				, new
				{
						UserId = userId
						, OldPassword = oldPassword
						, NewPassword = newPassword
						,
				});

		if (changePassword != 1)
			throw new StandardExceptions.WrongPasswordException();
	}

	/// <summary>
	///  Creates a request in the database for a reset password token.
	/// </summary>
	/// <param name="email">Email address of the user who has forgotten the password</param>
	/// <returns>Returns user information and secret token that are used to format the reset password email</returns>
	public async Task<DetailedUserWithToken> RequestResetForgottenPasswordAsync(string email)
	{
		await dba.BeginTransactionAsync();

		try
		{
			// Get the user's details via the submitted email address
			var columns = new List<string>
			{
					"email"
					, "user_id"
					, "forename"
					, "surname"
					, "organization"
					,
			};

			var where = new List<string> { "email" };

			var userInfo = await dba.QuerySingleAsync<DetailedUserWithToken>(
					UserDetails.GetQuery(columns, where)
					, new { Email = email });

			// Generate our secret token
			var token = Guid.NewGuid();

			// Write the token to the database
			var tokenEntry = await dba.ExecuteAsync(
					CreateUserEmailTokenQuery.GetQuery()
					, new
					{
							Token = token
							, userInfo.UserId
							, Type = CreateUserEmailTokenQuery.ResetPassword
							,
					});

			if (tokenEntry != 1)
				return null;

			// Cleanup
			dba.CommitTransaction();

			// Pass the token back in the user info object
			userInfo.Token = token;

			return userInfo;
		}
		catch { } // Suppress errors here. We don't want to risk people fishing for valid email addresses,

		// though any errors are suppressed in the controller too.

		return null;
	}

	/// <summary>
	///  Resets a user's password based on the secret reset password token sent to the user's email address
	/// </summary>
	/// <param name="token">Secret token for resetting password</param>
	/// <param name="password">New password for the user's account</param>
	/// <returns></returns>
	public async Task<DetailedUser> ResetForgottenPasswordAsync(string token, string password)
	{
		await dba.BeginTransactionAsync();
		var detailedUserInfo = await GetDetailedUserByTokenAsync(token);

		var resetPassword = await dba.ExecuteAsync(
				UpdatePasswordByToken.GetQuery
				, new
				{
						Token = Guid.Parse(token)
						, Password = password
						,
				});

		if (resetPassword != 1)
			throw new StandardExceptions.DataNotWrittenException("reset password");

		// Get all unused ResetPassword tokens
		var tokens = await dba.QueryAsync<Guid>(
				GetTokensQuery.GetQuery
				, new
				{
						Token = Guid.Parse(token)
						, Type = CreateUserEmailTokenQuery.ResetPassword
						,
				});

		// Delete them all
		await dba.ExecuteAsync(
				DeleteUserEmailTokenQuery.GetTokenQuery
				, new
				{
						Tokens = tokens
						, Type = CreateUserEmailTokenQuery.ResetPassword
						,
				});

		dba.CommitTransaction(); // Close the transaction

		return detailedUserInfo;
	}

	public async Task<string> GetUserDataStoreAsync(uint userId)
		=> await dba.QuerySingleAsync<string>(
				GetInformationFromUserDataStore.GetQuery
				, new { UserId = userId });

	public async Task SetUserDataStoreAsync(uint userId, string data)
	{
		var insertData = await dba.ExecuteAsync(
				SetInformationInUserDataStore.GetQuery
				, new
				{
						UserId = userId
						, Data = data
						,
				});

		if (insertData != 1)
			throw new StandardExceptions.DataNotWrittenException("INSERT INTO user data store");
	}

	public async Task<List<EditorInfo>> GetEditionEditorsAsync(uint editionId)
		=> (await dba.QueryAsync<EditorInfo>(GetEditorInfo.GetQuery, new { EditionId = editionId }))
				.ToList();

	public async Task<DatabaseVersion> GetDatabaseVersion(IDbConnection connection = null)
	{
		const string databaseVersionQuery = @"
SELECT Version, completed AS Date
FROM db_version
ORDER BY completed DESC
LIMIT 1";

		return await dba.QuerySingleAsync<DatabaseVersion>(databaseVersionQuery);
	}

	/// <summary>
	///  When a new user confirms registration, they must be assigned the default user role
	///  for a registered user.  This function receives the registration token and sets
	///  the necessary system role for the new user.
	/// </summary>
	/// <param name="token">Registration token from the new user who is confirming registration</param>
	/// <returns></returns>
	/// <exception cref="StandardExceptions.DataNotWrittenException">The role could not be added for the new user</exception>
	private async Task SetNewUserRole(string token)
	{
		var setUserRole = await dba.ExecuteAsync(
				SetUserSystemRole.GetQuery
				, new
				{
						Token = Guid.Parse(token)
						, SystemRole = "REGISTERED_USER"
						,
				});

		// Confirm the entry was written
		if (setUserRole != 1)
			throw new StandardExceptions.DataNotWrittenException("set user role");
	}
}
