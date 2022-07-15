using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers
{
	[Authorize]
	[ApiController]
	public class SearchController : ControllerBase
	{
		private readonly ISearchService _searchService;
		private readonly IUserService   _userService;

		public SearchController(ISearchService searchService, IUserService userService)
		{
			_searchService = searchService;
			_userService = userService;
		}

		/// <summary>
		///  Basic searching of the Qumranica database. Results are truncated
		///  to 100 results per search category.
		/// </summary>
		/// <param name="searchParameters">The parameters of the search</param>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpPost("v1/search")]
		public async Task<ActionResult<DetailedSearchResponseDTO>> PerformSearch(
				[FromBody] DetailedSearchRequestDTO searchParameters)
			=> await _searchService.PerformDetailedSearchAsync(
					_userService.GetCurrentUserId()
					, searchParameters);
	}
}
