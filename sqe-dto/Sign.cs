using System.Collections.Generic;

namespace SQE.API.DTO
{
    public class SignDTO
    {
        public List<SignInterpretationDTO> signInterpretations { get; set; }
    }

    public class NextSignInterpretationDTO
    {
        public uint nextSignInterpretationId { get; set; }
        public uint editorId { get; set; }
    }

    public class SignInterpretationBaseDTO
    {
        public string character { get; set; }
        public new List<InterpretationAttributeCreateDTO> attributes { get; set; }
        public List<InterpretationRoiDTO> rois { get; set; }
        public string commentary { get; set; }
    }

    public class SignInterpretationCreateDTO : SignInterpretationBaseDTO
    {
        public List<uint> nextSignInterpretations { get; set; }
    }

    public class SignInterpretationDTO : SignInterpretationBaseDTO
    {
        public uint signInterpretationId { get; set; }
        public List<InterpretationAttributeDTO> attributes { get; set; }
        public List<NextSignInterpretationDTO> nextSignInterpretations { get; set; }
    }

    public class InterpretationAttributeCreateDTO
    {
        public uint interpretationAttributeId { get; set; }
        public byte sequence { get; set; }
        public uint attributeValueId { get; set; }
        public string attributeValueString { get; set; }
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
}