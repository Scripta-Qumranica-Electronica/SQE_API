using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
    public class SignInterpretation
    {
        public readonly List<CharAttribute> attributes = new List<CharAttribute>();
        public readonly HashSet<NextSignInterpretation> nextSignInterpretations = new HashSet<NextSignInterpretation>();
        public readonly List<SignInterpretationROI> signInterpretationRois = new List<SignInterpretationROI>();
        public uint signInterpretationId { get; set; }
        public string character { get; set; }
    }

    public class NextSignInterpretation
    {
        public uint nextSignInterpretationId { get; }
        public uint signSequenceAuthor { get; }

        // The override for Equals and GetHashCode methods here enable the
        // HashSet nextSignInterpretations of the SignInterpretation object
        // to ensure that no duplicate values will be inserted into the set.
        public override bool Equals(object obj)
        {
            return obj is NextSignInterpretation q
                   && q.nextSignInterpretationId == nextSignInterpretationId
                   && q.signSequenceAuthor == signSequenceAuthor;
        }

        public override int GetHashCode()
        {
            return nextSignInterpretationId.GetHashCode() ^ signSequenceAuthor.GetHashCode();
        }
    }
}