using System.Collections.Generic;

namespace SQE.SqeApi.DataAccess.Models
{
    public class Line
    {
        public uint lineId { get; set; }
        public string line { get; set; }
        public uint lineAuthor { get; set; }
        public readonly List<Sign> signs = new List<Sign>();
    }
    
    public class LineData
    {
        public uint lineId { get; set; }
        public string lineName { get; set; }
    }
}