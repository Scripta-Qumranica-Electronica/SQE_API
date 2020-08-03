using System.Collections.Generic;
using System.Linq;
using CaseExtensions;

namespace SQE.DatabaseAccess.Queries
{
    /// <summary>
    ///     An extensible Query to get user details
    /// </summary>
    internal static class UserDetails
    {
        private const string _query = @"
SELECT $Columns
FROM user
$Join
WHERE $Where";

        /// <summary>
        ///     Formats a query on the user table
        /// </summary>
        /// <param name="columns">Names of the columns to be retrieved (in snake_case)</param>
        /// <param name="where">Names of the where parameters (in snake_case)</param>
        /// <param name="whereAnd">True concatenates multiple WHERE statements with AND, false uses OR</param>
        /// <returns>Returns the formatted SQL query string</returns>
        public static string GetQuery(List<string> columns, List<string> where, bool whereAnd = true)
        {
            var join = "";
            if (columns.Any(x => x == "token"))
                join = "JOIN user_email_token USING(user_id)";
            return _query.Replace( // Add the columns to the query
                    "$Columns",
                    string.Join(",", columns.Select(x => $"{x} AS {x.ToPascalCase()}"))
                )
                .Replace( // Add the where clause parameters to the query
                    "$Where",
                    string.Join(
                        $" {(whereAnd ? "AND" : "OR")} ",
                        where.Select(x => $"{x} = @{x.ToPascalCase()}")
                    )
                )
                .Replace("$Join", join)
                .Replace("@Pw", "SHA2(@Pw, 224)"); // Hash the password (if it is used)
        }
    }

    /// <summary>
    ///     Retrieves the permission of @UserId for edition @EditionId.  This includes the ability to read, write, lock,
    ///     and admin, as well as the unique editor id the user has for working on this edition.
    /// </summary>
    internal static class UserPermissionQuery
    {
        public const string GetQuery = @"
SELECT user.user_id AS UserId,
       edition_editor_id AS EditionEditionEditorId, 
       COALESCE(may_write, FALSE) AS MayWrite, 
       COALESCE(may_lock, FALSE) AS MayLock, 
       COALESCE(may_read, public) AS MayRead, 
       COALESCE(is_admin, FALSE) AS IsAdmin,
       locked AS Locked
FROM edition
LEFT JOIN user ON user.user_id = @UserId
LEFT JOIN edition_editor ON edition_editor.edition_id = @EditionId
    AND edition_editor.user_id = @UserId
WHERE edition.edition_id = @EditionId";
    }

    /// <summary>
    ///     Retrieves the roles for a user.
    /// </summary>
    internal static class UserSystemRolesQuery
    {
        public const string GetQuery = @"
SELECT system_roles.role_title
FROM users_system_roles
JOIN system_roles USING(system_roles_id)
WHERE users_system_roles.user_id = @UserID";
    }

    /// <summary>
    ///     Creates a new user account for @Email, and @Password (the account is not activated);
    ///     the fields @Forename, @Surname, and @Organization may be empty.
    /// </summary>
    internal static class CreateNewUserQuery
    {
        public const string GetQuery = @"
INSERT INTO user (email, pw, forename, surname, organization)
VALUES(@Email, SHA2(@Password, 224), @Forename, @Surname, @Organization)";
    }

    /// <summary>
    ///     Deletes the user with id @UserId.  We can only ever delete accounts that have not been activated
    ///     (thus they never were able to create any data of their own).
    /// </summary>
    internal static class DeleteUserQuery
    {
        public const string GetQuery = @"
DELETE FROM user WHERE user_id = @UserId AND activated = 0";
    }

    /// <summary>
    ///     Sets the user's password to @NewPassword, but only if the input @UserId and @OldPassword match the current record.
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
    ///     Sets the user record to activated when provided with a valid token @Token.
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

    internal static class GetTokensQuery
    {
        public const string GetQuery = @"
SELECT all_tokens.token AS token
FROM SQE.user_email_token
JOIN SQE.user_email_token AS all_tokens 
  ON all_tokens.user_id = SQE.user_email_token.user_id
  AND all_tokens.type = @Type
WHERE user_email_token.token = @Token
";
    }

    /// <summary>
    ///     Creates an entry in the user_email_token table for @UserId with the token @Token for the request type @Type
    ///     (CreateUserEmailTokenQuery.Activate or CreateUserEmailTokenQuery.ResetPassword).
    /// </summary>
    internal static class CreateUserEmailTokenQuery
    {
        public const string _query = @"
INSERT INTO user_email_token (user_id, token, type)
$VALUES
ON DUPLICATE KEY UPDATE `type` = @Type, date_created = NOW()";

        public const string Activate = "ACTIVATE_ACCOUNT";
        public const string ResetPassword = "RESET_PASSWORD";
        public const string DeleteEdition = "DELETE_EDITION";
        public const string EditorInvite = "EDITOR_INVITE";

        public static string GetQuery(bool emailOnly = false)
        {
            return _query.Replace(
                "$VALUES",
                emailOnly
                    ? "SELECT user_id, @Token, @Type FROM user WHERE email = @Email"
                    : "VALUES(@UserId, @Token, @Type)"
            );
        }
    }

    /// <summary>
    ///     Deletes the record from user_email_token for the @UserId (GetUserIdQuery) or @Token(GetTokenQuery)
    /// </summary>
    internal static class DeleteUserEmailTokenQuery
    {
        public const string GetUserIdQuery = @"
DELETE FROM user_email_token WHERE user_id = @UserId";

        public const string GetTokenQuery = @"
DELETE FROM user_email_token WHERE token in @Tokens AND type = @Type";
    }

    /// <summary>
    ///     Updates the user account with the specified email address to a new email address.
    ///     Not that this only works with accounts that have not yet been activated.
    /// </summary>
    internal static class ChangeUnactivatedUserEmail
    {
        public const string GetQuery = @"
UPDATE user
SET email = @NewEmail
WHERE email = @OldEmail AND activated = 0
";
    }

    /// <summary>
    ///     Returns the user info for the account with the user email token @Token.  Only use this if you know what you are
    ///     doing!
    /// </summary>
    internal static class UserByTokenQuery
    {
        public const string GetQuery = @"
SELECT email AS Email, forename AS Forename, surname AS Surname
FROM user
JOIN user_email_token USING(user_id)
WHERE user_email_token.token = @Token
";
    }

    /// <summary>
    ///     Changes the password to @Password for the user who received the reset password token @Token.
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

    internal static class UpdateUserInfo
    {
        public const string _query = @"
UPDATE user
SET email = @Email, forename = @Forename, surname = @Surname, organization = @Organization$Activated
WHERE user_id = @UserId AND pw = SHA2(@Pw, 224)
";

        public static string GetQuery(bool resetActivation)
        {
            return _query.Replace("$Activated", resetActivation ? ", activated = 0" : "");
        }
    }

    // We do not return the "email" field here, because we do not want to make that information is public.
    // All users have agreed to license their editorial decisions at registration and that cannot be revoked,
    // so it is safe to send this information to anyone when it is connected with licensing information of an
    // edition.
    internal static class GetEditorInfo
    {
        public const string GetQuery = @"
SELECT user_id AS UserId, forename AS Forename, surname AS Surname, organization AS Organization
FROM edition_editor
JOIN user USING(user_id)
WHERE edition_editor.edition_id = @EditionId";
    }
}