using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
	public class Sign
	{
		public readonly List<SignInterpretation> signInterpretations = new List<SignInterpretation>();
		public uint signId { get; set; }
	}
}