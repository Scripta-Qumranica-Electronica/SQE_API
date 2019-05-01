using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.Services;
using SQE.SqeHttpApi.Server.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers
{

    [Authorize]
    [Route("v1/[controller]")]
    [ApiController]
    public class ArtefactController : ControllerBase
    {
        private IUserService _userService;
        private IArtefactService _artefactService;

        public ArtefactController(IUserService userService, IArtefactService artefactService)
        {
            this._artefactService = artefactService;
            this._userService = userService;
        }

        [AllowAnonymous]
        [HttpGet("{artefactId}")]
        public async Task<ActionResult<List<ArtefactDTO>>> GetArtefact(int artefactId)
        {
            try
            {
                var artefacts = await _artefactService.GetAtrefactAsync(_userService.GetCurrentUserId(), artefactId, null, null);
                return artefacts;
            }
            catch (NotFoundException)
            {
                return NotFound(); 
            }
        }
    }
}