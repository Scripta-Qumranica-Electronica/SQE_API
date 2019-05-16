using SQE.SqeHttpApi.DataAccess.Models;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal class UserQueryResponse : IQueryResponse<User>
    {
        public string user_name { get; set; }
        public uint user_id { get; set; }

        public User CreateModel()
        {
            return new User
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

        public class Return
        {
            private uint edition_editor_id { get; set; }
            private bool may_write { get; set; }
            private bool may_lock { get; set; }
            private bool may_read { get; set; }
            private bool is_admin { get; set; }
        }
    }

    /// <summary>
    /// Checks whether an account with @Username or @Email exists and whether it is authenticated yet.
    /// </summary>
    internal static class CheckUserAuthentication
    {
        public const string GetQuery = @"
SELECT user_id, authenticated, user_name, email
FROM user
WHERE user_name = @Username OR email = @Email";

        public class Return
        {
            public int user_id { get; set; } 
            public bool authenticated { get; set; } 
            public string user_name { get; set; }
            public string email { get; set; }
        }
    }
    
    /// <summary>
    /// Creates a new unauthenticated user account for @Username, @Email, and @Password;
    /// the fields @Forename, @Surname, and @Organization may be empty.
    /// </summary>
    internal static class CreateNewUserQuery
    {
        public const string GetQuery = @"
INSERT INTO user (user_name, email, pw, forename, surname, organization)
VALUES(@Username, @Email, SHA2(@Password, 224), @Forename, @Surname, @Organization)";
    }

    /// <summary>
    /// Deletes the user with id @UserId.  We can only ever delete accounts that have not been authenticated
    /// (thus they never were able to create any data of their own).
    /// </summary>
    internal static class DeleteUserQuery
    {
        public const string GetQuery = @"
DELETE FROM user WHERE user_id = @UserId AND authenticated = 0";
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
    AND authenticated = 1 ## Only authenticated users may change their password.
";
    }

    /// <summary>
    /// Sets the user record to authenticated when provided with a valid token @Token.
    /// </summary>
    internal static class ConfirmNewUserAccount
    {
        public const string GetQuery = @"
UPDATE user
JOIN user_email_token USING(user_id)
SET authenticated = 1
WHERE user_email_token.token = @Token 
    AND user_email_token.type = 'ACTIVATE_ACCOUNT' 
    AND user.authenticated = 0 ## Only new users can authenticate an account
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
VALUES(@UserId, @Token, @Type)";

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
    AND user.authenticated = 1 ## Only authenticated users can reset their password
";
    }
    
    /// <summary>
    /// This is used to keep the user_email_token table clean and up-to-date.
    /// It deletes records that are more than @Days old.
    /// I could do this as a trigger in the database, and perhaps that would be better.
    /// TODO: Decide whether to keep this or to use a trigger. Either way remove `date_expires` from the table.
    /// </summary>
    internal static class DeleteOldTokens
    {
        public const string GetQuery = @"
DELETE FROM  user_email_token WHERE NOW() > date_created + interval @Days day
";
    }
}
