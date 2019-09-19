namespace SQE.SqeHttpApi.Server.DTOs
{
	public class TransformationDTO
	{
		public float? scale { get; set; }
		public float? rotate { get; set; }
		public TranslateDTO translate { get; set; }
	}

	public class TranslateDTO
	{
		public int translateX { get; set; }
		public int translateY { get; set; }
	}
}