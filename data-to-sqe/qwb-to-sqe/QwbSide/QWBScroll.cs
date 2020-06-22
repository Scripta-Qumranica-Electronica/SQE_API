using System.Collections.Generic;

namespace qwb_to_sqe
{
    public class QWBScroll
    {
        public uint Id;
        public string Name;
        public readonly List<QWBFragment> fragments = new List<QWBFragment>();
        
        
    }
}