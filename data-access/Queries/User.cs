using System;
using System.Collections.Generic;
using System.Text;
using SQE.SqeHttpApi.DataAccess.Models;

namespace SQE.SqeHttpApi.DataAccess.Queries
{
    internal class UserQueryResponse : IQueryResponse<User>
    {
        public string user_name { get; set; }
        public uint user_id { get; set; }

        public User CreateModel()
        {
            return new User
            {
                UserName = user_name,
                UserId = user_id
            };
        }
    }
    
    internal static class UserPermissionQuery
    {
        public const string GetQuery = @"
SELECT edition_editor_id, may_write, may_lock, may_read, is_admin
FROM edition_editor
WHERE edition_id = @EditionId AND user_id = @UserId";

        public class Return
        {
            private uint edition_editor_id { get; set; }
            private bool may_write { get; set; }
            private bool may_lock { get; set; }
            private bool may_read { get; set; }
            private bool is_admin { get; set; }
        }
    }
}
