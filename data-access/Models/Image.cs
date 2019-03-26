using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.Backend.DataAccess.Models
{
    public class Image
    {
        public int Id { get; set; }
        public string URL { get; set; }
        public int DPI { get; set; }
        public int Type { get; set; }
        public int WavelengthStart { get; set; }
        public int WavelengthEnd { get; set; }
        public bool IsMaster { get; set; }
    }

    public class ImageInstitution
    {
        public string Name { get; set; }
    }
}
