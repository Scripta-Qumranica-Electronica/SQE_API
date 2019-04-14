using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class ImagedFragmentDTO
    {
        public string id { get; set; }
        public ImageStackDTO recto { get; set; } 
        public ImageStackDTO verso { get; set; }
        public List<ArtefactDTO> Artefacts { get; set; }

    }
    public class ImagedFragmentListDTO
    {
        public List<ImagedFragmentDTO> result { get; set; }
    };
}
