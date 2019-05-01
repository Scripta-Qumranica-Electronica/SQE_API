using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class ArtefactDTO
    {
<<<<<<< HEAD
        public int id { get; set; }
        public int scrollVersionId { get; set; }
=======
        public uint id { get; set; }
        public uint editionId { get; set; }
>>>>>>> 6cc19a4187d1bfe5c70efc913e4adf5b324c1a4e
        public string imageFragmentId {get; set;}
        public string name { get; set; }
        public PolygonDTO mask { get; set; }
        public string transformMatrix { get; set; }
        public uint zOrder { get; set; }
        public artSide side { get; set; }

        public enum artSide { recto, verso}
    }
    public class ArtefactListDTO
    {
        public List<ArtefactDTO> result { get; set; }
    }
}
