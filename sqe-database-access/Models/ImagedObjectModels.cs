﻿using System.Collections.Generic;

namespace SQE.DatabaseAccess.Models
{
    public class ImagedObject
    {
        public string Id { get; set; }

        public string Institution { get; set; }
        public string Catalog1 { get; set; }
        public string Catalog2 { get; set; }
    }

    public class ImageStack
    {
        public uint Id { get; set; }
        public List<Image> Images { set; get; }
        public int MasterIndex { set; get; }
    }

    public class ImagedObjectTextFragmentMatch
    {
        public uint EditionId { get; set; }
        public string ManuscriptName { get; set; }
        public uint TextFragmentId { get; set; }
        public string TextFragmentName { get; set; }
        public byte Side { get; set; }
    }
}