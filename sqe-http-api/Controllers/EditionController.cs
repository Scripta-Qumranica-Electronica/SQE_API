using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Helpers;

namespace SQE.SqeHttpApi.Server.Controllers
{

    [Authorize]
    [Route("v1/[controller]s")]
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
        /// <param name="editionId">Unique Id of the desired edition</param>
        [AllowAnonymous]
        [HttpGet("{editionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
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
        [ProducesResponseType(200)]
        public async Task<ActionResult<EditionListDTO>> ListEditions()
        {
            var groups = await _editionService.ListEditionsAsync(_userService.GetCurrentUserId());
            return groups;
        }

        /// <summary>
        /// Updates data for the specified edition
        /// </summary>
        /// <param name="request">JSON object with the attributes to be updated</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPut("{editionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EditionDTO>> UpdateEdition([FromBody] EditionUpdateRequestDTO request, [FromRoute] uint editionId)
        {
            try
            {
                return await _editionService.UpdateEditionAsync(
                    _userService.GetCurrentUserObject(editionId), 
                    request.name,
                    request.copyrightHolder,
                    request.collaborators);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch(ForbiddenException)
            {
                return Forbid();
            }
            catch(NoPermissionException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Creates a copy of the specified edition
        /// </summary>
        /// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <exception cref="NullReferenceException"></exception>
        [HttpPost("{editionId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EditionDTO>> CopyEdition([FromBody] EditionUpdateRequestDTO request, [FromRoute] uint editionId)
        {
            try
            {
                return await _editionService.CopyEditionAsync(_userService.GetCurrentUserObject(editionId), request);
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
        
        // TODO: delete edition.
    }
}
