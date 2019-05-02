using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class ArtefactDTO
    {
        public uint Id { get; set; }
        public uint EditionId { get; set; }
        public string ImagedObjectId {get; set;}
        public string Name { get; set; }
        public PolygonDTO Mask { get; set; }
        public string TransformMatrix { get; set; }
        public uint zOrder { get; set; }
        public artSide Side { get; set; }

        public enum artSide { recto, verso}
    }

    public class ArtefactDesignationDTO
    {
        public uint ArtefactId { get; set; }
        public uint ImageCatalogId { get; set; }
        public string Name { get; set; }
        public string Side { get; set; }
    }

    public class ArtefactListDTO
    {
        public List<ArtefactDTO> result { get; set; }
    }
    
    public class ArtefactDesignationListDTO
    {
        public List<ArtefactDesignationDTO> result { get; set; }
    }
}
