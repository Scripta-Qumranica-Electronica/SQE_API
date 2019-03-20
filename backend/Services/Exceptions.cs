using System;

namespace SQE.Backend.Server.Services
{
    public class NotFoundException : Exception {
        public NotFoundException(int id)
            : base(String.Format("Scroll {0} not found", id))
        {
        }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(int id)
            : base(String.Format("Scroll {0}: Forbidden access", id))
        {
        }
    }
}
