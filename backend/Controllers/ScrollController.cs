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
        private ScrollRepository _repo;
        private IUserService _userService;

        public ScrollController(ScrollRepository scrollRepo, IUserService userService)
        {
            this._repo = scrollRepo;
            this._userService = userService;
        }

        [HttpGet("{id}/artefacts")]
        public async Task<ActionResult<ListResult<Artefact>>> ListArtefacts(int id)
        {
            var artefacts = await _repo.ListArtefactsAsync(id);
            return null;
            //return new ListResult<Artefact>(artefacts);
             
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<ListResult<ScrollVersion>>> scrollVersionData(int scroll_id)
        {
            var user = _userService.GetCurrentUser();
            //var scrollVersions = await _repo.ListMyScrollVersions(user.UserId, scroll_id);
            //var result = scrollVersions.Select(svModel => CreateScrollVersionDTO(svModel));

            //return new ListResult<ScrollVersion>(result);
            return null;
        }

        [AllowAnonymous]
        [HttpGet("list")]
        public async Task<ActionResult<ListResult<ScrollVersion>>> scrollVersionList()
        {
            var user = _userService.GetCurrentUser();
            //var scrollList = await _repo.scrollList(user.UserId); // TODO: Allow passing a null userId (if user is null)
            // var result = scrollList.Select(svModel => CreateScrollVersionDTO(svModel));

            //return new ListResult<ScrollVersion>(result);
            return null;
        }

        [AllowAnonymous]
        [HttpGet("scroll/{id}")]
        public async Task<ActionResult<ListResult<ScrollVersion>>> scrollVersion(int id)
        {
            List<int> scrollIds = new List<int>(new int[] { 513, 12 , 3 });
            var user = _userService.GetCurrentUser();
            var scrollList = await _repo.scrollVersion(user.UserId, scrollIds); // TODO: Allow passing a null userId (if user is null)
            var result = scrollList.Select(svModel => CreateScrollVersionDTO(svModel));

            return new ListResult<ScrollVersion>(result); ;
        }


        private ScrollVersion CreateScrollVersionDTO(SQE.Backend.DataAccess.Models.ScrollVersion model)
        {
            List<Share> shares = null;

            if (model.Sharing != null)
            {
                shares = model.Sharing.Select(sharingModel => new Share
                {
                    user = CreateUserDTO(sharingModel.User),
                    permission = CreatePermissionDTO(sharingModel.Permission)
                }).ToList();
            }



            return new ScrollVersion
            {
                id = model.Id,
                name = model.Name,
                permission = CreatePermissionDTO(model.Permission),
                thumbnailUrls = model.Thumbnail,
                shares = shares,
                locked = model.Locked,
                isPublic = model.IsPublic
            };
        }

        private Artefact CreateArtefactDto(SQE.Backend.DataAccess.Models.Artefact model)
        {
            Polygon myMask = null;
            if(model.Mask != null)
            {
            }
            return new Artefact
            {
                id = model.Id,
                scrollVersionId = model.ScrollVersionId,
                imageFragmentId = model.ImagedFragmentId,
                name = model.Name,
                // mask = model.Mask,
                transformMatrix = model.TransformMatrix,
            };
        }

        private UserData CreateUserDTO(SQE.Backend.DataAccess.Models.User model)
        {
            return new UserData
            {
                userName = model.UserName,
                userId = model.UserId
            };
        }

        private Permission CreatePermissionDTO(SQE.Backend.DataAccess.Models.Permission model)
        {
            return new Permission
            {
                canWrite = model.CanWrite,
                canLock = model.CanWrite,
            };
        }
    }
}
