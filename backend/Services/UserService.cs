using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SQE.Backend.Server.Helpers;
using SQE.Backend.DataAccess;
using SQE.Backend.Server.DTOs;

namespace SQE.Backend.Server.Services
{
    public interface IUserService
    {
        Task<UserWithToken> AuthenticateAsync(string username, string password); // TODO: Return a User object, not a LoginResponse
        UserWithToken GetCurrentUser();
        uint? GetCurrentUserId();
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


        public async Task<UserWithToken> AuthenticateAsync(string username, string password)
        {
            var result = await _repo.GetUserByPassword(username, password);

            if (result == null)
                return null;

            var user = new UserWithToken
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

        public UserWithToken GetCurrentUser()
        {
            // TODO: Check if ...User.Identity.Name exists. Return null if not.
            var currentUserName = _accessor.HttpContext.User.Identity.Name;
            var currentUserId = GetCurrentUserId();

            if (currentUserName != null && currentUserId.HasValue)
            {
                var user = new UserWithToken
                {
                    userName = currentUserName,
                    userId = currentUserId.Value,
                    token = BuildUserToken(currentUserName, currentUserId.Value).ToString(),
                };

                return user;
            }
            return null;
        }

        public uint? GetCurrentUserId()
        {
            var identity = (ClaimsIdentity)_accessor.HttpContext.User.Identity;
            var claims = identity.Claims;
            foreach (var claim in claims)
            {
                var splitted = claim.Type.Split("/");
                if (splitted[splitted.Length - 1] == "nameidentifier")
                {
                    return UInt32.Parse(claim.Value);
                }
            }
            return null;
        }

        public static User UserModelToDTO(DataAccess.Models.User model)
        {
            return new User
            {
                userId = model.UserId,
                userName = model.UserName,
            };
        }
    }
}
