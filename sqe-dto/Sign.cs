using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
    public class SignDTO
    {
        [Required] public List<SignInterpretationDTO> signInterpretations { get; set; }
    }

    public class NextSignInterpretationDTO
    {
        [Required] public uint nextSignInterpretationId { get; set; }
        [Required] public uint editorId { get; set; }
    }

    public class SignInterpretationBaseDTO
    {
        [Required] public uint signInterpretationId { get; set; }
        public string character { get; set; }
        [Required] public List<InterpretationAttributeDTO> attributes { get; set; }
        [Required] public List<InterpretationRoiDTO> rois { get; set; }
        [Required] public List<NextSignInterpretationDTO> nextSignInterpretations { get; set; }
    }

    public class InterpretationAttributeCreateDTO
    {
        [Required] public uint interpretationAttributeId { get; set; }
        public byte sequence { get; set; }
        [Required] public uint attributeValueId { get; set; }
        [Required] public string attributeValueString { get; set; }
        [Required] public uint editorId { get; set; }
        public float value { get; set; }
        public string commentary { get; set; }
    }

    public class InterpretationAttributeDTO : InterpretationAttributeCreateDTO
    {
        public uint editorId { get; set; }
    }

    public class InterpretationAttributeCreateListDTO
    {
        public List<InterpretationAttributeCreateDTO> attributes { get; set; }
    }

    public class InterpretationAttributeListDTO
    {
        public List<InterpretationAttributeDTO> attributes { get; set; }
    }

    public enum AttributeTypes
    {
        Boolean,
        Number,
        String,
    }

    public class AttributeValueDTO
    {
        [Required] public string value { get; set; }
        public string description { get; set; }
        public string cssDirectives { get; set; }
    }

    public class CreateAttributeDTO
    {
        [Required] public string attributeName { get; set; }
        [Required] public AttributeValueDTO[] values { get; set; }
        public string description { get; set; }
        [Required] public AttributeTypes attributeType { get; set; }
    }

    public class AttributeDTO : CreateArtefactDTO
    {
        [Required] public uint attributeId { get; set; }
    }

    public class AttributeListDTO
    {
        [Required]
        public AttributeDTO[] attributes { get; set; }
    }
}