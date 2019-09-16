namespace SQE.SqeHttpApi.Server.DTOs
{
	public class PolygonDTO
	{
		public string mask { get; set; }
		public uint maskEditorId { get; set; }
		public float? scale { get; set; } // Can be null
		public float? rotate { get; set; } // Can be null
		public uint? translateX { get; set; } // Can be null
		public uint? translateY { get; set; } // Can be null
		public uint positionEditorId { get; set; }
	}
}