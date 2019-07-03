using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class TextFragment
    {
        public uint textFragmentId { get; set; }
        public string fragment { get; set; }
        public uint textFragmentAuthor { get; set; }
        public readonly List<Line> lines = new List<Line>();
    }
    
    public class TextFragmentData
    {
        public string ColName { get; set; }
        public uint ColId { get; set; }
    }
}