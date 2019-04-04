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
    [Route("v1/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private IImageService _imageService;
        private IUserService _userService;

        public ImageController(IImageService imageService, IUserService userService)
        {
            this._imageService = imageService;
            this._userService = userService;
        }

        //[AllowAnonymous]
        //[HttpGet("{id}")]
        //public async Task<ActionResult<ScrollVersionGroup>> GetScrollVersion(int id)
        //{
        //    var vg = await _imageService.GetScrollVersionAsync(id, _userService.GetCurrentUserId(), false, false);

        //    if (vg == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(vg);
        //}

        [AllowAnonymous]
        [HttpGet("list")]
        public async Task<ActionResult<ImageGroupListDTO>> ListImageGroups()
        {
            var images = await _imageService.GetImageAsync(_userService.GetCurrentUserId(), new List<uint>());
            return Ok(images);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<ImageGroupListDTO>> ListImageGroupsOfScroll(uint id)
        {
            var images = await _imageService.GetImageAsync(_userService.GetCurrentUserId(), new List<uint>(new uint[] {id }));
            return Ok(images);
        }

        /// <summary>
        /// Provides a list of all institutional image providers.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("institution/list")]
        public async Task<ActionResult<ImageInstitutionListDTO>> ListImageInstitutions()
        {
            var institutions = await _imageService.GetImageInstitutionsAsync();
            return Ok(institutions);
        }
    }
}
