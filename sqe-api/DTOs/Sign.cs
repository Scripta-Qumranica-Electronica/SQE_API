using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;

namespace SQE.SqeApi.Server.DTOs
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
    
    public class SignInterpretationDTO
    {
        public uint signInterpretationId { get; set; }
        public string character { get; set; }
        public List<InterpretationAttributeDTO> attributes { get; set; }
        public List<InterpretationRoiDTO> rois { get; set; }
        public List<NextSignInterpretationDTO> nextSignInterpretations { get; set; }
    }
    
    public class InterpretationAttributeDTO
    {
        public uint interpretationAttributeId { get; set; }
        public byte sequence { get; set; }
        public uint attributeValueId { get; set; }
        public uint editorId { get; set; }
        public float value { get; set; }
    }

    public class InterpretationRoiDTO
    {
        public uint interpretationRoiId { get; set; }
        public uint editorId { get; set; }
        public uint artefactId { get; set; }
        public string shape { get; set; }
        public string position { get; set; }
        public bool exceptional { get; set; }
        public bool valuesSet { get; set; }
    }
}