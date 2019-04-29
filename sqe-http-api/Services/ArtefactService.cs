using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.Server.DTOs;


namespace SQE.SqeHttpApi.Server.Services
{
    public interface IArtefactService
    {
        Task<ArtefactDTO> GetArtefact(uint scrollVersionId);
        Task<ArtefactListDTO> GetEditionArtefactListings(uint? userId, uint scrollVersionId);
        Task<ArtefactListDTO> GetEditionArtefactListingsWithImages(uint? userId, uint editionId);
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

        public async Task<ArtefactListDTO> GetEditionArtefactListings(uint? userId, uint scrollVersionId)
        {
            var listings = await _artefactRepository.GetEditionArtefactList(userId, scrollVersionId);
            return new ArtefactListDTO { result = listings.Select(x => new ArtefactDesignationDTO { 
                ArtefactId = x.artefact_id,
                ImageCatalogId = x.image_catalog_id,
                Name = x.name,
                Side = x.catalog_side == 0 ? "recto" : "verso"}
            ).ToList() };
        }
        
        public async Task<ArtefactListDTO> GetEditionArtefactListingsWithImages(uint? userId, uint editionId)
        {
            var artefactListings = await _artefactRepository.GetEditionArtefactList(userId, editionId);
            var imagedObjectIds = artefactListings.Select(x => x.image_catalog_id);
            
            
            return new ArtefactListDTO { result = artefactListings.Select(x => new ArtefactDesignationDTO { 
                ArtefactId = x.artefact_id,
                ImageCatalogId = x.image_catalog_id,
                Name = x.name,
                Side = x.catalog_side == 0 ? "recto" : "verso"}
            ).ToList() };
        }
    }
}
