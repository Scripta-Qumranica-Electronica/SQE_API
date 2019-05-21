using SQE.SqeHttpApi.DataAccess.Models;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal class UserQueryResponse : IQueryResponse<UserToken>
    {
        public string user_name { get; set; }
        public uint user_id { get; set; }

        public UserToken CreateModel()
        {
            return new UserToken
            {
                UserName = user_name,
                UserId = user_id
            };
        }
    }

    /// <summary>
    /// Confirms whether the user account with @Username, @Email, and @Password exists.
    /// </summary>
    internal static class ConfirmUserCreateQuery
    {
        public const string GetQuery = @"
SELECT user_id AS UserId, user_name AS UserName
FROM user
WHERE user_name = @Username AND email = @Email AND pw = SHA2(@Password, 224)";
    }
    
    /// <summary>
    /// Retrieves the permission of @UserId for edition @EditionId.  This includes the ability to read, write, lock,
    /// and admin, as well as the unique editor id the user has for working on this edition.
    /// </summary>
    internal static class UserPermissionQuery
    {
        public const string GetQuery = @"
SELECT edition_editor_id AS EditionEditionEditorId, 
       may_write AS MayWrite, 
       may_lock AS MayLock, 
       may_read AS MayRead, 
       is_admin AS IsAdmin
FROM edition_editor
WHERE edition_id = @EditionId AND user_id = @UserId";
    }

    /// <summary>
    /// Checks whether an account with @Username or @Email exists and whether it is activated yet.
    /// </summary>
    internal static class CheckUserActivation
    {
        public const string GetQuery = @"
SELECT user_id, activated, user_name, email
FROM user
WHERE user_name = @Username OR email = @Email";

        public class Result
        {
            public int user_id { get; set; } 
            public bool activated { get; set; } 
            public string user_name { get; set; }
            public string email { get; set; }
        }
    }
    
    /// <summary>
    /// Creates a new user account for @Username, @Email, and @Password (the account is not activated);
    /// the fields @Forename, @Surname, and @Organization may be empty.
    /// </summary>
    internal static class CreateNewUserQuery
    {
        public const string GetQuery = @"
INSERT INTO user (user_name, email, pw, forename, surname, organization)
VALUES(@Username, @Email, SHA2(@Password, 224), @Forename, @Surname, @Organization)";
    }

    /// <summary>
    /// Deletes the user with id @UserId.  We can only ever delete accounts that have not been activated
    /// (thus they never were able to create any data of their own).
    /// </summary>
    internal static class DeleteUserQuery
    {
        public const string GetQuery = @"
DELETE FROM user WHERE user_id = @UserId AND activated = 0";
    }

    /// <summary>
    /// Sets the user's password to @NewPassword, but only if the input @UserId and @OldPassword match the current record.
    /// </summary>
    internal static class ChangePasswordQuery
    {
        public const string GetQuery = @"
UPDATE user
SET pw = SHA2(@NewPassword, 224)
WHERE user_id = @UserId
    AND pw = SHA2(@OldPassword, 224)
    AND activated = 1 ## Only activated users may change their password.
";
    }

    /// <summary>
    /// Sets the user record to activated when provided with a valid token @Token.
    /// </summary>
    internal static class ConfirmNewUserAccount
    {
        public const string GetQuery = @"
UPDATE user
JOIN user_email_token USING(user_id)
SET activated = 1
WHERE user_email_token.token = @Token 
    AND user_email_token.type = 'ACTIVATE_ACCOUNT' 
    AND user.activated = 0 ## Only new users can activate an account
";
    }
    
    /// <summary>
    /// Creates an entry in the user_email_token table for @UserId with the token @Token for the request type @Type
    /// (CreateUserEmailTokenQuery.Activate or CreateUserEmailTokenQuery.ResetPassword).
    /// </summary>
    internal static class CreateUserEmailTokenQuery
    {
        public const string GetQuery = @"
INSERT INTO user_email_token (user_id, token, type)
VALUES(@UserId, @Token, @Type)
ON DUPLICATE KEY UPDATE token = @Token, date_created = NOW()";

        public const string Activate = "ACTIVATE_ACCOUNT";
        public const string ResetPassword = "RESET_PASSWORD";
    }
    
    /// <summary>
    /// Delets the record from user_email_token for the @UserId (GetUserIdQuery) or @Token(GetTokenQuery)
    /// </summary>
    internal static class DeleteUserEmailTokenQuery
    {
        public const string GetUserIdQuery = @"
DELETE FROM user_email_token WHERE user_id = @UserId";
        
        public const string GetTokenQuery = @"
DELETE FROM user_email_token WHERE token = @Token";
    }
    
    /// <summary>
    /// Returns the user name and user id for the account with the email address @Email.
    /// </summary>
    internal static class UserByEmailQuery
    {
        public const string GetQuery = @"
SELECT user_name AS UserName, user_id AS UserId
FROM user
WHERE email = @Email
";
    }
    
    /// <summary>
    /// Returns the user info for the account with the user email token @Token.  Only use this if you know what you are doing!
    /// </summary>
    internal static class UserByTokenQuery
    {
        public const string GetQuery = @"
SELECT user_name AS UserName, forename AS Forename, surname AS Surname, email AS Email
FROM user
JOIN user_email_token USING(user_id)
WHERE user_email_token.token = @Token
";
    }
    
    /// <summary>
    /// Changes the password to @Password for the user who received the reset password token @Token.
    /// </summary>
    internal static class UpdatePasswordByToken
    {
        public const string GetQuery = @"
UPDATE user
JOIN user_email_token USING(user_id)
SET user.pw = SHA2(@Password, 224)
WHERE user_email_token.token = @Token 
    AND user_email_token.type = 'RESET_PASSWORD' 
    AND user.activated = 1 ## Only activated users can reset their password
";
    }
}
