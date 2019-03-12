using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.DTOs
{
    public class ImageStack
    {
        public int id { get; set; }
        public List<Image> images {get; set;}
        public int masterIndex { get; set; }
    }
}
