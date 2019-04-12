using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.Backend.Server.DTOs;
using SQE.Backend.DataAccess;



namespace SQE.Backend.Server.Services
{
    public interface IArtefactService
    {
        Task<ArtefactDTO> GetArtefact(uint scrollVersionId);
        Task<ArtefactListDTO> GetScrollArtefactListings(uint? userId, uint scrollVersionId);
        Task<ArtefactListDTO> GetScrollArtefactListingsWithImages(uint? userId, uint scrollVersionId);
    }

    public class ArtefactService : IArtefactService
    {
        IArtefactRepository _artefactRepository;

        public ArtefactService(IArtefactRepository artefactRepository)
        {
            _artefactRepository = artefactRepository;
        }

        public Task<ArtefactDTO> GetArtefact(uint scrollVersionId)
        {

            throw new NotImplementedException();
        }

        public async Task<ArtefactListDTO> GetScrollArtefactListings(uint? userId, uint scrollVersionId)
        {
            var listings = await _artefactRepository.GetScrollArtefactList(userId, scrollVersionId);
            return new ArtefactListDTO { result = listings.Select(x => new ArtefactDesignationDTO { 
                ArtefactId = x.artefact_id,
                ImageCatalogId = x.image_catalog_id,
                Name = x.name,
                Side = x.catalog_side == 0 ? "recto" : "verso"}
            ).ToList() };
        }
        
        public async Task<ArtefactListDTO> GetScrollArtefactListingsWithImages(uint? userId, uint scrollVersionId)
        {
            var listings = await _artefactRepository.GetScrollArtefactList(userId, scrollVersionId);
            var imageCatalogIds = listings.Select(x => x.image_catalog_id);
            
            
            return new ArtefactListDTO { result = listings.Select(x => new ArtefactDesignationDTO { 
                ArtefactId = x.artefact_id,
                ImageCatalogId = x.image_catalog_id,
                Name = x.name,
                Side = x.catalog_side == 0 ? "recto" : "verso"}
            ).ToList() };
        }
    }
}
