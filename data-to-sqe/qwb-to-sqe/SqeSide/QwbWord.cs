using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
	public class QwbWord
	{
		public List<WordData> WordIds = new List<WordData>();
		public uint?          QwbWordId { get; set; }
	}
}
