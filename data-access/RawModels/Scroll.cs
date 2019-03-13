using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQE.Backend.DataAccess.Models;
using Newtonsoft.Json;

namespace SQE.Backend.DataAccess.RawModels
{

    internal class ListScrollQueryResponse : IQueryResponse<ScrollVersion>
    {
        public string name { get; set; }
        public int scroll_version_id { get; set; }
        public string scroll_version_ids { get; set; }
        public string thumbnails { get; set; }

        public ScrollVersion CreateModel()
        {
            return new ScrollVersion
            {
                Name = name,
                ScrollVersionId = scroll_version_id,
                ScrollVersionIds = JsonConvert.DeserializeObject<List<int>>(scroll_version_ids),
                ThumbnailURLs = thumbnails == null ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(thumbnails),
            };
        }
    }

    internal class ListVersionsOfScrollQueryResponse : IQueryResponse<ScrollVersion>
    {
        public string name { get; set; }
        public int scroll_version_id { get; set; }
        public string owner { get; set; }
        public int? numOfArtefacts { get; set; }
        public int? numOfColsFrags { get; set; }
        public int locked { get; set; }
        public int can_write { get; set; }
        public int can_lock { get; set; }
        public DateTime lastEdit { get; set; }

        private class RawOwner
        {
            public string name { get; set; } 
            public int user_id { get; set; }
        }

        public ScrollVersion CreateModel()
        {
            var ownerObj = JsonConvert.DeserializeObject<RawOwner>(owner);

            var sv = new ScrollVersion
            {
                Name = name,
                Id = scroll_version_id,
                Permission = new Permission
                {
                    CanWrite = can_write == 1,
                    CanRead = can_lock == 1,
                },
                Locked = locked == 1
            };

            return sv;
        }
    }

    internal class ListMyScrollVersionsQueryResponse : IQueryResponse<ScrollVersion>
    {
        public string name { get; set; }
        public int scroll_version_group_id { get; set; }
        public string owner { get; set; }
        public string shared { get; set; }
        public string scroll_version_ids { get; set; }
        public string thumbnails { get; set; }
        public int image_fragments { get; set; }
        public int scroll_version_id { get; set; }

        private class RawOwner
        {
            public string name { get; set; }
            public int user_id { get; set; }
        }

        private class RawShareInfo
        {
            public string name { get; set; }
            public int user_id { get; set; }
            public int scroll_version_id { get; set; }
            public int may_write { get; set; }
            public int may_lock { get; set; }
        }

        public ScrollVersion CreateModel()
        {
            var ownerObj = JsonConvert.DeserializeObject<RawOwner>(owner);
            var sharedList = JsonConvert.DeserializeObject<List<RawShareInfo>>(shared);
            var thumbnailList = JsonConvert.DeserializeObject<List<string>>(thumbnails);

            var sv = new ScrollVersion
            {
                Name = name,
                Id = scroll_version_id,
                ThumbnailURLs = thumbnailList,
                Sharing = sharedList.Select(shared => new Share
                {
                    User = new User
                    {
                        UserName = shared.name,
                        UserId = shared.user_id,
                    },
                    Permission = new Permission
                    {
                        CanWrite = shared.may_write == 1,
                        CanRead = shared.may_lock == 1,
                    },
                }).ToList(),
            };

            return sv;
        }
    }

    internal class ListScrollVersionsQueryResponse: IQueryResponse<ScrollVersion>
    {
        public int scroll_id { get; set; }
        public string name { get; set; }
        public int can_lock { get; set; }
        public int can_write { get; set; }
        public int locked { get; set; }
        public string owner { get; set; }
        public DateTime lastEdit { get; set; }


        public ScrollVersion CreateModel()
        {
            var ownerObj = JsonConvert.DeserializeObject<RawOwner>(owner);

            var sv = new ScrollVersion
            {
                Name = name,
                Id = scroll_id,
                Permission = new Permission
                {
                    CanWrite = can_write == 1,
                    CanRead = can_lock == 1,
                },
                Locked = locked == 1,
                LastEdit = lastEdit
            };

            return sv;
        }
        private class RawOwner
        {
            public string name { get; set; }
            public int user_id { get; set; }
        }
    }

    internal class ScrollVersionsQueryResponse : IQueryResponse<ScrollVersion>
    {
        public int id { get; set; }
        public string name { get; set; }
        public int locked { get; set; }
        public DateTime lastEdit { get; set; }
        public string thumbnails { get; set; }
        public string user_name { get; set; }
        public string user_id { get; set; }


        public ScrollVersion CreateModel()
        {
            var sv = new ScrollVersion
            {
                Name = name,
                Id = id,
                Permission = new Permission
                {
                    CanWrite  = true,
                    CanRead = true,
                },
                Locked = locked == 1,
                LastEdit = lastEdit,
                Thumbnail = thumbnails,
            };
            if(user_name == "sqe_api" )
            {
                sv.IsPublic = true;   
            }
            else
            {
                sv.IsPublic = false;
            }

            return sv;
        }
    }
}
