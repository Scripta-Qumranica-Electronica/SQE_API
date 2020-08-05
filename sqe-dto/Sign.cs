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

    public class SignInterpretationDTO
    {
        [Required] public uint signInterpretationId { get; set; }
        public string character { get; set; }
        [Required] public List<InterpretationAttributeDTO> attributes { get; set; }
        [Required] public List<InterpretationRoiDTO> rois { get; set; }
        [Required] public List<NextSignInterpretationDTO> nextSignInterpretations { get; set; }
    }

    public class InterpretationAttributeDTO
    {
        [Required] public uint interpretationAttributeId { get; set; }
        public byte sequence { get; set; }
        [Required] public uint attributeValueId { get; set; }
        [Required] public string attributeValueString { get; set; }
        [Required] public uint editorId { get; set; }
        public float value { get; set; }
    }
}