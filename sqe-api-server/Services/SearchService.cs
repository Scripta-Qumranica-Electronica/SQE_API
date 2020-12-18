using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Serialization;
using SQE.DatabaseAccess;
// ReSharper disable ArrangeRedundantParentheses

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
		private readonly IArtefactService                 _artefactService;
		private readonly IEditionRepository               _editionRepo;
		private readonly IHubContext<MainHub, ISQEClient> _hubContext;
		private readonly ISearchRepository                _searchRepository;
		private readonly IUserService                     _userService;

		public SearchService(
				IHubContext<MainHub, ISQEClient> hubContext
				, IEditionRepository             editionRepo
				, ISearchRepository              searchRepository
				, IArtefactService               artefactService
				, IUserService                   userService)
		{
			_hubContext = hubContext;
			_editionRepo = editionRepo;
			_searchRepository = searchRepository;
			_artefactService = artefactService;
			_userService = userService;
		}

		public async Task<DetailedSearchResponseDTO> PerformDetailedSearchAsync(
				uint?                      userId
				, DetailedSearchRequestDTO request)
		{
			var editions = new FlatEditionListDTO();

			var textFragments = new TextFragmentSearchResponseListDTO
			{
					textFragments = new List<TextFragmentSearchResponseDTO>(),
			};

			var artefacts = new ArtefactListDTO { artefacts = new List<ArtefactDTO>() };
			var images = new ImageSearchResponseListDTO();

			if (!string.IsNullOrEmpty(request.imageDesignation))
			{
				const string imagedObjectRegex = "(.*)-(.*)-(.*)";
				string searchString = null;

				if (Regex.IsMatch(request.imageDesignation, imagedObjectRegex))
				{
					var mc = Regex.Matches(request.imageDesignation, imagedObjectRegex);
					searchString = mc[0].Value;
				}
				else
				{
					const string imageRegex = @"(\d+)";
					var mc = Regex.Matches(request.imageDesignation, imageRegex);

					if (mc.Count > 0)
					{
						if (mc[0].Groups.Count > 0)
							searchString = mc[0].Groups[0].Value;

						if ((mc.Count > 1)
							&& (mc[1].Groups.Count > 0))
							searchString += "-" + mc[1].Groups[0].Value;
					}
				}

				if (!string.IsNullOrEmpty(searchString))
				{
					// Search for the imaged object
					images = (await _searchRepository.SearchImagedObjects(
							searchString
							, request.exactImageDesignation)).ToDTO();
				}
			}

			if (!string.IsNullOrEmpty(request.textDesignation))
			{
				var editionIds = await _searchRepository.SearchEditions(
						userId ?? 1
						, request.textDesignation
						, request.exactTextDesignation);

				var editionList = await Task.WhenAll(
						editionIds.Select(
								async x => await _editionRepo.GetEditionAsync(userId ?? 1, x)));

				editions = new FlatEditionListDTO { editions = editionList.ToDTO() };
			}

			var searchEditionIds = editions.editions.Select(x => x.id);

			foreach (var artDesignation in request.artefactDesignation.Where(
					artDesignation => !string.IsNullOrEmpty(artDesignation)))
			{
				var editionArtefacts = await _searchRepository.SearchArtefacts(
						userId ?? 1
						, artDesignation
						, searchEditionIds
						, request.exactTextReference);

				// Todo: this is pretty clunky and cannot perform well, consider writing a custom method
				foreach (var editionArtefact in editionArtefacts)
				{
					var userInfo =
							await _userService.GetCurrentUserObjectAsync(editionArtefact.EditionId);

					artefacts.artefacts.Add(
							await _artefactService.GetEditionArtefactAsync(
									userInfo
									, editionArtefact.ArtefactId
									, new List<string> { "images", "masks" }));
				}
			}

			foreach (var textReference in request.textReference.Where(
					artDesignation => !string.IsNullOrEmpty(artDesignation)))
			{
				var results = await _searchRepository.SearchTextFragments(
						userId ?? 1
						, textReference
						, searchEditionIds
						, request.exactArtefactDesignation);

				if (results.Any())
					textFragments.textFragments.AddRange(results.ToDTO().textFragments);
			}

			return new DetailedSearchResponseDTO
			{
					artefacts = artefacts
					, editions = editions
					, images = images
					, textFragments = textFragments
					,
			};
		}
	}
}