using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SQE.Backend.DataAccess;
using SQE.Backend.Server.DTOs;
using SQE.Backend.Server.Services;

namespace SQE.Backend.Server.Controllers
{

    [Authorize]
    [Route("v1/[controller]")]
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
        [HttpGet("{scrollVersionId}")]
        public async Task<ActionResult<ScrollVersionGroup>> GetScrollVersion(uint scrollVersionId)
        {
            var vg = await _scrollService.GetScrollVersionAsync(scrollVersionId, _userService.GetCurrentUserId(), false, false);

            if(vg==null)
            {
                return NotFound();
            }

            return Ok(vg);
        }

        [AllowAnonymous]
        [HttpGet("list")]
        public async Task<ActionResult<ScrollVersionListDTO>> ListScrollVersions()
        {
            var groups = await _scrollService.ListScrollVersionsAsync(_userService.GetCurrentUserId());
            return Ok(groups);
        }

        [HttpPost("update/{scrollVersionId}")]
        public async Task<ActionResult<ScrollVersion>> UpdateScrollVersion([FromBody] ScrollUpdateRequest request, uint scrollVersionId)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                if (!userId.HasValue)
                {
                    throw new System.NullReferenceException("No userId found"); // Do we have a central way to pass these exceptions?
                }
                var scroll = await _scrollService.UpdateScroll(scrollVersionId, request.name, userId.Value);
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

        [HttpPost("copy/{scrollVersionId}")] //not working well..
        public async Task<ActionResult<ScrollVersion>> CopyScrollVersion([FromBody] ScrollUpdateRequest request, uint scrollVersionId)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                if (!userId.HasValue)
                {
                    throw new System.NullReferenceException("No userId found"); // Do we have a central way to pass these exceptions?
                }
                var scroll = await _scrollService.CopyScroll(scrollVersionId, request.name, userId.Value);
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
        public async Task<ActionResult<ImagedFragmentList>> GetImagedFragments(uint scrollVersionId)
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
