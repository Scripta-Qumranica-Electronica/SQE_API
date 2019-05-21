using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.SqeHttpApi.Server.Helpers
{
    public interface IUserService
    {
        Task<LoginResponseDTO> AuthenticateAsync(string username, string password); // TODO: Return a User object, not a LoginResponse
        UserDTO GetCurrentUser();
        uint? GetCurrentUserId();
        UserInfo GetCurrentUserObject(uint? editionId = null);
        Task<UserDTO> CreateNewUserAsync(NewUserRequestDTO newUserData);
        Task<UserDTO> UpdateUserAsync(UserInfo user, NewUserRequestDTO newUserData);
        Task UpdateUnactivatedAccountEmail(string oldEmail, string newEmail);
        Task ConfirmUserRegistrationAsync(string token);
        Task ChangePasswordAsync(UserInfo user, string oldPassword, string newPassword);
        Task RequestResetLostPasswordAsync(string email);
        Task ResetLostPasswordAsync(string token, string password);
    }

    public class UserService : IUserService
    {
        private readonly AppSettings _appSettings;
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _accessor;
        private readonly IEmailSender _emailSender;


        public UserService(IOptions<AppSettings> appSettings, IUserRepository userRepository, IHttpContextAccessor accessor, IEmailSender emailSender)

        // http://jasonwatmore.com/post/2018/08/14/aspnet-core-21-jwt-authentication-tutorial-with-example-api
        {
            _userRepository = userRepository;
            _appSettings = appSettings.Value; // For the secret key
            _accessor = accessor;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Authenticate user by credentials and create a JWT for the user.
        /// </summary>
        /// <param name="username">The user's username</param>
        /// <param name="password">The user's password</param>
        /// <returns>Returns a response including a JWT for the user to use as a Bearer token, or an email 
        /// address if the user account has not yet been activated.</returns>
        public async Task<LoginResponseDTO> AuthenticateAsync(string username, string password)
        {
            var result = await _userRepository.GetUserByPasswordAsync(username, password);

            if (result == null)
                return null;

            if (result.Activated == 1) // The user account is activated and a token should be returned
                return new LoginResponseDTO
                {
                    userName = result.UserName,
                    userId = result.UserId,
                    token = BuildUserToken(result.UserName, result.UserId).ToString(),
                };
            else // The user account is not yet activated and an email address should be returned
                return new LoginResponseDTO
                {
                    userName = result.UserName,
                    userId = result.UserId,
                    email = result.Email,
                };
        }

        /// <summary>
        /// Constructs a unique, encrypted JWT for a user account.
        /// </summary>
        /// <param name="userName">The user's username</param>
        /// <param name="userId">The user's user id</param>
        /// <returns>Returns a JWT string</returns>
        private string BuildUserToken(string userName, uint userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var s3 = Convert.ToBase64String(key);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        
        public UserDTO GetCurrentUser()
        {
            // TODO: Check if ...User.Identity.Name exists. Return null if not.
            var currentUserName = _accessor.HttpContext.User.Identity.Name;
            var currentUserId = GetCurrentUserId();

           return (currentUserName == null || !currentUserId.HasValue) ?
                null :
                new UserDTO
                {
                    userName = currentUserName,
                    userId = currentUserId.Value,
                };
        }

        public uint? GetCurrentUserId()
        {
            var identity = (ClaimsIdentity)_accessor.HttpContext.User.Identity;
            var claims = identity.Claims;
            foreach (var claim in claims)
            {
                var split = claim.Type.Split("/");
                if (split[split.Length - 1] == "nameidentifier")
                {
                    return uint.Parse(claim.Value);
                }
            }
            return null;
        }

        /// <summary>
        /// This will create a new user account in the database and email an authorization token to the user.
        /// </summary>
        /// <param name="newUserData">All of the information for the new user.</param>
        /// <returns>Returns a UserDTO for the newly created user account.</returns>
        public async Task<UserDTO> CreateNewUserAsync(NewUserRequestDTO newUserData)
        {
            // Ask the repo to create the new user
            var createdUser = await _userRepository.CreateNewUserAsync(newUserData.userName, newUserData.email, newUserData.password,
                newUserData.forename, newUserData.surname, newUserData.organization);
            
            // Email the user
            await SendAccountActivationEmail(new DetailedUserToken()
            {
                Forename = newUserData.forename,
                Surname = newUserData.surname,
                UserName = createdUser.UserName,
                Token = createdUser.Token,
                UserId = createdUser.UserId,
                Email = newUserData.email
            });
            
            return UserModelToDTO(createdUser);
        }

        /// <summary>
        /// This will create a new user account in the database and email an authorization token to the user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="updatedUserData">All of the information for the new user.</param>
        /// <returns>Returns a UserDTO for the newly created user account.</returns>
        public async Task<UserDTO> UpdateUserAsync(UserInfo user, NewUserRequestDTO updatedUserData)
        {
            // Get current user data and merge with the new request
            var originalUserInfo = await _userRepository.GetDetailedUserById(user);
            var resetActivation = originalUserInfo.Email != updatedUserData.email;
            if (resetActivation) // Make sure email is unique
                await _userRepository.ResolveExistingUserConflictAsync("", updatedUserData.email);
            if (originalUserInfo.UserName != updatedUserData.userName) // Make sure username is unique
                await _userRepository.ResolveExistingUserConflictAsync(updatedUserData.userName, "");
            // Ask the repo to update the user
            await _userRepository.UpdateUserAsync(
                user, 
                updatedUserData.userName ?? originalUserInfo.UserName, 
                updatedUserData.email ?? originalUserInfo.Email,
                resetActivation,
                updatedUserData.forename ?? originalUserInfo.Forename, 
                updatedUserData.surname ?? originalUserInfo.Surname, 
                updatedUserData.organization ?? originalUserInfo.Organization);
            
            // Get the updated user info
            DetailedUserToken updatedUserInfo;

            if (resetActivation) // Create activation token and send email notification
            {
                updatedUserInfo = await _userRepository.CreateUserActivateTokenAsync(
                    updatedUserData.userName,
                    updatedUserData.email);
                // Email the user
                await SendAccountActivationEmail(new DetailedUserToken()
                {
                    Forename = updatedUserData.forename,
                    Surname = updatedUserData.surname,
                    UserName = updatedUserData.userName,
                    Token = updatedUserInfo.Token,
                    UserId = updatedUserInfo.UserId,
                    Email = updatedUserData.email
                });
            }
            else // Collect the updated account info
            {
                updatedUserInfo = await _userRepository.GetDetailedUserById(user);
            }
            
            return UserModelToDTO(updatedUserInfo);
        }

        private async Task SendAccountActivationEmail(DetailedUserToken userInfo)
        {
            // TODO: Add link to web endpoint when we know what that is. Can token be in URL query?
            const string emailBody = @"
<html><body>Dear $User,<br>
<br>
Thank you for registering with the Scripta Qumranica Electronica research platform.  The token
to activate your new account is: $Token.<br>
<br>
Best wishes,<br>
The Scripta Qumranica Electronica team</body></html>";
            const string emailSubject = "Activation of your Scripta Qumranica Electronica account";
            var name = !string.IsNullOrEmpty(userInfo.Forename) || !string.IsNullOrEmpty(userInfo.Surname)
                ? (userInfo.Forename + " " + userInfo.Surname).Trim() 
                : userInfo.UserName;
            await _emailSender.SendEmailAsync(
                userInfo.Email,
                emailSubject,
                emailBody.Replace("$User", name)
                    .Replace("$Token", userInfo.Token)
            );
        }

        /// <summary>
        /// Confirms the registration of a new user via a secret authentication token, which
        /// was sent to the user's email address.
        /// </summary>
        /// <param name="token">Secret authentication token for user's new account</param>
        /// <returns></returns>
        public async Task ConfirmUserRegistrationAsync(string token)
        {
            await _userRepository.ConfirmAccountCreationAsync(token);
        }

        /// <summary>
        /// Updates the email address for an account that has not yet been activated
        /// </summary>
        /// <param name="oldEmail">Email address that was originally entered when creating the account</param>
        /// <param name="newEmail">New email address to use for the account</param>
        /// <returns></returns>
        public async Task UpdateUnactivatedAccountEmail(string oldEmail, string newEmail)
        {
            await _userRepository.UpdateUnactivatedUserEmailAsync(oldEmail, newEmail);
            var userInfo = await _userRepository.GetUserByEmailAsync(newEmail);
            await SendAccountActivationEmail(userInfo);
        }
        
        /// <summary>
        /// Creates a token for validating a reset password request.  The token is emailed to the user.
        /// </summary>
        /// <param name="email">Email address of the user who has requested reset of a forgotten password</param>
        /// <returns></returns>
        public async Task RequestResetLostPasswordAsync(string email)
        {
            var userInfo = await _userRepository.RequestResetForgottenPasswordAsync(email);
            if (userInfo == null) // Silently return on error
                return;
            
            // Email the user
            // TODO: Add link to web endpoint when we know what that is. Can token be in URL query?
            const string emailBody = @"
<html><body>Dear $User,<br>
<br>
Sorry to hear that you have lost your password for Scripta Qumranica Electronica.  You may reset your password with the token: $Token.<br>
<br>
Best wishes,<br>
The Scripta Qumranica Electronica team</body></html>";
            const string emailSubject = "Lost password for your Scripta Qumranica Electronica account";
            await _emailSender.SendEmailAsync(
                email,
                emailSubject,
                emailBody.Replace("$User", userInfo.UserName)
                    .Replace("$Token", userInfo.Token)
            );
        }

        /// <summary>
        /// Change password from old password to new password for the user's account.
        /// </summary>
        /// <param name="user">User object with all information for the user requesting a password change</param>
        /// <param name="oldPassword">The old password for the user's account</param>
        /// <param name="newPassword">The new password for the user's account</param>
        /// <returns></returns>
        public async Task ChangePasswordAsync(UserInfo user, string oldPassword, string newPassword)
        {
            await _userRepository.ChangePasswordAsync(user, oldPassword, newPassword);
        }

        /// <summary>
        /// Resets the password for a user's account; the request is verified by a secret token
        /// that was emailed to the user.
        /// </summary>
        /// <param name="token">Secret token for validating change password request.</param>
        /// <param name="password">New password for the user's account</param>
        /// <returns></returns>
        public async Task ResetLostPasswordAsync(string token, string password)
        {
            var userInfo = await _userRepository.ResetForgottenPasswordAsync(token, password);
            
            const string emailBody = @"
<html><body>Dear $User,<br>
<br>
You have recently changed your password for Scripta Qumranica Electronica.  If you feel you have received this email
in error, please contact the project administrator.<br>
<br>
Best wishes,<br>
The Scripta Qumranica Electronica team</body></html>";
            const string emailSubject = "Reset password for your Scripta Qumranica Electronica account";
            await _emailSender.SendEmailAsync(
                userInfo.Email,
                emailSubject,
                emailBody.Replace("$User", userInfo.UserName)
            );
        }

        /// <summary>
        /// This returns a UserInfo object that will persist only for the life of the current
        /// HTTP request.  The UserInfo object can fetch the permissions if requested, and once
        /// the permissions have been requested, they are "cached" for the life of the object.
        /// </summary>
        /// <param Name="editionId">Optional id of the edition the user is requesting to work with</param>
        /// <returns></returns>
        public UserInfo GetCurrentUserObject(uint? editionId = null)
        {
            return new UserInfo(GetCurrentUserId(), editionId, _userRepository);
        }


        public static UserDTO UserModelToDTO(DataAccess.Models.UserToken model)
        {
            return new UserDTO
            {
                userId = model.UserId,
                userName = model.UserName,
            };
        }
    }
}
