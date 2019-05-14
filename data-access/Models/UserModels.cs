﻿using System.Text.RegularExpressions;
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
        public uint EditionEditionEditorId { get; set; }
        public bool MayWrite { get; set; }
        public bool MayLock { get; set; }
        public bool MayRead { get; set; }
        public bool IsAdmin { get; set; }
    }
    
    public class UserInfo
    {
        public readonly uint? userId;
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

        /// <summary>
        /// Set the editionId of the user to a new editionID (the permissions are also
        /// retrieved for the new editionId).
        /// </summary>
        /// <param Name="editionId"></param>
        public async void SetEditionId(uint editionId)
        {
            if (!this.editionId.HasValue || this.editionId.Value != editionId)
            {
                this.editionId = editionId;
                await SetPermissions();
            }
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
                _mayWrite = permissions.MayWrite;
                _mayLock = permissions.MayLock;
                _isAdmin = permissions.IsAdmin;
                _editionEditorId = permissions.EditionEditionEditorId;
                completed = true;
            }
            return completed;
        }
    }
}