using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Serialization;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
	public interface ICatalogService
	{
		Task<CatalogueMatchListDTO> GetAllMatches();

		Task<CatalogueMatchListDTO> GetImagedObjectsOfTextFragment(uint textFragmentId);

		Task<CatalogueMatchListDTO> GetTextFragmentsOfImagedObject(string imagedObjectId);

		Task<CatalogueMatchListDTO> GetTextFragmentsAndImagedObjectsOfEdition(uint editionId);

		Task<CatalogueMatchListDTO> GetTextFragmentsAndImagedObjectsOfManuscript(uint manuscriptId);

		Task<NoContentResult> CreateTextFragmentImagedObjectMatch(
				UserInfo                 user
				, CatalogueMatchInputDTO match
				, string                 clientId = null);

		Task<NoContentResult> ConfirmTextFragmentImagedObjectMatch(
				UserInfo user
				, uint   textFragmentImagedObjectMatchId
				, bool   confirm
				, string clientId = null);
	}

	public class CatalogService : ICatalogService
	{
		private readonly ICatalogueRepository             _catalogueRepo;
		private readonly IHubContext<MainHub, ISQEClient> _hubContext;

		public CatalogService(
				ICatalogueRepository               catalogueRepository
				, IHubContext<MainHub, ISQEClient> hubContext)
		{
			_catalogueRepo = catalogueRepository;
			_hubContext = hubContext;
		}

		public async Task<CatalogueMatchListDTO> GetAllMatches()
			=> (await _catalogueRepo.GetAllMetchesAsync()).ToDTO();

		public async Task<CatalogueMatchListDTO> GetImagedObjectsOfTextFragment(uint textFragmentId)
			=> (await _catalogueRepo.GetImagedObjectMatchesForTextFragmentAsync(textFragmentId))
					.ToDTO();

		public async Task<CatalogueMatchListDTO>
				GetTextFragmentsOfImagedObject(string imagedObjectId)
			=> (await _catalogueRepo.GetTextFragmentMatchesForImagedObjectAsync(
					HttpUtility.UrlDecode(imagedObjectId))).ToDTO();

		public async Task<CatalogueMatchListDTO>
				GetTextFragmentsAndImagedObjectsOfEdition(uint editionId)
			=> (await _catalogueRepo.GetImagedObjectAndTextFragmentMatchesForEditionAsync(editionId)
					).ToDTO();

		public async Task<CatalogueMatchListDTO>
				GetTextFragmentsAndImagedObjectsOfManuscript(uint manuscriptId)
			=> (await _catalogueRepo.GetImagedObjectAndTextFragmentMatchesForManuscriptAsync(
					manuscriptId)).ToDTO();

		public async Task<NoContentResult> CreateTextFragmentImagedObjectMatch(
				UserInfo                 user
				, CatalogueMatchInputDTO match
				, string                 clientId = null)
		{
			CheckCatalogueEditRights(user);

			await _catalogueRepo.CreateNewImagedObjectTextFragmentMatchAsync(
					user.userId.Value
					, match.imagedObjectId
					, (byte) match.catalogSide
					, match.textFragmentId
					, match.editionId
					, match.editionName
					, match.editionVolume
					, match.editionLocation1
					, match.editionLocation2
					, (byte) match.editionSide
					, match.comment
					, match.manuscriptName);

			return new NoContentResult();
		}

		public async Task<NoContentResult> ConfirmTextFragmentImagedObjectMatch(
				UserInfo user
				, uint   textFragmentImagedObjectMatchId
				, bool   confirm
				, string clientId = null)
		{
			CheckCatalogueEditRights(user);

			await _catalogueRepo.ConfirmImagedObjectTextFragmentMatchAsync(
					user.userId.Value
					, textFragmentImagedObjectMatchId
					, confirm);

			return new NoContentResult();
		}

		private static void CheckCatalogueEditRights(UserInfo user)
		{
			if (!user.userId.HasValue
				|| user.SystemRoles.All(x => x != UserSystemRoles.CATALOGUE_CURATOR))
				throw new StandardExceptions.NoSystemPermissionsException(user);
		}
	}
}
