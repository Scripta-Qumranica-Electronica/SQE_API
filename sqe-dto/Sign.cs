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
        [Required] public uint creatorId { get; set; }
        [Required] public uint editorId { get; set; }
    }

    public class SignInterpretationCreateDTO
    {
        public string character { get; set; }
        [Required] public InterpretationAttributeDTO[] attributes { get; set; }
        [Required] public InterpretationRoiDTO[] rois { get; set; }
        [Required] public NextSignInterpretationDTO[] nextSignInterpretations { get; set; }
        [Required] public bool isVariant { get; set; }
        public CommentaryDTO commentary { get; set; }
    }

    public class SignInterpretationDTO : SignInterpretationCreateDTO
    {
        [Required] public uint signInterpretationId { get; set; }
    }

    public class InterpretationAttributeBaseDTO
    {
        public byte? sequence { get; set; }
        [Required] public uint attributeId { get; set; }
        [Required] public uint attributeValueId { get; set; }
        public float? value { get; set; }
    }

    public class InterpretationAttributeCreateDTO : InterpretationAttributeBaseDTO
    {
        public string commentary { get; set; }
    }

    public class InterpretationAttributeDTO : InterpretationAttributeBaseDTO
    {
        [Required] public uint interpretationAttributeId { get; set; }
        [Required] public string attributeValueString { get; set; }
        [Required] public uint creatorId { get; set; }
        [Required] public uint editorId { get; set; }
        public CommentaryDTO commentary { get; set; }
    }

    public class InterpretationAttributeCreateListDTO
    {
        public List<InterpretationAttributeCreateDTO> attributes { get; set; }
    }

    public class InterpretationAttributeListDTO
    {
        public List<InterpretationAttributeDTO> attributes { get; set; }
    }

    public class CreateAttributeValueDTO
    {
        [Required] public string value { get; set; }
        public string description { get; set; }
        public string cssDirectives { get; set; }
    }

    public class UpdateAttributeValueDTO : CreateAttributeValueDTO
    {
        [Required] public uint id { get; set; }
    }

    public class AttributeValueDTO : UpdateAttributeValueDTO
    {
        [Required] public uint creatorId { get; set; }
        [Required] public uint editorId { get; set; }
    }

    public class AttributeBaseDTO
    {
        public string description { get; set; }
    }

    public class CreateAttributeDTO : AttributeBaseDTO
    {
        [Required] public string attributeName { get; set; }
        [Required] public CreateAttributeValueDTO[] values { get; set; }
    }

    public class UpdateAttributeDTO : AttributeBaseDTO
    {
        public string attributeName { get; set; }
        [Required] public CreateAttributeValueDTO[] createValues { get; set; }
        [Required] public UpdateAttributeValueDTO[] updateValues { get; set; }
        [Required] public uint[] deleteValues { get; set; }
    }

    public class AttributeDTO : AttributeBaseDTO
    {
        [Required] public uint attributeId { get; set; }
        [Required] public string attributeName { get; set; }
        [Required] public AttributeValueDTO[] values { get; set; }
        [Required] public uint creatorId { get; set; }
        [Required] public uint editorId { get; set; }
    }

    public class AttributeListDTO
    {
        [Required] public AttributeDTO[] attributes { get; set; }
    }
}