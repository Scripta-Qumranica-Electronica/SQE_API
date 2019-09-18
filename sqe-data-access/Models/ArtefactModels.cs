namespace SQE.API.DATA.Models
{
	public class ArtefactModel
	{
		public uint ArtefactId { get; set; }
		public uint ArtefactDataEditorId { get; set; }
		public string Name { get; set; }
		public string Mask { get; set; }
		public uint MaskEditorId { get; set; }
		public float? Scale { get; set; }
		public float? Rotate { get; set; }
		public uint? TranslateX { get; set; }
		public uint? TranslateY { get; set; }
		public uint PositionEditorId { get; set; }
		public short ZIndex { get; set; }
		public byte CatalogSide { get; set; }
		public uint ImageId { get; set; }
		public uint ImageCatalogId { get; set; }
		public string ImagedObjectId { get; set; }
		public string WorkStatusMessage { get; set; }
	}
}