using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
    public class ArtefactDataDTO
    {
        public uint id { get; set; }
        public string name { get; set; }
    }

    public class ArtefactDTO : ArtefactDataDTO
    {
        public uint editionId { get; set; }
        public string imagedObjectId { get; set; }
        public uint imageId { get; set; }
        public uint artefactDataEditorId { get; set; }
        public PolygonDTO mask { get; set; }
        public string side { get; set; }
        public string statusMessage { get; set; }

        public class ArtefactSide
        {
            public const string recto = "recto";
            public const string verso = "verso";
        }
    }

    public class ArtefactListDTO
    {
        public ArtefactListDTO(List<ArtefactDTO> artefacts)
        {
            this.artefacts = artefacts;
        }

        public List<ArtefactDTO> artefacts { get; set; }
    }

    public class ArtefactDataListDTO
    {
        public ArtefactDataListDTO(List<ArtefactDataDTO> artefacts)
        {
            this.artefacts = artefacts;
        }

        public List<ArtefactDataDTO> artefacts { get; set; }
    }

    public class UpdateArtefactDTO
    {
        public virtual SetPolygonDTO polygon { get; set; }

        [StringLength(
            255,
            MinimumLength = 1,
            ErrorMessage = "Artefact names must be between 1 and 255 characters long"
        )]
        public string name { get; set; }

        public string statusMessage { get; set; }
    }

    public class UpdateArtefactTransformDTO
    {
        public uint artefactId { get; set; }
        public TransformationDTO transform { get; set; }
    }

    public class BatchUpdateArtefactTransformDTO
    {
        public List<UpdateArtefactTransformDTO> artefactTransforms { get; set; }
    }

    public class UpdatedArtefactTransformDTO : UpdateArtefactTransformDTO
    {
        public uint positionEditorId { get; set; }
    }

    public class BatchUpdatedArtefactTransformDTO
    {
        public List<UpdatedArtefactTransformDTO> artefactTransforms { get; set; }
    }

    public class CreateArtefactDTO : UpdateArtefactDTO
    {
        [Required] public uint masterImageId { get; set; }

        [Required] public override SetPolygonDTO polygon { get; set; }
    }
}