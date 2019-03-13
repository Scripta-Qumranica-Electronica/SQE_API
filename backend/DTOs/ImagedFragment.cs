using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class ImagedFragment
    {
        public string id { get; set; }
        public Image recto { get; set; }
        public Image verso { get; set; }
        public List<Artefact> Artefacts { get; set; }

    }
}
