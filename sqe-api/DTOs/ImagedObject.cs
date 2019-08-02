using System.Collections.Generic;

namespace SQE.SqeApi.Server.DTOs
{
    public class ImageStackDTO
    {
        public uint? id { get; set; }
        public List<ImageDTO> images { get; set; }
        public int? masterIndex { get; set; }
    }

    public class ImagedObjectDTO
    {
        public string id { get; set; }
        public ImageStackDTO recto { get; set; }
        public ImageStackDTO verso { get; set; }
        public List<ArtefactDTO> artefacts { get; set; }
    }

    public class ImagedObjectListDTO
    {
        public List<ImagedObjectDTO> imagedObjects { get; set; }
    }
}
