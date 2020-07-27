using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Serialization;
using SQE.DatabaseAccess;

namespace SQE.API.Server.Services
{
    public interface ICatalogService
    {
        Task<CatalogueMatchListDTO> GetImagedObjectsOfTextFragment(uint textFragmentId);
        Task<CatalogueMatchListDTO> GetTextFragmentsOfImagedObject(string imagedObjectId);
        Task<CatalogueMatchListDTO> GetTextFragmentsAndImagedObjectsOfEdition(uint editionId);
        Task<CatalogueMatchListDTO> GetTextFragmentsAndImagedObjectsOfManuscript(uint manuscriptId);
        Task<NoContentResult> CreateTextFragmentImagedObjectMatch(uint? userId,
            CatalogueMatchInputDTO match, string clientId = null);
        Task<NoContentResult> ConfirmTextFragmentImagedObjectMatch(uint? userId, uint textFragmentImagedObjectMatchId,
            bool confirm, string clientId = null);
    }

    public class CatalogService : ICatalogService
    {
        private readonly ICatalogueRepository _catalogueRepo;
        private readonly IHubContext<MainHub, ISQEClient> _hubContext;

        public CatalogService(ICatalogueRepository catalogueRepository, IHubContext<MainHub, ISQEClient> hubContext)
        {
            _catalogueRepo = catalogueRepository;
            _hubContext = hubContext;
        }

        public async Task<CatalogueMatchListDTO> GetImagedObjectsOfTextFragment(uint textFragmentId)
        {
            return (await _catalogueRepo.GetImagedObjectMatchesForTextFragmentAsync(textFragmentId)).ToDTO();
        }

        public async Task<CatalogueMatchListDTO> GetTextFragmentsOfImagedObject(string imagedObjectId)
        {
            return (await _catalogueRepo.GetTextFragmentMatchesForImagedObjectAsync(
                System.Web.HttpUtility.UrlDecode(imagedObjectId))).ToDTO();
        }

        public async Task<CatalogueMatchListDTO> GetTextFragmentsAndImagedObjectsOfEdition(uint editionId)
        {
            return (await _catalogueRepo.GetImagedObjectAndTextFragmentMatchesForEditionAsync(editionId)).ToDTO();
        }

        public async Task<CatalogueMatchListDTO> GetTextFragmentsAndImagedObjectsOfManuscript(uint manuscriptId)
        {
            return (await _catalogueRepo.GetImagedObjectAndTextFragmentMatchesForManuscriptAsync(manuscriptId)).ToDTO();
        }

        public async Task<NoContentResult> CreateTextFragmentImagedObjectMatch(uint? userId,
            CatalogueMatchInputDTO match,
            string clientId = null)
        {
            if (userId.HasValue)
                await _catalogueRepo.CreateNewImagedObjectTextFragmentMatchAsync(userId.Value, match.imagedObjectId,
                    (byte)match.catalogSide, match.textFragmentId, match.editionId, match.editionName,
                    match.editionVolume, match.editionLocation1, match.editionLocation2, (byte)match.editionSide,
                    match.comment);
            return new NoContentResult();
        }

        public async Task<NoContentResult> ConfirmTextFragmentImagedObjectMatch(uint? userId,
            uint textFragmentImagedObjectMatchId,
            bool confirm,
            string clientId = null)
        {
            if (userId.HasValue)
                await _catalogueRepo.ConfirmImagedObjectTextFragmentMatchAsync(userId.Value,
                    textFragmentImagedObjectMatchId, confirm);
            return new NoContentResult();
        }
    }
}