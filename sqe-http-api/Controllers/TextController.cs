using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Helpers;
using SQE.SqeHttpApi.Server.Services;

namespace SQE.SqeHttpApi.Server.Controllers
{
    [Authorize]
    [Route("v1")]
    [ApiController]
    public class TextController : ControllerBase
    {
        private readonly ITextService _textService;
        private readonly IUserService _userService;
        
        public TextController(ITextService textService, IUserService userService)
        {
            _textService = textService;
            _userService = userService;
        }

        /// <summary>
        /// Retrieves all signs and their data from the given line
        /// </summary>
        /// <param name="lineId">Id of the line</param>
        /// <param name="editionId">Id of the edition</param>
        /// <returns>A scroll object with includes the fragments and their lines
        /// in a hierarchical order and in correct sequence</returns>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/lines/{lineId}")]
        public async Task<ActionResult<LineTextDTO>> RetrieveTextOfLineById([FromRoute] uint editionId, [FromRoute] uint lineId)
        {
            return await _textService.GetLineByIdAsync(_userService.GetCurrentUserObject(editionId), lineId);
        }
   
        /// <summary>
        /// Retrieves all signs and their data from the given textFragmentName
        /// </summary>
        /// <param name="textFragmentId">Id of the textFragmentName</param>
        /// <param name="editionId">Id of the edition</param>
        /// <returns>A scroll object with includes the textFragmentName and its lines
        /// in a hierarchical order and in correct sequence</returns>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/text-fragments/{textFragmentId}")]
        public async Task<ActionResult<TextEditionDTO>> RetrieveTextOfFragmentById([FromRoute] uint editionId, [FromRoute] uint textFragmentId)
        {
            return await _textService.GetFragmentByIdAsync(_userService.GetCurrentUserObject(editionId), textFragmentId);
        }
  
        
        /// <summary>
        /// Retrieves the ids of all fragments in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <returns>An array of the ids in correct sequence</returns>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/text-fragments")]
        public async Task<ActionResult<TextFragmentDataListDTO>> RetrieveFragmentIds([FromRoute] uint editionId)
        {
            return await _textService.GetFragmentDataAsync(_userService.GetCurrentUserObject(editionId));
        }
 
        /// <summary>
        /// Retrieves the ids of all lines in the given textFragmentName
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="textFragmentId">Id of the textFragmentName</param>
        /// <returns>An array of the ids in the right sequence</returns>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/text-fragments/{textFragmentId}/lines")]
        public async Task<ActionResult<LineDataListDTO>> RetrieveLineIds([FromRoute] uint editionId, [FromRoute] uint textFragmentId)
        {
            return await _textService.GetLineIdsAsync(_userService.GetCurrentUserObject(editionId), textFragmentId);
        }
    }
}