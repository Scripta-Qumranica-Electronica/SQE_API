using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.DTOs
{
    public class ScrollVersion
    {
        public int id { get; set; }
        public string name { get; set; }
        public Permission permission { get; set; }
        public string thumbnailUrls { get; set; }
        public List<Share> shares { get; set; }
        public bool? locked { get; set; }
        public bool? isPublic { get; set; }
        public DateTime lastEdit { set; get; }

    }

    public class Permission
    {
        public bool canWrite { get; set; }
        public bool canLock { get; set; }
    }

    public class Share
    {
        public UserData user { get; set; }
        public Permission permission { get; set; }
    }

    public class UserData
    {
        public int userId { get; set; }
        public string userName { get; set; }
    }
}
