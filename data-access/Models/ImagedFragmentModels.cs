using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.Backend.DataAccess.Models
{
    public class ImagedFragment
    {
        public string Id
        {
            get
            {
                return Institution + "-" + Catalog1 + "-" + Catalog2;
            }
        }
        public string Institution { get; set; }
        public string Catalog1 { get; set; }
        public string Catalog2 { get; set; }

        public static ImagedFragment FromId(string id)
        {
            if (id == null)
                return null;
            var tokens = id.Split("-");

            var imagedFragment = new ImagedFragment
            {
                Institution = tokens[0],
                Catalog1 = tokens[1],
                Catalog2 = tokens[2]
            };

            // TODO: Create an ImagedFragment model from the id
            return imagedFragment;
        }
    }

    public class ImageStack
    {
        public uint Id { get; set; }
        public List<Image> Images { set; get; }
        public int MasterIndex { set; get; }
    }

}
