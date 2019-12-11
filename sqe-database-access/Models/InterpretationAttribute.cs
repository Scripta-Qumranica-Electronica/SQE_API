namespace SQE.DatabaseAccess.Models
{
    public class CharAttribute
    {
        public uint interpretationAttributeId { get; set; }
        public byte sequence { get; set; }
        public uint attributeValueId { get; set; }
        public string attributeString { get; set; }
        public uint signInterpretationAttributeAuthor { get; set; }
        public float value { get; set; }
        
        // The override for Equals and GetHashCode methods here enable the
        // HashSet nextSignInterpretations of the SignInterpretation object
        // to ensure that no duplicate values will be inserted into the set.
        public override bool Equals(object obj)
        {
            return obj is CharAttribute q 
                   && q.interpretationAttributeId == this.interpretationAttributeId;
        }

        public override int GetHashCode()
        {
            return unchecked((int) this.interpretationAttributeId);
        }
    }

    public class AttributeDefinition
    {
        public uint attributeValueId { get; set; }
        public string attributeString { get; set; }
    }
}