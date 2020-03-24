using System;
using System.Threading.Tasks;

namespace SQE.DatabaseAccess.Models
{
    public class User
    {
        public string Email { get; set; }
        public uint UserId { get; set; }
    }

    public class UserToken : User
    {
        public Guid Token { get; set; }
    }

    public class DetailedUser : User
    {
        public string Forename { get; set; }
        public string Surname { get; set; }
        public string Organization { get; set; }
    }

    public class DetailedUserWithToken : DetailedUser
    {
        public bool Activated { get; set; }
        public Guid Token { get; set; }
        public DateTime Date { get; set; }
    }

    public class UserEditionPermissions
    {
        public uint EditionEditionEditorId { get; set; }
        public bool MayWrite { get; set; }
        public bool MayLock { get; set; }
        public bool MayRead { get; set; }
        public bool IsAdmin { get; set; }
        public bool Locked { get; set; }
    }

    public class EditionUserInfo
    {
        private readonly IUserRepository _userRepo;
        public readonly uint? userId;

        public EditionUserInfo(uint? userId, uint editionId, IUserRepository userRepository)
        {
            EditionId = editionId;
            this.userId = userId;
            _userRepo = userRepository;
        }

        public uint EditionId { get; private set; }

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
    }

    public class EditorInfo
    {
        public uint UserId { get; set; }
        public string Forename { get; set; }
        public string Surname { get; set; }
        public string Organization { get; set; }
    }
}