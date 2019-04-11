using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class ImageDTO
    {
        public string url { get; set; }
        public lighting lightingType { get; set; }
        public direction lightingDirection { get; set; }
        public string[] waveLength { get; set; }
        public string type { get; set; }
        public string side { get; set; }
        public PolygonDTO regionInMaster { get; set; }
        public PolygonDTO regionOfMaster { get; set; }
        public string transformToMaster { get; set; }
        public bool master { get; set; }
        public uint catalog_number { get; set; }

        public enum lighting {direct, raking }
        public enum direction { left, right, top}
    }

    public class ImageListDTO
    {
        public List<ImageDTO> ImagesList { get; set; }
    }

    public class ImageGroupDTO
    {
        public uint Id { get; set; }
        public string Institution { get; set; }
        public string CatalogNumber1 { get; set; }
        public string CatalogNumber2 { get; set; }
        public byte CatalogSide { get; set; }
        public List<ImageDTO> Images { get; set; }

        public ImageGroupDTO(uint id, string institution, string catalogNumber1, string catalogNumber2, byte catalogSide, List<ImageDTO> images)
        {
            Id = id;
            Institution = institution;
            CatalogNumber1 = catalogNumber1;
            CatalogNumber2 = catalogNumber2;
            CatalogSide = catalogSide;
            Images = images;
        }
    }

    public class ImageGroupListDTO
    {
        public List<ImageGroupDTO> ImageGroup { get; set; }
        public ImageGroupListDTO(List<ImageGroupDTO> imageGroup)
        {
            ImageGroup = imageGroup;
        }
    }

    public class ImageInstitutionDTO
    {
        public string Name { get; set; }
        public ImageInstitutionDTO(string name)
        {
            Name = name;
        }
    }

    public class ImageInstitutionListDTO
    {
        public List<ImageInstitutionDTO> Institutions { get; set; }
        public ImageInstitutionListDTO(List<ImageInstitutionDTO> institutions)
        {
            Institutions = institutions;
        }
    }
}
