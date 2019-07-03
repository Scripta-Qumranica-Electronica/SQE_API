using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class SignInterpretation
    {
        public uint signInterpretationId { get; set; }
        public string signInterpretation { get; set; }
        public readonly List<CharAttribute> attributes = new List<CharAttribute>();
        public readonly List<SignInterpretationROI> signInterpretationRois = new List<SignInterpretationROI>();
    }
}