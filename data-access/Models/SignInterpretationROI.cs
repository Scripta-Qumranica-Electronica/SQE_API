namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class SignInterpretationROI
    {
        public uint SignInterpretationRoiId { get; set; }
        public uint ArtefactId { get; set; }
        public string Shape { get; set; }
        public string Position { get; set; }
        public bool ValuesSet { get; set; }
        public bool Exceptional { get; set; }
    }
}