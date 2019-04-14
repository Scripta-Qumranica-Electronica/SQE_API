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
        [HttpGet("{id}")]
        public async Task<ActionResult<ArtefactDTO>> GetArtefact(int id)
        {
            return new ArtefactDTO();
        }
    }
}