using System.Collections.Generic;

namespace SQE.SqeHttpApi.Server.DTOs
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
}