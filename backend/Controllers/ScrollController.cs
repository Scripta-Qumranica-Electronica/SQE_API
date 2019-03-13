using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SQE.Backend.DataAccess;
using SQE.Backend.Server.DTOs;
using SQE.Backend.Server.Services;

namespace SQE.Backend.Server.Controllers
{
    [Authorize]
    [Route("api/v1/scroll-version")]
    [ApiController]
    public class ScrollController : ControllerBase
    {
        private IScrollService _scrollService;
        private IUserService _userService;

        public ScrollController(IScrollService scrollService, IUserService userService)
        {
            this._scrollService = scrollService;
            this._userService = userService;
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<ScrollVersion>> GetScrollVersion(int id)
        {
            var version = await _scrollService.GetScrollVersionAsync(id, _userService.GetCurrentUserId(), false, false);

            if(version==null)
            {
                return NotFound();
            }

            return Ok(version);
        }
    }
}
