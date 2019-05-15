using System.Collections.Generic;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class ImageStackDTO
    {
        public uint? id { get; set; }
        public List<ImageDTO> images { get; set; }
        public int? masterIndex { get; set; }
    }

    public class ImagedObjectDTO
    {
        public string id { get; set; }
        public ImageStackDTO recto { get; set; }
        public ImageStackDTO verso { get; set; }
        public List<ArtefactDTO> artefacts { get; set; }
    }

    public class ImagedObjectListDTO
    {
        public List<ImagedObjectDTO> imagedObjects { get; set; }
    };

    public static class ImagedObjectIdFormat
    {
        /// <summary>
        /// Creates a properly formatted imagedObjectId from the input parameters
        /// </summary>
        /// <param Name="Institution">Name of the Institution that created the image</param>
        /// <param Name="CatalogNumber1">Primary indexing number/identification</param>
        /// <param Name="CatalogNumber2">Secondary indexing number/identification, if it exists</param>
        /// <returns></returns>
        public static string Serialize(string institution, string catalogNumber1, string catalogNumber2)
        {
            return institution + "-"
                               + catalogNumber1
                               + (string.IsNullOrEmpty(catalogNumber2)
                                   ? ""
                                   : "-" + catalogNumber2);
        }

        // Warning: I like the idea to create a readable imagedObjectID from the object's cataloguing information.
        // It should be noted that it is possible that Deserialization may fail. If there is a "-" in the Institution 
        // Name, the CatalogNumber1, or the CatalogNumber2, this deserialize function will fail.
        // TODO: Can we find a safer method to join/deconstruct this imagedObjectID
        
        /// <summary>
        /// Breaks an imagedObjectId into its constituent parts
        /// </summary>
        /// <param Name="imagedObjectId"></param>
        /// <returns></returns>
        public static (string institution, string catalogNumber1, string catalogNumber2) Deserialize(
            string imagedObjectId)
        {
            var constituents = imagedObjectId.Split("-");
            return constituents.Length > 1
                       ? (constituents[0], constituents[1], constituents.Length > 2 ? constituents[2] : null)
                       : (null, null, null);
        }
    }
}
