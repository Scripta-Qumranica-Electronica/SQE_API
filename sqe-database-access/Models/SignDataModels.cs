using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
	public class SignData
	{
		public List<SignInterpretationData> SignInterpretations { get; set; } =
			new List<SignInterpretationData>();

		public uint? SignId { get; set; }
	}
}
