using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
	public class TextFragment
	{
		public readonly List<Line> lines = new List<Line>();
		public uint textFragmentId { get; set; }
		public string textFragmentName { get; set; }
		public uint textFragmentAuthor { get; set; }
	}

	public class TextFragmentData
	{
		public string TextFragmentName { get; set; }
		public uint TextFragmentId { get; set; }
		public uint EditionEditorId { get; set; }
		public ushort Position { get; set; }
		public uint TextFragmentSequenceId { get; set; }
	}
}