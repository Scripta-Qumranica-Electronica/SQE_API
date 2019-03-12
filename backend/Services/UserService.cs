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
using data_access.Models;
using backend.Helpers;
using data_access;

namespace backend.Services
{
    public interface IUserService
    {
        Task<User> AuthenticateAsync(string username, string password); // TODO: Return a User object, not a LoginResponse
        User GetCurrentUser(); // TODO: Return a User model object
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


        public async Task<User> AuthenticateAsync(string username, string password)
        {
            var result = await _repo.GetUserByPassword(username, password);

            if (result == null)
                return null;

            User user = new User { UserName = result.UserName, UserId = result.UserId };
            user.Token = BuildUserToken(user.UserName, user.UserId).ToString();

            return user;
        }

        private string BuildUserToken(string userName, int userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            string s3 = Convert.ToBase64String(key);

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

        public User GetCurrentUser()
        {
            // TODO: Check if ...User.Identity.Name exists. Return null if not.
            var userName = _accessor.HttpContext.User.Identity.Name;
            if (userName != null)
            {
                User user = new User
                {
                    UserName = _accessor.HttpContext.User.Identity.Name
                };
                var userId = GetCurrentUserId();
                if (userId != null)
                {
                    user.UserId = userId.Value;
                }
                user.Token = BuildUserToken(user.UserName, user.UserId).ToString();

                return user;
            }
            return null;
        }

        private int? GetCurrentUserId()
        {
            var identity = (ClaimsIdentity)_accessor.HttpContext.User.Identity;
            IEnumerable<Claim> claims = identity.Claims;
            foreach (var claim in claims)
            {
                var splitted = claim.Type.Split("/");
                if (splitted[splitted.Length - 1] == "nameidentifier")
                {
                    return Int32.Parse(claim.Value);
                }
            }
            return null;
        }
    }
}
