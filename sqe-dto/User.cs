using System;
using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
	#region Request DTO's

	public class LoginRequestDTO
	{
		[Required]
		[RegularExpression(
				@"^.*@.*\..*$"
				, ErrorMessage = "The email address appears to be improperly formatted")]
		public string email { get; set; }

		[Required]
		[StringLength(
				1024
				, MinimumLength = 4
				, ErrorMessage = "Password must be more than 4 characters long")]
		public string password { get; set; }
	}

	#region Account update and registration DTO's

	public class UserUpdateRequestDTO
	{
		/// <summary>
		///  An object containing all data necessary to create a new user account. This is also used
		///  when updating existing user account details, since we need to verify the password in such instances.
		/// </summary>
		/// <param name="email">Email address for the new user account, must be unique</param>
		/// <param name="password">Password for the new user account</param>
		/// <param name="organization">Name of affiliated organization (if any)</param>
		/// <param name="forename">The user's given name (may be empty)</param>
		/// <param name="surname">The user's family name (may be empty)</param>
		public UserUpdateRequestDTO(
				string   email
				, string password
				, string organization
				, string forename
				, string surname)
		{
			this.password = password;
			this.email = email;
			this.organization = organization;
			this.forename = forename;
			this.surname = surname;
		}

		public UserUpdateRequestDTO() : this(
				string.Empty
				, string.Empty
				, string.Empty
				, string.Empty
				, string.Empty) { }

		[Required]
		public string password { get; set; }

		public string email        { get; set; }
		public string organization { get; set; }
		public string forename     { get; set; }
		public string surname      { get; set; }
	}

	public class NewUserRequestDTO : UserUpdateRequestDTO
	{
		public NewUserRequestDTO(
				string   email
				, string password
				, string organization
				, string forename
				, string surname) : base(
				email
				, password
				, organization
				, forename
				, surname)
		{
			this.email = email;
			this.password = password;
		}

		public NewUserRequestDTO() : this(
				string.Empty
				, string.Empty
				, string.Empty
				, string.Empty
				, string.Empty) { }

		[Required]
		[RegularExpression(
				@"^.*@.*\..*$"
				, ErrorMessage = "The email address appears to be improperly formatted")]
		public new string email { get; set; }

		[Required]
		[StringLength(
				1024
				, MinimumLength = 4
				, ErrorMessage = "Password must be more than 4 characters long")]
		public new string password { get; set; }
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
		[RegularExpression(
				@"^.*@.*\..*$"
				, ErrorMessage = "The email address appears to be improperly formatted")]
		public string email { get; set; }
	}

	public class UnactivatedEmailUpdateRequestDTO : ResendUserAccountActivationRequestDTO
	{
		[Required]
		[RegularExpression(
				@"^.*@.*\..*$"
				, ErrorMessage = "The email address appears to be improperly formatted")]
		public string newEmail { get; set; }
	}

	#endregion Account activation DTO's

	#region Password management DTO's

	public class ResetUserPasswordRequestDTO
	{
		[Required]
		[RegularExpression(
				@"^.*@.*\..*$"
				, ErrorMessage = "The email address appears to be improperly formatted")]
		public string email { get; set; }
	}

	public class ResetForgottenUserPasswordRequestDTO : AccountActivationRequestDTO
	{
		[Required]
		[StringLength(
				1024
				, MinimumLength = 4
				, ErrorMessage = "Password must be more than 4 characters long")]
		public string password { get; set; }
	}

	public class ResetLoggedInUserPasswordRequestDTO
	{
		[Required]
		public string oldPassword { get; set; }

		[Required]
		[StringLength(
				1024
				, MinimumLength = 4
				, ErrorMessage = "Password must be more than 4 characters long")]
		public string newPassword { get; set; }
	}

	#endregion Password management DTO's

	#endregion Request DTO's

	#region Response DTO's

	// The minimal data necessary to identify a user
	public class UserDTO
	{
		[Required]
		public uint userId { get; set; }

		[Required]
		public string email { get; set; }
	}

	// More detailed user data, this could be seen by colleagues who share an edition
	public class DetailedUserDTO : UserDTO
	{
		public string forename     { get; set; }
		public string surname      { get; set; }
		public string organization { get; set; }

		[Required]
		public bool activated { get; set; }
	}

	// A user may only receive his or her own DetailedUserTokenDTO
	public class DetailedUserTokenDTO : DetailedUserDTO
	{
		[Required]
		public string token { get; set; }
	}

	public class EditorDTO
	{
		[Required]
		public string email { get; set; }

		public string forename     { get; set; }
		public string surname      { get; set; }
		public string organization { get; set; }
	}

	public class UserDataStoreDTO
	{
		[Required]
		[StringLength(
				1000000
				, MinimumLength = 2
				, ErrorMessage = "The submitted data may not be larger than 1000000 character")]
		public string data { get; set; }
	}

	#endregion Response DTO's

	public class DatabaseVersionDTO
	{
		public string   version     { get; set; }
		public DateTime lastUpdated { get; set; }
	}

	public class APIVersionDTO : DatabaseVersionDTO { }

	// When user reports a problem in the app, a Github issue is created
	public class GithubIssueReportDTO
	{
		[Required]
		[StringLength(
				100
				, MinimumLength = 3
				, ErrorMessage = "The submitted title may not be larger than 100 characters")]
		public string title { get; set; }

		[Required]
		[StringLength(
				1000
				, MinimumLength = 3
				, ErrorMessage = "The submitted message body may not be larger than 1000 characters")]
		public string body { get; set; }
	}
}
