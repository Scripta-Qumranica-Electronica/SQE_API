using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class ImageDTO
    {
        public string url { get; set; }
        public Lighting lightingType { get; set; }
        public Direction lightingDirection { get; set; }
        public string[] waveLength { get; set; }
        public string type { get; set; }
        public string side { get; set; }
        public PolygonDTO regionInMaster { get; set; }
        public PolygonDTO regionOfMaster { get; set; }
        public string transformToMaster { get; set; }
        public bool master { get; set; }
        public uint catalogNumber { get; set; }

        public enum Lighting { direct, raking }
        public enum Direction { left, right, top }
    }

    public class ImageListDTO
    {
        public List<ImageDTO> images { get; set; }
    }

    public class ImageGroupDTO
    {
        public uint id { get; set; }
        public string institution { get; set; }
        public string catalogNumber1 { get; set; }
        public string catalogNumber2 { get; set; }
        public byte catalogSide { get; set; }
        public List<ImageDTO> images { get; set; }

        public ImageGroupDTO(uint id, string institution, string catalogNumber1, string catalogNumber2, byte catalogSide, List<ImageDTO> images)
        {
            this.id = id;
            this.institution = institution;
            this.catalogNumber1 = catalogNumber1;
            this.catalogNumber2 = catalogNumber2;
            this.catalogSide = catalogSide;
            this.images = images;
        }
    }

    public class ImageGroupListDTO
    {
        public List<ImageGroupDTO> imageGroups { get; set; }
        public ImageGroupListDTO(List<ImageGroupDTO> groups)
        {
            this.imageGroups = groups;
        }
    }

    public class ImageInstitutionDTO
    {
        public string name { get; set; }
        public ImageInstitutionDTO(string name)
        {
            this.name = name;
        }
    }

    public class ImageInstitutionListDTO
    {
        public List<ImageInstitutionDTO> institutions { get; set; }
        public ImageInstitutionListDTO(List<ImageInstitutionDTO> institutions)
        {
            this.institutions = institutions;
        }
    }
}
