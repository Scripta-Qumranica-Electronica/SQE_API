using System;
using System.Collections.Generic;
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
		Task<EditionGroupDTO> GetEditionAsync(EditionUserInfo editionUser,
			bool artefacts = false,
			bool fragments = false);

		Task<EditionListDTO> ListEditionsAsync(uint? userId);

		Task<EditionDTO> UpdateEditionAsync(EditionUserInfo editionUser,
			string name,
			string copyrightHolder = null,
			string collaborators = null);

		Task<EditionDTO> CopyEditionAsync(EditionUserInfo editionUser, EditionCopyDTO editionInfo);
		Task<DeleteTokenDTO> DeleteEditionAsync(EditionUserInfo editionUser, string token, List<string> optional);
		Task<EditorRightsDTO> AddEditionEditor(EditionUserInfo editionUser, EditorRightsDTO newEditor);
		Task<EditorRightsDTO> ChangeEditionEditorRights(EditionUserInfo editionUser, EditorRightsDTO updatedEditor);
	}

	public class EditionService : IEditionService
	{
		private readonly IEditionRepository _editionRepo;
		private readonly IUserRepository _userRepo;

		public EditionService(IEditionRepository editionRepo, IUserRepository userRepo)
		{
			_editionRepo = editionRepo;
			_userRepo = userRepo;
		}

		public async Task<EditionGroupDTO> GetEditionAsync(EditionUserInfo editionUser,
			bool artefacts = false,
			bool fragments = false)
		{
			var scrollModels = await _editionRepo.ListEditionsAsync(editionUser.userId, editionUser.EditionId);

			var primaryModel = scrollModels.FirstOrDefault(sv => sv.EditionId == editionUser.EditionId);
			if (primaryModel == null) // User is not allowed to see this scroll version
				return null;
			var otherModels = scrollModels.Where(sv => sv.EditionId != editionUser.EditionId)
				.OrderBy(sv => sv.EditionId);

			var editionGroup = new EditionGroupDTO
			{
				primary = EditionModelToDTO(primaryModel),
				others = otherModels.Select(EditionModelToDTO)
			};

			return editionGroup;
		}

		public async Task<EditionListDTO> ListEditionsAsync(uint? userId)
		{
			return new EditionListDTO
			{
				editions = (await _editionRepo.ListEditionsAsync(userId, null))
					.GroupBy(x => x.ScrollId) // Group the edition listings by scroll_id
					.Select(x => x.Select(EditionModelToDTO)) // Format each entry as an EditionDTO
					.Select(x => x.ToList()) // Convert the groups from IEnumerable to List
					.ToList() // Convert the list of groups from IEnumerable to List so we now have List<List<EditionDTO>>
			};
		}

		public async Task<EditionDTO> UpdateEditionAsync(EditionUserInfo editionUser,
			string name,
			string copyrightHolder = null,
			string collaborators = null)
		{
			var editionBeforeChanges =
				(await _editionRepo.ListEditionsAsync(editionUser.userId, editionUser.EditionId)).First();

			if (copyrightHolder != null
				|| editionBeforeChanges.Collaborators != collaborators)
				await _editionRepo.ChangeEditionCopyrightAsync(editionUser, copyrightHolder, collaborators);

			if (!string.IsNullOrEmpty(name)) await _editionRepo.ChangeEditionNameAsync(editionUser, name);

			var editions = await _editionRepo.ListEditionsAsync(
				editionUser.userId,
				editionUser.EditionId
			); //get wanted edition by edition Id

			return EditionModelToDTO(editions.First(x => x.EditionId == editionUser.EditionId));
		}

		public async Task<EditionDTO> CopyEditionAsync(EditionUserInfo editionUser, EditionCopyDTO editionInfo)
		{
			EditionDTO edition;
			// Clone edition
			var copyToEditionId = await _editionRepo.CopyEditionAsync(
				editionUser,
				editionInfo.copyrightHolder,
				editionInfo.collaborators
			);
			if (editionUser.EditionId == copyToEditionId)
				// Check if is success is true, else throw error.
				throw new Exception($"Failed to clone {editionUser.EditionId}.");
			editionUser.SetEditionId(copyToEditionId); // Update user object for the new editionId

			//Change the Name, if a Name has been passed
			if (!string.IsNullOrEmpty(editionInfo.name))
			{
				edition = await UpdateEditionAsync(editionUser, editionInfo.name); // Change the Name.
			}
			else
			{
				var editions = await _editionRepo.ListEditionsAsync(
					editionUser.userId,
					editionUser.EditionId
				); //get wanted scroll by Id
				var unformattedEdition = editions.First(x => x.EditionId == editionUser.EditionId);
				//I think we do not get this far if no records were found, `First` will, I think throw an error.
				//Maybe we should more often make use of try/catch.
				if (unformattedEdition == null) throw new StandardErrors.DataNotFound("edition", editionUser.EditionId);
				edition = EditionModelToDTO(unformattedEdition);
			}

			return edition; //need to return the updated scroll
		}

		/// <summary>
		///     Delete all data from the edition that the user is currently subscribed to. The user must be admin and
		///     provide a valid delete token.
		/// </summary>
		/// <param name="editionUser">User object requesting the delete</param>
		/// <param name="optional">optional parameters: "deleteForAllEditors"</param>
		/// <param name="token">token required for optional "deleteForAllEditors"</param>
		/// <returns></returns>
		public async Task<DeleteTokenDTO> DeleteEditionAsync(EditionUserInfo editionUser,
			string token,
			List<string> optional)
		{
			_parseOptional(optional, out var deleteForAllEditors);

			// Check if the edition should be deleted for all users
			if (deleteForAllEditors)
			{
				// Try to delete the edition fully for all editors
				var newToken = await _editionRepo.DeleteAllEditionDataAsync(editionUser, token);

				// End the request with null for successful delete or a proper token for requests without a confirmation token
				return string.IsNullOrEmpty(newToken)
					? null
					: new DeleteTokenDTO
					{
						editionId = editionUser.EditionId,
						token = newToken
					};
			}

			// The edition should only be made inaccessible for the current user
			var userInfo = await _userRepo.GetDetailedUserByIdAsync(editionUser.userId);

			// Setting all permission to false is how we delete a user's access to an edition.
			await _editionRepo.ChangeEditionEditorRights(
				editionUser,
				userInfo.Email,
				false,
				false,
				false,
				false
			);
			return null;
		}

		/// <summary>
		///     Adds a new editor to an edition with the requested access rights
		/// </summary>
		/// <param name="editionUser">User object making the request</param>
		/// <param name="newEditor">Details of the new editor to be added</param>
		/// <returns></returns>
		public async Task<EditorRightsDTO> AddEditionEditor(EditionUserInfo editionUser, EditorRightsDTO newEditor)
		{
			var newUserPermissions = await _editionRepo.AddEditionEditor(
				editionUser,
				newEditor.email,
				newEditor.mayRead,
				newEditor.mayWrite,
				newEditor.mayLock,
				newEditor.isAdmin
			);
			return _permissionsToEditorRightsDTO(newEditor.email, newUserPermissions);
		}

		/// <summary>
		///     Changes the access rights of an editor
		/// </summary>
		/// <param name="editionUser">User object making the request</param>
		/// <param name="updatedEditor">Details of the editor and the desired access rights</param>
		/// <returns></returns>
		public async Task<EditorRightsDTO> ChangeEditionEditorRights(EditionUserInfo editionUser,
			EditorRightsDTO updatedEditor)
		{
			var updatedUserPermissions = await _editionRepo.ChangeEditionEditorRights(
				editionUser,
				updatedEditor.email,
				updatedEditor.mayRead,
				updatedEditor.mayWrite,
				updatedEditor.mayLock,
				updatedEditor.isAdmin
			);
			return _permissionsToEditorRightsDTO(updatedEditor.email, updatedUserPermissions);
		}

		private static EditionDTO EditionModelToDTO(Edition model)
		{
			return new EditionDTO
			{
				id = model.EditionId,
				name = model.Name,
				editionDataEditorId = model.EditionDataEditorId,
				permission = PermissionModelToDTO(model.Permission),
				owner = UserService.UserModelToDto(model.Owner),
				thumbnailUrl = model.Thumbnail,
				locked = model.Locked,
				isPublic = model.IsPublic,
				lastEdit = model.LastEdit,
				copyright = model.Copyright
			};
		}

		private static PermissionDTO PermissionModelToDTO(Permission model)
		{
			return new PermissionDTO
			{
				isAdmin = model.IsAdmin,
				mayWrite = model.MayWrite
			};
		}

		internal static UserToken OwnerToModel(UserDTO user)
		{
			return new UserToken
			{
				UserId = user.userId,
				Email = user.email
			};
		}

		internal static Permission PermissionDtoTOModel(PermissionDTO permission)
		{
			return new Permission
			{
				IsAdmin = permission.isAdmin,
				MayWrite = permission.mayWrite
			};
		}

		private static EditorRightsDTO _permissionsToEditorRightsDTO(string editorEmail, Permission permissions)
		{
			return new EditorRightsDTO
			{
				email = editorEmail,
				mayRead = permissions.MayRead,
				mayWrite = permissions.MayWrite,
				mayLock = permissions.MayLock,
				isAdmin = permissions.IsAdmin
			};
		}

		private static void _parseOptional(List<string> optional, out bool deleteForAllEditors)
		{
			deleteForAllEditors = optional.Contains("deleteForAllEditors");
		}
	}
}