using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class Fragment
    {
        public uint fragmentId { get; set; }
        public string fragment { get; set; }
        public readonly List<Line> lines = new List<Line>();
    }
}