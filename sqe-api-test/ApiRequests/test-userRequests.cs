
using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


            public static partial class POST
            {
        

                public class V1_Users_Login
                : RequestObject<LoginRequestDTO, DetailedUserTokenDTO, DetailedUserTokenDTO>
                {
                    /// <summary>
        ///     Provides a JWT bearer token for valid email and password
        /// </summary>
        /// <param name="payload">JSON object with an email and password parameter</param>
        /// <returns>
        ///     A DetailedUserTokenDTO with a JWT for activated user accounts, or the email address of an unactivated user
        ///     account
        /// </returns>
                    public V1_Users_Login(LoginRequestDTO payload) 
                        : base(payload) { }
                }
        

                public class V1_Users_ChangeUnactivatedEmail
                : RequestObject<UnactivatedEmailUpdateRequestDTO, EmptyOutput, EmptyOutput>
                {
                    /// <summary>
        ///     Allows a user who has not yet activated their account to change their email address. This will not work if the user
        ///     account associated with the email address has already been activated
        /// </summary>
        /// <param name="payload">JSON object with the current email address and the new desired email address</param>
                    public V1_Users_ChangeUnactivatedEmail(UnactivatedEmailUpdateRequestDTO payload) 
                        : base(payload) { }
                }
        

                public class V1_Users_ChangeForgottenPassword
                : RequestObject<ResetForgottenUserPasswordRequestDTO, EmptyOutput, EmptyOutput>
                {
                    /// <summary>
        ///     Uses the secret token from /users/forgot-password to validate a reset of the user's password
        /// </summary>
        /// <param name="payload">A JSON object with the secret token and the new password</param>
                    public V1_Users_ChangeForgottenPassword(ResetForgottenUserPasswordRequestDTO payload) 
                        : base(payload) { }
                }
        

                public class V1_Users_ChangePassword
                : RequestObject<ResetLoggedInUserPasswordRequestDTO, EmptyOutput, EmptyOutput>
                {
                    /// <summary>
        ///     Changes the password for the currently logged in user
        /// </summary>
        /// <param name="payload">A JSON object with the old password and the new password</param>
                    public V1_Users_ChangePassword(ResetLoggedInUserPasswordRequestDTO payload) 
                        : base(payload) { }
                }
        

                public class V1_Users_ConfirmRegistration
                : RequestObject<AccountActivationRequestDTO, EmptyOutput, EmptyOutput>
                {
                    /// <summary>
        ///     Confirms registration of new user account.
        /// </summary>
        /// <param name="payload">JSON object with token from user registration email</param>
        /// <returns>Returns a DetailedUserDTO for the confirmed account</returns>
                    public V1_Users_ConfirmRegistration(AccountActivationRequestDTO payload) 
                        : base(payload) { }
                }
        

                public class V1_Users_ForgotPassword
                : RequestObject<ResetUserPasswordRequestDTO, EmptyOutput, EmptyOutput>
                {
                    /// <summary>
        ///     Sends a secret token to the user's email to allow password reset.
        /// </summary>
        /// <param name="payload">JSON object with the email address for the user who wants to reset a lost password</param>
                    public V1_Users_ForgotPassword(ResetUserPasswordRequestDTO payload) 
                        : base(payload) { }
                }
        

                public class V1_Users
                : RequestObject<NewUserRequestDTO, UserDTO, UserDTO>
                {
                    /// <summary>
        ///     Creates a new user with the submitted data.
        /// </summary>
        /// <param name="payload">A JSON object with all data necessary to create a new user account</param>
        /// <returns>Returns a UserDTO for the newly created account</returns>
                    public V1_Users(NewUserRequestDTO payload) 
                        : base(payload) { }
                }
        

                public class V1_Users_ResendActivationEmail
                : RequestObject<ResendUserAccountActivationRequestDTO, EmptyOutput, EmptyOutput>
                {
                    /// <summary>
        ///     Sends a new activation email for the user's account. This will not work if the user account associated with the
        ///     email address has already been activated.
        /// </summary>
        /// <param name="payload">JSON object with the current email address and the new desired email address</param>
                    public V1_Users_ResendActivationEmail(ResendUserAccountActivationRequestDTO payload) 
                        : base(payload) { }
                }
        
	}

            public static partial class PUT
            {
        

                public class V1_Users
                : RequestObject<UserUpdateRequestDTO, DetailedUserDTO, DetailedUserDTO>
                {
                    /// <summary>
        ///     Updates a user's registration details.  Note that the if the email address has changed, the account will be set to
        ///     inactive until the account is activated with the secret token.
        /// </summary>
        /// <param name="payload">
        ///     A JSON object with all data necessary to update a user account.  Null fields (but not empty
        ///     strings!) will be populated with existing user data
        /// </param>
        /// <returns>Returns a DetailedUserDTO with the updated user account details</returns>
                    public V1_Users(UserUpdateRequestDTO payload) 
                        : base(payload) { }
                }
        
	}

            public static partial class GET
            {
        

                public class V1_Users
                : RequestObject<EmptyInput, UserDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides the user details for a user with valid JWT in the Authorize header
        /// </summary>
        /// <returns>A UserDTO for user account.</returns>
                    public V1_Users() 
                        : base() { }
                }
        
	}

}
