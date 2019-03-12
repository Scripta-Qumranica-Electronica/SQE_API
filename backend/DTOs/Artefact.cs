using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.DTOs
{
    public class Artefact
    {
        public string id { get; set; }
        public string scrollVersionId { get; set; }
        public string imageFragmentId {get; set;}
        public string name { get; set; }
        public Polygon mask { get; set; }
        public string transformMatrix { get; set; }
        public string zOrder { get; set; }
        public artSide side { get; set; }

        public enum artSide { recto, verso}
    }
}
