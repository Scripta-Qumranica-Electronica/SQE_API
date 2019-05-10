using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQE.SqeHttpApi.DataAccess.Models
{
    public class User
    {
        public string UserName { get; set; }
        public uint UserId { get; set; }
        public string Token { get; set; }
    }
    
    public class UserEditionPermissions
    {
        public uint edition_editor_id { get; set; }
        public bool may_write { get; set; }
        public bool may_lock { get; set; }
        public bool may_read { get; set; }
        public bool is_admin { get; set; }
    }
    
    public class UserInfo
    {
        public uint? userId { get; set; }
        private readonly IUserRepository _userRepo;
        public uint? editionId;
        private uint? _editionEditorId;
        private bool? _mayWrite;
        private bool? _mayLock;
        private bool? _isAdmin;

        public UserInfo(uint? userId, uint? editionId, IUserRepository userRepository)
        {
            this.editionId = editionId;
            this.userId = userId;
            _userRepo = userRepository;
            _mayWrite = null;
            _mayLock = null;
            _isAdmin = null;
        }

        public void SetEditionId(uint editionId)
        {
            this.editionId = editionId;
        }

        public async Task<uint?> EditionEditorId()
        {
            if (!_editionEditorId.HasValue && editionId.HasValue)
            {
                await SetPermissions();
            }
            return _editionEditorId.HasValue && _editionEditorId.Value == 0 ? null : _editionEditorId;
        }

        public async Task<bool> MayWrite()
        {
            if (!_mayWrite.HasValue)
            {
                await SetPermissions();
            }
            return _mayWrite ?? false;
        }
        
        public async Task<bool> MayLock()
        {
            if (!_mayLock.HasValue)
            {
                await SetPermissions();
            }
            return _mayLock ?? false;
        }
        
        public async Task<bool> IsAdmin()
        {
            if (!_isAdmin.HasValue)
            {
                await SetPermissions();
            }
            return _isAdmin ?? false;
        }

        private async Task<bool> SetPermissions()
        {
            var completed = false;
            if (editionId.HasValue && userId.HasValue)
            {
                var permissions = await _userRepo.GetUserEditionPermissionsAsync(userId.Value, editionId.Value);
                _mayWrite = permissions.may_write;
                _mayLock = permissions.may_lock;
                _isAdmin = permissions.is_admin;
                _editionEditorId = permissions.edition_editor_id;
                completed = true;
            }
            return completed;
        }
    }
}
