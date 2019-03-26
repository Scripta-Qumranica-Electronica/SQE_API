using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.Backend.DataAccess.Models
{
    public class ImageGroup
    {
        public int Id { get; set; }
        public string Institution { get; set; }
        public string CatalogNumber1 { get; set; }
        public string CatalogNumber2 { get; set; }
        public int CatalogSide { get; set; }
        public List<Image> Images { get; set; }
    }
}
