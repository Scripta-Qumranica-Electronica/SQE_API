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
			uint editionId,
			uint artefactId,
			string mask = null,
			string name = null,
			string position = null,
			string clientId = null);

		Task<ArtefactDTO> CreateArtefactAsync(EditionUserInfo editionUser,
			uint editionId,
			uint masterImageId,
			string mask = null,
			string name = null,
			string position = null,
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
			uint editionId,
			uint artefactId,
			string mask = null,
			string name = null,
			string position = null,
			string clientId = null)
		{
			var withMask = false;
			var resultList = new List<AlteredRecord>();
			if (editionUser.userId.HasValue)
			{
				if (!string.IsNullOrEmpty(mask))
				{
					// UpdateArtefactShapeAsync will inform us if the WKT mask is in an invalid format
					resultList.AddRange(
						await _artefactRepository.UpdateArtefactShapeAsync(editionUser, artefactId, mask)
					);
					withMask = true;
				}

				if (!string.IsNullOrEmpty(name))
					resultList.AddRange(
						await _artefactRepository.UpdateArtefactNameAsync(editionUser, artefactId, name)
					);

				if (!string.IsNullOrEmpty(position)
				) // TODO: we should allow for null here, which would mean we need to delete the position from the edition
				{
					if (!GeometryValidation.ValidateTransformMatrix(position))
						throw new StandardErrors.ImproperInputData("artefact position");
					resultList.AddRange(
						await _artefactRepository.UpdateArtefactPositionAsync(editionUser, artefactId, position)
					);
				}
			}

			var updatedArtefact = await GetEditionArtefactAsync(
				editionUser,
				artefactId,
				withMask ? new List<string> {"masks"} : null
			);

			return updatedArtefact;
		}

		public async Task<ArtefactDTO> CreateArtefactAsync(EditionUserInfo editionUser,
			uint editionId,
			uint masterImageId,
			string mask = null,
			string name = null,
			string position = null,
			string clientId = null)
		{
			uint newArtefactId = 0;
			if (editionUser.userId.HasValue)
			{
				if (!string.IsNullOrEmpty(position)
				    && !GeometryValidation.ValidateTransformMatrix(position))
					throw new StandardErrors.ImproperInputData("artefact position");
				newArtefactId = await _artefactRepository.CreateNewArtefactAsync(
					editionUser,
					editionId,
					masterImageId,
					mask,
					name,
					position
				);
			}

			var optional = mask != null ? new List<string> {"masks"} : new List<string>();

			var newArtefact = newArtefactId != 0
				? await GetEditionArtefactAsync(editionUser, newArtefactId, optional)
				: null;

			return newArtefact;
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