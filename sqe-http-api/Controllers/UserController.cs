using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Helpers;


namespace SQE.SqeHttpApi.Server.Controllers
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
        [HttpGet]
        public ActionResult<UserDTO> GetCurrentUser()
        {
            var user = _userService.GetCurrentUser();
            if (user == null)
                return Unauthorized(new { message = "No current user", code =601 });

            return user;
        }
        
        /// <summary>
        /// Creates a new user with the submitted data.
        /// </summary>
        /// <param name="userInfo">A JSON object with all data necessary to create a new user account</param>
        /// <returns>Returns a UserDTO for the newly created account. Throws an HTTP 409 if the
        /// username or email is already in use.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<UserDTO>> CreateNewUser([FromBody] NewUserRequestDTO userInfo)
        {
            try
            {
                return await _userService.CreateNewUserAsync(userInfo);
            }
            catch(DbDetailedFailedWrite err)
            {
                return Conflict(new {message = err.Message});
            }
        }
        
        /// <summary>
        /// Updates a user's registration details.  Note that the if the email address has changed,
        /// the account will be set to inactive until the account is activated with the secret token.
        /// </summary>
        /// <param name="userInfo">A JSON object with all data necessary to update a user account</param>
        /// <returns>Returns a UserDTO for the newly created account. Throws an HTTP 409 if the
        /// username or email is already in use by someone else.</returns>
        [HttpPut]
        public async Task<ActionResult<UserDTO>> ChangeUserInfo([FromBody] NewUserRequestDTO userInfo)
        {
            try
            {
                return await _userService.UpdateUserAsync(_userService.GetCurrentUserObject(), userInfo);
            }
            catch(DbDetailedFailedWrite err)
            {
                return Conflict(new {message = err.Message});
            }
        }
        
        /// <summary>
        /// Confirms registration of new user account.
        /// </summary>
        /// <param name="payload">JSON object with token from user registration email.</param>
        /// <returns>Returns 204 for success and 404 for failure.</returns>
        [AllowAnonymous]
        [HttpPost("confirm-registration")]
        public async Task<ActionResult<UserDTO>> ConfirmUserRegistration([FromBody] EmailTokenDTO payload)
        {
            try
            {
                await _userService.ConfirmUserRegistrationAsync(payload.token);
                return NoContent();
            }
            catch
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Provides a JWT bearer token for valid username and password
        /// </summary>
        /// <param Name="userParam">JSON object with a username and password parameter</param>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> AuthenticateAsync([FromBody]LoginRequestDTO userParam)
        {
            var user = await _userService.AuthenticateAsync(userParam.userName, userParam.password);
            if (user == null)
                return Unauthorized(new { message = "Username or password is incorrect ", code = 600}); // TODO: Add Error Code

            return user;
        }
        
        /// <summary>
        /// Allows a user who has not yet activated their account to change their email address.
        /// This will not work if the user account associated with the email address has already been activated.
        /// </summary>
        /// <param Name="userParam">JSON object with a username and password parameter</param>
        [AllowAnonymous]
        [HttpPost("change-unactivated-email")]
        public async Task<ActionResult> ChangeEmailOfUnactivatedUserAccount(
            [FromBody]UnactivatedEmailUpdateRequestDTO emailDetails)
        {
            try
            {
                await _userService.UpdateUnactivatedAccountEmail(emailDetails.oldEmail, emailDetails.newEmail);
                return NoContent();
            }
            catch
            {
                return NoContent(); // Returns no content no matter what
            }
            
        }
        
        /// <summary>
        /// Sends a secret token to the user's email to allow password reset.
        /// </summary>
        /// <param name="payload">JSON object with the email address for the user who wants to reset a lost password.</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ResetUserPasswordRequestDTO payload)
        {
            await _userService.RequestResetLostPasswordAsync(payload.email);
            return NoContent();
        }
        
        /// <summary>
        /// Changes the password for the currently logged in user.
        /// </summary>
        /// <param name="payload">A JSON object with the old password and the new password.</param>
        /// <returns>Status 204 with a successful request or status 409 for an unsuccessful request</returns>
        [HttpPost("change-password")]
        public async Task<ActionResult> ResetForgottenPassword([FromBody] ResetLoggedInUserPasswordRequestDTO payload)
        {
            try
            {
                await _userService.ChangePasswordAsync(_userService.GetCurrentUserObject(), payload.oldPassword, payload.newPassword);
                return NoContent();
            }
            catch
            {
                return Conflict();
            }
        }
        
        /// <summary>
        /// Uses the secret token from /users/forgot-password to validate a reset of the user's password.
        /// </summary>
        /// <param name="payload">A JSON object with the secret token and the new password.</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("change-forgotten-password")]
        public async Task<ActionResult> ResetForgottenPassword([FromBody] ResetForgottenUserPasswordDTO payload)
        {
            try
            {
                await _userService.ResetLostPasswordAsync(payload.token, payload.password);
                return Ok();
            }
            catch
            {
                return NotFound();
            }
        }
    }
}
