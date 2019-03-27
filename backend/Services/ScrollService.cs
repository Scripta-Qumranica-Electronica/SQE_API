using SQE.Backend.DataAccess;
using SQE.Backend.Server.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace SQE.Backend.Server.Services
{
    public interface IScrollService
    {
        Task<ScrollVersionGroup> GetScrollVersionAsync(int scrollId, int? userId, bool artefacts = false, bool fragments = false);
        Task<ScrollVersionList> ListScrollVersionsAsync(int? userId);
        Task<ScrollVersion> UpdateScroll(int scrollId, string name, int? userId);
        Task<ScrollVersion> CopyScroll(int scrollId, string name, int? userId);
    }

    public class ScrollService : IScrollService
    {
        IScrollRepository _repo;

        public ScrollService(IScrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<ScrollVersionGroup> GetScrollVersionAsync(int scrollId, int? userId, bool artefacts, bool fragments)
        {
            var groups = await _repo.GetScrollVersionGroups(scrollId);
            if (groups.Count == 0)
                return null;
            Debug.Assert(groups.Count == 1, "How come we got more than 1 group?!");

            var scrollIds = groups[groups.Keys.First()];

            var scrollModels = await _repo.ListScrollVersions(userId, scrollIds);

            var primaryModel = scrollModels.FirstOrDefault(sv => sv.Id == scrollId);
            if (primaryModel == null) // User is not allowed to see this scroll version
                return null;
            var otherModels = scrollModels.Where(sv => sv.Id != scrollId).OrderBy(sv => sv.Id);

            var svg = new ScrollVersionGroup
            {
                primary = ScrollVersionModelToDTO(primaryModel),
                others = otherModels.Select(model => ScrollVersionModelToDTO(model)),
            };

            return svg;
        }

        public async Task<ScrollVersionList> ListScrollVersionsAsync(int? userId)
        {
            var groups = await _repo.GetScrollVersionGroups(null);
            var scrollVersions = await _repo.ListScrollVersions(userId, null);

            var scrollVersionDict = new Dictionary<int, DataAccess.Models.ScrollVersion>();
            foreach (var sv in scrollVersions)
                scrollVersionDict[sv.Id] = sv;

            var result = new ScrollVersionList
            {
                scrollVersions = new List<List<ScrollVersion>>(),
            };

            foreach (var groupId in groups.Keys)
            {
                var groupList = new List<ScrollVersion>();
                foreach (var scrollVersionId in groups[groupId])
                {
                    if (scrollVersionDict.TryGetValue(scrollVersionId, out var scrollVersion))
                        groupList.Add(ScrollVersionModelToDTO(scrollVersion));
                }
                if (groupList.Count > 0)
                    result.scrollVersions.Add(groupList);
            }

            return result;
        }

        internal static ScrollVersion ScrollVersionModelToDTO(DataAccess.Models.ScrollVersion model)
        {
            return new ScrollVersion
            {
                id = model.Id,
                name = model.Name,
                permission = PermissionModelToDTO(model.Permission),
                owner = UserService.UserModelToDTO(model.Owner),
                thumbnailUrl = model.Thumbnail,
                locked = model.Locked,
                isPublic = model.IsPublic,
                lastEdit = model.LastEdit
            };
        }

        internal static Permission PermissionModelToDTO(DataAccess.Models.Permission model)
        {
            return new Permission
            {
                canAdmin = model.CanAdmin,
                canWrite = model.CanWrite,
            };
        }

        internal static DataAccess.Models.User OwnerToModel(User user)
        {
            return new DataAccess.Models.User
            {
                UserId = user.userId,
                UserName = user.userName
            };
        }

        internal static DataAccess.Models.Permission PermissionDtoTOModel(Permission permission)
        {
            return new DataAccess.Models.Permission
            {
                CanAdmin = permission.canAdmin,
                CanWrite = permission.canWrite
            };
        }

        public async Task<ScrollVersion> UpdateScroll(int scrollId, string name, int? userId)
        {
            List<int> scrollID = new List<int>(new int[] { scrollId });

            var scroll = await _repo.ListScrollVersions(userId, scrollID); //get wanted scroll by scroll id
            var sv = scroll.First();

            if (sv == null)
            {
                throw new NotFoundException(scrollId);
            }

            if (sv.Permission.CanWrite == false)
            {
                throw new ForbiddenException(scrollId);
            }
            if (name != sv.Name) //there is sv, but the name is diffrent and have permission of write
            { /**
                await _repo.ChangeScrollVersionName(sv, name);
                var updatedScroll = await _repo.ListScrollVersions(userId, scrollID);
                return ScrollVersionModelToDTO(updatedScroll.First());**/
            }
            return ScrollVersionModelToDTO(sv); //need to chane to update scroll

        }

        public async Task<ScrollVersion> CopyScroll(int scrollId, string name, int? userId)
        {
            List<int> scrollID = new List<int>(new int[] { scrollId });

            var scroll = await _repo.ListScrollVersions(userId, scrollID); //get wanted scroll by id
            var sv = scroll.First();

            if (sv == null)
            {
                throw new NotFoundException(scrollId);
            }
            var canRead = await _repo.CanRead(scrollId, userId);

            if (canRead == false)
            {
                throw new ForbiddenException(scrollId);
            }
            /**
            var updatedScroll = await _repo.CopyScrollVersion(sv, name, userId);

            var Scroll = await _repo.ListScrollVersions(userId, scrollID);
            return ScrollVersionModelToDTO(Scroll.First());**/

            return null; //need to return the updated scroll
        }
    }
}
