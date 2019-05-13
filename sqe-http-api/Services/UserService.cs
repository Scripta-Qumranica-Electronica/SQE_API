using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Helpers;

namespace SQE.SqeHttpApi.Server.Helpers
{
    public interface IUserService
    {
        Task<LoginResponseDTO> AuthenticateAsync(string username, string password); // TODO: Return a User object, not a LoginResponse
        UserDTO GetCurrentUser();
        uint? GetCurrentUserId();
        UserInfo GetCurrentUserObject(uint? editionId = null);
    }

    public class UserService : IUserService
    {
        private readonly AppSettings _appSettings;
        private IUserRepository _repo;
        private IHttpContextAccessor _accessor;


        public UserService(IOptions<AppSettings> appSettings, IUserRepository userRepository, IHttpContextAccessor accessor)

        // http://jasonwatmore.com/post/2018/08/14/aspnet-core-21-jwt-authentication-tutorial-with-example-api
        {
            _repo = userRepository;
            _appSettings = appSettings.Value; // For the secret key
            _accessor = accessor;
        }


        public async Task<LoginResponseDTO> AuthenticateAsync(string username, string password)
        {
            var result = await _repo.GetUserByPasswordAsync(username, password);

            if (result == null)
                return null;

            var user = new LoginResponseDTO
            {
                userName = result.UserName,
                userId = result.UserId,
                token = BuildUserToken(result.UserName, result.UserId).ToString(),
            };   
            return user;
        }

        private string BuildUserToken(string userName, uint userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var s3 = Convert.ToBase64String(key);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public UserDTO GetCurrentUser()
        {
            // TODO: Check if ...User.Identity.Name exists. Return null if not.
            var currentUserName = _accessor.HttpContext.User.Identity.Name;
            var currentUserId = GetCurrentUserId();

           return (currentUserName == null || !currentUserId.HasValue) ?
                null :
                new UserDTO
                {
                    userName = currentUserName,
                    userId = currentUserId.Value,
                };
        }

        public uint? GetCurrentUserId()
        {
            var identity = (ClaimsIdentity)_accessor.HttpContext.User.Identity;
            var claims = identity.Claims;
            foreach (var claim in claims)
            {
                var split = claim.Type.Split("/");
                if (split[split.Length - 1] == "nameidentifier")
                {
                    return uint.Parse(claim.Value);
                }
            }
            return null;
        }

        /// <summary>
        /// This returns a UserInfo object that will persist only for the life of the current
        /// HTTP request.  The UserInfo object can fetch the permissions if requested, and once
        /// the permissions have been requested, they are "cached" for the life of the object.
        /// </summary>
        /// <param Name="editionId"></param>
        /// <returns></returns>
        public UserInfo GetCurrentUserObject(uint? editionId = null)
        {
            return new UserInfo(GetCurrentUserId(), editionId, _repo);
        }


        public static UserDTO UserModelToDTO(DataAccess.Models.User model)
        {
            return new UserDTO
            {
                userId = model.UserId,
                userName = model.UserName,
            };
        }
    }
}
