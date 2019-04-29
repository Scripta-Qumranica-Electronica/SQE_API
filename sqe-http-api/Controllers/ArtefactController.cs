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
    [Route("v1")]
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
        
        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="editionId">Unique id of the desired edition</param>
        [AllowAnonymous]
        [HttpGet("edition/{editionId}/artefact/list")]
        public async Task<ActionResult<ArtefactListDTO>> GetArtefacts(uint editionId)
        {
            try
            {
                return Ok(await _artefactService.GetEditionArtefactListings(_userService.GetCurrentUserId(), editionId));
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
        
        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition, including data about
        /// the related images
        /// </summary>
        /// <param name="editionId">Unique id of the desired edition</param>
        [AllowAnonymous]
        [HttpGet("edition/{editionId}/artefact/list/with-image-refs")]
        public async Task<ActionResult<ArtefactListDTO>> GetArtefactsWithImageRefs(uint editionId)
        {
            try
            {
                return Ok(await _artefactService.GetEditionArtefactListingsWithImages(_userService.GetCurrentUserId(), editionId));
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
    }
}