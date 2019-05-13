namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class ArtefactModel
    {
        public uint artefactId { get; set; }
        public string name { get; set; }
        public string mask { get; set; }
        public string transformMatrix { get; set; }
        public short zIndex { get; set; }
        public string institution { get; set; }
        public string catalogNumber1 { get; set; }
        public string catalogNumber2 { get; set; }
        public byte catalogSide { get; set; }
        public uint imageCatalogId { get; set; }
    }
}
