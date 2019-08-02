using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SQE.SqeApi.Server.DTOs;
using SQE.SqeApi.Server.Helpers;
using SQE.SqeApi.DataAccess.Helpers;


namespace SQE.SqeApi.Server.Controllers
{

    [Produces("application/json")]
    [Authorize]
    [Route("v1/[controller]s")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private IUserService _userService;

        public UserController(IUserService userServiceAuthenticate)
        {
            _userService = userServiceAuthenticate;
        }
        
        /// <summary>
        /// Provides the user details for a user with valid JWT in the Authorize header
        /// </summary>
        /// <returns>A UserDTO for user account.</returns>
        /// <response code="200">User account information was found</response>
        /// <response code="401">Credentials could not be authorized</response>
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<DetailedUserDTO>> GetCurrentUser()
        {
            return await _userService.GetCurrentUser();
        }
        
        /// <summary>
        /// Creates a new user with the submitted data.
        /// </summary>
        /// <param name="payload">A JSON object with all data necessary to create a new user account</param>
        /// <returns>Returns a UserDTO for the newly created account.</returns>
        /// <response code="200">New user account was created and an activation email has been sent to the new user</response>
        /// <response code="409">Email already in use by another user account</response>
        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<UserDTO>> CreateNewUser([FromBody] NewUserRequestDTO payload)
        {
            return await _userService.CreateNewUserAsync(payload);
        }
        
        /// <summary>
        /// Updates a user's registration details.  Note that the if the email address has changed,
        /// the account will be set to inactive until the account is activated with the secret token.
        /// </summary>
        /// <param name="payload">A JSON object with all data necessary to update a user account.  Null fields (but not empty strings!)
        /// will be populated with existing user data.</param>
        /// <returns>Returns a UserDTO with the updated user account details.</returns>
        /// <response code="200">User account details have been updated</response>
        /// <response code="409">Email already in use by another user account</response>
        [HttpPut]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<DetailedUserDTO>> ChangeUserInfo([FromBody] UserUpdateRequestDTO payload)
        {
            return await _userService.UpdateUserAsync(_userService.GetCurrentUserObject(), payload);
        }
        
        /// <summary>
        /// Confirms registration of new user account.
        /// </summary>
        /// <param name="payload">JSON object with token from user registration email.</param>
        /// <returns></returns>
        /// <response code="204">The user account has been activated</response>
        /// <response code="404">The token is incorrect or out of date</response>
        [AllowAnonymous]
        [HttpPost("confirm-registration")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserDTO>> ConfirmUserRegistration([FromBody] AccountActivationRequestDTO payload)
        {
            await _userService.ConfirmUserRegistrationAsync(payload.token);
            return NoContent();
        }

        /// <summary>
        /// Provides a JWT bearer token for valid email and password
        /// </summary>
        /// <param name="payload">JSON object with an email and password parameter</param>
        /// <returns>A DetailedUserTokenDTO with a JWT for activated user accounts, or the email address of an unactivated user account.</returns>
        /// <response code="200">User has been authenticated</response>
        /// <response code="401">User credentials could not be authenticated</response>
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<DetailedUserTokenDTO>> AuthenticateAsync([FromBody]LoginRequestDTO payload)
        {
            return await _userService.AuthenticateAsync(payload.email, payload.password);
        }
        
        /// <summary>
        /// Allows a user who has not yet activated their account to change their email address.
        /// This will not work if the user account associated with the email address has already been activated.
        /// </summary>
        /// <param name="payload">JSON object with the current email address and the new desired email address</param>
        /// <returns></returns>
        /// <response code="204">Returns 204 if successful</response>
        /// <response code="409">Email already in use by another user account or account already activated</response>
        [AllowAnonymous]
        [HttpPost("change-unactivated-email")]
        [ProducesResponseType(204)]
        [ProducesResponseType(409)]
        public async Task<ActionResult> ChangeEmailOfUnactivatedUserAccount(
            [FromBody]UnactivatedEmailUpdateRequestDTO payload)
        {
            await _userService.UpdateUnactivatedAccountEmailAsync(payload.email, payload.newEmail);
            return NoContent();
            
        }
        
        /// <summary>
        /// Sends a new activation email for the user's account.
        /// This will not work if the user account associated with the email address has already been activated.
        /// </summary>
        /// <param name="payload">JSON object with the current email address and the new desired email address</param>
        /// <returns></returns>
        /// <response code="204">Always returns 204 whether successful or not</response>
        [AllowAnonymous]
        [HttpPost("resend-activation-email")]
        [ProducesResponseType(204)]
        public async Task<ActionResult> ResendUserAccountActivationEmail(
            [FromBody]ResendUserAccountActivationRequestDTO payload)
        {
            await _userService.ResendActivationEmail(payload.email);
            return NoContent();
            
        }
        
        /// <summary>
        /// Sends a secret token to the user's email to allow password reset.
        /// </summary>
        /// <param name="payload">JSON object with the email address for the user who wants to reset a lost password.</param>
        /// <returns></returns>
        /// <response code="204">Always returns 204 whether successful or not</response>
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [ProducesResponseType(204)]
        public async Task<ActionResult> ForgotPassword([FromBody] ResetUserPasswordRequestDTO payload)
        {
            await _userService.RequestResetLostPasswordAsync(payload.email);
            return NoContent();
        }
        
        /// <summary>
        /// Changes the password for the currently logged in user.
        /// </summary>
        /// <param name="payload">A JSON object with the old password and the new password.</param>
        /// <returns></returns>
        /// <response code="204">Password was correctly set</response>
        /// <response code="401">Incorrect password entered</response>
        [HttpPost("change-password")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> ChangePassword([FromBody] ResetLoggedInUserPasswordRequestDTO payload)
        {
            await _userService.ChangePasswordAsync(_userService.GetCurrentUserObject(), payload.oldPassword,
                payload.newPassword);
            return NoContent();
        }
        
        /// <summary>
        /// Uses the secret token from /users/forgot-password to validate a reset of the user's password.
        /// </summary>
        /// <param name="payload">A JSON object with the secret token and the new password.</param>
        /// <returns></returns>
        /// <response code="204">Password has been reset</response>
        /// <response code="404">Token was not found or was no longer valid</response>
        [AllowAnonymous]
        [HttpPost("change-forgotten-password")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> ChangeForgottenPassword([FromBody] ResetForgottenUserPasswordRequestDto payload)
        {
            await _userService.ResetLostPasswordAsync(payload.token, payload.password);
            return NoContent();
        }
    }
}
