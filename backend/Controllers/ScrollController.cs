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

    [Produces("application/json")]
    [Authorize]
    [Route("v1/scroll-version")]
    [ApiController]
    public class ScrollController : ControllerBase
    {
        private IScrollService _scrollService;
        private IUserService _userService;
        private IImagedFragmentsService _imagedFragmentService;


        public ScrollController(IScrollService scrollService, IUserService userService, IImagedFragmentsService imagedFragmentService)
        {
            this._scrollService = scrollService;
            this._userService = userService;
            _imagedFragmentService = imagedFragmentService;
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<ScrollVersionGroup>> GetScrollVersion(int id)
        {
            var vg = await _scrollService.GetScrollVersionAsync(id, _userService.GetCurrentUserId(), false, false);

            if(vg==null)
            {
                return NotFound();
            }

            return Ok(vg);
        }

        [AllowAnonymous]
        [HttpGet("list")]
        public async Task<ActionResult<ScrollVersionList>> ListScrollVersions()
        {
            var groups = await _scrollService.ListScrollVersionsAsync(_userService.GetCurrentUserId());
            return Ok(groups);
        }

        [HttpPost("update/{id}")] //not working well..
        public async Task<ActionResult<ScrollVersion>> UpdateScrollVersion([FromBody] ScrollUpdateRequest request, int id)
        {
            try
            {
                var scroll = await _scrollService.UpdateScroll(id, request.name, _userService.GetCurrentUserId());
                return scroll;
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch(ForbiddenException)
            {
                return Forbid();
            }
        }

        [HttpPost("copy/{id}")] //not working well..
        public async Task<ActionResult<ScrollVersion>> CopyScrollVersion([FromBody] ScrollUpdateRequest request, int id)
        {
            try
            {
                var scroll = await _scrollService.CopyScroll(id, request.name, _userService.GetCurrentUserId());
                return scroll;
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (ForbiddenException)
            {
                return Forbid();
            }
        }

        [AllowAnonymous]
        [HttpGet("{scrollVersionId}/imaged-fragments")]
        public async Task<ActionResult<ImagedFragmentList>> GetImagedFragments(int scrollVersionId)
        {
            try
            {
                var imagedFragment = await _imagedFragmentService.GetImagedFragments(_userService.GetCurrentUserId(), scrollVersionId);
                return imagedFragment;
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
    }
}
