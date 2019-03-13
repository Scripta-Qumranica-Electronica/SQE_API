using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using SQE.Backend.DataAccess.Models;
using Dapper;
using SQE.Backend.DataAccess.RawModels;
using Microsoft.Extensions.Configuration;

namespace SQE.Backend.DataAccess
{
    public interface IUserRepository
    {
        Task<User> GetUserByPassword(string userName, string password);
    }
    public class UserRepository: DBConnectionBase , IUserRepository
    {
        public UserRepository(IConfiguration config): base(config) { }

        public async Task<User> GetUserByPassword(string userName, string password)
        {
            var sql = @"SELECT user_name, user_id FROM user WHERE user_name =@UserName AND pw = SHA2(@Password, 224)";
            using (var connection = OpenConnection())
            {
                var results = await connection.QueryAsync<UserQueryResponse>(sql, new
                {
                    UserName = userName,
                    Password = password
                });

                var firstUser = results.FirstOrDefault();
                if (firstUser == null)
                    return null;

                return firstUser.CreateModel();
            }
        }
    }
}
