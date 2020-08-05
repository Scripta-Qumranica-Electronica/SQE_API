using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
    public class SetInterpretationRoiDTO
    {
        [Required] public uint artefactId { get; set; }

        public uint? signInterpretationId { get; set; }

        [Required] public string shape { get; set; }

        [Required] public TranslateDTO translate { get; set; }

        public ushort stanceRotation { get; set; }

        [Required] public bool exceptional { get; set; }

        [Required] public bool valuesSet { get; set; }
    }

    public class InterpretationRoiDTO : SetInterpretationRoiDTO
    {
        [Required] public uint interpretationRoiId { get; set; }

        [Required] public uint editorId { get; set; }
    }

    public class UpdatedInterpretationRoiDTO : InterpretationRoiDTO
    {
        [Required] public uint oldInterpretationRoiId { get; set; }
    }

    public class SetInterpretationRoiDTOList
    {
        [Required] public List<SetInterpretationRoiDTO> rois { get; set; }
    }

    public class InterpretationRoiDTOList
    {
        [Required] public List<InterpretationRoiDTO> rois { get; set; }
    }

    public class UpdatedInterpretationRoiDTOList
    {
        [Required] public List<UpdatedInterpretationRoiDTO> rois { get; set; }
    }

    public class BatchEditRoiDTO
    {
        public List<InterpretationRoiDTO> createRois { get; set; }
        public List<UpdatedInterpretationRoiDTO> updateRois { get; set; }
        public List<uint> deleteRois { get; set; }
    }

    public class BatchEditRoiResponseDTO
    {
        [Required] public List<InterpretationRoiDTO> createRois { get; set; }

        [Required] public List<UpdatedInterpretationRoiDTO> updateRois { get; set; }

        [Required] public List<uint> deleteRois { get; set; }
    }
}