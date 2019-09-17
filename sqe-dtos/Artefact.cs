using System.Collections.Generic;

namespace SQE.API.DTO
{
	public class ArtefactDTO
	{
		public uint id { get; set; }
		public uint editionId { get; set; }
		public string imagedObjectId { get; set; }
		public uint imageId { get; set; }
		public uint artefactDataEditorId { get; set; }
		public string name { get; set; }
		public PolygonDTO mask { get; set; }
		public short zOrder { get; set; }
		public string side { get; set; }
		public string statusMessage { get; set; }

		public class ArtefactSide
		{
			public const string recto = "recto";
			public const string verso = "verso";
		}
	}

	public class ArtefactListDTO
	{
		public List<ArtefactDTO> artefacts { get; set; }
	}

	// TODO: Should we make the mask and positioning data a PolygonDTO here, like it is in the GET DTO?
	public class UpdateArtefactDTO
	{
		public string mask { get; set; }
		public string name { get; set; }
		public float? scale { get; set; }
		public float? rotate { get; set; }
		public uint? translateX { get; set; }
		public uint? translateY { get; set; }
		public string statusMessage { get; set; }
	}

	public class CreateArtefactDTO : UpdateArtefactDTO
	{
		public uint masterImageId { get; set; }
	}
}