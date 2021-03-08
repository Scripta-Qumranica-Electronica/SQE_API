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
		private readonly IImagedObjectRepository          _iooRepository;
		private readonly ISearchRepository                _searchRepository;
		private readonly IUserService                     _userService;

		public SearchService(
				IHubContext<MainHub, ISQEClient> hubContext
				, IEditionRepository             editionRepo
				, ISearchRepository              searchRepository
				, IArtefactService               artefactService
				, IUserService                   userService
				, IImagedObjectRepository        iooRepository)
		{
			_hubContext = hubContext;
			_editionRepo = editionRepo;
			_searchRepository = searchRepository;
			_artefactService = artefactService;
			_userService = userService;
			_iooRepository = iooRepository;
		}

		// TODO: create separate methods to run the search for each entity
		public async Task<DetailedSearchResponseDTO> PerformDetailedSearchAsync(
				uint?                      userId
				, DetailedSearchRequestDTO request)
		{
			var editions = new FlatEditionListDTO { editions = new List<EditionDTO>() };

			var textFragments = new TextFragmentSearchResponseListDTO
			{
					textFragments = new List<TextFragmentSearchResponseDTO>(),
			};

			var artefacts =
					new ExtendedArtefactListDTO { artefacts = new List<ExtendedArtefactDTO>() };

			var images =
					new ImageSearchResponseListDTO
					{
							imagedObjects = new List<ImageSearchResponseDTO>(),
					};

			// Find editions first
			if (!string.IsNullOrEmpty(request.textDesignation))
			{
				var editionIds = await _searchRepository.SearchEditions(
						userId ?? 1
						, request.textDesignation
						, request.exactTextDesignation);

				// TODO: fix this so we have only one DB query to get all the editions
				var editionList = await Task.WhenAll(
						editionIds.Select(
								async x => await _editionRepo.GetEditionAsync(userId ?? 1, x)));

				editions = new FlatEditionListDTO { editions = editionList.ToDTO() };
			}

			var searchEditionIds = editions.editions.Select(x => x.id);

			// Find imaged objects
			// TODO: the string parsing logic could be a lot better here. After some usage, make refinements
			if (!string.IsNullOrEmpty(request.imageDesignation))
			{
				// The imaged object ids consist of 2–3 entities separated by a hyphen.
				// See first if the query input can be parsed this way.
				const string imagedObjectRegex = "(.*)-(.*)-(.*)";
				string searchString = null;

				if (Regex.IsMatch(request.imageDesignation, imagedObjectRegex))
				{
					var mc = Regex.Matches(request.imageDesignation, imagedObjectRegex);
					searchString = mc[0].Value;
				}
				else
				{
					// Check now for any numbers in the search query and extract those
					const string imageRegex = @"(\d+)";
					var mc = Regex.Matches(request.imageDesignation, imageRegex);

					// paste the numbers together in a way that matches the imaged object id style
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
					var initialImages = await _searchRepository.SearchImagedObjects(
							searchString
							, request.exactImageDesignation);

					// Use a traditional for loop here because we may modify the
					// initialImages.imagedObjects in place by removing some entries.
					foreach (var image in initialImages)
					{
						var matchingEditions =
								await _iooRepository.GetImagedObjectEditionsAsync(userId, image.Id);

						// Check if a text designation was submitted
						if (!string.IsNullOrEmpty(request.textDesignation))
						{
							// Find all edition ids shared between the text designation editions
							// and the imaged object editions
							var intersection = editions.editions.Select(x => x.id)
													   .Intersect(matchingEditions);

							// Store the intersections or reject the search result if there are none
							if (intersection.Any())
							{
								var formattedImage = image.ToDTO();
								formattedImage.editionIds = intersection.ToArray();
								images.imagedObjects.Add(formattedImage);
							}
						}
						else // No text designation was submitted, so return all found edition ids
						{
							var formattedImage = image.ToDTO();
							formattedImage.editionIds = matchingEditions.ToArray();
							images.imagedObjects.Add(formattedImage);
						}
					}
				}
			}

			// Find artefacts
			if (request.artefactDesignation != null)
			{
				foreach (var artDesignation in request.artefactDesignation.Where(
						artDesignation => !string.IsNullOrEmpty(artDesignation)))
				{
					var editionArtefacts = await _searchRepository.SearchArtefacts(
							userId ?? 1
							, artDesignation
							, searchEditionIds
							, request.exactTextReference);

					// Todo: this is pretty clunky and cannot perform well, consider writing a custom method
					// Really we could gather all the needed info in one query in the
					// _searchRepository.SearchArtefacts method.
					foreach (var editionArtefact in editionArtefacts)
					{
						var userInfo =
								await _userService.GetCurrentUserObjectAsync(
										editionArtefact.EditionId);

						var artInfo = await _artefactService.GetEditionArtefactAsync(
								userInfo
								, editionArtefact.ArtefactId
								, new List<string> { "images", "masks" });

						artefacts.artefacts.Add(
								new ExtendedArtefactDTO
								{
										artefactDataEditorId = artInfo.artefactDataEditorId
										, artefactMaskEditorId =
												artInfo.artefactMaskEditorId
										, artefactPlacementEditorId =
												artInfo.artefactPlacementEditorId
										, editionId = artInfo.editionId
										, id = artInfo.id
										, imagedObjectId = artInfo.imagedObjectId
										, imageId = artInfo.imageId
										, isPlaced = artInfo.isPlaced
										, mask = artInfo.mask
										, name = artInfo.name
										, placement = artInfo.placement
										, ppi = editionArtefact.PixelsPerInch
										, side = artInfo.side
										, url = editionArtefact.Url
										, statusMessage = artInfo.statusMessage
										,
								});
					}
				}
			}

			// Find Text Fragments
			if (request.textReference != null)
			{
				foreach (var textReference in request.textReference.Where(
						textReference => !string.IsNullOrEmpty(textReference)))
				{
					var results = await _searchRepository.SearchTextFragments(
							userId ?? 1
							, textReference
							, searchEditionIds
							, request.exactArtefactDesignation);

					if (results.Any())
						textFragments.textFragments.AddRange(results.ToDTO().textFragments);
				}
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
