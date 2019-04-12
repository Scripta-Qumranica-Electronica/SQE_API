using SQE.Backend.DataAccess;
using SQE.Backend.DataAccess.Helpers;
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
        Task<ScrollVersionGroupDTO> GetScrollVersionAsync(uint scrollVersionId, uint? userId, bool artefacts = false, bool fragments = false);
        Task<ScrollVersionListDTO> ListScrollVersionsAsync(uint? userId);
        Task<ScrollVersionDTO> UpdateScroll(uint scrollVersionId, string name, uint userId);
        Task<ScrollVersionDTO> CopyScroll(uint scrollVersionId, string name, uint userId);
    }

    public class ScrollService : IScrollService
    {
        IScrollRepository _repo;

        public ScrollService(IScrollRepository repo)
        {
            _repo = repo;
        }

        public async Task<ScrollVersionGroupDTO> GetScrollVersionAsync(uint scrollVersionId, uint? userId, bool artefacts, bool fragments)
        {
            var groups = await _repo.GetScrollVersionGroups(scrollVersionId);
            if (groups.Count == 0)
                return null;
            Debug.Assert(groups.Count == 1, "How come we got more than 1 group?!");

            var scrollIds = groups[groups.Keys.First()];

            var scrollModels = await _repo.ListScrollVersions(userId, scrollIds);

            var primaryModel = scrollModels.FirstOrDefault(sv => sv.Id == scrollVersionId);
            if (primaryModel == null) // User is not allowed to see this scroll version
                return null;
            var otherModels = scrollModels.Where(sv => sv.Id != scrollVersionId).OrderBy(sv => sv.Id);

            var svg = new ScrollVersionGroupDTO
            {
                primary = ScrollVersionModelToDTO(primaryModel),
                others = otherModels.Select(model => ScrollVersionModelToDTO(model)),
            };

            return svg;
        }

        public async Task<ScrollVersionListDTO> ListScrollVersionsAsync(uint? userId)
        {
            var groups = await _repo.GetScrollVersionGroups(null);
            var scrollVersions = await _repo.ListScrollVersions(userId, null);

            var scrollVersionDict = new Dictionary<uint, DataAccess.Models.ScrollVersion>();
            foreach (var sv in scrollVersions)
                scrollVersionDict[sv.Id] = sv;

            var result = new ScrollVersionListDTO
            {
                result = new List<List<ScrollVersionDTO>>(),
            };

            foreach (var groupId in groups.Keys)
            {
                var groupList = new List<ScrollVersionDTO>();
                foreach (var scrollVersionId in groups[groupId])
                {
                    if (scrollVersionDict.TryGetValue(scrollVersionId, out var scrollVersion))
                        groupList.Add(ScrollVersionModelToDTO(scrollVersion));
                }
                if (groupList.Count > 0)
                    result.result.Add(groupList);
            }

            return result;
        }

        internal static ScrollVersionDTO ScrollVersionModelToDTO(DataAccess.Models.ScrollVersion model)
        {
            return new ScrollVersionDTO
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

        internal static PermissionDTO PermissionModelToDTO(DataAccess.Models.Permission model)
        {
            return new PermissionDTO
            {
                canAdmin = model.CanAdmin,
                canWrite = model.CanWrite,
            };
        }

        internal static DataAccess.Models.User OwnerToModel(UserDTO user)
        {
            return new DataAccess.Models.User
            {
                UserId = user.userId,
                UserName = user.userName
            };
        }

        internal static DataAccess.Models.Permission PermissionDtoTOModel(PermissionDTO permission)
        {
            return new DataAccess.Models.Permission
            {
                CanAdmin = permission.canAdmin,
                CanWrite = permission.canWrite
            };
        }

        public async Task<ScrollVersionDTO> UpdateScroll(uint scrollVersionId, string name, uint userId)
        {
            if (name != "") 
            {
                // Bronson: Look how I handled the case of no permission
                // Itay: Awesome, thanks.  That is nice.
                try
                {
                    await _repo.ChangeScrollVersionName(scrollVersionId, name, userId);
                } 
                catch(NoPermissionException)
                {
                    throw new NotFoundException(scrollVersionId);
                }
            }
            else
            {
                throw new ImproperRequestException("change scroll name", "scroll name cannot be empty");
            }
            
            var scrollID = new List<uint>(new uint[] { scrollVersionId });
            var scroll = await _repo.ListScrollVersions(userId, scrollID); //get wanted scroll by scroll id
            var sv = scroll.First();             // Bronson - here we do not expect a permission error, since the rename has already happened.

            return ScrollVersionModelToDTO(sv);
        }

        async Task<ScrollVersionDTO> IScrollService.CopyScroll(uint scrollVersionId, string name, uint userId)
        {
            ScrollVersionDTO sv;
            // Clone scroll
            var copyToScrollVersionId = await _repo.CopyScrollVersion(scrollVersionId, (ushort) userId);
            if (scrollVersionId == copyToScrollVersionId)
            {
                // Check if is success is true, else throw error.
                throw new System.Exception($"Failed to clone {scrollVersionId}.");
            }
                
            scrollVersionId = copyToScrollVersionId;
            
            //Change the name, if a name has been passed
            if (!String.IsNullOrEmpty(name))
            {
                sv = await UpdateScroll(scrollVersionId, name, userId);
            }
            else
            {
                var scrollID = new List<uint>(new uint[] { scrollVersionId });

                var scroll = await _repo.ListScrollVersions(userId, scrollID); //get wanted scroll by id
                var unformattedSv = scroll.First();
                //I think we do not get this far if no records were found, `First` will, I think throw an error.
                //Maybe we should more often make use of try/catch.
                if (unformattedSv == null)
                {
                    throw new NotFoundException(scrollVersionId);
                }
                sv = ScrollVersionModelToDTO(unformattedSv);
            }
            return sv; //need to return the updated scroll
        }
    }
}
