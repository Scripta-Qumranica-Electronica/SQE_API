/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-http-server` project. Changes made
 * there will automatically be incorporated here the next time the 
 * `sqe-realtime-hub-builder` is run.
 */

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;

namespace SQE.API.Realtime.Hubs
{
	public partial class MainHub : Hub
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
			return await _userService.AuthenticateAsync(payload.email, payload.password, Context.ConnectionId);
		}


		/// <summary>
		///     Allows a user who has not yet activated their account to change their email address. This will not work if the user
		///     account associated with the email address has already been activated
		/// </summary>
		/// <param name="payload">JSON object with the current email address and the new desired email address</param>
		[AllowAnonymous]
		public async Task PostV1UsersChangeUnactivatedEmail(UnactivatedEmailUpdateRequestDTO payload)
		{
			await _userService.UpdateUnactivatedAccountEmailAsync(
				payload.email,
				payload.newEmail,
				Context.ConnectionId
			);
		}


		/// <summary>
		///     Uses the secret token from /users/forgot-password to validate a reset of the user's password
		/// </summary>
		/// <param name="payload">A JSON object with the secret token and the new password</param>
		[AllowAnonymous]
		public async Task PostV1UsersChangeForgottenPassword(ResetForgottenUserPasswordRequestDTO payload)
		{
			await _userService.ResetLostPasswordAsync(payload.token, payload.password, Context.ConnectionId);
		}


		/// <summary>
		///     Changes the password for the currently logged in user
		/// </summary>
		/// <param name="payload">A JSON object with the old password and the new password</param>
		[Authorize]
		public async Task PostV1UsersChangePassword(ResetLoggedInUserPasswordRequestDTO payload)
		{
			await _userService.ChangePasswordAsync(
				_userService.GetCurrentUserId(),
				payload.oldPassword,
				payload.newPassword,
				Context.ConnectionId
			);
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
			return await _userService.UpdateUserAsync(_userService.GetCurrentUserId(), payload, Context.ConnectionId);
		}


		/// <summary>
		///     Confirms registration of new user account.
		/// </summary>
		/// <param name="payload">JSON object with token from user registration email</param>
		/// <returns>Returns a DetailedUserDTO for the confirmed account</returns>
		[AllowAnonymous]
		public async Task PostV1UsersConfirmRegistration(AccountActivationRequestDTO payload)
		{
			await _userService.ConfirmUserRegistrationAsync(payload.token, Context.ConnectionId);
		}


		/// <summary>
		///     Sends a secret token to the user's email to allow password reset.
		/// </summary>
		/// <param name="payload">JSON object with the email address for the user who wants to reset a lost password</param>
		[AllowAnonymous]
		public async Task PostV1UsersForgotPassword(ResetUserPasswordRequestDTO payload)
		{
			await _userService.RequestResetLostPasswordAsync(payload.email, Context.ConnectionId);
		}


		/// <summary>
		///     Provides the user details for a user with valid JWT in the Authorize header
		/// </summary>
		/// <returns>A UserDTO for user account.</returns>
		[Authorize]
		public async Task<UserDTO> GetV1Users()
		{
			return await _userService.GetCurrentUser();
		}


		/// <summary>
		///     Creates a new user with the submitted data.
		/// </summary>
		/// <param name="payload">A JSON object with all data necessary to create a new user account</param>
		/// <returns>Returns a UserDTO for the newly created account</returns>
		[AllowAnonymous]
		public async Task<UserDTO> PostV1Users(NewUserRequestDTO payload)
		{
			return await _userService.CreateNewUserAsync(payload, Context.ConnectionId);
		}


		/// <summary>
		///     Sends a new activation email for the user's account. This will not work if the user account associated with the
		///     email address has already been activated.
		/// </summary>
		/// <param name="payload">JSON object with the current email address and the new desired email address</param>
		[AllowAnonymous]
		public async Task PostV1UsersResendActivationEmail(ResendUserAccountActivationRequestDTO payload)
		{
			await _userService.ResendActivationEmail(payload.email, Context.ConnectionId);
		}
	}
}