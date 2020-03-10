using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
    public class LineData
    {
        public List<SignData> Signs { get; set; } = new List<SignData>();

        public uint? LineId { get; set; }
        public string LineName { get; set; }
        public uint? LineAuthor { get; set; }
    }
}