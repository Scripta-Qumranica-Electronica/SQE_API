using System;
using System.Collections.Generic;
using System.Text;
using data_access.Models;

namespace data_access.RawModels
{
    internal interface IQueryResponse<T>
    {
        T CreateModel();
    }

    internal class UserQueryResponse : IQueryResponse<User>
    {
        public string user_name { get; set; }
        public int user_id { get; set; }

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
