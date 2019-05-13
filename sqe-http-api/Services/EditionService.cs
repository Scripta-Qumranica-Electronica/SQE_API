using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;


namespace SQE.SqeHttpApi.Server.Helpers
{
    public interface IEditionService
    {
        Task<EditionGroupDTO> GetEditionAsync(uint editionId, UserInfo user, bool artefacts = false, bool fragments = false);
        Task<EditionListDTO> ListEditionsAsync(uint? userId);
        Task<EditionDTO> UpdateEditionAsync(UserInfo user, string name);
        Task<EditionDTO> CopyEditionAsync(UserInfo user, string name);
    }

    public class EditionService : IEditionService
    {
        private readonly IEditionRepository _repo;

        public EditionService(IEditionRepository repo)
        {
            _repo = repo;
        }

        public async Task<EditionGroupDTO> GetEditionAsync(uint editionId, UserInfo user, bool artefacts, bool fragments)
        {
            var scrollModels = await _repo.ListEditionsAsync(user.userId, editionId);

            var primaryModel = scrollModels.FirstOrDefault(sv => sv.EditionId == editionId);
            if (primaryModel == null) // User is not allowed to see this scroll version
                return null;
            var otherModels = scrollModels.Where(sv => sv.EditionId != editionId).OrderBy(sv => sv.EditionId);

            var svg = new EditionGroupDTO
            {
                primary = EditionModelToDTO(primaryModel),
                others = otherModels.Select(EditionModelToDTO),
            };

            return svg;
        }

        public async Task<EditionListDTO> ListEditionsAsync(uint? userId)
        {
            var groups = await _repo.GetEditionsAsync(null, null);
            var editions = await _repo.ListEditionsAsync(userId, null);

            var editionDict = new Dictionary<uint, DataAccess.Models.Edition>();
            foreach (var edition in editions)
                editionDict[edition.EditionId] = edition;

            var result = new EditionListDTO
            {
                editions = new List<List<EditionDTO>>(),
            };

            foreach (var groupId in groups.Keys)
            {
                var groupList = new List<EditionDTO>();
                foreach (var editionId in groups[groupId])
                {
                    if (editionDict.TryGetValue(editionId, out var scrollVersion))
                        groupList.Add(EditionModelToDTO(scrollVersion));
                }
                if (groupList.Count > 0)
                    result.editions.Add(groupList);
            }

            return result;
        }

        private static EditionDTO EditionModelToDTO(DataAccess.Models.Edition model)
        {
            return new EditionDTO
            {
                id = model.EditionId,
                name = model.Name,
                permission = PermissionModelToDTO(model.Permission),
                owner = UserService.UserModelToDTO(model.Owner),
                thumbnailUrl = model.Thumbnail,
                locked = model.Locked,
                isPublic = model.IsPublic,
                lastEdit = model.LastEdit
            };
        }

        private static PermissionDTO PermissionModelToDTO(DataAccess.Models.Permission model)
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

        public async Task<EditionDTO> UpdateEditionAsync(UserInfo user, string name)
        {
            if (!string.IsNullOrEmpty(name)) 
            {
                try
                {
                    await _repo.ChangeEditionNameAsync(user, name);
                } 
                catch(NoPermissionException)
                {
                    throw new NotFoundException(user.editionId.Value);
                }
            }
            else
            {
                throw new ImproperRequestException("change scroll Name", "scroll Name cannot be empty");
            }
            
            var editions = await _repo.ListEditionsAsync(user.userId, user.editionId); //get wanted edition by edition Id
            
            return EditionModelToDTO(editions.First(x => x.EditionId == user.editionId));
        }

        public async Task<EditionDTO> CopyEditionAsync(UserInfo user, string name)
        {
            EditionDTO edition;
            // Clone edition
            var copyToEditionId = await _repo.CopyEditionAsync(user);
            if (user.editionId == copyToEditionId)
            {
                // Check if is success is true, else throw error.
                throw new System.Exception($"Failed to clone {user.editionId}.");
            }
            user.SetEditionId(copyToEditionId); // Update user object for the new editionId
            
            //Change the Name, if a Name has been passed
            if (!string.IsNullOrEmpty(name))
            {
                edition = await UpdateEditionAsync(user, name); // Change the name.
            }
            else
            {
                var editions = await _repo.ListEditionsAsync(user.userId, user.editionId); //get wanted scroll by Id
                var unformattedEdition = editions.First(x => x.EditionId == user.editionId);
                //I think we do not get this far if no records were found, `First` will, I think throw an error.
                //Maybe we should more often make use of try/catch.
                if (unformattedEdition == null)
                {
                    throw new NotFoundException(user.editionId.Value);
                }
                edition = EditionModelToDTO(unformattedEdition);
            }
            return edition; //need to return the updated scroll
        }
    }
}
