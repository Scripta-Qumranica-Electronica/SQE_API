using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers
{
	[Authorize]
	[ApiController]
	public class UserController : ControllerBase
	{
		private readonly IUserService _userService;

		public UserController(IUserService userService)
		{
			_userService = userService;
		}

		/// <summary>
		///     Provides a JWT bearer token for valid email and password
		/// </summary>
		/// <param name="payload">JSON object with an email and password parameter</param>
		/// <returns>
		///     A DetailedUserTokenDTO with a JWT for activated user accounts, or the email address of an unactivated user
		///     account
		/// </returns>
		[AllowAnonymous]
		[HttpPost("v1/[controller]s/login")]
		public async Task<ActionResult<DetailedUserTokenDTO>> AuthenticateAsync([FromBody] LoginRequestDTO payload)
		{
			return await _userService.AuthenticateAsync(payload.email, payload.password);
		}

		/// <summary>
		///     Allows a user who has not yet activated their account to change their email address. This will not work if the user
		///     account associated with the email address has already been activated
		/// </summary>
		/// <param name="payload">JSON object with the current email address and the new desired email address</param>
		[AllowAnonymous]
		[HttpPost("v1/[controller]s/change-unactivated-email")]
		public async Task<ActionResult> ChangeEmailOfUnactivatedUserAccount(
			[FromBody] UnactivatedEmailUpdateRequestDTO payload)
		{
			return await _userService.UpdateUnactivatedAccountEmailAsync(payload.email, payload.newEmail);
		}

		/// <summary>
		///     Uses the secret token from /users/forgot-password to validate a reset of the user's password
		/// </summary>
		/// <param name="payload">A JSON object with the secret token and the new password</param>
		[AllowAnonymous]
		[HttpPost("v1/[controller]s/change-forgotten-password")]
		public async Task<ActionResult> ChangeForgottenPassword([FromBody] ResetForgottenUserPasswordRequestDTO payload)
		{
			return await _userService.ResetLostPasswordAsync(payload.token, payload.password);
		}

		/// <summary>
		///     Changes the password for the currently logged in user
		/// </summary>
		/// <param name="payload">A JSON object with the old password and the new password</param>
		[HttpPost("v1/[controller]s/change-password")]
		public async Task<ActionResult> ChangePassword([FromBody] ResetLoggedInUserPasswordRequestDTO payload)
		{
			return await _userService.ChangePasswordAsync(
				_userService.GetCurrentUserId(),
				payload.oldPassword,
				payload.newPassword
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
		[HttpPut("v1/[controller]s")]
		public async Task<ActionResult<DetailedUserDTO>> ChangeUserInfo([FromBody] UserUpdateRequestDTO payload)
		{
			return await _userService.UpdateUserAsync(
				_userService.GetCurrentUserId(),
				payload
			);
		}

		/// <summary>
		///     Confirms registration of new user account.
		/// </summary>
		/// <param name="payload">JSON object with token from user registration email</param>
		/// <returns>Returns a DetailedUserDTO for the confirmed account</returns>
		[AllowAnonymous]
		[HttpPost("v1/[controller]s/confirm-registration")]
		public async Task<ActionResult> ConfirmUserRegistration(
			[FromBody] AccountActivationRequestDTO payload)
		{
			return await _userService.ConfirmUserRegistrationAsync(payload.token);
		}

		/// <summary>
		///     Sends a secret token to the user's email to allow password reset.
		/// </summary>
		/// <param name="payload">JSON object with the email address for the user who wants to reset a lost password</param>
		[AllowAnonymous]
		[HttpPost("v1/[controller]s/forgot-password")]
		public async Task<ActionResult> ForgotPassword([FromBody] ResetUserPasswordRequestDTO payload)
		{
			return await _userService.RequestResetLostPasswordAsync(payload.email);
		}

		/// <summary>
		///     Provides the user details for a user with valid JWT in the Authorize header
		/// </summary>
		/// <returns>A UserDTO for user account.</returns>
		[HttpGet("v1/[controller]s")]
		public async Task<ActionResult<UserDTO>> GetCurrentUser()
		{
			return await _userService.GetCurrentUser();
		}

		/// <summary>
		///     Creates a new user with the submitted data.
		/// </summary>
		/// <param name="payload">A JSON object with all data necessary to create a new user account</param>
		/// <returns>Returns a UserDTO for the newly created account</returns>
		[AllowAnonymous]
		[HttpPost("v1/[controller]s")]
		public async Task<ActionResult<UserDTO>> NewUserRequest([FromBody] NewUserRequestDTO payload)
		{
			return await _userService.CreateNewUserAsync(payload);
		}

		/// <summary>
		///     Sends a new activation email for the user's account. This will not work if the user account associated with the
		///     email address has already been activated.
		/// </summary>
		/// <param name="payload">JSON object with the current email address and the new desired email address</param>
		[AllowAnonymous]
		[HttpPost("v1/[controller]s/resend-activation-email")]
		public async Task<ActionResult> ResendUserAccountActivationEmail(
			[FromBody] ResendUserAccountActivationRequestDTO payload)
		{
			return await _userService.ResendActivationEmail(payload.email);
		}
	}
}