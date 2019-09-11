namespace SQE.SqeHttpApi.DataAccess.Models
{
	public class SetSignInterpretationROI
	{
		public uint ArtefactId { get; set; }
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
}