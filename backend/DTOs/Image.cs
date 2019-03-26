using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class Image
    {
        public string url { get; set; }
        public ligthing ligthingType { get; set; }
        public direction ligthingDirection { get; set; }
        public string [] waveLength { get; set; }
        public string type { get; set; }
        public string side { get; set; }
        public Polygon regionInMaster { get; set; }
        public Polygon regionOfMaster { get; set; }
        public string transformToMaster { get; set; }
        public int master { get; set; }
        public int catalog_number { get; set; }

        public enum ligthing { direct, raking }
        public enum direction { left, right, top }
    }

    public class ImageList
    {
        public List<Image> ImagesList { get; set; }
    }
}
