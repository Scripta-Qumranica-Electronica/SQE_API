using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.Services;

namespace SQE.SqeHttpApi.Server.Controllers
{
    [Authorize]
    [Route("va/[controller]")]
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
        /// <param name="lineId">Id of the line</param>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="withLicence">Optional (default: false); if true, then a licence text is added to the output</param>
        /// <param name="verbose">Optional (default: true): if true all data are included, if false the attribute 1
        /// (designing a letter simply as a letter) is skipped."</param>
        /// <returns>A scroll object with includes the fragments and their lines
        /// in a hierarchical order and in correct sequence</returns>
        [AllowAnonymous]
        [HttpGet("line")]
        public async Task<ActionResult<Scroll>> RetrieveTextOfLineById(uint lineId, uint editionId ,
            bool withLicence=false, bool verbose=true)
        {
            var scroll = await _service.GetLineById( lineId, editionId, withLicence, verbose);
            return scroll;
        }
   
        /// <summary>
        /// Retrieves all signs and their data from the given fragment
        /// </summary>
        /// <param name="fragmentId">Id of the fragment</param>
        /// <param name="editionId">Id of the edition</param>
        /// <returns>A scroll object with includes the fragment and its lines
        /// in a hierarchical order and in correct sequence</returns>
        [AllowAnonymous]
        [HttpGet("fragment")]
        public async Task<ActionResult<Scroll>> RetrieveTextOfFragmentById(uint fragmentId, uint editionId,
            bool withLicence=false, bool verbose=true)
        {
            var scroll = await _service.GetFragmentById(fragmentId, editionId, withLicence,verbose);
            return scroll;
        }
  
        
        /// <summary>
        /// Retrieves the ids of all fragments in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <returns>An array of the ids in correct sequence</returns>
        [AllowAnonymous]
        [HttpGet("fragmentIds")]
        public async Task<ActionResult<uint[]>> RetrieveFragmentIds(uint editionId)
        {
            var fragmentIds = await _service.GetFragmentIds(editionId);
            return fragmentIds;
        }
 
        /// <summary>
        /// Retrieves the ids of all lines in the given fragment
        /// </summary>
        /// <param name="fragmentId">Id of the fragment</param>
        /// <param name="editionId">Id of the edition</param>
        /// <returns>An array of the ids in the right sequence</returns>
        [AllowAnonymous]
        [HttpGet("lineIds")]
        public async Task<ActionResult<uint[]>> RetrieveLineIds(uint fragmentId, uint editionId)
        {
            var lineIds = await _service.GetLineIds(fragmentId, editionId);
            return lineIds;
        }
    }
}