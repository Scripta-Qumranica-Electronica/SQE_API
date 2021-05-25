using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SQE.DatabaseAccess.Models
{
	public class User
	{
		public string Email  { get; set; }
		public uint   UserId { get; set; }
	}

	public class DetailedUser : User
	{
		public string Forename     { get; set; }
		public string Surname      { get; set; }
		public string Organization { get; set; }
	}

	public class DetailedUserWithToken : DetailedUser
	{
		public bool     Activated { get; set; }
		public Guid     Token     { get; set; }
		public DateTime Date      { get; set; }
	}

	public class UserEditionPermissions
	{
		public uint EditionEditionEditorId { get; set; }
		public bool MayWrite               { get; set; }
		public bool MayLock                { get; set; }
		public bool MayRead                { get; set; }
		public bool IsAdmin                { get; set; }
		public bool Locked                 { get; set; }
	}

	// See the table system_roles in the database for these values
	public enum UserSystemRoles
	{
		/// <summary>
		///  This role is for all general users of the system. It should permit all activities related to the edition system.
		///  Such users may perform CREATE on all *_owner table, they may UPDATE/DELETE only those rows with an edition_id for
		///  which they have write permissions. They may CREATE in any data table with a corresponding *_owned table.
		/// </summary>
		REGISTERED_USER = 1

		,

		/// <summary>
		///  This role is for users who may edit cataloguing data. This refers mainly to CREATE/UPDATE/DELETE operations on
		///  tables that contain references to cataloguing information, such as textual references and museum numbers. Any
		///  operations performed by such users should maintain a record of who made the changes—see, e.g., the *_author tables.
		/// </summary>
		CATALOGUE_CURATOR = 2

		,

		/// <summary>
		///  This role is for users who can add images to the system and alter information related to the images. Mainly this
		///  constitutes CREATE/UPDATE/DELETE access to the SQE_image and image_urls table.
		/// </summary>
		IMAGE_DATA_CURATOR = 3

		,

		/// <summary>
		///  This role is for administrators of the user access system. It permits CREATE/UPDATE/DELETE access to the user,
		///  system_roles, and users_system_roles tables.
		/// </summary>
		USER_ADMIN = 4

		,
	}

	public class UserInfo
	{
		private readonly IUserRepository _userRepo;
		public readonly  uint?           userId;

		public UserInfo(uint? userId, uint? editionId, IUserRepository userRepository)
		{
			EditionId = editionId;
			this.userId = userId;
			_userRepo = userRepository;
		}

		public List<UserSystemRoles> SystemRoles { get; private set; }

		public uint? EditionId { get; private set; }

		public uint? EditionEditorId { get; private set; }

		public bool EditionLocked { get; private set; }

		public bool MayRead { get; private set; }

		public bool MayWrite { get; private set; }

		public bool MayLock { get; private set; }

		public bool IsAdmin { get; private set; }

		public async Task SetEditionId(uint newEditionId)
		{
			EditionId = newEditionId;
			await ReadPermissions();
		}

		public async Task ReadPermissions()
		{
			var permissions = await _userRepo.GetUserEditionPermissionsAsync(this);

			MayRead = permissions.MayRead;
			MayWrite = permissions.MayWrite && !permissions.Locked;
			EditionLocked = permissions.Locked;
			MayLock = permissions.MayLock;
			IsAdmin = permissions.IsAdmin;
			EditionEditorId = permissions.EditionEditionEditorId;
		}

		public async Task ReadRoles()
		{
			SystemRoles = await _userRepo.GetUserSystemRolesAsync(this);
		}
	}

	public class EditorInfo
	{
		public uint   UserId       { get; set; }
		public uint   EditorId     { get; set; }
		public string Forename     { get; set; }
		public string Surname      { get; set; }
		public string Organization { get; set; }
	}

	public class EditorWithPermissions : EditorInfo
	{
		public string EditorEmail { get; set; }
		public bool   MayRead     { get; set; }

		public bool MayWrite { get; set; }

		public bool MayLock { get; set; }

		public bool IsAdmin { get; set; }
	}

	public class DatabaseVersion
	{
		public string   Version { get; set; }
		public DateTime Date    { get; set; }
	}
}
