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
    }

    public class AttributeDefinition
    {
        public uint attributeValueId { get; set; }
        public string attributeString { get; set; }
    }
}