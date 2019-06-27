using System.Collections.Generic;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class SignDTO
    {
        public uint signId { get; set; }
        public uint nextSignId { get; set; }
        public List<SignCharDTO> signChars { get; set; }
    }
    
    public class SignCharDTO
    {
        public uint signCharId { get; set; }
        public string signChar { get; set; }
        public List<CharAttributeDTO> attributes { get; set; }
    }
    
    public class CharAttributeDTO
    {
        public uint charAttributeId { get; set; }
        public uint attributeValueId { get; set; }
        public float value { get; set; }
    }
}