using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Services;

namespace SQE.SqeHttpApi.Server.Controllers
{

    [Authorize]
    [Route("v1/edition")]
    [ApiController]
    public class EditionController : ControllerBase
    {
        private readonly IEditionService _editionService;
        private readonly IUserService _userService;
        private readonly IImagedObjectService _imagedObjectService;
        private readonly IArtefactService _artefactService;
        private readonly IBroadcastService _broadcastService;


        public EditionController(
            IEditionService editionService, 
            IUserService userService, 
            IImagedObjectService imagedObjectService,
            IArtefactService artefactService,
            IBroadcastService broadcastService)
        {
            _editionService = editionService;
            _userService = userService;
            _imagedObjectService = imagedObjectService;
            _artefactService = artefactService;
            _broadcastService = broadcastService;
        }

        /// <summary>
        /// Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique id of the desired edition</param>
        [AllowAnonymous]
        [HttpGet("{editionId}")]
        public async Task<ActionResult<EditionGroupDTO>> GetEdition(uint editionId)
        {
            var edition = await _editionService.GetEditionAsync(editionId, _userService.GetCurrentUserObject(), false, false);

            if(edition==null)
            {
                return NotFound();
            }

            return Ok(edition);
        }

        /// <summary>
        /// Provides a listing of all editions accessible to the current user
        /// </summary>
        [AllowAnonymous]
        [HttpGet("list")]
        public async Task<ActionResult<EditionListDTO>> ListEditions()
        {
            var groups = await _editionService.ListEditionsAsync(_userService.GetCurrentUserId());
            return Ok(groups);
        }

        /// <summary>
        /// Updates data for the specified edition
        /// </summary>
        /// <param name="request">JSON object with the attributes to be updated</param>
        /// <param name="editionId">Unique id of the desired edition</param>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPut("{editionId}")]
        public async Task<ActionResult<EditionDTO>> UpdateEdition([FromBody] ScrollUpdateRequestDTO request, uint editionId)
        {
            try
            {
                var user = _userService.GetCurrentUserObject();
                if (!user.userId.HasValue && !(await user.EditionEditorId()).HasValue && !await user.MayWrite())
                {
                    throw new System.NullReferenceException("No userId found"); // Do we have a central way to pass these exceptions?
                }
                var edition = await _editionService.UpdateEdition(editionId, request.name, user);
                //await _broadcastService.Broadcast(editionId, JsonConvert.SerializeObject(edition));
                return Ok(edition);
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

        /// <summary>
        /// Creates a copy of the specified edition
        /// </summary>
        /// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
        /// <param name="editionId">Unique id of the desired edition</param>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPost("{editionId}")]
        public async Task<ActionResult<EditionDTO>> CopyEdition([FromBody] ScrollUpdateRequestDTO request, uint editionId)
        {
            try
            {
                var user = _userService.GetCurrentUserObject();
                if (!user.userId.HasValue)
                {
                    throw new System.NullReferenceException("No userId found"); // Do we have a central way to pass these exceptions?
                }
                var edition = await _editionService.CopyEdition(editionId, request.name, user);
                return Ok(edition);
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
    }
}
