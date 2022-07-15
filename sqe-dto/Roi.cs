using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
	public class SetReconstructedInterpretationRoiDTO
	{
		[Required]
		public string shape { get; set; }

		[Required]
		public TranslateDTO translate { get; set; }
	}

	public class SetInterpretationRoiDTO : SetReconstructedInterpretationRoiDTO
	{
		[Required]
		public uint artefactId { get; set; }

		[Required]
		public uint signInterpretationId { get; set; }

		public ushort stanceRotation { get; set; }

		[Required]
		public bool exceptional { get; set; }

		[Required]
		public bool valuesSet { get; set; }
	}

	public class UpdateInterpretationRoiDTO : SetInterpretationRoiDTO
	{
		[Required]
		public uint interpretationRoiId { get; set; }
	}

	public class InterpretationRoiDTO : UpdateInterpretationRoiDTO
	{
		[Required]
		public uint creatorId { get; set; }

		[Required]
		public uint editorId { get; set; }
	}

	public class UpdatedInterpretationRoiDTO : InterpretationRoiDTO
	{
		[Required]
		public uint oldInterpretationRoiId { get; set; }
	}

	public class SetInterpretationRoiDTOList
	{
		[Required]
		public List<SetInterpretationRoiDTO> rois { get; set; }
	}

	public class InterpretationRoiDTOList
	{
		[Required]
		public List<InterpretationRoiDTO> rois { get; set; }
	}

	public class UpdateInterpretationRoiDTOList
	{
		[Required]
		public List<UpdateInterpretationRoiDTO> rois { get; set; }
	}

	public class UpdatedInterpretationRoiDTOList
	{
		[Required]
		public List<UpdatedInterpretationRoiDTO> rois { get; set; }
	}

	public class BatchEditRoiDTO
	{
		public List<SetInterpretationRoiDTO>    createRois { get; set; }
		public List<UpdateInterpretationRoiDTO> updateRois { get; set; }
		public List<uint>                       deleteRois { get; set; }
	}

	public class BatchEditRoiResponseDTO
	{
		[Required]
		public List<InterpretationRoiDTO> createRois { get; set; }

		[Required]
		public List<UpdatedInterpretationRoiDTO> updateRois { get; set; }

		[Required]
		public List<uint> deleteRois { get; set; }
	}
}
