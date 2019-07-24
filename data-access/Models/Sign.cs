using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class Sign
    {
        public uint signId { get; set; }
        public readonly HashSet<NextSignInterpretation> nextSignInterpretations = new HashSet<NextSignInterpretation>();
        public readonly List<SignInterpretation> signInterpretations = new List<SignInterpretation>();
    }
}