using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;


namespace SQE.SqeHttpApi.Server.Services
{
    public interface IArtefactService
    {
        Task<ArtefactDTO> GetArtefact(uint scrollVersionId);
        Task<ArtefactListDTO> GetEditionArtefactListingsAsync(uint? userId, uint editionId, bool withMask = false);
        Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(uint? userId, uint editionId,
            bool withMask = false);

        Task<List<AlteredRecord>> UpdateArtefact(UserInfo user, uint editionId, uint artefactId, string mask = null, string name = null,
            string position = null);

        Task<uint> CreateArtefact(UserInfo user, uint editionId, uint masterImageId, string mask = null,
            string name = null, string position = null);
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

        public async Task<ArtefactListDTO> GetEditionArtefactListingsAsync(uint? userId, uint editionId,
            bool withMask = false)
        {
            var listings = await _artefactRepository.GetEditionArtefactListAsync(userId, editionId, withMask);
            return new ArtefactListDTO { result = listings.Select(x => new ArtefactDTO { 
                Id = x.artefact_id,
                EditionId = editionId,
                ImagedObjectId = x.institution + "-" + x.catalog_number_1 
                                 + (String.IsNullOrEmpty(x.catalog_number_2) ? "" : x.catalog_number_2),
                Mask = new PolygonDTO()
                {
                    mask = x.mask,
                },
                TransformMatrix = "",
                zOrder = 0,
                Name = x.name,
                Side = x.catalog_side == 0 ? ArtefactDTO.artSide.recto : ArtefactDTO.artSide.verso}
            ).ToList() };
        }
        
        public async Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(uint? userId, uint editionId,
            bool withMask = false)
        {
            var artefactListings = await _artefactRepository.GetEditionArtefactListAsync(userId, editionId, withMask);
            var imagedObjectIds = artefactListings.Select(x => x.image_catalog_id);
            
            
            return new ArtefactListDTO { result = artefactListings.Select(x => new ArtefactDTO { 
                Id = x.artefact_id,
                EditionId = editionId,
                ImagedObjectId = x.institution + "-" + x.catalog_number_1 
                                 + (string.IsNullOrEmpty(x.catalog_number_2) ? "" : x.catalog_number_2),
                Mask = new PolygonDTO()
                {
                    mask = x.mask,
                },
                TransformMatrix = "",
                zOrder = 0,
                Name = x.name,
                Side = x.catalog_side == 0 ? ArtefactDTO.artSide.recto : ArtefactDTO.artSide.verso}
            ).ToList() };
        }

        public async Task<List<AlteredRecord>> UpdateArtefact(UserInfo user, uint editionId, uint artefactId, string mask = null,
            string name = null, string position = null)
        {
            var resultList = new List<AlteredRecord>();
            if (user.userId.HasValue)
            {
                if (!string.IsNullOrEmpty(mask))
                    resultList.AddRange(await _artefactRepository.UpdateArtefactShape(user, editionId, artefactId, mask));
                if (!string.IsNullOrEmpty(name))
                    resultList.AddRange(await _artefactRepository.UpdateArtefactName(user, editionId, artefactId, name));
                if (!string.IsNullOrEmpty(position))
                    resultList.AddRange(await _artefactRepository.UpdateArtefactPosition(user, editionId, artefactId, position));
            }

            return resultList;
        }
        
        public async Task<uint> CreateArtefact(UserInfo user, uint editionId, uint masterImageId, string mask = null,
            string name = null, string position = null)
        {
            uint newArtefactId = 0;
            if (user.userId.HasValue)
            {
                newArtefactId = await _artefactRepository.CreateNewArtefact(user, editionId, masterImageId, mask, name, position);
            }

            return newArtefactId;
        }
    }
}
