using System;

namespace SQE.SqeHttpApi.Server.Services
{
    public class NotFoundException : Exception {
        public NotFoundException(uint id)
            : base(String.Format("Scroll {0} not found", id))
        {
        }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(uint id)
            : base(String.Format("Scroll {0}: Forbidden access", id))
        {
        }
    }
}
