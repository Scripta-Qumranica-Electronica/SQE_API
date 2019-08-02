using System.Collections.Generic;

namespace SQE.SqeApi.Server.DTOs
{
    public class ImageDTO
    {
        public string id { get; set; }
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
