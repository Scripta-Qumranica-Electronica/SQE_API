using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class ImageStackDTO
    {
        public int id { get; set; }
        public List<ImageDTO> images {get; set;}
        public int masterIndex { get; set; }
    }
}
