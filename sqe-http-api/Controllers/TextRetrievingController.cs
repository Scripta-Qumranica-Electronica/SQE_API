using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Helpers;

namespace SQE.SqeHttpApi.Server.Controllers
{
    [Authorize]
    [Route("v1")]
    [ApiController]
    public class TextRetrievingController : ControllerBase
    {
        private ITextRetrievingService _service;
        
        
        public TextRetrievingController(ITextRetrievingService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves all signs and their data from the given line
        /// </summary>
        /// <param Name="lineId">Id of the line</param>
        /// <param Name="editionId">Id of the edition</param>
        /// <returns>A scroll object with includes the fragments and their lines
        /// in a hierarchical order and in correct sequence</returns>
        [AllowAnonymous]
        [HttpGet("line")]
        public async Task<ActionResult<Scroll>> RetrieveTextOfLineById(uint lineId, uint editionId)
        {
            return await _service.GetLineById( lineId, editionId);
        }
   
        /// <summary>
        /// Retrieves all signs and their data from the given fragment
        /// </summary>
        /// <param Name="textFragmentId">Id of the fragment</param>
        /// <param Name="editionId">Id of the edition</param>
        /// <returns>A scroll object with includes the fragment and its lines
        /// in a hierarchical order and in correct sequence</returns>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/text-fragments/{textFragmentId}")]
        public async Task<ActionResult<Scroll>> RetrieveTextOfFragmentById([FromRoute] uint editionId, [FromRoute] uint textFragmentId)
        {
            return await _service.GetFragmentByIdAsync(textFragmentId, editionId);
        }
  
        
        /// <summary>
        /// Retrieves the ids of all fragments in the given edition of a scroll
        /// </summary>
        /// <param Name="editionId">Id of the edition</param>
        /// <returns>An array of the ids in correct sequence</returns>
        [AllowAnonymous]
        [HttpGet("editions/{editionId}/text-fragments")]
        public async Task<ActionResult<TextFragmentListDTO>> RetrieveFragmentIds([FromRoute] uint editionId)
        {
            return await _service.GetFragmentIdsAsync(editionId);
        }
 
        /// <summary>
        /// Retrieves the ids of all lines in the given fragment
        /// </summary>
        /// <param Name="fragmentId">Id of the fragment</param>
        /// <param Name="editionId">Id of the edition</param>
        /// <returns>An array of the ids in the right sequence</returns>
        [AllowAnonymous]
        [HttpGet("lineIds")]
        public async Task<ActionResult<uint[]>> RetrieveLineIds(uint fragmentId, uint editionId)
        {
            return await _service.GetLineIds(fragmentId, editionId);
        }
    }
}