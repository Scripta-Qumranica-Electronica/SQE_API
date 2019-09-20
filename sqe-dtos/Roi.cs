using System.Collections.Generic;

namespace SQE.SqeHttpApi.Server.DTOs
{
	public class SetInterpretationRoiDTO
	{
		public uint artefactId { get; set; }
		public uint? signInterpretationId { get; set; }
		public string shape { get; set; }
		public string position { get; set; }
		public bool exceptional { get; set; }
		public bool valuesSet { get; set; }
	}

	public class InterpretationRoiDTO : SetInterpretationRoiDTO
	{
		public uint interpretationRoiId { get; set; }
		public uint editorId { get; set; }
	}

	public class UpdatedInterpretationRoiDTO : InterpretationRoiDTO
	{
		public uint oldInterpretationRoiId { get; set; }
	}

	public class SetInterpretationRoiDTOList
	{
		public List<SetInterpretationRoiDTO> rois { get; set; }
	}

	public class InterpretationRoiDTOList
	{
		public List<InterpretationRoiDTO> rois { get; set; }
	}

	public class UpdatedInterpretationRoiDTOList
	{
		public List<UpdatedInterpretationRoiDTO> rois { get; set; }
	}
}