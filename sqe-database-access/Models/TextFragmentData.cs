using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
    public class TextFragmentX
    {
        public List<LineData> Lines   = new List<LineData>();
        public uint TextFragmentId { get; set; }
        public string TextFragmentName { get; set; }
        public uint? PreviousTextFragmentId { get; set; }
        public uint? NextTextFragmentId { get; set; }
        public uint TextFragmentAuthor { get; set; }
    }

    public class TextFragmentData
    {
        public List<LineData> Lines { get; set; } = new List<LineData>();
        public string TextFragmentName { get; set; }
        public uint? TextFragmentId { get; set; }
        public uint? PreviousTextFragmentId { get; set; }
        public uint? NextTextFragmentId { get; set; }
        public uint? EditionEditorId { get; set; }
    }
}