using System.ComponentModel.DataAnnotations;


namespace SQE.SqeHttpApi.Server.DTOs
{
    public class LoginRequestDTO
    {
        [Required]
        public string userName { get; set; }

        [Required]
        public string password { get; set; }
    }

    public class UserDTO
    {
        public uint userId { get; set; }
        public string userName { get; set; }
    }

    public class LoginResponseDTO : UserDTO
    {
        public string token { get; set; }
    }
    
    public class NewUserRequestDTO
    {
        /// <summary>
        /// An object containing all data necessary to create a new user account
        /// </summary>
        /// <param name="userName">Short username for the new user account, must be unique</param>
        /// <param name="email">Email address for the new user account, must be unique</param>
        /// <param name="password">Password for the new user account</param>
        /// <param name="organization">Name of affiliated organization (if any)</param>
        /// <param name="forename">The user's given name (may be empty)</param>
        /// <param name="surname">The user's family name (may be empty)</param>
        public NewUserRequestDTO(string userName, string email, string password, string organization, string forename, string surname)
        {
            this.userName = userName;
            this.email = email;
            this.password = password;
            this.organization = organization;
            this.forename = forename;
            this.surname = surname;
        }

        public string userName { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string organization { get; set; }
        public string forename { get; set; }
        public string surname { get; set; }
    }

    public class ResetUserPasswordRequestDTO
    {
        public string email { get; set; }
    }
    
    public class ResetLoggedInUserPasswordRequestDTO
    {
        public string oldPassword { get; set; }
        public string newPassword { get; set; }
    }
    
    public class EmailTokenDTO
    {
        public string token { get; set; }
    }
    
    public class ResetForgottenUserPasswordDTO : EmailTokenDTO
    {
        public string password { get; set; }
    }
}
