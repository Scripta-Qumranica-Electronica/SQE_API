namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class ArtefactModel
    {
        public uint ArtefactId { get; set; }
        public string Name { get; set; }
        public string Mask { get; set; }
        public string TransformMatrix { get; set; }
        public short ZIndex { get; set; }
        public byte CatalogSide { get; set; }
        public uint ImageCatalogId { get; set; }
        public string ImagedObjectId { get; set; }
    }
}
