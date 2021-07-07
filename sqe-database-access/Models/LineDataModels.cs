using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
	public class LineData
	{
		public List<SignData> Signs { get; set; } = new List<SignData>();

		public uint?  LineId     { get; set; }
		public string LineName   { get; set; }
		public uint?  LineAuthor { get; set; }
	}

	public class LetterShape
	{
		public uint   Id             { get; set; }
		public char   Letter         { get; set; }
		public byte[] Polygon        { get; set; }
		public uint   TranslateX     { get; set; }
		public uint   TranslateY     { get; set; }
		public ushort LetterRotation { get; set; }
		public string ImageURL       { get; set; }
		public string IrImageURL     { get; set; }
		public string ImageSuffix    { get; set; }
		public float  ImageRotation  { get; set; }
		public string Attributes     { get; set; }
	}
}
