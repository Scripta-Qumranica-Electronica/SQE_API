using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
	public class Sign
	{
		public readonly List<SignInterpretation> signInterpretations = new List<SignInterpretation>();
		public uint signId { get; set; }
	}
}