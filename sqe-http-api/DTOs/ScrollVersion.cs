using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.SqeHttpApi.Server.DTOs
{
    public class EditionDTO
    {
        public uint id { get; set; }
        public string name { get; set; }
        public PermissionDTO permission { get; set; }
        public UserDTO owner { get; set; }
        public string thumbnailUrl { get; set; }
        public List<Share> shares { get; set; }
        public bool locked { get; set; }
        public bool isPublic { get; set; }
        public DateTime? lastEdit { set; get; }
    }

    public class EditionGroupDTO
    {
        public EditionDTO primary { get; set; }
        public IEnumerable<EditionDTO> others { get; set; }
    };

    public class EditionListDTO
    {

        public List<List<EditionDTO>> result { get; set; }
    };

    public class PermissionDTO
    {
        public bool canWrite { get; set; }
        public bool canAdmin { get; set; }
    }

    public class Share
    {
        public UserDTO user { get; set; }
        public PermissionDTO permission { get; set; }
    }

    public class ScrollUpdateRequestDTO
    {
        // Currently only the Name can be updated
        public string name { get; set; }
    }
}
