using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.API.Server.RealtimeHubs;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
    public interface IUserService
    {
        Task<DetailedUserTokenDTO>
            AuthenticateAsync(string email,
                string password,
                string clientId = null); // TODO: Return a User object, not a LoginResponse

        Task<DetailedUserDTO> GetCurrentUser();
        uint? GetCurrentUserId();

        Task<EditionUserInfo> GetCurrentUserObjectAsync(uint editionId,
            bool write = false,
            bool locking = false,
            bool admin = false);

        Task<DetailedUserTokenDTO> CreateNewUserAsync(NewUserRequestDTO newUserData, string clientId = null);

        Task<DetailedUserTokenDTO> UpdateUserAsync(uint? userId,
            UserUpdateRequestDTO updateUserData,
            string clientId = null);

        Task<NoContentResult> UpdateUnactivatedAccountEmailAsync(string oldEmail,
            string newEmail,
            string clientId = null);

        Task<NoContentResult> ResendActivationEmail(string email, string clientId = null);
        Task<NoContentResult> ConfirmUserRegistrationAsync(string token, string clientId = null);

        Task<NoContentResult> ChangePasswordAsync(uint? userId,
            string oldPassword,
            string newPassword,
            string clientId = null);

        Task<NoContentResult> RequestResetLostPasswordAsync(string email, string clientId = null);
        Task<NoContentResult> ResetLostPasswordAsync(string token, string password, string clientId = null);
    }

    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly AppSettings _appSettings;
        private readonly IConfiguration _config;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<MainHub, ISQEClient> _hubContext;
        private readonly IUserRepository _userRepository;
        private readonly string webServer;


        public UserService(IOptions<AppSettings> appSettings,
                IUserRepository userRepository,
                IHttpContextAccessor accessor,
                IEmailSender emailSender,
                IConfiguration config,
                IWebHostEnvironment env,
                IHubContext<MainHub, ISQEClient> hubContext)

        // http://jasonwatmore.com/post/2018/08/14/aspnet-core-21-jwt-authentication-tutorial-with-example-api
        {
            _userRepository = userRepository;
            _appSettings = appSettings.Value; // For the secret key
            _accessor = accessor;
            _emailSender = emailSender;
            _config = config;
            _env = env;
            _hubContext = hubContext;
            webServer = _config.GetConnectionString("WebsiteHost");
        }

        /// <summary>
        ///     Authenticate user by credentials and create a JWT for the user.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password">The user's password</param>
        /// <param name="clientId"></param>
        /// <returns>
        ///     Returns a response including a JWT for the user to use as a Bearer token, or an email
        ///     address if the user account has not yet been activated.
        /// </returns>
        public async Task<DetailedUserTokenDTO> AuthenticateAsync(string email, string password, string clientId = null)
        {
            var result = await _userRepository.GetUserByPasswordAsync(email, password);

            return new DetailedUserTokenDTO
            {
                email = result.Email,
                userId = result.UserId,
                forename = result.Forename,
                surname = result.Surname,
                organization = result.Organization,
                token = result.Activated ? BuildUserToken(result.Email, result.UserId) : null,
                activated = result.Activated
            };
        }

        public async Task<DetailedUserDTO> GetCurrentUser()
        {
            var userInfo = await _userRepository.GetDetailedUserByIdAsync(GetCurrentUserId());
            return new DetailedUserDTO
            {
                email = userInfo.Email,
                forename = userInfo.Forename,
                surname = userInfo.Surname,
                organization = userInfo.Organization,
                userId = userInfo.UserId,
                activated = userInfo.Activated
            };
        }

        public uint? GetCurrentUserId()
        {
            var identity = (ClaimsIdentity)_accessor.HttpContext.User.Identity;
            var claims = identity.Claims;
            foreach (var claim in claims)
            {
                var split = claim.Type.Split("/");
                if (split[split.Length - 1] == "nameidentifier") return uint.Parse(claim.Value);
            }

            return null;
        }

        /// <summary>
        ///     This will create a new user account in the database and email an authorization token to the user.
        /// </summary>
        /// <param name="newUserData">All of the information for the new user.</param>
        /// <param name="clientId"></param>
        /// <returns>Returns a UserDTO for the newly created user account.</returns>
        public async Task<DetailedUserTokenDTO> CreateNewUserAsync(NewUserRequestDTO newUserData,
            string clientId = null)
        {
            // Ask the repo to create the new user
            var createdUser = await _userRepository.CreateNewUserAsync(
                newUserData.email,
                newUserData.password,
                newUserData.forename,
                newUserData.surname,
                newUserData.organization
            );

            // Email the user
            await SendAccountActivationEmail(
                new DetailedUserWithToken
                {
                    Forename = newUserData.forename,
                    Surname = newUserData.surname,
                    Token = createdUser.Token,
                    UserId = createdUser.UserId,
                    Email = newUserData.email
                }
            );

            return DetailedUserModelToDto(createdUser);
        }

        /// <summary>
        ///     This will update a user account in the database and email an authorization token to the user
        ///     if the email has changed.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="updateUserData">All of the information for the new user.</param>
        /// <param name="clientId"></param>
        /// <returns>Returns a UserDTO for the newly created user account.</returns>
        public async Task<DetailedUserTokenDTO> UpdateUserAsync(uint? userId,
            UserUpdateRequestDTO updateUserData,
            string clientId = null)
        {
            if (!userId.HasValue)
                throw new StandardExceptions.ImproperInputDataException("user id");

            // Get current user data
            var originalUserInfo = await _userRepository.GetDetailedUserByIdAsync(GetCurrentUserId());
            var resetActivation = updateUserData.email != null && originalUserInfo.Email != updateUserData.email;

            // Ask the repo to update the user (merge nul fields with the new request)
            await _userRepository.UpdateUserAsync(
                userId.Value,
                updateUserData.password,
                updateUserData.email ?? originalUserInfo.Email,
                resetActivation,
                updateUserData.forename ?? originalUserInfo.Forename,
                updateUserData.surname ?? originalUserInfo.Surname,
                updateUserData.organization ?? originalUserInfo.Organization
            );

            // Get the updated user info
            DetailedUserWithToken updatedUserWithInfo;

            if (resetActivation) // Create activation token and send email notification
            {
                updatedUserWithInfo = await _userRepository.CreateUserActivateTokenAsync(updateUserData.email);
                // Email the user
                await SendAccountActivationEmail(
                    new DetailedUserWithToken
                    {
                        Forename = updateUserData.forename,
                        Surname = updateUserData.surname,
                        Token = updatedUserWithInfo.Token,
                        UserId = updatedUserWithInfo.UserId,
                        Email = updateUserData.email
                    }
                );
            }
            else // Collect the updated account info
            {
                updatedUserWithInfo = await _userRepository.GetDetailedUserByIdAsync(GetCurrentUserId());
            }

            return DetailedUserModelToDto(updatedUserWithInfo);
        }

        /// <summary>
        ///     Confirms the registration of a new user via a secret authentication token, which
        ///     was sent to the user's email address.
        /// </summary>
        /// <param name="token">Secret authentication token for user's new account</param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<NoContentResult> ConfirmUserRegistrationAsync(string token, string clientId = null)
        {
            var userInfo = await _userRepository.GetDetailedUserByTokenAsync(token);
            await _userRepository.ConfirmAccountCreationAsync(token);

            // TODO: Use Razor to format this and ad organization name.
            const string emailBody = @"
<html><body>Dear $User,<br>
<br>
Congratulations! You have activated your Scripta Qumranica Electronica account.  Login <a href=""$WebServer"">here</a> 
to begins using the system.<br>
<br>
Best wishes,<br>
The Scripta Qumranica Electronica team</body></html>";
            const string emailSubject = "Confirmation of your Scripta Qumranica Electronica account activation";
            var name = !string.IsNullOrEmpty(userInfo.Forename) || !string.IsNullOrEmpty(userInfo.Surname)
                ? (userInfo.Forename + " " + userInfo.Surname).Trim()
                : userInfo.Email;
            await _emailSender.SendEmailAsync(
                userInfo.Email,
                emailSubject,
                emailBody.Replace("$User", name)
                    .Replace("$WebServer", webServer)
            );

            return new NoContentResult();
        }

        /// <summary>
        ///     Updates the email address for an account that has not yet been activated
        /// </summary>
        /// <param name="oldEmail">Email address that was originally entered when creating the account</param>
        /// <param name="newEmail">New email address to use for the account</param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<NoContentResult> UpdateUnactivatedAccountEmailAsync(string oldEmail,
            string newEmail,
            string clientId = null)
        {
            // Check if account is already activated
            await _userRepository.GetUnactivatedUserByEmailAsync(oldEmail);
            // Check for a conflicting email address and resolve if possible
            if (oldEmail != newEmail)
                await _userRepository.ResolveExistingUserConflictAsync(newEmail);
            await _userRepository.UpdateUnactivatedUserEmailAsync(oldEmail, newEmail);
            // Get the account info and send a new account activation email
            await ResendActivationEmail(newEmail);

            return new NoContentResult();
        }

        /// <summary>
        ///     Updates the email address for an account that has not yet been activated
        /// </summary>
        /// <param name="email">Email address that was originally entered when creating the account</param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<NoContentResult> ResendActivationEmail(string email, string clientId = null)
        {
            try
            {
                // Get the account info and send a new account activation email
                var userInfo = await _userRepository.GetUnactivatedUserByEmailAsync(email);
                await SendAccountActivationEmail(userInfo);
            }
            catch
            {
                // Rethrow error in development, otherwise the error is swallowed in production for security reasons.
                if (_env.IsDevelopment())
                    throw;
            }

            return new NoContentResult();
        }

        /// <summary>
        ///     Creates a token for validating a reset password request.  The token is emailed to the user.
        /// </summary>
        /// <param name="email">Email address of the user who has requested reset of a forgotten password</param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<NoContentResult> RequestResetLostPasswordAsync(string email, string clientId = null)
        {
            try
            {
                var userInfo = await _userRepository.RequestResetForgottenPasswordAsync(email);
                if (userInfo == null) // Silently return on error
                    return new NoContentResult();

                // Email the user
                // TODO: Use Razor to format this and ad organization name.
                const string emailBody = @"
<html><body>Dear $User,<br>
<br>
Sorry to hear that you have lost your password for Scripta Qumranica Electronica.  You may reset your password 
<a href=""$WebServer/changeForgottenPassword/token/$Token"">by clicking here</a>.<br>
<br>
Best wishes,<br>
The Scripta Qumranica Electronica team</body></html>";
                const string emailSubject = "Lost password for your Scripta Qumranica Electronica account";
                var name = !string.IsNullOrEmpty(userInfo.Forename) || !string.IsNullOrEmpty(userInfo.Surname)
                    ? (userInfo.Forename + " " + userInfo.Surname).Trim()
                    : userInfo.Email;
                await _emailSender.SendEmailAsync(
                    email,
                    emailSubject,
                    emailBody.Replace("$User", name)
                        .Replace("$Token", userInfo.Token)
                        .Replace("$WebServer", webServer)
                );
            }
            catch
            {
                // Rethrow error in development, otherwise the error is swallowed in production for security reasons.
                if (_env.IsDevelopment())
                    throw;
            }

            return new NoContentResult();
        }

        /// <summary>
        ///     Change password from old password to new password for the user's account.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="oldPassword">The old password for the user's account</param>
        /// <param name="newPassword">The new password for the user's account</param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<NoContentResult> ChangePasswordAsync(uint? userId,
            string oldPassword,
            string newPassword,
            string clientId = null)
        {
            if (!userId.HasValue)
                throw new StandardExceptions.ImproperInputDataException("user id");

            await _userRepository.ChangePasswordAsync(userId.Value, oldPassword, newPassword);
            return new NoContentResult();
        }

        /// <summary>
        ///     Resets the password for a user's account; the request is verified by a secret token
        ///     that was emailed to the user.
        /// </summary>
        /// <param name="token">Secret token for validating change password request.</param>
        /// <param name="password">New password for the user's account</param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<NoContentResult> ResetLostPasswordAsync(string token, string password, string clientId = null)
        {
            var userInfo = await _userRepository.ResetForgottenPasswordAsync(token, password);

            // TODO: Use Razor to format this and ad organization name.
            const string emailBody = @"
<html><body>Dear $User,<br>
<br>
You have recently changed your password for Scripta Qumranica Electronica.  If you feel you have received this email
in error, please contact the project administrator.<br>
<br>
Best wishes,<br>
The Scripta Qumranica Electronica team</body></html>";
            const string emailSubject = "Reset password for your Scripta Qumranica Electronica account";
            var name = !string.IsNullOrEmpty(userInfo.Forename) || !string.IsNullOrEmpty(userInfo.Surname)
                ? (userInfo.Forename + " " + userInfo.Surname).Trim()
                : userInfo.Email;
            await _emailSender.SendEmailAsync(
                userInfo.Email,
                emailSubject,
                emailBody.Replace("$User", name)
            );

            return new NoContentResult();
        }

        /// <summary>
        ///     This returns a UserInfo object that will persist only for the life of the current
        ///     HTTP request.  The UserInfo object can fetch the permissions if requested, and once
        ///     the permissions have been requested, they are "cached" for the life of the object.
        /// </summary>
        /// <param name="editionId">Id of the edition the user will be accessing</param>
        /// <param name="write">Whether the user must have write access</param>
        /// <param name="locking">Whether the user must have locking access</param>
        /// <param name="admin">Whether the user must have admin access</param>
        /// <returns></returns>
        public async Task<EditionUserInfo> GetCurrentUserObjectAsync(uint editionId,
            bool write = false,
            bool locking = false,
            bool admin = false)
        {
            var access = new EditionAccessLevels(true, write, locking, admin);

            var user = new EditionUserInfo(GetCurrentUserId(), editionId, _userRepository);
            await user.ReadPermissions();

            // Immediately reject requests without proper permissions
            if (access.Read
                && !user.MayRead)
                throw new StandardExceptions.NoReadPermissionsException(user);
            if (access.Write
                && !user.MayWrite)
                throw new StandardExceptions.NoWritePermissionsException(user);
            if (access.Locking
                && !user.MayLock)
                throw new StandardExceptions.NoLockPermissionsException(user);
            if (access.Admin
                && !user.IsAdmin)
                throw new StandardExceptions.NoAdminPermissionsException(user);

            return user;
        }

        /// <summary>
        ///     Constructs a unique, encrypted JWT for a user account.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="userId">The user's user id</param>
        /// <returns>Returns a JWT string</returns>
        private string BuildUserToken(string email, uint userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.Name, email),
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }
                ),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task SendAccountActivationEmail(DetailedUserWithToken userWithInfo)
        {
            // TODO: Add link to web endpoint when we know what that is. Use Razor to format this and ad organization name.
            const string emailBody = @"
<html><body>Dear $User,<br>
<br>
Thank you for registering with the Scripta Qumranica Electronica research platform.  
<a href=""$WebServer/activateUser/token/$Token"">Click here</a> to activate your new account.<br>
<br>
Best wishes,<br>
The Scripta Qumranica Electronica team</body></html>";
            const string emailSubject = "Activation of your Scripta Qumranica Electronica account";
            var name = !string.IsNullOrEmpty(userWithInfo.Forename) || !string.IsNullOrEmpty(userWithInfo.Surname)
                ? (userWithInfo.Forename + " " + userWithInfo.Surname).Trim()
                : userWithInfo.Email;
            await _emailSender.SendEmailAsync(
                userWithInfo.Email,
                emailSubject,
                emailBody.Replace("$User", name)
                    .Replace("$WebServer", webServer)
                    .Replace("$Token", userWithInfo.Token)
            );
        }

        /// <summary>
        ///     Packages a DetailedUser as a DTO to send back to the client.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static DetailedUserTokenDTO DetailedUserModelToDto(DetailedUserWithToken model)
        {
            return new DetailedUserTokenDTO
            {
                userId = model.UserId,
                email = model.Email,
                forename = model.Forename,
                surname = model.Surname,
                organization = model.Organization,
                activated = model.Activated
            };
        }

        /// <summary>
        ///     Packages a DetailedUser as a DTO to send back to the client.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static UserDTO UserModelToDto(User model)
        {
            return new UserDTO
            {
                userId = model.UserId,
                email = model.Email
            };
        }
    }

    public class EditionAccessLevels
    {
        public EditionAccessLevels(bool read = true, bool write = false, bool locking = false, bool admin = false)
        {
            Read = read;
            Write = write;
            Locking = locking;
            Admin = admin;
        }

        public bool Read { get; }
        public bool Write { get; }
        public bool Locking { get; }
        public bool Admin { get; }
    }
}