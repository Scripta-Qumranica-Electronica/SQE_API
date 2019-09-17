using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;
using SQE.SqeHttpApi.Server.Helpers;

namespace SQE.SqeHttpApi.Server.Services
{
	public interface IArtefactService
	{
		Task<ArtefactDTO> GetEditionArtefactAsync(EditionUserInfo editionUser, uint artefactId, List<string> optional);

		Task<ArtefactListDTO> GetEditionArtefactListingsAsync(EditionUserInfo editionUser,
			List<string> optional);

		Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(EditionUserInfo editionUser,
			bool withMask = false);

		Task<ArtefactDTO> UpdateArtefactAsync(EditionUserInfo editionUser,
			uint artefactId,
			UpdateArtefactDTO updateArtefact,
			string clientId = null);

		Task<ArtefactDTO> CreateArtefactAsync(EditionUserInfo editionUser,
			CreateArtefactDTO createArtefact,
			string clientId = null);

		Task<NoContentResult> DeleteArtefactAsync(EditionUserInfo editionUser, uint artefactId, string clientId = null);
		Task<TextFragmentDataListDTO> ArtefactSuggestedTextFragmentsAsync(EditionUserInfo editionUser, uint artefactId);
	}

	public class ArtefactService : IArtefactService
	{
		private readonly IArtefactRepository _artefactRepository;

		public ArtefactService(IArtefactRepository artefactRepository)
		{
			_artefactRepository = artefactRepository;
		}

		public async Task<ArtefactDTO> GetEditionArtefactAsync(EditionUserInfo editionUser,
			uint artefactId,
			List<string> optional)
		{
			ParseOptionals(optional, out _, out var withMask);
			var artefact = await _artefactRepository.GetEditionArtefactAsync(editionUser, artefactId, withMask);
			return ArtefactDTOTransformer.QueryArtefactToArtefactDTO(artefact, editionUser.EditionId);
		}

		public async Task<ArtefactListDTO> GetEditionArtefactListingsAsync(EditionUserInfo editionUser,
			List<string> optional)
		{
			ParseOptionals(optional, out var withImages, out var withMask);
			ArtefactListDTO artefacts;
			if (withImages)
			{
				artefacts = await GetEditionArtefactListingsWithImagesAsync(editionUser, withMask);
			}
			else
			{
				var listings = await _artefactRepository.GetEditionArtefactListAsync(editionUser, withMask);
				artefacts = ArtefactDTOTransformer.QueryArtefactListToArtefactListDTO(
					listings.ToList(),
					editionUser.EditionId
				);
			}

			return artefacts;
		}

		public async Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(EditionUserInfo editionUser,
			bool withMask = false)
		{
			var artefactListings = await _artefactRepository.GetEditionArtefactListAsync(editionUser, withMask);
			var imagedObjectIds = artefactListings.Select(x => x.ImageCatalogId);


			return ArtefactDTOTransformer.QueryArtefactListToArtefactListDTO(
				artefactListings.ToList(),
				editionUser.EditionId
			);
		}

		public async Task<ArtefactDTO> UpdateArtefactAsync(EditionUserInfo editionUser,
			uint artefactId,
			UpdateArtefactDTO updateArtefact,
			string clientId = null)
		{
			var withMask = false;
			var tasks = new List<Task<List<AlteredRecord>>>();
			if (!string.IsNullOrEmpty(updateArtefact.mask))
			{
				// UpdateArtefactShapeAsync will inform us if the WKT mask is in an invalid format
				tasks.Add(_artefactRepository.UpdateArtefactShapeAsync(editionUser, artefactId, updateArtefact.mask));
				withMask = true;
			}

			if (!string.IsNullOrEmpty(updateArtefact.name))
				tasks.Add(_artefactRepository.UpdateArtefactNameAsync(editionUser, artefactId, updateArtefact.name));

			tasks.Add(
				_artefactRepository.UpdateArtefactPositionAsync(
					editionUser,
					artefactId,
					updateArtefact.scale,
					updateArtefact.rotate,
					updateArtefact.translateX,
					updateArtefact.translateY
				)
			);

			tasks.Add(
				_artefactRepository.UpdateArtefactStatusAsync(editionUser, artefactId, updateArtefact.statusMessage)
			);

			await Task.WhenAll(tasks);
			var updatedArtefact = await GetEditionArtefactAsync(
				editionUser,
				artefactId,
				withMask ? new List<string> { "masks" } : null
			);

			return updatedArtefact;
		}

		public async Task<ArtefactDTO> CreateArtefactAsync(EditionUserInfo editionUser,
			CreateArtefactDTO createArtefact,
			string clientId = null)
		{
			uint newArtefactId = 0;
			if (editionUser.userId.HasValue)
				newArtefactId = await _artefactRepository.CreateNewArtefactAsync(
					editionUser,
					createArtefact.masterImageId,
					createArtefact.mask,
					createArtefact.name,
					createArtefact.scale,
					createArtefact.rotate,
					createArtefact.translateX,
					createArtefact.translateY,
					createArtefact.statusMessage
				);

			var optional = createArtefact.mask != null ? new List<string> { "masks" } : new List<string>();

			var createArtefactnewArtefact = newArtefactId != 0
				? await GetEditionArtefactAsync(editionUser, newArtefactId, optional)
				: null;

			return createArtefactnewArtefact;
		}

		public async Task<NoContentResult> DeleteArtefactAsync(EditionUserInfo editionUser,
			uint artefactId,
			string clientId = null)
		{
			await _artefactRepository.DeleteArtefactAsync(editionUser, artefactId);

			return new NoContentResult();
		}

		public async Task<TextFragmentDataListDTO> ArtefactSuggestedTextFragmentsAsync(EditionUserInfo editionUser,
			uint artefactId)
		{
			return new TextFragmentDataListDTO
			{
				textFragments = (await _artefactRepository.ArtefactSuggestedTextFragmentsAsync(editionUser, artefactId))
					.Select(
						x => new TextFragmentDataDTO(x.TextFragmentId, x.TextFragmentName, x.EditionEditorId)
					)
					.ToList()
			};
		}

		private void ParseOptionals(List<string> optionals, out bool images, out bool masks)
		{
			images = masks = false;
			if (optionals == null)
				return;
			images = optionals.Contains("images");
			masks = optionals.Contains("masks");
		}
	}
}