using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;


namespace SQE.SqeHttpApi.Server.Services
{
    public interface IArtefactService
    {
        Task<ArtefactDTO> GetEditionArtefactAsync(UserInfo user, uint artefactId, bool withMask);
        Task<ArtefactListDTO> GetEditionArtefactListingsAsync(uint? userId, uint editionId, bool withMask = false,
            bool withImages = false);

        Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(uint? userId, uint editionId,
            bool withMask = false);

        Task<ArtefactDTO> UpdateArtefact(UserInfo user, uint editionId, uint artefactId, string mask = null,
            string name = null,
            string position = null);

        Task<ArtefactDTO> CreateArtefact(UserInfo user, uint editionId, uint masterImageId, string mask = null,
            string name = null, string position = null);
    }

    public class ArtefactService : IArtefactService
    {
        IArtefactRepository _artefactRepository;

        public ArtefactService(IArtefactRepository artefactRepository)
        {
            _artefactRepository = artefactRepository;
        }

        public async Task<ArtefactDTO> GetEditionArtefactAsync(UserInfo user, uint artefactId, bool withMask)
        {
            if (!user.EditionId().HasValue) return null;
            var artefact = await _artefactRepository.GetEditionArtefactAsync(user, artefactId, withMask);
            return new ArtefactDTO()
            {
                id = artefact.artefact_id,
                editionId = user.EditionId().Value,
                imagedObjectId = ImagedObjectIdFormat.Serialize(
                    artefact.institution,
                    artefact.catalog_number_1,
                    artefact.catalog_number_2),
                name = artefact.name,
                zOrder = 0,
                side = artefact.catalog_side == 0 ? ArtefactDTO.ArtefactSide.recto : ArtefactDTO.ArtefactSide.verso,
                mask = new PolygonDTO()
                {
                    mask = artefact.mask,
                    transformMatrix = ""
                }
            };
        }

        public async Task<ArtefactListDTO> GetEditionArtefactListingsAsync(uint? userId, uint editionId,
            bool withMask = false, bool withImages = false)
        {
            if (withImages)
                return await GetEditionArtefactListingsWithImagesAsync(userId, editionId, withMask);
            
            var listings = await _artefactRepository.GetEditionArtefactListAsync(userId, editionId, withMask);
            return ArtefactDTOTransform.QueryArtefactListToArtefactListDTO(listings.ToList(), editionId);
        }
        
        public async Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(uint? userId, uint editionId,
            bool withMask = false)
        {
            var artefactListings = await _artefactRepository.GetEditionArtefactListAsync(userId, editionId, withMask);
            var imagedObjectIds = artefactListings.Select(x => x.image_catalog_id);
            
            
            return ArtefactDTOTransform.QueryArtefactListToArtefactListDTO(artefactListings.ToList(), editionId);
        }

        public async Task<ArtefactDTO> UpdateArtefact(UserInfo user, uint editionId, uint artefactId, string mask = null,
            string name = null, string position = null)
        {
            var resultList = new List<AlteredRecord>();
            var withMask = false;
            if (user.userId.HasValue)
            {
                if (!string.IsNullOrEmpty(mask))
                {
                    resultList.AddRange(await _artefactRepository.UpdateArtefactShape(user, artefactId, mask));
                    withMask = true;
                }
                if (!string.IsNullOrEmpty(name))
                    resultList.AddRange(await _artefactRepository.UpdateArtefactName(user, artefactId, name));
                if (!string.IsNullOrEmpty(position))
                    resultList.AddRange(await _artefactRepository.UpdateArtefactPosition(user, artefactId, position));
            }
            
            

            return await GetEditionArtefactAsync(user, artefactId, withMask);
        }
        
        public async Task<ArtefactDTO> CreateArtefact(UserInfo user, uint editionId, uint masterImageId, string mask = null,
            string name = null, string position = null)
        {
            uint newArtefactId = 0;
            if (user.userId.HasValue)
            {
                newArtefactId = await _artefactRepository.CreateNewArtefact(user, editionId, masterImageId, mask, name, position);
            }

            return newArtefactId != 0 
                ? await GetEditionArtefactAsync(user, newArtefactId, mask != null)
                : null;
        }
    }
}
