using System.ComponentModel.DataAnnotations;
using SQE.API.DTO.Validators;

namespace SQE.API.DTO
{
    public class TransformationDTO
    {
        [Range(0.1, 99.9999, ErrorMessage = "The scale must be between 0.1 and 99.9999")]
        [ValidDecimal(6, 4,
            ErrorMessage = "The scale cannot have more than 2 digits to the left of the decimal and 4 digits to the right")]
        public decimal scale { get; set; }
        [Range(0, 360, ErrorMessage = "The rotate must be between 0 and 360")]
        [ValidDecimal(6, 2,
            ErrorMessage = "The rotate cannot have more than 4 digits to the left of the decimal and 2 digits to the right")]
        public decimal rotate { get; set; }
        public uint zIndex { get; set; }
        public TranslateDTO translate { get; set; }
    }

    public class TranslateDTO
    {
        [Required] public uint x { get; set; }

        [Required] public uint y { get; set; }
    }
}