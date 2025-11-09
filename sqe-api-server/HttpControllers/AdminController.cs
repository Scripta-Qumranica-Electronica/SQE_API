using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers;

[Authorize]
[ApiController]
public class AdminController : ControllerBase
{
	private readonly IAdminService _adminService;
	private readonly IUserService  _userService;

	public AdminController(IAdminService adminService, IUserService userService)
	{
		_adminService = adminService;
		_userService = userService;
	}

	/// <summary>
	///  Checks a WKT polygon to ensure validity. If the polygon is invalid,
	///  it attempts to construct a valid polygon that matches the original
	///  as closely as possible.
	/// </summary>
	/// <param name="payload">JSON object with the WKT polygon to validate</param>
	[HttpGet("v1/[controller]/db-accessible")]
	public async Task<ActionResult<ServiceStatusDTO>> GetDatabaseStatusAsync()
		=> await _adminService.GetDatabaseStatusAsync(
				await _userService.GetCurrentUserObjectAsync(null));

	/// <summary>
	///  Checks a WKT polygon to ensure validity. If the polygon is invalid,
	///  it attempts to construct a valid polygon that matches the original
	///  as closely as possible.
	/// </summary>
	/// <param name="payload">JSON object with the WKT polygon to validate</param>
	[HttpGet("v1/[controller]/emailer-functioning")]
	public async Task<ActionResult<ServiceStatusDTO>> GetEmailerStatusAsync()
		=> await _adminService.GetEmailStatusAsync(
				await _userService.GetCurrentUserObjectAsync(null));
}
