using System.Collections.Generic;
using System.Linq;
using SQE.API.DTO;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Serialization
{
    public static partial class ExtensionsDTO
    {
        public static InstitutionalImageDTO ToDTO(this InstitutionImage image)
        {
            return new InstitutionalImageDTO
            {
                id = image.Name,
                thumbnailUrl = image.Thumbnail,
                license = image.License
            };
        }

        public static InstitutionalImageListDTO ToDTO(this IEnumerable<InstitutionImage> images)
        {
            return new InstitutionalImageListDTO
            {
                institutionalImages = images.Select(x => x.ToDTO()).ToList()
            };
        }
    }
}