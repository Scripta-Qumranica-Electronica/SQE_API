using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
    #region Request DTO's

    public class LoginRequestDTO
    {
        [Required]
        [RegularExpression(@"^.*@.*\..*$", ErrorMessage = "The email address appears to be improperly formatted")]
        public string email { get; set; }

        [Required]
        [StringLength(1024, MinimumLength = 4, ErrorMessage = "Password must be more than 4 characters long")]
        public string password { get; set; }
    }

    #region Account update and registration DTO's

    public class NewUserRequestDTO
    {
        /// <summary>
        ///     An object containing all data necessary to create a new user account. This is also used
        ///     when updating existing user account details, since we need to verify the password in such instances.
        /// </summary>
        /// <param name="email">Email address for the new user account, must be unique</param>
        /// <param name="password">Password for the new user account</param>
        /// <param name="organization">Name of affiliated organization (if any)</param>
        /// <param name="forename">The user's given name (may be empty)</param>
        /// <param name="surname">The user's family name (may be empty)</param>
        public NewUserRequestDTO(string email, string password, string organization, string forename, string surname)
        {
            this.password = password;
            this.email = email;
            this.organization = organization;
            this.forename = forename;
            this.surname = surname;
        }

        [Required]
        [StringLength(1024, MinimumLength = 4, ErrorMessage = "Password must be more than 4 characters long")]
        public string password { get; set; }
        [Required]
        [RegularExpression(@"^.*@.*\..*$", ErrorMessage = "The email address appears to be improperly formatted")]
        public string email { get; set; }
        public string organization { get; set; }
        public string forename { get; set; }
        public string surname { get; set; }
    }

    public class UserUpdateRequestDTO : NewUserRequestDTO
    {
        public UserUpdateRequestDTO(string email, string password, string organization, string forename, string surname)
            : base(email, password, organization, forename, surname) { }
    }

    #endregion Account update and registration DTO's

    #region Account activation DTO's

    public class AccountActivationRequestDTO
    {
        [Required]
        public string token { get; set; }
    }

    public class ResendUserAccountActivationRequestDTO
    {
        [Required]
        [RegularExpression(@"^.*@.*\..*$", ErrorMessage = "The email address appears to be improperly formatted")]
        public string email { get; set; }
    }

    public class UnactivatedEmailUpdateRequestDTO : ResendUserAccountActivationRequestDTO
    {
        [Required]
        [RegularExpression(@"^.*@.*\..*$", ErrorMessage = "The email address appears to be improperly formatted")]
        public string newEmail { get; set; }
    }

    #endregion Account activation DTO's

    #region Password management DTO's

    public class ResetUserPasswordRequestDTO
    {
        [Required]
        [RegularExpression(@"^.*@.*\..*$", ErrorMessage = "The email address appears to be improperly formatted")]
        public string email { get; set; }
    }

    public class ResetForgottenUserPasswordRequestDTO : AccountActivationRequestDTO
    {
        [Required]
        [StringLength(1024, MinimumLength = 4, ErrorMessage = "Password must be more than 4 characters long")]
        public string password { get; set; }
    }

    public class ResetLoggedInUserPasswordRequestDTO
    {
        [Required]
        public string oldPassword { get; set; }
        [Required]
        [StringLength(1024, MinimumLength = 4, ErrorMessage = "Password must be more than 4 characters long")]
        public string newPassword { get; set; }
    }

    #endregion Password management DTO's

    #endregion Request DTO's

    #region Response DTO's

    // The minimal data necessary to identify a user
    public class UserDTO
    {
        public uint userId { get; set; }
        public string email { get; set; }
    }

    // More detailed user data, this could be seen by colleagues who share an edition
    public class DetailedUserDTO : UserDTO
    {
        public string forename { get; set; }
        public string surname { get; set; }
        public string organization { get; set; }
        public bool activated { get; set; }
    }

    // A user may only receive his or her own DetailedUserTokenDTO
    public class DetailedUserTokenDTO : DetailedUserDTO
    {
        public string token { get; set; }
    }

    public class EditorDTO
    {
        public string forename { get; set; }
        public string surname { get; set; }
        public string organization { get; set; }
    }

    #endregion Response DTO's
}