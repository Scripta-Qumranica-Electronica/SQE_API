using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
    public class SignInterpretationData
    {
        public uint? SignInterpretationId { get; set; }
        public List<SignInterpretationAttributeData> Attributes { get; set; } = new List<SignInterpretationAttributeData>();
        public List<SignInterpretationCommentaryData> Commentaries { get; set; } = new List<SignInterpretationCommentaryData>();
        public HashSet<NextSignInterpretation> NextSignInterpretations { get; set; } = new HashSet<NextSignInterpretation>();
        public List<SignInterpretationRoiData> SignInterpretationRois { get; set; }= new List<SignInterpretationRoiData>();
        public string Character { get; set; }
        
    }

    public class NextSignInterpretation
    {
        public readonly uint NextSignInterpretationId;
        public readonly uint SignSequenceAuthor;

        public NextSignInterpretation(uint nextSignInterpretationId, uint signSequenceAuthor)
        {
            NextSignInterpretationId = nextSignInterpretationId;
            SignSequenceAuthor = signSequenceAuthor;
        }

        // The override for Equals and GetHashCode methods here enable the
        // HashSet nextSignInterpretations of the SignInterpretation object
        // to ensure that no duplicate values will be inserted into the set.
        public override bool Equals(object obj)
        {
            return obj is NextSignInterpretation q
                   && q.NextSignInterpretationId == this.NextSignInterpretationId
                   && q.SignSequenceAuthor == this.SignSequenceAuthor;
        }

        public override int GetHashCode()
        {
            return this.NextSignInterpretationId.GetHashCode() ^ this.SignSequenceAuthor.GetHashCode();
        }
    }
}