using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;


namespace SQE.SqeHttpApi.Server.Services
{
    public interface IEditionService
    {
        Task<EditionGroupDTO> GetEditionAsync(uint editionId, UserInfo user, bool artefacts = false, bool fragments = false);
        Task<EditionListDTO> ListEditionsAsync(uint? userId);
        Task<EditionDTO> UpdateEditionAsync(uint editionId, string name, UserInfo user);
        Task<EditionDTO> CopyEditionAsync(uint editionId, string name, UserInfo user);
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
//            var groups = await _repo.GetEditionsAsync(EditionId, user.userId);
//            if (groups.Count == 0)
//                return null;
//            //Debug.Assert(groups.Count == 1, "How come we got more than 1 group?!");
//
//            var scrollVersionGroupIds = groups[groups.Keys.First()];

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

        public async Task<EditionDTO> UpdateEditionAsync(uint editionId, string name, UserInfo user)
        {
            if (!string.IsNullOrEmpty(name)) 
            {
                try
                {
                    await _repo.ChangeEditionNameAsync(editionId, name, user);
                } 
                catch(NoPermissionException)
                {
                    throw new NotFoundException(editionId);
                }
            }
            else
            {
                throw new ImproperRequestException("change scroll Name", "scroll Name cannot be empty");
            }
            
            var editions = await _repo.ListEditionsAsync(user.userId, user.EditionId()); //get wanted edition by edition Id
            
            return EditionModelToDTO(editions.First(x => x.EditionId == editionId));
        }

        public async Task<EditionDTO> CopyEditionAsync(uint editionId, string name, UserInfo user)
        {
            EditionDTO edition;
            // Clone edition
            var copyToEditionId = await _repo.CopyEditionAsync(editionId, user);
            if (editionId == copyToEditionId)
            {
                // Check if is success is true, else throw error.
                throw new System.Exception($"Failed to clone {editionId}.");
            }
                
            editionId = copyToEditionId;
            user.SetEditionId(editionId);
            
            //Change the Name, if a Name has been passed
            if (!string.IsNullOrEmpty(name))
            {
                edition = await UpdateEditionAsync(editionId, name, user);
            }
            else
            {
                var editions = await _repo.ListEditionsAsync(user.userId, editionId); //get wanted scroll by Id
                var unformattedEdition = editions.First();
                //I think we do not get this far if no records were found, `First` will, I think throw an error.
                //Maybe we should more often make use of try/catch.
                if (unformattedEdition == null)
                {
                    throw new NotFoundException(editionId);
                }
                edition = EditionModelToDTO(unformattedEdition);
            }
            return edition; //need to return the updated scroll
        }
    }
}
