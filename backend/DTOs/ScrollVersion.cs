using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class ScrollVersionDTO
    {
        public int id { get; set; }
        public string name { get; set; }
        public PermissionDTO permission { get; set; }
        public UserDTO owner { get; set; }
        public string thumbnailUrl { get; set; }
        public List<Share> shares { get; set; }
        public bool locked { get; set; }
        public bool isPublic { get; set; }
        public DateTime? lastEdit { set; get; }
    }

    public class ScrollVersionGroupDTO
    {
        public ScrollVersionDTO primary { get; set; }
        public IEnumerable<ScrollVersionDTO> others { get; set; }
    };

    public class ScrollVersionListDTO
    {

        public List<List<ScrollVersionDTO>> result { get; set; }
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
        // Currently only the name can be updated
        public string name { get; set; }
    }
}
