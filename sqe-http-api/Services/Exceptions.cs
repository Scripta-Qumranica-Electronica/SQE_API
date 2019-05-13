using System;

namespace SQE.SqeHttpApi.Server.Helpers
{
    
    // TODO Should be renamed to EditionNotFoundException
    public class NotFoundException : Exception {
        public NotFoundException(uint id)
            : base(String.Format("Scroll {0} not found", id))
        {
        }
    }

    // TODO Should be renamed to EditionForbiddenException
    public class ForbiddenException : Exception
    {
        public ForbiddenException(uint id)
            : base(String.Format("Scroll {0}: Forbidden access", id))
        {
        }
    }
    
    
    public class LineNotFoundException : Exception
    {
        public LineNotFoundException(uint id, uint editionId)
            : base($"Line with Id {id} not found in edition with Id {editionId}")
        {
        }
    }

    public class FragmentNotFoundException : Exception
    {
        public FragmentNotFoundException(uint id, uint editionId)
            : base($"Fragment with Id {id} not found in edition with Id {editionId}")
        {
        }
    }

}
