using System.Collections.Generic;

namespace SQE.API.DATA.Models
{
	public class Line
	{
		public readonly List<Sign> signs = new List<Sign>();
		public uint lineId { get; set; }
		public string line { get; set; }
		public uint lineAuthor { get; set; }
	}

	public class LineData
	{
		public uint lineId { get; set; }
		public string lineName { get; set; }
	}
}