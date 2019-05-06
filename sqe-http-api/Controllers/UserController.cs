using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Services;


namespace SQE.SqeHttpApi.Server.Controllers
{

    [Produces("application/json")]
    [Authorize]
    [Route("v1/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private IUserService _userService;

        public UserController(IUserService userServiceAuthenticate)
        {
            _userService = userServiceAuthenticate;
        }

        /// <summary>
        /// Provides a JWT bearer token for valid username and password
        /// </summary>
        /// <param Name="userParam">JSON object with a username and password parameter</param>
        [AllowAnonymous]
        [HttpPost("login")] // api/v1/user/login
        public async Task<ActionResult<LoginResponseDTO>> AuthenticateAsync([FromBody]LoginRequestDTO userParam)
        {
            var user = await _userService.AuthenticateAsync(userParam.userName, userParam.password);
            if (user == null)
                return Unauthorized(new { message = "Username or password is incorrect", code = 600}); // TODO: Add Error Code

            return user;
        }

        /// <summary>
        /// Provides the user details for a user with valid JWT in the Authorize header
        /// </summary>
        [HttpGet] // api/v1/user
        public ActionResult<UserDTO> GetCurrentUser()
        {
            var user = _userService.GetCurrentUser();

            if (user == null)
                return Unauthorized(new { message = "No current user", code =601 }); // TODO: Add Error Code

            return user;
        }
    }
}
