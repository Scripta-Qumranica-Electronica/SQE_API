namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class TextFragment
    {
        public string ColName { get; set; }
        public uint ColId { get; set; }
    }

    public class LineData
    {
        public uint lineId { get; set; }
        public string lineName { get; set; }
    }
}