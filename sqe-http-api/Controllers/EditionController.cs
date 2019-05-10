using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Services;

namespace SQE.SqeHttpApi.Server.Controllers
{

    [Authorize]
    [Route("v1/editions")]
    [ApiController]
    public class EditionController : ControllerBase
    {
        private readonly IEditionService _editionService;
        private readonly IUserService _userService;
        private readonly IImagedObjectService _imagedObjectService;
        private readonly IArtefactService _artefactService;


        public EditionController(
            IEditionService editionService, 
            IUserService userService, 
            IImagedObjectService imagedObjectService,
            IArtefactService artefactService)
        {
            _editionService = editionService;
            _userService = userService;
            _imagedObjectService = imagedObjectService;
            _artefactService = artefactService;
        }

        /// <summary>
        /// Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        [AllowAnonymous]
        [HttpGet("{editionId}")]
        public async Task<ActionResult<EditionGroupDTO>> GetEdition([FromRoute] uint editionId)
        {
            var edition = await _editionService.GetEditionAsync(editionId, _userService.GetCurrentUserObject(editionId), false, false);

            if(edition==null)
            {
                return NotFound();
            }

            return edition;
        }

        /// <summary>
        /// Provides a listing of all editions accessible to the current user
        /// </summary>
        [AllowAnonymous]
        [HttpGet("")]
        public async Task<ActionResult<EditionListDTO>> ListEditions()
        {
            var groups = await _editionService.ListEditionsAsync(_userService.GetCurrentUserId());
            return groups;
        }

        /// <summary>
        /// Updates data for the specified edition
        /// </summary>
        /// <param Name="request">JSON object with the attributes to be updated</param>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPut("{editionId}")]
        public async Task<ActionResult<EditionDTO>> UpdateEdition([FromBody] EditionUpdateRequestDTO request, [FromRoute] uint editionId)
        {
            try
            {
                var user = _userService.GetCurrentUserObject(editionId);
                if (!user.userId.HasValue && !(await user.EditionEditorId()).HasValue && !await user.MayWrite())
                {
                    throw new System.NullReferenceException("No userId found"); // Do we have a central way to pass these exceptions?
                }
                var edition = await _editionService.UpdateEditionAsync(editionId, request.name, user);
                //await _broadcastService.Broadcast(EditionId, JsonConvert.SerializeObject(edition));
                return edition;
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
        /// <param Name="request">JSON object with the attributes to be changed in the copied edition</param>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPost("{editionId}")]
        public async Task<ActionResult<EditionDTO>> CopyEdition([FromBody] EditionUpdateRequestDTO request, [FromRoute] uint editionId)
        {
            try
            {
                var user = _userService.GetCurrentUserObject(editionId);
                if (!user.userId.HasValue)
                {
                    throw new System.NullReferenceException("No userId found"); // Do we have a central way to pass these exceptions?
                }
                var edition = await _editionService.CopyEditionAsync(editionId, request.name, user);
                return edition;
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
