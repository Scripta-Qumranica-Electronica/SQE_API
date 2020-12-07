using System.Collections.Generic;

namespace SQE.API.DTO
{
	public class DetailedSearchRequestDTO
	{
		public string       textDesignation     { get; set; }
		public string       imageDesignation    { get; set; }
		public List<string> textReference       { get; set; }
		public List<string> artefactDesignation { get; set; }
	}

	public class DetailedSearchResponseDTO
	{
		public EditionListDTO          editions      { get; set; }
		public TextFragmentDataListDTO textFragments { get; set; }
		public ArtefactDataListDTO     artefacts     { get; set; }
		public ImagedObjectListDTO     images        { get; set; }
	}
}
