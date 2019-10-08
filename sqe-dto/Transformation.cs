namespace SQE.API.DTO
{
	public class TransformationDTO
	{
		public float? scale { get; set; }
		public float? rotate { get; set; }
		public TranslateDTO translate { get; set; }
	}

	public class TranslateDTO
	{
		public uint x { get; set; }
		public uint y { get; set; }
	}
}