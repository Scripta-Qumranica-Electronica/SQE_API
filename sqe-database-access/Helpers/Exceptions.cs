using System;
using System.Collections.Generic;
using System.Net;
using SQE.DatabaseAccess.Models;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.DatabaseAccess.Helpers
{
	public interface IExceptionWithData
	{
		// Property declaration:
		Dictionary<string, object> CustomReturnedData { get; set; }
	}

	#region Exception Base Class

	// Exceptions of this class will be caught by the middleware and thrown back to HTTP requests with the proper
	// response codes.  This enables a RESTful experience regardless of whether a request is made via HTTP or by
	// websocket.  In either case the HTTP router or the Hub will catch a child of the ApiException class
	// and return that information to the user.
	// Errors should be thrown properly at the lowest levels of code, this will reduce boiler-plate in higher
	// level functions like the Services and ensure more consistent responses.
	public abstract class ApiException : Exception
	{
		public readonly HttpStatusCode StatusCode;
		public          string         Error;

		protected ApiException(HttpStatusCode httpStatusCode) => StatusCode = httpStatusCode;

		protected ApiException(HttpStatusCode httpStatusCode, string error) : this(httpStatusCode)
			=> Error = error;
	}

	#endregion Exception Base Class

	#region Exception Main Subclasses

	public abstract class ForbiddenDataAccessException : ApiException
	{
		private const HttpStatusCode httpStatusCode = HttpStatusCode.Forbidden;

		protected ForbiddenDataAccessException() : base(httpStatusCode) { }
	}

	public abstract class LockedDataException : ApiException
	{
		private const HttpStatusCode httpStatusCode = HttpStatusCode.Locked;

		protected LockedDataException() : base(httpStatusCode) { }
	}

	public abstract class UnauthorizedException : ApiException
	{
		private const HttpStatusCode httpStatusCode = HttpStatusCode.Unauthorized;

		protected UnauthorizedException() : base(httpStatusCode) { }
	}

	public abstract class DataNotFoundException : ApiException
	{
		private const HttpStatusCode httpStatusCode = HttpStatusCode.NotFound;

		protected DataNotFoundException() : base(httpStatusCode) { }
	}

	public abstract class BadInputException : ApiException
	{
		private const HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;

		protected BadInputException() : base(httpStatusCode) { }
	}

	public abstract class ConflictingInputException : ApiException
	{
		private const HttpStatusCode httpStatusCode = HttpStatusCode.Conflict;

		protected ConflictingInputException() : base(httpStatusCode) { }
	}

	public abstract class ServerErrorException : ApiException
	{
		private const HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;

		protected ServerErrorException() : base(httpStatusCode) { }
	}

	#endregion Exception Main Subclasses

	// This is a collection of ready-made errors that can be thrown. They include a standard HTTP status error code
	// and an internal project error code with accompanying message in English.
	public static class StandardExceptions
	{
		#region System errors

		public class EmailNotSentException : ServerErrorException
		{
			private const string customMsg =
					"Failed sending email to $Email. This is probably a server error and should be reported to the webmaster.";

			public EmailNotSentException(string email)
				=> Error = customMsg.Replace("$Email", email);
		}

		#endregion System errors

		#region Permissions errors

		public class NoAuthorizationException : ForbiddenDataAccessException
		{
			private const string customMsg =
					"The client must be authorized (logged in) for this request.";

			public NoAuthorizationException() => Error = customMsg;
		}

		public class NoPermissionsException : ForbiddenDataAccessException
		{
			private const string customMsg =
					"User $UserId has no permissions associated with edition $EditionId.";

			public NoPermissionsException(UserInfo editionUser) => Error = customMsg
																		   .Replace(
																				   "$UserId"
																				   , editionUser
																					 .userId
																					 .ToString())
																		   .Replace(
																				   "$EditionId"
																				   , editionUser
																					 .EditionId
																					 .ToString());
		}

		public class NoReadPermissionsException : ForbiddenDataAccessException
		{
			private const string customMsg =
					"User $UserId does not have read access to edition $EditionId.";

			public NoReadPermissionsException(UserInfo editionUser) => Error =
					customMsg.Replace("$UserId", editionUser.userId.ToString())
							 .Replace("$EditionId", editionUser.EditionId.ToString());
		}

		public class NoWritePermissionsException : ForbiddenDataAccessException
		{
			private const string customMsg =
					"User $UserId does not have write access to edition $EditionId.";

			public NoWritePermissionsException(UserInfo editionUser) => Error =
					customMsg.Replace("$UserId", editionUser.userId.ToString())
							 .Replace("$EditionId", editionUser.EditionId.ToString());
		}

		public class NoLockPermissionsException : ForbiddenDataAccessException
		{
			private const string customMsg =
					"User $UserId is not allowed to lock edition $EditionId.";

			public NoLockPermissionsException(UserInfo editionUser) => Error =
					customMsg.Replace("$UserId", editionUser.userId.ToString())
							 .Replace("$EditionId", editionUser.EditionId.ToString());
		}

		public class NoAdminPermissionsException : ForbiddenDataAccessException
		{
			private const string customMsg =
					"User $UserId does not have admin privilege for edition $EditionId.";

			public NoAdminPermissionsException(UserInfo editionUser) => Error =
					customMsg.Replace("$UserId", editionUser.userId.ToString())
							 .Replace("$EditionId", editionUser.EditionId.ToString());
		}

		public class NoSystemPermissionsException : ForbiddenDataAccessException
		{
			private const string customMsg =
					"User $UserId does not have permissions to access this.";

			public NoSystemPermissionsException(UserInfo editionUser)
				=> Error = customMsg.Replace("$UserId", editionUser.userId.ToString());
		}

		public class LockedDataException : Helpers.LockedDataException
		{
			private const string customMsg =
					"Edition $EditionId is locked. User $UserId $Permission admin privilege to unlock it.";

			public LockedDataException(UserInfo editionUser) => Error = customMsg
																		.Replace(
																				"$UserId"
																				, editionUser.userId
																							 .ToString())
																		.Replace(
																				"$EditionId"
																				, editionUser
																				  .EditionId
																				  .ToString())
																		.Replace(
																				"$Permission"
																				, editionUser
																						.IsAdmin
																						? "has"
																						: "does not have");
		}

		public class BadLoginException : UnauthorizedException
		{
			private const string customMsg = "Failed login for $Email.";

			public BadLoginException(string email) => Error = customMsg.Replace("$Email", email);
		}

		// Do not use this for login related errors, it is only for actions that require an authenticated user
		// to resubmit their password.
		public class WrongPasswordException : UnauthorizedException
		{
			private const string customMsg = "The password is incorrect.";

			public WrongPasswordException() => Error = customMsg;
		}

		#endregion Permissions errors

		#region Data errors

		public class DataNotFoundException : Helpers.DataNotFoundException
		{
			private const string customMsg = "Data not found. ";

			private const string reason =
					"No entries could be found for $DataType, when searching on $SearchEntity with id $Id.";

			public DataNotFoundException(
					string   datatype     = null
					, string id           = "0"
					, string searchEntity = null)
			{
				if (!string.IsNullOrEmpty(datatype)
					&& string.IsNullOrEmpty(searchEntity))
					searchEntity = datatype;

				var fullMsg = string.IsNullOrEmpty(datatype) || (id == "0")
						? ""
						: reason.Replace("$DataType", datatype)
								.Replace("$SearchEntity", searchEntity)
								.Replace("$Id", id);

				Error = customMsg + fullMsg;
			}

			public DataNotFoundException(
					string   datatype     = null
					, uint   id           = 0
					, string searchEntity = null) : this(datatype, id.ToString(), searchEntity) { }

			public Dictionary<string, object> customReturnedData { get; set; }
		}

		// This exception should be used sparingly. It is usually better to throw the actual database error.
		public class DataNotWrittenException : ServerErrorException
		{
			private const string customMsg = "System failed while trying to $Operation. $Reason";

			public DataNotWrittenException(string operation, string reason = null) => Error =
					customMsg.Replace("$Operation", operation)
							 .Replace(
									 "$Reason"
									 , string.IsNullOrEmpty(reason)
											 ? ""
											 : $"This happened because {reason}.");
		}

		public class ImproperInputDataException : BadInputException
		{
			private const string customMsg =
					"The input data for $DataType is incorrect or out of date.";

			public ImproperInputDataException(string datatype)
				=> Error = customMsg.Replace("$DataType", datatype);
		}

		public class InputDataRuleViolationException : BadInputException
		{
			private const string customMsg =
					"The request is not allowed because it violates the rule: $Rule.";

			public InputDataRuleViolationException(string rule)
				=> Error = customMsg.Replace("$Rule", rule);
		}

		public class ConflictingDataException : ConflictingInputException
		{
			private const string customMsg =
					"The submitted $DataType conflicts with data already existing in the system.";

			public ConflictingDataException(string datatype)
				=> Error = customMsg.Replace("$DataType", datatype);
		}

		public class EditionCopyLockProtectionException : ForbiddenDataAccessException
		{
			private const string customMsg =
					"The edition $EditionId must be locked before attempting to copy it. User $UserId $Permission admin privilege to unlock it.";

			public EditionCopyLockProtectionException(UserInfo editionUser) => Error = customMsg
																					   .Replace(
																							   "$UserId"
																							   , editionUser
																								 .userId
																								 .ToString())
																					   .Replace(
																							   "$EditionId"
																							   , editionUser
																								 .EditionId
																								 .ToString())
																					   .Replace(
																							   "$Permission"
																							   , editionUser
																									   .IsAdmin
																									   ? "has"
																									   : "does not have");
		}

		public class EmailAddressImproperlyFormattedException : BadInputException
		{
			private const string customMsg =
					"The email address $Email could not be parsed by the system as a valid.";

			public EmailAddressImproperlyFormattedException(string email)
				=> Error = customMsg.Replace("$Email", email);
		}

		public class EmailAddressUndeliverableException : BadInputException
		{
			private const string customMsg =
					"The email address $Email could not be reached by the system. The email address is almost certainly incorrect.";

			public EmailAddressUndeliverableException(string email)
				=> Error = customMsg.Replace("$Email", email);
		}

		#endregion Data errors
	}
}
