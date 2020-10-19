using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
	public class TextFragmentData
	{
		public List<LineData> Lines                  { get; set; } = new List<LineData>();
		public string         TextFragmentName       { get; set; }
		public uint?          TextFragmentId         { get; set; }
		public uint?          PreviousTextFragmentId { get; set; }
		public uint?          NextTextFragmentId     { get; set; }
		public uint?          TextFragmentEditorId   { get; set; }
	}
}
