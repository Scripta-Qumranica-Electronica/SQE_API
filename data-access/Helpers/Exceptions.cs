using System;
using System.Net;
using Newtonsoft.Json;

namespace SQE.SqeHttpApi.DataAccess.Helpers
{
    // TODO: Decide whether the controller is aware of these exceptions, or the service translates them
    public abstract class RepositoryException: Exception
    {
        protected RepositoryException(string msg) : base(msg) { }
    }

    public class NoPermissionException: RepositoryException
    {
        public NoPermissionException(uint? userId, string operation, string entity, uint? entityId) : 
            base($"User ${userId?.ToString() ?? "anonymous"} can't perform ${operation} on ${entity} ${entityId?.ToString()}")
        { }
    }
    
    public class ImproperRequestException: RepositoryException
    {
        public ImproperRequestException(string operation, string requestError) : 
            base($"Request ${operation} failed; ${requestError}.")
        { }
    }
    
    public class DbFailedWrite: RepositoryException
    {
        public DbFailedWrite() : 
            base($"Failed writing to DB.")
        { }
    }
    
    public class DbDetailedFailedWrite: RepositoryException
    {
        public DbDetailedFailedWrite(string msg) : 
            base($"Failed writing to DB. {msg}")
        { }
    }
    
    // Todo: Make all exceptions children of this class
    // Exceptions of this class will be caught by the middleware and thrown back to HTTP requests with the proper
    // response codes.  This enables a RESTful experience regardless of whether a request is made via HTTP or by
    // websocket.  In either case the HTTP router or the Hub will catch a child of the HttpException class
    // and return that information to the user.
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
        public static HttpException NoPermissionException(uint? userId, string operation, string entity, uint? entityId)
        {
            return new HttpException(HttpStatusCode.Forbidden, 600, $"User ${userId?.ToString() ?? "anonymous"} can't perform ${operation} on ${entity}, id ${entityId?.ToString()}.");
        }
    }
}
