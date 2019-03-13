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
        public double waveLength { get; set; }
        public string type { get; set; }
        public Polygon regionInMaster { get; set; }
        public Polygon regionOfMaster { get; set; }
        public int transformToMaster { get; set; }
        

        public enum ligthing {direct, raking }
        public enum direction { left, right, top}
    }
}
