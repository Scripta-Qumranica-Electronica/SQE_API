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
        public short zOrder { get; set; }
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
        public List<ArtefactDTO> artefacts { get; set; }

        public ArtefactListDTO(List<ArtefactDTO> artefacts)
        {
            this.artefacts = artefacts;
        }
    }

    public class ArtefactDataListDTO
    {
        public List<ArtefactDataDTO> artefacts { get; set; }

        public ArtefactDataListDTO(List<ArtefactDataDTO> artefacts)
        {
            this.artefacts = artefacts;
        }
    }

    public class UpdateArtefactDTO
    {
        public virtual PolygonDTO polygon { get; set; }
        public string name { get; set; }
        public string statusMessage { get; set; }
    }

    public class CreateArtefactDTO : UpdateArtefactDTO
    {
        [Required]
        public uint masterImageId { get; set; }

        [Required]
        public override PolygonDTO polygon { get; set; }
    }
}