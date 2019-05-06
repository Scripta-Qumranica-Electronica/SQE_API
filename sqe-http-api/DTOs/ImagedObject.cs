using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class ImagedObjectDTO
    {
        public string id { get; set; }
        public ImageStackDTO recto { get; set; }
        public ImageStackDTO verso { get; set; }
        public List<ArtefactDTO> artefacts { get; set; }
    }

    public class ImagedObjectListDTO
    {
        public List<ImagedObjectDTO> result { get; set; }
    };
}
