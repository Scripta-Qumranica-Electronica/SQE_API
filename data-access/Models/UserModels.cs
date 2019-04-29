using System;
using System.Collections.Generic;
using System.Text;
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
        private string _requestPath { get; set; }
        private readonly IUserRepository _userRepo;
        private uint? _editionId;
        private uint? _editionEditorId;
        private bool? _mayWrite;
        private bool? _mayLock;
        private bool? _isAdmin;

        public UserInfo(string requestPath, IUserRepository userRepository)
        {
            userId = null;
            this._requestPath = requestPath;
            _userRepo = userRepository;
            _mayWrite = null;
            _mayLock = null;
            _isAdmin = null;
        }
        
        public uint? EditionId()
        {
            if (!_editionId.HasValue)
            {
                var scrollEditionTest = new Regex(@"^.*/edition/(\d{1,32}).*$");
                var scrollEditionMatch = scrollEditionTest.Match(_requestPath);
                if (scrollEditionMatch.Groups.Count == 2)
                    _editionId = uint.TryParse(scrollEditionMatch.Groups[1].Value, out var i) ? i : 0;
                else _editionId = 0;
            }
            return _editionId.HasValue && _editionId.Value == 0 ? null : _editionId;
        }

        public void SetEditionId(uint editionId)
        {
            _editionId = editionId;
        }

        public async Task<uint?> EditionEditorId()
        {
            if (!_editionEditorId.HasValue && EditionId().HasValue)
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
            if (EditionId().HasValue && userId.HasValue)
            {
                var permissions = await _userRepo.GetUserEditionPermissions(userId.Value, EditionId().Value);
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
