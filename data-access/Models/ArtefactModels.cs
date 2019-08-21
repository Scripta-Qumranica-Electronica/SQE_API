namespace SQE.SqeHttpApi.DataAccess.Models
{
	public class ArtefactModel
	{
		public uint ArtefactId { get; set; }
		public uint ArtefactDataEditorId { get; set; }
		public string Name { get; set; }
		public string Mask { get; set; }
		public uint MaskEditorId { get; set; }
		public string TransformMatrix { get; set; }
		public uint TransformMatrixEditorId { get; set; }
		public short ZIndex { get; set; }
		public byte CatalogSide { get; set; }
		public uint ImageId { get; set; }
		public uint ImageCatalogId { get; set; }
		public string ImagedObjectId { get; set; }
	}
}