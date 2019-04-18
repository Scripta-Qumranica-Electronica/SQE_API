using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Services;

namespace SQE.SqeHttpApi.Server.Controllers
{

    [Authorize]
    [Route("v1/scroll-version")]
    [ApiController]
    public class ScrollVersionController : ControllerBase
    {
        private IScrollService _scrollService;
        private IUserService _userService;
        private IImagedFragmentsService _imagedFragmentService;
        private IArtefactService _artefactService;


        public ScrollVersionController(
            IScrollService scrollService, 
            IUserService userService, 
            IImagedFragmentsService imagedFragmentService,
            IArtefactService artefactService)
        {
            this._scrollService = scrollService;
            this._userService = userService;
            this._imagedFragmentService = imagedFragmentService;
            this._artefactService = artefactService;
        }

        [AllowAnonymous]
        [HttpGet("{scrollVersionId}")]
        public async Task<ActionResult<ScrollVersionGroupDTO>> GetScrollVersion(uint scrollVersionId)
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
        public async Task<ActionResult<ScrollVersionDTO>> UpdateScrollVersion([FromBody] ScrollUpdateRequestDTO request, uint scrollVersionId)
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
        public async Task<ActionResult<ScrollVersionDTO>> CopyScrollVersion([FromBody] ScrollUpdateRequestDTO request, uint scrollVersionId)
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
        public async Task<ActionResult<ImagedFragmentListDTO>> GetImagedFragments(uint scrollVersionId)
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

        [AllowAnonymous]
        [HttpGet("{scrollVersionId}/artefacts")]
        public async Task<ActionResult<ArtefactListDTO>> GetArtefacts(uint scrollVersionId)
        {
            try
            {
                return await _artefactService.GetScrollArtefactListings(_userService.GetCurrentUserId(), scrollVersionId);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
        
        [AllowAnonymous]
        [HttpGet("{scrollVersionId}/artefacts/with-image-refs")]
        public async Task<ActionResult<ArtefactListDTO>> GetArtefactsWithImageRefs(uint scrollVersionId)
        {
            try
            {
                return await _artefactService.GetScrollArtefactListingsWithImages(_userService.GetCurrentUserId(), scrollVersionId);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
    }
}
