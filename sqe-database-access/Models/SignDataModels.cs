using System;
using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
	public class SignData
	{
		public List<SignInterpretationData> SignInterpretations { get; set; } =
			new List<SignInterpretationData>();

		public uint? SignId { get; set; }
	}

	public class SignStreamMaterializationSchedule
	{
		public uint     EditionId            { get; set; }
		public uint     SignInterpretationId { get; set; }
		public DateTime CreatedDate          { get; set; }
		public DateTime CurrentTime          { get; set; }
	}
}
