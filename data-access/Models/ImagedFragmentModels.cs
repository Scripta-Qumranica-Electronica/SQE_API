using System;
using System.Collections.Generic;
using System.Text;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class ImagedObject
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

        public static ImagedObject FromId(string id)
        {
            if (id == null)
                return null;
            var tokens = id.Split("-");

            var imagedFragment = new ImagedObject
            {
                Institution = tokens[0],
                Catalog1 = tokens[1],
                Catalog2 = tokens[2]
            };

            // TODO: Create an ImagedObject model from the id
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
