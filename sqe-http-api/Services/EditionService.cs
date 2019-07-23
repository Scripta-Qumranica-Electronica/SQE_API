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
        Task<EditionGroupDTO> GetEditionAsync(UserInfo user, bool artefacts = false, bool fragments = false);
        Task<EditionListDTO> ListEditionsAsync(uint? userId);
        Task<EditionDTO> UpdateEditionAsync(UserInfo user, string name, string copyrightHolder = null,
            string collaborators = null);
        Task<EditionDTO> CopyEditionAsync(UserInfo user, EditionCopyDTO editionInfo);
        Task DeleteEditionAsync(UserInfo user);
        Task<EditorRightsDTO> AddEditionEditor(UserInfo user, EditorRightsDTO newEditor);
        Task<EditorRightsDTO> ChangeEditionEditorRights(UserInfo user, EditorRightsDTO newEditor);
    }

    public class EditionService : IEditionService
    {
        private readonly IEditionRepository _editionRepo;

        public EditionService(IEditionRepository editionRepo)
        {
            _editionRepo = editionRepo;
        }

        public async Task<EditionGroupDTO> GetEditionAsync(UserInfo user, bool artefacts = false,
            bool fragments = false)
        {
            var scrollModels = await _editionRepo.ListEditionsAsync(user.userId, user.editionId);

            var primaryModel = scrollModels.FirstOrDefault(sv => sv.EditionId == user.editionId);
            if (primaryModel == null) // User is not allowed to see this scroll version
                return null;
            var otherModels = scrollModels.Where(sv => sv.EditionId != user.editionId).OrderBy(sv => sv.EditionId);

            var svg = new EditionGroupDTO
            {
                primary = EditionModelToDTO(primaryModel),
                others = otherModels.Select(EditionModelToDTO),
            };

            return svg;
        }

        public async Task<EditionListDTO> ListEditionsAsync(uint? userId)
        {
            return new EditionListDTO()
            {
                editions = (await _editionRepo.ListEditionsAsync(userId, null))
                    .GroupBy(x => x.ScrollId) // Group the edition listings by scroll_id
                    .Select(x => x.Select(EditionModelToDTO)) // Format each entry as an EditionDTO
                    .Select(x => x.ToList()) // Convert the groups from IEnumerable to List
                    .ToList() // Convert the list of groups from IEnumerable to List so we now have List<List<EditionDTO>>
            };
        }

        private static EditionDTO EditionModelToDTO(DataAccess.Models.Edition model)
        {
            return new EditionDTO
            {
                id = model.EditionId,
                name = model.Name,
                permission = PermissionModelToDTO(model.Permission),
                owner = UserService.UserModelToDto(model.Owner),
                thumbnailUrl = model.Thumbnail,
                locked = model.Locked,
                isPublic = model.IsPublic,
                lastEdit = model.LastEdit,
                copyright = model.Copyright
            };
        }

        private static PermissionDTO PermissionModelToDTO(DataAccess.Models.Permission model)
        {
            return new PermissionDTO
            {
                isAdmin = model.IsAdmin,
                mayWrite = model.MayWrite,
            };
        }

        internal static DataAccess.Models.UserToken OwnerToModel(UserDTO user)
        {
            return new DataAccess.Models.UserToken
            {
                UserId = user.userId,
                Email = user.email
            };
        }

        internal static DataAccess.Models.Permission PermissionDtoTOModel(PermissionDTO permission)
        {
            return new DataAccess.Models.Permission
            {
                IsAdmin = permission.isAdmin,
                MayWrite = permission.mayWrite
            };
        }

        public async Task<EditionDTO> UpdateEditionAsync(UserInfo user, string name, string copyrightHolder = null,
            string collaborators = null)
        {
            var editionBeforeChanges = (await _editionRepo.ListEditionsAsync(user.userId, user.editionId)).First();
            
            if (copyrightHolder != null || editionBeforeChanges.Collaborators != collaborators)
            {
                await _editionRepo.ChangeEditionCopyrightAsync(user, copyrightHolder, collaborators);
            }

            if (!string.IsNullOrEmpty(name))
            {
                await _editionRepo.ChangeEditionNameAsync(user, name);
            }
            
            var editions = await _editionRepo.ListEditionsAsync(user.userId, user.editionId); //get wanted edition by edition Id
            
            return EditionModelToDTO(editions.First(x => x.EditionId == user.editionId));
        }

        public async Task<EditionDTO> CopyEditionAsync(UserInfo user, EditionCopyDTO editionInfo)
        {
            EditionDTO edition;
            // Clone edition
            var copyToEditionId = await _editionRepo.CopyEditionAsync(user, editionInfo.copyrightHolder, editionInfo.collaborators);
            if (user.editionId == copyToEditionId)
            {
                // Check if is success is true, else throw error.
                throw new System.Exception($"Failed to clone {user.editionId}.");
            }
            user.SetEditionId(copyToEditionId); // Update user object for the new editionId
            
            //Change the Name, if a Name has been passed
            if (!string.IsNullOrEmpty(editionInfo.name))
            {
                edition = await UpdateEditionAsync(user, editionInfo.name); // Change the Name.
            }
            else
            {
                var editions = await _editionRepo.ListEditionsAsync(user.userId, user.editionId); //get wanted scroll by Id
                var unformattedEdition = editions.First(x => x.EditionId == user.editionId);
                //I think we do not get this far if no records were found, `First` will, I think throw an error.
                //Maybe we should more often make use of try/catch.
                if (unformattedEdition == null)
                {
                    throw new StandardErrors.DataNotFound("edition", user.editionId ?? 0);
                }
                edition = EditionModelToDTO(unformattedEdition);
            }
            return edition; //need to return the updated scroll
        }

        /// <summary>
        /// Delete all data from the edition that the user is currently subscribed to.
        /// </summary>
        /// <param name="user">User object requesting the delete</param>
        /// <returns></returns>
        public async Task DeleteEditionAsync(UserInfo user)
        {
            await _editionRepo.DeleteAllEditionDataAsync(user);
        }

        /// <summary>
        /// Adds a new editor to an edition with the requested access rights
        /// </summary>
        /// <param name="user">User object making the request</param>
        /// <param name="newEditor">Details of the new editor to be added</param>
        /// <returns></returns>
        public async Task<EditorRightsDTO> AddEditionEditor(UserInfo user, EditorRightsDTO newEditor)
        {
            var newUserPermissions = await _editionRepo.AddEditionEditor(user, newEditor.email, newEditor.mayRead,
                newEditor.mayWrite, newEditor.mayLock, newEditor.isAdmin);
            return _permissionsToEditorRightsDTO(newEditor.email, newUserPermissions);
        }
        
        /// <summary>
        /// Changes the access rights of an editor
        /// </summary>
        /// <param name="user">User object making the request</param>
        /// <param name="newEditor">Details of the editor and the desired access rights</param>
        /// <returns></returns>
        public async Task<EditorRightsDTO> ChangeEditionEditorRights(UserInfo user, EditorRightsDTO newEditor)
        {
            var updatedUserPermissions = await _editionRepo.AddEditionEditor(user, newEditor.email, newEditor.mayRead,
                newEditor.mayWrite, newEditor.mayLock, newEditor.isAdmin);
            return _permissionsToEditorRightsDTO(newEditor.email, updatedUserPermissions);
        }

        private static EditorRightsDTO _permissionsToEditorRightsDTO(string editorEmail, Permission permissions)
        {
            return new EditorRightsDTO()
            {
                email = editorEmail,
                mayRead = permissions.MayRead,
                mayWrite = permissions.MayWrite,
                mayLock = permissions.MayLock,
                isAdmin = permissions.IsAdmin
            };
        }
    }
}
