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
<<<<<<< HEAD

    [Authorize]
    [Route("v1/[controller]")]
=======
    [Authorize]
    [Route("v1")]
>>>>>>> 6cc19a4187d1bfe5c70efc913e4adf5b324c1a4e
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
<<<<<<< HEAD
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
=======
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
>>>>>>> 6cc19a4187d1bfe5c70efc913e4adf5b324c1a4e
            }
        }
    }
}