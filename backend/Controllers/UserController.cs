﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SQE.Backend.DataAccess.Models;
using SQE.Backend.Server.Services;
using SQE.Backend.Server.DTOs;


namespace SQE.Backend.Server.Controllers
{

    [Produces("application/json")]
    [Authorize]
    [Route("v1/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private IUserService _userService;

        public UserController(IUserService userServiceAuthenticate)
        {
            _userService = userServiceAuthenticate;
        }

        [AllowAnonymous]
        [HttpPost("login")] // api/v1/user/login
        public async Task<ActionResult<UserWithToken>> AuthenticateAsync([FromBody]LoginRequest userParam)
        {
            var user = await _userService.AuthenticateAsync(userParam.userName, userParam.password);
            if (user == null)
                return Unauthorized(new { message = "Username or password is incorrect", code = 23456}); // TODO: Add Error Code

            return user;
        }

        [HttpGet] // api/v1/user
        public ActionResult<UserWithToken> GetCurrentUser()
        {
            var user = _userService.GetCurrentUser();

            if (user == null)
                return Unauthorized(new { message = "No current user", code =12345 }); // TODO: Add Error Code

            return user;
        }
    }
}
