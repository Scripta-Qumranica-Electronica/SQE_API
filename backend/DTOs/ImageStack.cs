using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class ImageStack
    {
        public int id { get; set; }
        public List<Image> images {get; set;}
        public int masterIndex { get; set; }
    }
}
