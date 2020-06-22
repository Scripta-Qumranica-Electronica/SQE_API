using System.Collections.Generic;
using Org.BouncyCastle.Bcpg;

namespace qwb_to_sqe
{
    public class QWBFragment
    {
        public string Name = "";
        public readonly List<QWBLine> Lines = new List<QWBLine>();

    }
}