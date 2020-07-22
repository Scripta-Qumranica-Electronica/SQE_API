using System.Threading.Tasks;
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
            return (await _catalogueRepo.GetTextFragmentMatchesForImagedObjectAsync(System.Web.HttpUtility.UrlDecode(imagedObjectId))).ToDTO();
        }
        public async Task<CatalogueMatchListDTO> GetTextFragmentsAndImagedObjectsOfEdition(uint editionId)
        {
            return (await _catalogueRepo.GetImagedObjectAndTextFragmentMatchesForEditionAsync(editionId)).ToDTO();
        }
        public async Task<CatalogueMatchListDTO> GetTextFragmentsAndImagedObjectsOfManuscript(uint manuscriptId)
        {
            return (await _catalogueRepo.GetImagedObjectAndTextFragmentMatchesForManuscriptAsync(manuscriptId)).ToDTO();
        }
    }
}