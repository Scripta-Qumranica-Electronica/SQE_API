using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Helpers;

namespace SQE.SqeHttpApi.Server.Controllers
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
        
        private void ParseOptionals(List<string> optionals, out bool images, out bool masks)
        {
            images = masks = false;
            if (optionals == null) 
                return;
            images = optionals.Contains("artefacts");
            masks = optionals.Contains("masks");
        }
        
        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        /// <param Name="images">Defines whether image references should be included in the response</param>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/artefacts")]
        public async Task<ActionResult<ArtefactListDTO>> GetArtefacts([FromRoute] uint editionId, [FromQuery] List<string> optional)
        {
            ParseOptionals(optional, out var images, out var masks);
            try
            {
                return await _artefactService.GetEditionArtefactListingsAsync(_userService.GetCurrentUserId(), editionId, masks, images);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
        
        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        /// <param Name="ArtefactId">Unique Id of the desired artefact</param>
        /// <param Name="images">Defines whether image references should be included in the response</param>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/artefacts/{ArtefactId}")]
        public async Task<ActionResult<ArtefactDTO>> GetArtefact([FromRoute] uint editionId, [FromRoute] uint artefactId, [FromQuery] List<string> optional)
        {
            ParseOptionals(optional, out var images, out var masks);
            try
            {
                return await _artefactService.GetEditionArtefactAsync(_userService.GetCurrentUserObject(editionId), artefactId, masks);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
        
        /// <summary>
        /// Updates the specified artefact
        /// </summary>
        /// <param Name="editionId">Unique Id of the desired edition</param>
        [HttpPut("editions/{editionId}/artefacts/{ArtefactId}")]
        public async Task<ActionResult<ArtefactDTO>> UpdateArtefact(
            [FromRoute] uint editionId, 
            [FromRoute] uint artefactId, 
            [FromBody] UpdateArtefactDTO payload
            )
        {
            try
            {
                return await _artefactService.UpdateArtefact(
                    _userService.GetCurrentUserObject(editionId), 
                    editionId, 
                    artefactId, 
                    payload.mask, 
                    payload.name, 
                    payload.position);
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
        /// <param Name="editionId">Unique Id of the desired edition</param>
        [HttpPost("editions/{editionId}/artefacts")]
        public async Task<ActionResult<ArtefactDTO>> CreateArtefact(
            [FromRoute] uint editionId,
            [FromBody] CreateArtefactDTO payload
        )
        {
            try
            {
                return await _artefactService.CreateArtefact(
                    _userService.GetCurrentUserObject(editionId), 
                    editionId, 
                    payload.masterImageId, 
                    payload.mask, 
                    payload.name, 
                    payload.position);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }

        public class UpdateArtefactDTO
        {
            public string mask { get; set; }
            public string name { get; set; }
            public string position { get; set; }
        }
        public class CreateArtefactDTO
        {
            public uint masterImageId { get; set; }
            public string mask { get; set; }
            public string name { get; set; }
            public string position { get; set; }
        }
    }
}