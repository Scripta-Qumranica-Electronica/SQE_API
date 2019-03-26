﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SQE.Backend.Server.Services;
using SQE.Backend.Server.DTOs;



namespace backend.Controllers
{
    [Authorize]
    [Route("api/imaged-fragments")]
    [ApiController]
    public class ImagedFragmentsController : ControllerBase
    {

        private IUserService _userService;
        private IImagedFragmentsService _imagedFragmentService;

        public ImagedFragmentsController(IUserService userService, IImagedFragmentsService imagedFragmentService)
        {
            this._imagedFragmentService = imagedFragmentService;
            this._userService = userService;
        }

        [AllowAnonymous]
        [HttpGet("{scrollVersionId}")]
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