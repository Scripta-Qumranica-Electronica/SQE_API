using System.ComponentModel.DataAnnotations;

namespace SQE.API.DTO
{
    public class WktPolygonDTO
    {
        [Required] public string wktPolygon { get; set; }
    }
}