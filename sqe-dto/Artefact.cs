using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
    public class ArtefactDataDTO
    {
        [Required]
        public uint id { get; set; }

        [Required]
        public string name { get; set; }
    }

    public class ArtefactDTO : ArtefactDataDTO
    {
        [Required]
        public uint editionId { get; set; }

        [Required]
        public string imagedObjectId { get; set; }

        [Required]
        public uint imageId { get; set; }

        [Required]
        public uint artefactDataEditorId { get; set; }

        [Required]
        public string mask { get; set; }

        [Required]
        public uint artefactMaskEditorId { get; set; }

        [Required]
        public bool isPlaced { get; set; }

        [Required]
        public PlacementDTO placement { get; set; }
        public uint? artefactPlacementEditorId { get; set; }

        [Required]
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
        [Required]
        public List<ArtefactDTO> artefacts { get; set; }
    }

    public class ArtefactDataListDTO
    {
        [Required]
        public List<ArtefactDataDTO> artefacts { get; set; }
    }

    public class ArtefactGroupDTO : UpdateArtefactGroupDTO { }

    public class ArtefactGroupListDTO
    {
        public List<ArtefactGroupDTO> artefactGroups { get; set; }
    }

    public class UpdateArtefactDTO
    {
        public virtual string mask { get; set; }
        public PlacementDTO placement { get; set; }

        [StringLength(
            255,
            MinimumLength = 1,
            ErrorMessage = "Artefact names must be between 1 and 255 characters long"
        )]
        public string name { get; set; }

        public string statusMessage { get; set; }
    }

    public class UpdateArtefactPlacementDTO
    {
        [Required]
        public uint artefactId { get; set; }

        [Required]
        public PlacementDTO placement { get; set; }
    }

    public class BatchUpdateArtefactPlacementDTO
    {
        [Required]
        public List<UpdateArtefactPlacementDTO> artefactPlacements { get; set; }
    }

    public class UpdatedArtefactPlacementDTO : UpdateArtefactPlacementDTO
    {
        [Required]
        public uint placementEditorId { get; set; }
    }

    public class BatchUpdatedArtefactTransformDTO
    {
        [Required]
        public List<UpdatedArtefactPlacementDTO> artefactPlacements { get; set; }
    }

    public class UpdateArtefactGroupDTO : CreateArtefactGroupDTO
    {
        [Required]
        public uint id { get; set; }
    }

    public class CreateArtefactDTO : UpdateArtefactDTO
    {
        [Required]
        public uint masterImageId { get; set; }

        // Run a quick regex to make sure we have a the valid text for a WKT polygon (does not check polygon validity)
        [RegularExpression(@"^\s*[Pp][Oo][Ll][Yy][Gg][Oo][Nn]\s*\(\s*\(.*\)\s*\)\s*$",
            ErrorMessage = "The mask must be a valid WKT POLYGON description.")]
        [Required]
        public override string mask { get; set; }
    }

    public class CreateArtefactGroupDTO
    {
        [MaxLength(255)]
        public string name { get; set; }
        [Required]
        public List<uint> artefacts { get; set; }
    }
}