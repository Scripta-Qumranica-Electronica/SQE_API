using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class ImageDTO
    {
        public string url { get; set; }
        public ligthing ligthingType { get; set; }
        public direction ligthingDirection { get; set; }
        public string [] waveLength { get; set; }
        public string type { get; set; }
        public string side { get; set; }
        public PolygonDTO regionInMaster { get; set; }
        public PolygonDTO regionOfMaster { get; set; }
        public string transformToMaster { get; set; }
        public int master { get; set; }
        public int catalog_number { get; set; }

        public enum ligthing { direct, raking }
        public enum direction { left, right, top }
    }

    public class ImageList
    {
        public List<ImageDTO> ImagesList { get; set; }
    }
}
