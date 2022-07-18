using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
	public class ImageStackDTO
	{
		public uint? id { get; set; }

		[Required]
		public List<ImageDTO> images { get; set; }

		public int? masterIndex { get; set; }
	}

	public class ImagedObjectDTO
	{
		[Required]
		public string id { get; set; }

		[Required]
		public ImageStackDTO recto { get; set; }

		[Required]
		public ImageStackDTO verso { get; set; }

		[Required]
		public List<ArtefactDTO> artefacts { get; set; }
	}

	public class ImagedObjectListDTO
	{
		[Required]
		public List<ImagedObjectDTO> imagedObjects { get; set; }
	}
}
