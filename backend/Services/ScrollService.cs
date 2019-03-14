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
        Task<ScrollVersionGroup> GetScrollVersionAsync(int scrollId, int? userId, bool artefacts=false, bool fragments=false);
        Task<ScrollVersionList> ListScrollVersionsAsync(int? userId);
    }

    public class ScrollService: IScrollService
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
                foreach(var scrollVersionId in groups[groupId])
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
    }
}
