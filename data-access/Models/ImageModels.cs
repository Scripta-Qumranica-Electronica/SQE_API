namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class Image
    {
        public string URL { get; set; }
        public string[] WaveLength { set; get; }
        public byte Type { set; get; }
        public string Side { get; set; }
        public Polygon RegionInMaster { set; get; }
        public Polygon RegionOfMaster { set; get; }
        public string TransformMatrix { set; get; }
        public uint ImageCatalogId { set; get; }
        public string Institution { set; get; }
        public string Catlog1 { set; get; }
        public string Catalog2 { set; get; }
        public string ObjectId { set; get; }
        public bool Master { set; get; }
    }

    public class ImageInstitution
    {
        public string Name { get; set; }
    }
}
