﻿using System;
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
    [Route("v1/image")]
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
        public async Task<ActionResult<ImageGroupList>> ListImageGroups()
        {
            var images = await _imageService.GetImageAsync(_userService.GetCurrentUserId(), new List<int>());
            return Ok(images);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<ImageGroupList>> ListImageGroupsOfScroll(int id)
        {
            var images = await _imageService.GetImageAsync(_userService.GetCurrentUserId(), new List<int>(new int[] {id }));
            return Ok(images);
        }

        /// <summary>
        /// Provides a list of all institutional image providers.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("institution/list")]
        public async Task<ActionResult<ImageInstitutionList>> ListImageInstitutions()
        {
            var institutions = await _imageService.GetImageInstitutionsAsync();
            return Ok(institutions);
        }
    }
}
