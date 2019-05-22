﻿using System.Collections.Generic;
using System.Linq;
using SQE.SqeHttpApi.DataAccess.Helpers;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    /// <summary>
    /// An extensible User info query builder.
    /// TODO: Upgrade all user queries to use this.
    /// </summary>
    internal static class UserDetails
    {
        private const string _query = @"
SELECT $Columns
FROM user
$Join
WHERE $Where";

        /// <summary>
        /// Formats a query on the user table
        /// </summary>
        /// <param name="columns">Names of the columns to be retrieved (in snake_case)</param>
        /// <param name="where">Names of the where parameters (in snake_case)</param>
        /// <returns>Returns the formatted SQL query string</returns>
        public static string GetQuery(List<string> columns, List<string> where, bool whereAnd = true)
        {
            var join = "";
            if (columns.Where(x => x == "token").Any())
                join = "JOIN user_email_token USING(user_id)";
            return _query.Replace( // Add the columns to the query
                "$Columns", 
                string.Join(",", columns.Select(x => $"{x} AS {StringFormatters.ToPascalCase(x)}")))
                .Replace( // Add the where clause parameters to the query
                    "$Where",
                    string.Join(
                        $" {(whereAnd ? "AND" : "OR")} ", 
                        where.Select(x => $"{x} = @{StringFormatters.ToPascalCase(x)}")))
                .Replace("$Join", join)
                .Replace("@Pw", "SHA2(@Pw, 224)"); // Hash the password (if it is used)
        }
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
    /// Creates a new user account for @UserName, @Email, and @Password (the account is not activated);
    /// the fields @Forename, @Surname, and @Organization may be empty.
    /// </summary>
    internal static class CreateNewUserQuery
    {
        public const string GetQuery = @"
INSERT INTO user (user_name, email, pw, forename, surname, organization)
VALUES(@UserName, @Email, SHA2(@Password, 224), @Forename, @Surname, @Organization)";
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
    /// Deletes the record from user_email_token for the @UserId (GetUserIdQuery) or @Token(GetTokenQuery)
    /// </summary>
    internal static class DeleteUserEmailTokenQuery
    {
        public const string GetUserIdQuery = @"
DELETE FROM user_email_token WHERE user_id = @UserId";
        
        public const string GetTokenQuery = @"
DELETE FROM user_email_token WHERE token = @Token";
    }

    /// <summary>
    /// Updates the user account with the specified email address to a new email address.
    /// Not that this only works with accounts that have not yet been activated.
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

    internal static class UpdateUserInfo
    {
        public const string _query = @"
UPDATE user
SET user_name = @UserName, email = @Email, forename = @Forename, surname = @Surname, organization = @Organization$Activated
WHERE user_id = @UserId
";

        public static string GetQuery(bool resetActivation)
        {
            return _query.Replace("$Activated", resetActivation ? ", activated = 0" : "");
        }
    }
}
