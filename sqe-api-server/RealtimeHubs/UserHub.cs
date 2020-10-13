/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-api-server` project. Changes made
 * there will automatically be incorporated here the next time the 
 * `sqe-realtime-hub-builder` is run.
 */

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SQE.API.DTO;
using SQE.API.Server.Services;
using Microsoft.AspNetCore.SignalR;

using SQE.DatabaseAccess.Helpers;

using System.Text.Json;

using SQE.API.Server.Helpers;

namespace SQE.API.Server.RealtimeHubs
{
    public partial class MainHub
    {
        /// <summary>
        ///     Provides a JWT bearer token for valid email and password
        /// </summary>
        /// <param name="payload">JSON object with an email and password parameter</param>
        /// <returns>
        ///     A DetailedUserTokenDTO with a JWT for activated user accounts, or the email address of an unactivated user
        ///     account
        /// </returns>
        [AllowAnonymous]
        public async Task<DetailedUserTokenDTO> PostV1UsersLogin(LoginRequestDTO payload)

        {
            try
            {
                return await _userService.AuthenticateAsync(payload.email, payload.password, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Allows a user who has not yet activated their account to change their email address. This will not work if the user
        ///     account associated with the email address has already been activated
        /// </summary>
        /// <param name="payload">JSON object with the current email address and the new desired email address</param>
        [AllowAnonymous]
        public async Task PostV1UsersChangeUnactivatedEmail(UnactivatedEmailUpdateRequestDTO payload)

        {
            try
            {
                await _userService.UpdateUnactivatedAccountEmailAsync(payload.email, payload.newEmail, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Uses the secret token from /users/forgot-password to validate a reset of the user's password
        /// </summary>
        /// <param name="payload">A JSON object with the secret token and the new password</param>
        [AllowAnonymous]
        public async Task PostV1UsersChangeForgottenPassword(ResetForgottenUserPasswordRequestDTO payload)

        {
            try
            {
                await _userService.ResetLostPasswordAsync(payload.token, payload.password, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Changes the password for the currently logged in user
        /// </summary>
        /// <param name="payload">A JSON object with the old password and the new password</param>
        [Authorize]
        public async Task PostV1UsersChangePassword(ResetLoggedInUserPasswordRequestDTO payload)

        {
            try
            {
                await _userService.ChangePasswordAsync(_userService.GetCurrentUserId(), payload.oldPassword, payload.newPassword, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Updates a user's registration details.  Note that the if the email address has changed, the account will be set to
        ///     inactive until the account is activated with the secret token.
        /// </summary>
        /// <param name="payload">
        ///     A JSON object with all data necessary to update a user account.  Null fields (but not empty
        ///     strings!) will be populated with existing user data
        /// </param>
        /// <returns>Returns a DetailedUserDTO with the updated user account details</returns>
        [Authorize]
        public async Task<DetailedUserDTO> PutV1Users(UserUpdateRequestDTO payload)

        {
            try
            {
                return await _userService.UpdateUserAsync(_userService.GetCurrentUserId(), payload, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Confirms registration of new user account.
        /// </summary>
        /// <param name="payload">JSON object with token from user registration email</param>
        /// <returns>Returns a DetailedUserDTO for the confirmed account</returns>
        [AllowAnonymous]
        public async Task PostV1UsersConfirmRegistration(AccountActivationRequestDTO payload)

        {
            try
            {
                await _userService.ConfirmUserRegistrationAsync(payload.token, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Sends a secret token to the user's email to allow password reset.
        /// </summary>
        /// <param name="payload">JSON object with the email address for the user who wants to reset a lost password</param>
        [AllowAnonymous]
        public async Task PostV1UsersForgotPassword(ResetUserPasswordRequestDTO payload)

        {
            try
            {
                await _userService.RequestResetLostPasswordAsync(payload.email, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Provides the user details for a user with valid JWT in the Authorize header
        /// </summary>
        /// <returns>A UserDTO for user account.</returns>
        [Authorize]
        public async Task<UserDTO> GetV1Users()

        {
            try
            {
                return await _userService.GetCurrentUser();
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Creates a new user with the submitted data.
        /// </summary>
        /// <param name="payload">A JSON object with all data necessary to create a new user account</param>
        /// <returns>Returns a UserDTO for the newly created account</returns>
        [AllowAnonymous]
        public async Task<UserDTO> PostV1Users(NewUserRequestDTO payload)

        {
            try
            {
                return await _userService.CreateNewUserAsync(payload, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        ///     Sends a new activation email for the user's account. This will not work if the user account associated with the
        ///     email address has already been activated.
        /// </summary>
        /// <param name="payload">JSON object with the current email address and the new desired email address</param>
        [AllowAnonymous]
        public async Task PostV1UsersResendActivationEmail(ResendUserAccountActivationRequestDTO payload)

        {
            try
            {
                await _userService.ResendActivationEmail(payload.email, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        /// Retrieve the information in the user's personal data store
        /// </summary>
        /// <param name="data">A JSON object with the data to store for the user</param>
        /// <returns></returns>
        [Authorize]
        public async Task<UserDataStoreDTO> GetV1UsersDataStore()

        {
            try
            {
                return await _userService.GetUserDataStoreAsync(_userService.GetCurrentUserId());
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


        /// <summary>
        /// Update the information in the user's personal data store
        /// </summary>
        /// <param name="data">A JSON object with the data to store for the user</param>
        /// <returns></returns>
        [Authorize]
        public async Task PutV1UsersDataStore(UserDataStoreDTO data)

        {
            try
            {
                await _userService.SetUserDataStoreAsync(_userService.GetCurrentUserId(), data, clientId: Context.ConnectionId);
            }
            catch (ApiException err)
            {
                throw new HubException(JsonSerializer.Serialize(new HttpExceptionMiddleware.ApiExceptionError(nameof(err), err.Error, err is IExceptionWithData exceptionWithData ? exceptionWithData.CustomReturnedData : null)));
            }
        }


    }
}
