/*
 * This file is automatically generated by the GenerateTestRequestObjects
 * project in the Utilities folder. Do not edit this file directly as
 * its contents may be overwritten at any point.
 *
 * Should a class here need to be altered for any reason, you should look
 * first to the auto generation program for possible updating to include
 * the needed special case. Otherwise, it is possible to create your own
 * manually written ApiRequest object, though this is generally discouraged.
 */


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


    public static partial class Get
    {


        public class V1_Users
        : RequestObject<EmptyInput, UserDTO>
        {




            /// <summary>
            ///     Provides the user details for a user with valid JWT in the Authorize header
            /// </summary>
            /// <returns>A UserDTO for user account.</returns>
            public V1_Users()

            {


            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString());
            }


        }
    }

    public static partial class Post
    {


        public class V1_Users_Login
        : RequestObject<LoginRequestDTO, DetailedUserTokenDTO>
        {
            private readonly LoginRequestDTO _payload;



            /// <summary>
            ///     Provides a JWT bearer token for valid email and password
            /// </summary>
            /// <param name="payload">JSON object with an email and password parameter</param>
            /// <returns>
            ///     A DetailedUserTokenDTO with a JWT for activated user accounts, or the email address of an unactivated user
            ///     account
            /// </returns>
            public V1_Users_Login(LoginRequestDTO payload)
                : base(payload)
            {
                _payload = payload;

            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
            }


        }

        public class V1_Users_ChangeUnactivatedEmail
        : RequestObject<UnactivatedEmailUpdateRequestDTO, EmptyOutput>
        {
            private readonly UnactivatedEmailUpdateRequestDTO _payload;



            /// <summary>
            ///     Allows a user who has not yet activated their account to change their email address. This will not work if the user
            ///     account associated with the email address has already been activated
            /// </summary>
            /// <param name="payload">JSON object with the current email address and the new desired email address</param>
            public V1_Users_ChangeUnactivatedEmail(UnactivatedEmailUpdateRequestDTO payload)
                : base(payload)
            {
                _payload = payload;

            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
            }


        }

        public class V1_Users_ChangeForgottenPassword
        : RequestObject<ResetForgottenUserPasswordRequestDTO, EmptyOutput>
        {
            private readonly ResetForgottenUserPasswordRequestDTO _payload;



            /// <summary>
            ///     Uses the secret token from /users/forgot-password to validate a reset of the user's password
            /// </summary>
            /// <param name="payload">A JSON object with the secret token and the new password</param>
            public V1_Users_ChangeForgottenPassword(ResetForgottenUserPasswordRequestDTO payload)
                : base(payload)
            {
                _payload = payload;

            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
            }


        }

        public class V1_Users_ChangePassword
        : RequestObject<ResetLoggedInUserPasswordRequestDTO, EmptyOutput>
        {
            private readonly ResetLoggedInUserPasswordRequestDTO _payload;



            /// <summary>
            ///     Changes the password for the currently logged in user
            /// </summary>
            /// <param name="payload">A JSON object with the old password and the new password</param>
            public V1_Users_ChangePassword(ResetLoggedInUserPasswordRequestDTO payload)
                : base(payload)
            {
                _payload = payload;

            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
            }


        }

        public class V1_Users_ConfirmRegistration
        : RequestObject<AccountActivationRequestDTO, EmptyOutput>
        {
            private readonly AccountActivationRequestDTO _payload;



            /// <summary>
            ///     Confirms registration of new user account.
            /// </summary>
            /// <param name="payload">JSON object with token from user registration email</param>
            /// <returns>Returns a DetailedUserDTO for the confirmed account</returns>
            public V1_Users_ConfirmRegistration(AccountActivationRequestDTO payload)
                : base(payload)
            {
                _payload = payload;

            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
            }


        }

        public class V1_Users_ForgotPassword
        : RequestObject<ResetUserPasswordRequestDTO, EmptyOutput>
        {
            private readonly ResetUserPasswordRequestDTO _payload;



            /// <summary>
            ///     Sends a secret token to the user's email to allow password reset.
            /// </summary>
            /// <param name="payload">JSON object with the email address for the user who wants to reset a lost password</param>
            public V1_Users_ForgotPassword(ResetUserPasswordRequestDTO payload)
                : base(payload)
            {
                _payload = payload;

            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
            }


        }

        public class V1_Users
        : RequestObject<NewUserRequestDTO, UserDTO>
        {
            private readonly NewUserRequestDTO _payload;



            /// <summary>
            ///     Creates a new user with the submitted data.
            /// </summary>
            /// <param name="payload">A JSON object with all data necessary to create a new user account</param>
            /// <returns>Returns a UserDTO for the newly created account</returns>
            public V1_Users(NewUserRequestDTO payload)
                : base(payload)
            {
                _payload = payload;

            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
            }


        }

        public class V1_Users_ResendActivationEmail
        : RequestObject<ResendUserAccountActivationRequestDTO, EmptyOutput>
        {
            private readonly ResendUserAccountActivationRequestDTO _payload;



            /// <summary>
            ///     Sends a new activation email for the user's account. This will not work if the user account associated with the
            ///     email address has already been activated.
            /// </summary>
            /// <param name="payload">JSON object with the current email address and the new desired email address</param>
            public V1_Users_ResendActivationEmail(ResendUserAccountActivationRequestDTO payload)
                : base(payload)
            {
                _payload = payload;

            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
            }


        }
    }

    public static partial class Put
    {


        public class V1_Users
        : RequestObject<UserUpdateRequestDTO, DetailedUserDTO>
        {
            private readonly UserUpdateRequestDTO _payload;



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
                : base(payload)
            {
                _payload = payload;

            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _payload);
            }


        }
    }

}
