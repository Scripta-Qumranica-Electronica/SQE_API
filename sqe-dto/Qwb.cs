namespace SQE.API.DTO
{
	public class QwbWordVariantListDTO
	{
		public QwbWordVariantDTO[] variants { get; set; }
	}

	public class QwbWordVariantDTO
	{
		public string               variantReading { get; set; }
		public QwbBibliographyDTO[] bibliography   { get; set; }
	}

	public class QwbBibliographyDTO
	{
		public uint   bibliographyId { get; set; }
		public string shortTitle     { get; set; }
		public string comment        { get; set; }
		public string pageReference  { get; set; }
	}

	public class QwbParallelWordDTO
	{
		public bool   isVariant        { get; set; }
		public bool   isReconstructed  { get; set; }
		public uint   qwbWordId        { get; set; }
		public uint   relatedQwbWordId { get; set; }
		public string word             { get; set; }
	}

	public class QwbParallelDTO
	{
		public string               qwbTextReference { get; set; }
		public QwbParallelWordDTO[] parallelWords    { get; set; }
	}

	public class QwbParallelListDTO
	{
		public QwbParallelDTO[] parallels { get; set; }
	}

	public class QwbBibliographyEntryDTO
	{
		public string entry { get; set; }
	}
}
