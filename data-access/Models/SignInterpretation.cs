using System.Collections.Generic;

namespace SQE.SqeApi.DataAccess.Models
{
    public class SignInterpretation
    {
        public uint signInterpretationId { get; set; }
        public string character { get; set; }
        public readonly List<CharAttribute> attributes = new List<CharAttribute>();
        public readonly List<SignInterpretationROI> signInterpretationRois = new List<SignInterpretationROI>();
        public readonly HashSet<NextSignInterpretation> nextSignInterpretations = new HashSet<NextSignInterpretation>();
    }
    
    public class NextSignInterpretation
    {
        public uint nextSignInterpretationId { get; set; }
        public uint signSequenceAuthor { get; set; }
    }
}