using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class Sign
    {
        public uint signId { get; set; }
        public string nextSignIds { get; set; }
        public uint signSequenceAuthor { get; set; }
        public readonly List<SignInterpretation> signInterpretations = new List<SignInterpretation>();
    }
}