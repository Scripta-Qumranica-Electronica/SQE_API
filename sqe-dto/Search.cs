using System.Collections.Generic;

namespace SQE.API.DTO
{
	public class DetailedSearchRequestDTO
	{
		public string       textDesignation          { get; set; }
		public bool         exactTextDesignation     { get; set; } = false;
		public string       imageDesignation         { get; set; }
		public bool         exactImageDesignation    { get; set; } = false;
		public List<string> textReference            { get; set; }
		public bool         exactTextReference       { get; set; } = false;
		public List<string> artefactDesignation      { get; set; }
		public bool         exactArtefactDesignation { get; set; } = false;
	}

	public class DetailedSearchResponseDTO
	{
		public FlatEditionListDTO                editions      { get; set; }
		public TextFragmentSearchResponseListDTO textFragments { get; set; }
		public ExtendedArtefactListDTO           artefacts     { get; set; }
		public ImageSearchResponseListDTO        images        { get; set; }
	}

	public class TextFragmentSearchResponseListDTO
	{
		public List<TextFragmentSearchResponseDTO> textFragments { get; set; }
	}

	public class TextFragmentSearchResponseDTO
	{
		public uint         id             { get; set; }
		public uint         editionId      { get; set; }
		public string       name           { get; set; }
		public string       editionName    { get; set; }
		public List<string> editionEditors { get; set; }
	}

	public class ImageSearchResponseListDTO
	{
		public List<ImageSearchResponseDTO> imagedObjects { get; set; }
	}

	public class ImageSearchResponseDTO
	{
		public string id             { get; set; }
		public string rectoThumbnail { get; set; }
		public string versoThumbnail { get; set; }
		public uint[] editionIds     { get; set; }
	}
}
