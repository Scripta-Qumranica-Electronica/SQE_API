using System;
using System.Collections.Generic;
using System.Text;
using SQE.Backend.DataAccess.Models;

namespace SQE.Backend.DataAccess.Queries
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
}
