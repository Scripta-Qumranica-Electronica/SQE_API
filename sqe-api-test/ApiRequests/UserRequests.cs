using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_Users : RequestObject<EmptyInput, UserDTO>
        {
            public V1_Users() : base(null)
            {
            }
        }
    }

    public static partial class Post
    {
        public class V1_Users : RequestObject<NewUserRequestDTO, UserDTO>
        {
            public V1_Users(NewUserRequestDTO payload) : base(payload)
            {
            }
        }

        public class V1_Users_Login : RequestObject<LoginRequestDTO, DetailedUserTokenDTO>
        {
            public V1_Users_Login(LoginRequestDTO payload) : base(payload)
            {
            }
        }

        public class V1_Users_ChangeUnactivatedEmail : RequestObject<UnactivatedEmailUpdateRequestDTO, EmptyOutput>
        {
            public V1_Users_ChangeUnactivatedEmail(UnactivatedEmailUpdateRequestDTO payload) : base(payload)
            {
            }
        }

        public class V1_Users_ChangeForgottenPassword : RequestObject<ResetForgottenUserPasswordRequestDTO, EmptyOutput>
        {
            public V1_Users_ChangeForgottenPassword(ResetForgottenUserPasswordRequestDTO payload) : base(payload)
            {
            }
        }

        public class V1_Users_ChangePassword : RequestObject<ResetLoggedInUserPasswordRequestDTO, EmptyOutput>
        {
            public V1_Users_ChangePassword(ResetLoggedInUserPasswordRequestDTO payload) : base(payload)
            {
            }
        }

        public class V1_Users_ConfirmRegistration : RequestObject<AccountActivationRequestDTO, EmptyOutput>
        {
            public V1_Users_ConfirmRegistration(AccountActivationRequestDTO payload) : base(payload)
            {
            }
        }

        public class V1_Users_ForgotPassword : RequestObject<ResetUserPasswordRequestDTO, EmptyOutput>
        {
            public V1_Users_ForgotPassword(ResetUserPasswordRequestDTO payload) : base(payload)
            {
            }
        }

        public class V1_Users_ResendActivationEmail : RequestObject<ResendUserAccountActivationRequestDTO, EmptyOutput>
        {
            public V1_Users_ResendActivationEmail(ResendUserAccountActivationRequestDTO payload) : base(payload)
            {
            }
        }
    }

    public static partial class Put
    {
        public class V1_Users : RequestObject<UserUpdateRequestDTO, DetailedUserDTO>
        {
            public V1_Users(UserUpdateRequestDTO payload) : base(payload)
            {
            }
        }
    }

    public static partial class Delete
    {
    }
}