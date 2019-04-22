namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class SignCharROI
    {
        public uint SignCharRoi { get; set; }
        public string Shape { get; set; }
        public string Position { get; set; }
        public bool ValuesSet { get; set; }
        public bool Exceptional { get; set; }
    }
}