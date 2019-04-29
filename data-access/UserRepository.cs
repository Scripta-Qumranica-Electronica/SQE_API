using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.DataAccess.Queries;

namespace SQE.SqeHttpApi.DataAccess
{
    public interface IUserRepository
    {
        Task<User> GetUserByPassword(string userName, string password);
        Task<UserEditionPermissions> GetUserEditionPermissions(uint userId, uint editionId);
    }
    public class UserRepository: DbConnectionBase , IUserRepository
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
                return firstUser?.CreateModel();
            }
        }
        
        public async Task<UserEditionPermissions> GetUserEditionPermissions(uint userId, uint editionId)
        {
            using (var connection = OpenConnection())
            {
                var results = await connection.QuerySingleAsync<UserEditionPermissions>(UserPermissionQuery.GetQuery, new
                {
                    EditionId = editionId,
                    UserId = userId,
                });
                return results;
            }
        }
    }
}
