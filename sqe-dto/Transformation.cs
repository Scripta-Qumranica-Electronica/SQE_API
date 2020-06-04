using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
    public class TransformationDTO
    {
        [Range(0, 99.9999)]
        public float scale { get; set; }
        [Range(0, 9999.99)]
        public float rotate { get; set; }
        public uint zIndex { get; set; }
        public TranslateDTO translate { get; set; }
    }

    public class TranslateDTO
    {
        [Required] public uint x { get; set; }

        [Required] public uint y { get; set; }
    }
}