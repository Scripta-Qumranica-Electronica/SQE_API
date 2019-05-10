using System;

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
}
