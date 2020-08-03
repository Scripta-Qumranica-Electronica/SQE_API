using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SQE.API.DTO
{
    public class SimpleImageDTO
    {
        public enum Direction
        {
            left,
            right,
            top
        }

        public enum Lighting
        {
            direct,
            raking
        }

        public uint id { get; set; }
        public string url { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Lighting lightingType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Direction lightingDirection { get; set; }

        public string[] waveLength { get; set; }
        public string type { get; set; }
        public string side { get; set; }
        public uint ppi { get; set; }
        public bool master { get; set; }
        public uint catalogNumber { get; set; }
    }

    public class ImageDTO : SimpleImageDTO
    {
        public uint? imageToImageMapEditorId { get; set; }
        public string regionInMasterImage { get; set; }
        public string regionInImage { get; set; }
        public string transformToMaster { get; set; }
    }

    public class SimpleImageListDTO
    {
        public SimpleImageDTO[] images { get; set; }
    }

    public class ImageInstitutionDTO
    {
        public ImageInstitutionDTO(string name)
        {
            this.name = name;
        }

        public string name { get; set; }
    }

    public class ImageInstitutionListDTO
    {
        public ImageInstitutionListDTO(List<ImageInstitutionDTO> institutions)
        {
            this.institutions = institutions;
        }

        public List<ImageInstitutionDTO> institutions { get; set; }
    }

    public class InstitutionalImageDTO
    {
        public string id { get; set; }
        public string thumbnailUrl { get; set; }
        public string license { get; set; }
    }

    public class InstitutionalImageListDTO
    {
        public List<InstitutionalImageDTO> institutionalImages { get; set; }
    }
}