using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class Image
    {
        public string url { get; set; }
        public lighting lightingType { get; set; }
        public direction lightingDirection { get; set; }
        public string[] waveLength { get; set; }
        public string type { get; set; }
        public string side { get; set; }
        public Polygon regionInMaster { get; set; }
        public Polygon regionOfMaster { get; set; }
        public string transformToMaster { get; set; }
        public bool master { get; set; }
        public uint catalog_number { get; set; }

        public enum lighting {direct, raking }
        public enum direction { left, right, top}
    }

    public class ImageList
    {
        public List<Image> ImagesList { get; set; }
    }

    public class ImageGroup
    {
        public uint Id { get; set; }
        public string Institution { get; set; }
        public string CatalogNumber1 { get; set; }
        public string CatalogNumber2 { get; set; }
        public byte CatalogSide { get; set; }
        public List<Image> Images { get; set; }

        public ImageGroup(uint id, string institution, string catalogNumber1, string catalogNumber2, byte catalogSide, List<Image> images)
        {
            Id = id;
            Institution = institution;
            CatalogNumber1 = catalogNumber1;
            CatalogNumber2 = catalogNumber2;
            CatalogSide = catalogSide;
            Images = images;
        }
    }

    public class ImageGroupList
    {
        public List<ImageGroup> ImageGroup { get; set; }
        public ImageGroupList(List<ImageGroup> imageGroup)
        {
            ImageGroup = imageGroup;
        }
    }

    public class ImageInstitution
    {
        public string Name { get; set; }
        public ImageInstitution(string name)
        {
            Name = name;
        }
    }

    public class ImageInstitutionList
    {
        public List<ImageInstitution> Institutions { get; set; }
        public ImageInstitutionList(List<ImageInstitution> institutions)
        {
            Institutions = institutions;
        }
    }
}
