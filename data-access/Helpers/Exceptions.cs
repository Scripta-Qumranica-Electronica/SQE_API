using System;
using System.Net;
using SQE.SqeApi.DataAccess.Models;

namespace SQE.SqeApi.DataAccess.Helpers
{
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
        public string Error;
        
        protected ApiException(HttpStatusCode httpStatusCode)
        {
            StatusCode = httpStatusCode;
        }
        
        protected ApiException(HttpStatusCode httpStatusCode, string error) : this(httpStatusCode)
        {
            Error = error;
        }
    }
    
    #endregion Exception Base Class

    #region Exception Main Subclasses
    public abstract class ForbiddenDataAccessException : ApiException
    {
        private const HttpStatusCode httpStatusCode = HttpStatusCode.Forbidden;
        protected ForbiddenDataAccessException() : base(httpStatusCode) {}
    }
    
    public abstract class LockedDataException : ApiException
    {
        private const HttpStatusCode httpStatusCode = HttpStatusCode.Locked;
        protected LockedDataException() : base(httpStatusCode) {}
    }
    
    public abstract class UnauthorizedException : ApiException
    {
        private const HttpStatusCode httpStatusCode = HttpStatusCode.Unauthorized;
        protected UnauthorizedException() : base(httpStatusCode) {}
    }
    
    public abstract class DataNotFoundException : ApiException
    {
        private const HttpStatusCode httpStatusCode = HttpStatusCode.NotFound;
        protected DataNotFoundException() : base(httpStatusCode) {}
    }
    
    public abstract class BadInputException : ApiException
    {
        private const HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;
        protected BadInputException() : base(httpStatusCode) {}
    }
    
    public abstract class ConflictingInputException : ApiException
    {
        private const HttpStatusCode httpStatusCode = HttpStatusCode.Conflict;
        protected ConflictingInputException() : base(httpStatusCode) {}
    }
    
    public abstract class ServerErrorException : ApiException
    {
        private const HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;
        protected ServerErrorException() : base(httpStatusCode) {}
    }
    #endregion Exception Main Subclasses
    
    // This is a collection of ready-made errors that can be thrown. They include a standard HTTP status error code
    // and an internal project error code with accompanying message in English.
    public static class StandardErrors
    {
        #region Permissions errors
        public class NoPermissions : ForbiddenDataAccessException
        {
            private const string customMsg = "User $UserId has no permissions associated with edition $EditionId.";
            public NoPermissions(UserInfo user)
            {
                this.Error = customMsg
                    .Replace("$UserId", user.userId.ToString())
                    .Replace("$EditionId", user.editionId.ToString());
            }
        }
        
        public class NoReadPermissions : ForbiddenDataAccessException
        {
            private const string customMsg = "User $UserId does not have read access to edition $EditionId.";
            public NoReadPermissions(UserInfo user)
            {
                this.Error = customMsg
                    .Replace("$UserId", user.userId.ToString())
                    .Replace("$EditionId", user.editionId.ToString());
            }
        }
        
        public class NoWritePermissions : ForbiddenDataAccessException
        {
            private const string customMsg = "User $UserId does not have write access to edition $EditionId.";
            public NoWritePermissions(UserInfo user)
            {
                this.Error = customMsg
                    .Replace("$UserId", user.userId.ToString())
                    .Replace("$EditionId", user.editionId.ToString());
            }
        }
        
        public class NoLockPermissions : ForbiddenDataAccessException
        {
            private const string customMsg = "User $UserId is not allowed to lock edition $EditionId.";
            public NoLockPermissions(UserInfo user)
            {
                this.Error = customMsg
                    .Replace("$UserId", user.userId.ToString())
                    .Replace("$EditionId", user.editionId.ToString());
            }
        }
        
        public class NoAdminPermissions : ForbiddenDataAccessException
        {
            private const string customMsg = "User $UserId does not have admin privilege for edition $EditionId.";
            public NoAdminPermissions(UserInfo user)
            {
                this.Error = customMsg
                    .Replace("$UserId", user.userId.ToString())
                    .Replace("$EditionId", user.editionId.ToString());
            }
        }
        
        public class LockedData : LockedDataException
        {
            private const string customMsg = "Edition $EditionId is locked. User $UserId $Permission admin privilege to unlock it.";
            public LockedData(UserInfo user)
            {
                this.Error = customMsg
                    .Replace("$UserId", user.userId.ToString())
                    .Replace("$EditionId", user.editionId.ToString())
                    .Replace("$Permission", user.IsAdmin().Result ? "has" : "does not have");
            }
        }
        
        public class BadLogin : UnauthorizedException
        {
            private const string customMsg = "Failed login for $Email.";
            public BadLogin(string email)
            {
                this.Error = customMsg.Replace("$Email", email);
            }
        }
        
        // Do not use this for login related errors, it is only for actions that require an authenticated user
        // to resubmit their password.
        public class WrongPassword : UnauthorizedException
        {
            private const string customMsg = "The password is incorrect.";
            public WrongPassword()
            {
                this.Error = customMsg;
            }
        }
        #endregion Permissions errors

        #region Data errors

        public class DataNotFound : DataNotFoundException
        {
            private const string customMsg = "Data not found. ";

            private const string reason =
                "No entries could be found for $DataType, when searching on $SearchEntity with id $Id.";
            public DataNotFound(string datatype = null, string id = "0", string searchEntity = null)
            {
                if (!string.IsNullOrEmpty(datatype) && string.IsNullOrEmpty(searchEntity))
                    searchEntity = datatype;

                var fullMsg = string.IsNullOrEmpty(datatype) || id == "0"
                    ? ""
                    : reason.Replace("$DataType", datatype)
                        .Replace("$SearchEntity", searchEntity)
                        .Replace("$Id", id);
                
                this.Error = customMsg + fullMsg;
            }
            
            public DataNotFound(string datatype = null, uint id = 0, string searchEntity = null) 
                : this(datatype, id.ToString(), searchEntity){}
        }
        
        // This exception should be used sparingly. It is usually better to throw the actual database error.
        public class DataNotWritten : ServerErrorException
        {
            private const string customMsg = "System failed while trying to $Operation.";
            public DataNotWritten(string operation, string reason = null)
            {
                this.Error = customMsg.Replace("$Operation", operation)
                    .Replace("$Reason", string.IsNullOrEmpty(reason) ? "" : $"This happened because {reason}.");
            }
        }
        
        public class ImproperInputData : BadInputException
        {
            private const string customMsg = "The input data for $DataType is incorrect or out of date.";
            public ImproperInputData(string datatype)
            {
                this.Error = customMsg.Replace("$DataType", datatype);
            }
        }
        
        public class InputDataRuleViolation : BadInputException
        {
            private const string customMsg = "The request is not allowed because it violates the rule: $Rule.";
            public InputDataRuleViolation(string rule)
            {
                this.Error = customMsg.Replace("$Rule", rule);
            }
        }
        
        public class ConflictingData : ConflictingInputException
        {
            private const string customMsg = "The submitted $DataType conflicts with data already existing in the system.";
            public ConflictingData(string datatype)
            {
                this.Error = customMsg.Replace("$DataType", datatype);
            }
        }
        
        public class EditionCopyLockProtection : ForbiddenDataAccessException
        {
            private const string customMsg = "The edition $EditionId must be locked before attempting to copy it. User $UserId $Permission admin privilege to unlock it.";
            public EditionCopyLockProtection(UserInfo user)
            {
                this.Error = customMsg
                    .Replace("$UserId", user.userId.ToString())
                    .Replace("$EditionId", user.editionId.ToString())
                    .Replace("$Permission", user.IsAdmin().Result ? "has" : "does not have");
            }
        }
        
        public class EmailAddressImproperlyFormatted : BadInputException
        {
            private const string customMsg = "The email address $Email could not be parsed by the system as a valid.";
            public EmailAddressImproperlyFormatted(string email)
            {
                this.Error = customMsg
                    .Replace("$Email", email);
            }
        }
        
        public class EmailAddressUndeliverable : BadInputException
        {
            private const string customMsg = "The email address $Email could not be reached by the system. The email address is almost certainly incorrect.";
            public EmailAddressUndeliverable(string email)
            {
                this.Error = customMsg
                    .Replace("$Email", email);
            }
        }

        #endregion Data errors
        
        #region System errors
        
        public class EmailNotSent : ServerErrorException
        {
            private const string customMsg = "Failed sending email to $Email. This is probably a server error and should be reported to the webmaster.";
            public EmailNotSent(string email)
            {
                this.Error = customMsg
                    .Replace("$Email", email);
            }
        }
       
        #endregion System errors
    }
}
