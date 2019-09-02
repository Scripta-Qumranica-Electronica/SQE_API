﻿using System.Collections.Generic;

namespace SQE.SqeHttpApi.Server.DTOs
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

	public class UpdateArtefactDTO
	{
		public string mask { get; set; }
		public string name { get; set; }
		public string position { get; set; }
	}

	public class CreateArtefactDTO
	{
		public uint masterImageId { get; set; }
		public string mask { get; set; }
		public string name { get; set; }
		public string position { get; set; }
	}
}