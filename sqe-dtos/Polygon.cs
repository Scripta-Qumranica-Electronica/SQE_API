﻿namespace SQE.SqeHttpApi.Server.DTOs
{
	public class PolygonDTO
	{
		public string mask { get; set; }
		public uint maskEditorId { get; set; }
		public TransformationDTO transformation { get; set; }
		public uint positionEditorId { get; set; }
	}
}