using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        [Required] public uint id { get; set; }
        [Required] public string url { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Required]
        public Lighting lightingType { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Required]
        public Direction lightingDirection { get; set; }

        [Required] public string[] waveLength { get; set; }
        [Required] public string type { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Required] public SideDesignation side { get; set; }
        [Required] public uint ppi { get; set; }
        [Required] public bool master { get; set; }
        [Required] public uint catalogNumber { get; set; }
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
        [Required] public SimpleImageDTO[] images { get; set; }
    }

    public class ImageInstitutionDTO
    {
        public ImageInstitutionDTO(string name)
        {
            this.name = name;
        }

        public ImageInstitutionDTO() : this(null) { }

        [Required] public string name { get; set; }
    }

    public class ImageInstitutionListDTO
    {
        public ImageInstitutionListDTO(List<ImageInstitutionDTO> institutions)
        {
            this.institutions = institutions;
        }

        public ImageInstitutionListDTO() : this(null) { }

        [Required] public List<ImageInstitutionDTO> institutions { get; set; }
    }

    public class InstitutionalImageDTO
    {
        [Required] public string id { get; set; }
        [Required] public string thumbnailUrl { get; set; }
        [Required] public string license { get; set; }
    }

    public class InstitutionalImageListDTO
    {
        [Required] public List<InstitutionalImageDTO> institutionalImages { get; set; }
    }
}