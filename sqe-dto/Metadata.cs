using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
	public class EditionManuscriptMetadataDTO
	{
		[Required]
		public string material { get; set; }

		[Required]
		public string publicationNumber { get; set; }

		public string plate { get; set; }

		[Required]
		public string frag { get; set; }

		[Required]
		public string site { get; set; }

		[Required]
		public string period { get; set; }

		[Required]
		public string composition { get; set; }

		[Required]
		public string copy { get; set; }

		[Required]
		public string manuscript { get; set; }

		[Required]
		public string otherIdentifications { get; set; }

		[Required]
		public string abbreviation { get; set; }

		[Required]
		public string manuscriptType { get; set; }

		[Required]
		public string compositionType { get; set; }

		[Required]
		public string language { get; set; }

		[Required]
		public string script { get; set; }

		[Required]
		public string publication { get; set; }
	}
}
