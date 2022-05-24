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

		public UtilController(IUtilService utilService) => _utilService = utilService;

		/// <summary>
		///  Checks a WKT polygon to ensure validity. If the polygon is invalid,
		///  it attempts to construct a valid polygon that matches the original
		///  as closely as possible.
		/// </summary>
		/// <param name="payload">JSON object with the WKT polygon to validate</param>
		[HttpPost("v1/[controller]s/repair-wkt-polygon")]
		public ActionResult<WktPolygonDTO> RepairWktPolygon([FromBody] WktPolygonDTO payload)
			=> _utilService.RepairWktPolygonAsync(payload.wktPolygon);

		/// <summary>
		///  Provides the current version designation of the database along with
		///  the date it was updated to that version.
		/// </summary>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpGet("v1/[controller]s/database-version")]
		public async Task<ActionResult<DatabaseVersionDTO>> GetDatabaseVersion()
			=> await _utilService.GetDatabaseVersion();

		/// <summary>
		///  Provides the current version designation of the API server along with
		///  the date it was updated to that version.
		/// </summary>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpGet("v1/[controller]s/api-version")]
		public async Task<ActionResult<APIVersionDTO>> GetAPIVersion()
			=> await _utilService.GetAPIVersion();

		/// <summary>
		///  Adds a new entry in Github issues
		/// </summary>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpPost("v1/[controller]s/report-github-issue")]
		public async Task<ActionResult> ReportGithubIssueRequest([FromBody] GithubIssueReportDTO payload)
			=> await _utilService.ReportGithubIssueRequestAsync(payload.title, payload.body);		
	}
}
