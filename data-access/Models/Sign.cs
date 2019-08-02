using System.Collections.Generic;

namespace SQE.SqeApi.DataAccess.Models
{
    public class Sign
    {
        public uint signId { get; set; }
        public readonly List<SignInterpretation> signInterpretations = new List<SignInterpretation>();
    }
}