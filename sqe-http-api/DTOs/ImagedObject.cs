using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class ImagedObjectDTO
    {
        public string id { get; set; }
        public ImageStackDTO recto { get; set; } // TODO: Change to ImageStack
        public ImageStackDTO verso { get; set; } // TODO: Change to ImageStack
        public List<ArtefactDTO> Artefacts { get; set; }

    }
    public class ImagedObjectListDTO
    {
        public List<ImagedObjectDTO> result { get; set; }
    };
}
