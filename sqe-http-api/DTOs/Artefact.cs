using System.Collections.Generic;
using System.Linq;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class ArtefactDTO
    {
        public uint id { get; set; }
        public uint editionId { get; set; }
        public string imagedObjectId {get; set;}
        public string name { get; set; }
        public PolygonDTO mask { get; set; }
        public short zOrder { get; set; }
        public ArtefactSide side { get; set; }

        public enum ArtefactSide { recto, verso}
    }
   

    public class ArtefactDesignationDTO
    {
        public uint artefactId { get; set; }
        public uint imageCatalogId { get; set; }
        public string name { get; set; }
        public string side { get; set; }
    }

    public class ArtefactListDTO
    {
        public List<ArtefactDTO> artefacts { get; set; }
    }
    
    public class ArtefactDesignationListDTO
    {
        public List<ArtefactDesignationDTO> artefactDesignations { get; set; }
    }
}
