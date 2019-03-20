using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.Backend.Server.DTOs
{
    public class ScrollVersion
    {
        public int id { get; set; }
        public string name { get; set; }
        public Permission permission { get; set; }
        public User owner { get; set; }
        public string thumbnailUrl { get; set; }
        public List<Share> shares { get; set; }
        public bool locked { get; set; }
        public bool isPublic { get; set; }
        public DateTime? lastEdit { set; get; }
    }

    public class ScrollVersionGroup
    {
        public ScrollVersion primary { get; set; }
        public IEnumerable<ScrollVersion> others { get; set; }
    };

    public class ScrollVersionList
    {
        public List<List<ScrollVersion>> scrollVersions { get; set; }
    };

    public class Permission
    {
        public bool canWrite { get; set; }
        public bool canAdmin { get; set; }
    }

    public class Share
    {
        public User user { get; set; }
        public Permission permission { get; set; }
    }

    public class ScrollUpdateRequest
    {
        // Currently only the name can be updated
        public string name { get; set; }
    }
}
