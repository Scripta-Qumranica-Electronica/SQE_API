using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.DatabaseAccess;

namespace SQE.API.Server.Services
{
	public interface ISearchService
	{
		Task<DetailedSearchResponseDTO> PerformDetailedSearchAsync(
				uint?                      userId
				, DetailedSearchRequestDTO request);
	}

	public class SearchService : ISearchService
	{
		private readonly IHubContext<MainHub, ISQEClient> _hubContext;
		private readonly ITextRepository                  _textRepository;

		public SearchService(
				IHubContext<MainHub, ISQEClient> hubContext
				, ITextRepository                textRepository)
		{
			_hubContext = hubContext;
			_textRepository = textRepository;
		}

		public async Task<DetailedSearchResponseDTO> PerformDetailedSearchAsync(
				uint?                      userId
				, DetailedSearchRequestDTO request)
		{
			var userDet = false;

			if (userId.HasValue)
				userDet = true;

			return new DetailedSearchResponseDTO();
		}
	}
}
