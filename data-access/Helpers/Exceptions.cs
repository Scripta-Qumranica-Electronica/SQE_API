using System;
using System.Net;
using Newtonsoft.Json;
using SQE.SqeHttpApi.DataAccess.Models;

namespace SQE.SqeHttpApi.DataAccess.Helpers
{
    // Exceptions of this class will be caught by the middleware and thrown back to HTTP requests with the proper
    // response codes.  This enables a RESTful experience regardless of whether a request is made via HTTP or by
    // websocket.  In either case the HTTP router or the Hub will catch a child of the HttpException class
    // and return that information to the user.
    // Errors should be thrown properly at the lowest levels of code, this will reduce boiler-plate in higher
    // level functions like the Services and ensure more consistent responses.
    public class HttpException : Exception
    {
        private readonly int httpStatusCode;

        public HttpException(HttpStatusCode httpStatusCode, HttpExceptionMessage msg) : base(JsonConvert.SerializeObject(msg))
        {
            this.httpStatusCode = (int)httpStatusCode;
        }
        
        public HttpException(HttpStatusCode httpStatusCode, uint internalErrorCode, string msg = null) 
            : base(JsonConvert.SerializeObject(new HttpExceptionMessage(internalErrorCode, msg)))
        {
            this.httpStatusCode = (int)httpStatusCode;
        }

        public int StatusCode { get { return this.httpStatusCode; } }
    }

    public class HttpExceptionMessage
    {
        public uint internalErrorCode { get; set; }
        public string message { get; set; }
        
        public HttpExceptionMessage(uint internalErrorCode, string message = null)
        {
            this.internalErrorCode = internalErrorCode;
            this.message = message;
        }

    }
    
    // This is a collection of ready-made errors that can be thrown. They include a standard HTTP status error code
    // and an internal project error code with accompanying message in English.
    public static class StandardErrors
    {

        #region Permissions errors
        public static HttpException NoPermissions(uint userId, uint editionId)
        {
            return new HttpException(HttpStatusCode.Forbidden, 620, 
                $"User {userId} has no permissions associated with edition {editionId}.");
        }
        public static HttpException NoReadPermissions(UserInfo user)
        {
            return new HttpException(HttpStatusCode.Forbidden, 621, 
                $"User {user.userId} does not have read access to edition {user.editionId}.");
        }
        
        public static HttpException NoWritePermissions(UserInfo user)
        {
            return new HttpException(HttpStatusCode.Forbidden, 622, 
                $"User {user.userId} does not have write access to edition {user.editionId}.");
        }
        
        public static HttpException NoLockPermissions(UserInfo user)
        {
            return new HttpException(HttpStatusCode.Forbidden, 623, 
                $"User {user.userId} is not allowed to lock edition {user.editionId}.");
        }
        
        public static HttpException NoAdminPermissions(UserInfo user)
        {
            return new HttpException(HttpStatusCode.Forbidden, 624, 
                $"User {user.userId} does not have admin privilege for edition {user.editionId}.");
        }
        
        public static HttpException EditionLocked(UserInfo user)
        {
            var unlockPermission = $"User {user.userId} {(user.IsAdmin().Result ? "has" : "does not have")} permission to unlock the edition.";
            
            return new HttpException(HttpStatusCode.Locked, 625, 
                $"The edition {user.editionId} is currently locked. {unlockPermission}");
        }
        
        public static HttpException BadLogin(string email)
        {
            return new HttpException(HttpStatusCode.Unauthorized, 626, 
                $"Failed login for {email}.");
        }
        
        // Do not use this for login related errors, it is only for actions that require an authenticated user
        // to resubmit their password.
        public static HttpException WrongPassword()
        {
            const string msg = "The password is incorrect.";
            return new HttpException(HttpStatusCode.Unauthorized, 626, msg);
        }
        #endregion Permissions errors

        #region Data errors

        public static HttpException DataNotFound(string datatype = null, uint id = 0, string searchEntity = null)
        {
            if (!string.IsNullOrEmpty(datatype) && string.IsNullOrEmpty(searchEntity))
                searchEntity = datatype;
            var info = !string.IsNullOrEmpty(datatype) || id == 0;
            return new HttpException(HttpStatusCode.NotFound, 640, 
                $"Data not found{(info ? "" : $" for {datatype} using {searchEntity} with id {id}")}.");
        }
        
        // This exception should be used sparingly. It is usually better to throw the actual database error.
        public static HttpException DataNotWritten(string operation = null)
        {
            return new HttpException(HttpStatusCode.InternalServerError, 641, 
                $"System failed while trying to {(!string.IsNullOrEmpty(operation) ? operation : "") }.");
        }
        
        public static HttpException ImproperInputData(string datatype)
        {
            return new HttpException(HttpStatusCode.BadRequest, 642, 
                $"The input data for {datatype} is incorrect or out of date.");
        }
        
        public static HttpException ConflictingData(string datatype)
        {
            return new HttpException(HttpStatusCode.Conflict, 643, 
                $"The submitted {datatype} conflicts with data already existing in the system");
        }
        
        public static HttpException EditionCopyLockProtection(UserInfo user)
        {
            var unlockPermission = $"User {user.userId} {(user.IsAdmin().Result ? "has" : "does not have")} permission to lock the edition.";
            
            return new HttpException(HttpStatusCode.Forbidden, 644, 
                $"The edition {user.editionId} must be locked before it can be copied. {unlockPermission}");
        }

        #endregion Data errors
        
        #region System errors
        public static HttpException EmailNotSent(string email)
        {
            return new HttpException(HttpStatusCode.InternalServerError, 660, 
                $"Failed sending email to {email}. The email address is probably incorrect.");
        }
        #endregion System errors
    }
}
