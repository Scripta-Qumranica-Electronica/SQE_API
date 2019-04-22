using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class Line
    {
        public uint lineId { get; set; }
        public string line { get; set; }
        public readonly List<Sign> signs = new List<Sign>();
    }
}