using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.Backend.DataAccess.Models
{
    public class Image
    {
        public string URL { get; set; }
        public string[] WaveLength { set; get; }
        public int Type { set; get; }
        public string Side { get; set; }
        public Polygon RegionInMaster { set; get; }
        public Polygon RegionOfMaster { set; get; }
        public string TransformMatrix { set; get; }
        public int ImageCatalogId { set; get; }
        public string Institution { set; get; }
        public string Catlog1 { set; get; }
        public string Catalog2 { set; get; }
        public int Master { set; get; }
   }
}
