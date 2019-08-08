using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class TextFragment
    {
        public uint textFragmentId { get; set; }
        public string textFragmentName { get; set; }
        public uint textFragmentAuthor { get; set; }
        public readonly List<Line> lines = new List<Line>();
    }
    
    public class TextFragmentData
    {
        public string TextFragmentName { get; set; }
        public uint TextFragmentId { get; set; }
        public uint EditionEditor { get; set; }
        public ushort Position { get; set; }
        public uint TextFragmentSequenceId { get; set; }
    }
}