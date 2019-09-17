namespace SQE.API.DATA.Models
{
	public class SetSignInterpretationROI
	{
		public uint ArtefactId { get; set; }
		public uint? SignInterpretationId { get; set; }
		public string Shape { get; set; }
		public string Position { get; set; }
		public bool ValuesSet { get; set; }
		public bool Exceptional { get; set; }
	}

	public class SignInterpretationROI : SetSignInterpretationROI
	{
		public uint SignInterpretationRoiId { get; set; }
		public uint SignInterpretationRoiAuthor { get; set; }
	}

	public class UpdatedSignInterpretationROI : SignInterpretationROI
	{
		public uint OldSignInterpretationRoiId { get; set; }
	}

	public class DetailedSignInterpretationROI : SignInterpretationROI
	{
		public uint RoiShapeId { get; set; }
		public uint RoiPositionId { get; set; }
	}
}