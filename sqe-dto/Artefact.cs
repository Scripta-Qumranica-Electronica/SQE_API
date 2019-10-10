using System.Collections.Generic;

namespace SQE.API.DTO
{
    public class ArtefactDTO
    {
        public uint id { get; set; }
        public uint editionId { get; set; }
        public string imagedObjectId { get; set; }
        public uint imageId { get; set; }
        public uint artefactDataEditorId { get; set; }
        public string name { get; set; }
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
    }

    public class UpdateArtefactDTO
    {
        public PolygonDTO polygon { get; set; }
        public string name { get; set; }
        public string statusMessage { get; set; }
    }

    public class CreateArtefactDTO : UpdateArtefactDTO
    {
        public uint masterImageId { get; set; }
    }
}