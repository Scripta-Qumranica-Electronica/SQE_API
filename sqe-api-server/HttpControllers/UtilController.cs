using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers
{
    [Authorize]
    [ApiController]
    public class UtilController : ControllerBase
    {
        private readonly IUtilService _utilService;

        public UtilController(IUtilService utilService)
        {
            _utilService = utilService;
        }

        /// <summary>
        ///     Checks a WKT polygon to ensure validity. If the polygon is invalid,
        ///     it attempts to construct a valid polygon that matches the original
        ///     as closely as possible.
        /// </summary>
        /// <param name="payload">JSON object with the WKT polygon to validate</param>
        [HttpPost("v1/[controller]s/repair-wkt-polygon")]
        public async Task<ActionResult<WktPolygonDTO>> RepairWktPolygon([FromBody] WktPolygonDTO payload)
        {
            return _utilService.RepairWktPolygonAsync(payload.wktPolygon);
        }
    }
}