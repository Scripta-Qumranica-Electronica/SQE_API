using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
    public class SignInterpretationData
    {
        public uint? SignInterpretationId { get; set; }
        public uint? SignInterpretationAttributeCreatorId { get; set; }
        public List<uint> SignStreamSectionIds { get; set; } = new List<uint>();

        public List<SignInterpretationAttributeData> Attributes { get; set; } =
            new List<SignInterpretationAttributeData>();

        public List<SignInterpretationCommentaryData> Commentaries { get; set; } =
            new List<SignInterpretationCommentaryData>();

        // NOTE Ingo changed the collection of nextSignInterpretationIds from hashset to list
        public List<NextSignInterpretation> NextSignInterpretations { get; set; } = new List<NextSignInterpretation>();

        public List<SignInterpretationRoiData> SignInterpretationRois { get; set; } =
            new List<SignInterpretationRoiData>();

        public string Character { get; set; }
        public bool IsVariant { get; set; }
        public string InterpretationCommentary { get; set; }
        public uint? InterpretationCommentaryCreator { get; set; }
        public uint? InterpretationCommentaryEditor { get; set; }
    }

    public class NextSignInterpretation
    {
        public NextSignInterpretation(uint nextSignInterpretationId, uint signSequenceAuthor)
        {
            NextSignInterpretationId = nextSignInterpretationId;
            SignSequenceAuthor = signSequenceAuthor;
        }

        public NextSignInterpretation()
        {
        }

        public uint NextSignInterpretationId { get; set; }
        public uint SignSequenceAuthor { get; set; }
        public bool IsMain { get; set; }
        public uint PositionCreatorId { get; set; }
        public uint PositionEditorId { get; set; }

        // The override for Equals and GetHashCode methods here enable the
        // HashSet nextSignInterpretations of the SignInterpretation object
        // to ensure that no duplicate values will be inserted into the set.
        public override bool Equals(object obj)
        {
            return obj is NextSignInterpretation q
                   && q.NextSignInterpretationId == NextSignInterpretationId
                   && q.SignSequenceAuthor == SignSequenceAuthor;
        }

        public override int GetHashCode()
        {
            return NextSignInterpretationId.GetHashCode() ^ SignSequenceAuthor.GetHashCode();
        }
    }
}