namespace SQE.DatabaseAccess.Models
{
	public class ImagedObject
	{
		public string Id { get; set; }

		public string Institution { get; set; }
		public string Catalog1    { get; set; }
		public string Catalog2    { get; set; }
	}

	public class ImagedObjectTextFragmentMatch
	{
		public uint   EditionId        { get; set; }
		public string ManuscriptName   { get; set; }
		public uint   TextFragmentId   { get; set; }
		public string TextFragmentName { get; set; }
		public byte   Side             { get; set; }
	}

	public class ImagedObjectImage
	{
		public string url              { get; set; }
		public string proxy            { get; set; }
		public string filename         { get; set; }
		public uint   sqe_image_id     { get; set; }
		public uint   image_catalog_id { get; set; }
		public byte   img_type         { get; set; }
		public uint   ppi              { get; set; }
		public byte   side             { get; set; }
		public bool   master           { get; set; }
		public ushort wave_start       { get; set; }
		public ushort wave_end         { get; set; }
		public string image_manifest   { get; set; }
		public string institution      { get; set; }
		public string catalog_1        { get; set; }
		public string catalog_2        { get; set; }
		public string object_id        { get; set; }
	}
}
