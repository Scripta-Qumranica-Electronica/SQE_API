using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using SQE.SqeHttpApi.DataAccess;
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

        [AllowAnonymous]
        [HttpGet("line")]
        public async Task<Scroll> RetrieveTextOfLineById(uint scrollVersionGroupId, uint lineId)
        {
            var scroll = await _service.GetLineById(scrollVersionGroupId, lineId);
            return scroll;
        }
   
        [AllowAnonymous]
        [HttpGet("fragment")]
        public async Task<Scroll> RetrieveTextOfFragmentById(uint scrollVersionGroupId, uint fragmentId)
        {
            var scroll = await _service.GetFragmentById(scrollVersionGroupId, fragmentId);
            return scroll;
        }
    }
}