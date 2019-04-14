using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class ImageGroup
    {
        public uint Id { get; set; }
        public string Institution { get; set; }
        public string CatalogNumber1 { get; set; }
        public string CatalogNumber2 { get; set; }
        public byte CatalogSide { get; set; }
        public List<Image> Images { get; set; }
    }
}
