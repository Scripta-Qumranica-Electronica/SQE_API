using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class ImagedFragmentDTO
    {
        public string id { get; set; }
        public ImageStackDTO recto { get; set; } // TODO: Change to ImageStack
        public ImageStackDTO verso { get; set; } // TODO: Change to ImageStack
        public List<ArtefactDTO> Artefacts { get; set; }

    }
    public class ImagedFragmentListDTO
    {
        public List<ImagedFragmentDTO> result { get; set; }
    };
}
