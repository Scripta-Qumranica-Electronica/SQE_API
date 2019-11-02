using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
    public class TransformationDTO
    {
        public float? scale { get; set; }
        public float? rotate { get; set; }
        public TranslateDTO translate { get; set; }
    }

    public class TranslateDTO
    {
        [Required]
        public uint x { get; set; }
        [Required]
        public uint y { get; set; }
    }
}