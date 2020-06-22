using System.Collections.Generic;

namespace qwb_to_sqe
{
    public class QWBLine
    {
        public string Name;
        public readonly List<QWBWord> words = new List<QWBWord>();
    }
}