using System.Collections.Generic;

namespace SQE.API.DTO
{
	public class ImageDTO
	{
		public enum Direction
		{
			left,
			right,
			top
		}

		public enum Lighting
		{
			direct,
			raking
		}

		public uint id { get; set; }
		public string url { get; set; }
		public uint? imageToImageMapEditorId { get; set; }
		public Lighting lightingType { get; set; }
		public Direction lightingDirection { get; set; }
		public string[] waveLength { get; set; }
		public string type { get; set; }
		public string side { get; set; }
		public string regionInMasterImage { get; set; }
		public string regionInImage { get; set; }
		public string transformToMaster { get; set; }
		public bool master { get; set; }
		public uint catalogNumber { get; set; }
	}

	public class ImageInstitutionDTO
	{
		public ImageInstitutionDTO(string name)
		{
			this.name = name;
		}

		public string name { get; set; }
	}

	public class ImageInstitutionListDTO
	{
		public ImageInstitutionListDTO(List<ImageInstitutionDTO> institutions)
		{
			this.institutions = institutions;
		}

		public List<ImageInstitutionDTO> institutions { get; set; }
	}
}