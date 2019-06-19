using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.SqeHttpApi.Server.Helpers
{
    public interface IArtefactService
    {
        Task<ArtefactDTO> GetEditionArtefactAsync(UserInfo user, uint artefactId, bool withMask);
        Task<ArtefactListDTO> GetEditionArtefactListingsAsync(uint? userId, uint editionId, bool withMask = false,
            bool withImages = false);

        Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(uint? userId, uint editionId,
            bool withMask = false);

        Task<ArtefactDTO> UpdateArtefactAsync(UserInfo user, uint editionId, uint artefactId, string mask = null,
            string name = null,
            string position = null);

        Task<ArtefactDTO> CreateArtefactAsync(UserInfo user, uint editionId, uint masterImageId, string mask = null,
            string name = null, string position = null);

        Task DeleteArtefactAsync(UserInfo user, uint artefactId);
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
            if (!user.editionId.HasValue) 
                return null;
            var artefact = await _artefactRepository.GetEditionArtefactAsync(user, artefactId, withMask);
            return ArtefactDTOTransformer.QueryArtefactToArtefactDTO(artefact, user.editionId.Value);
        }

        public async Task<ArtefactListDTO> GetEditionArtefactListingsAsync(uint? userId, uint editionId,
            bool withMask = false, bool withImages = false)
        {
            ArtefactListDTO artefacts;
            if (withImages)
                artefacts = await GetEditionArtefactListingsWithImagesAsync(userId, editionId, withMask);
            else
            {
                var listings = await _artefactRepository.GetEditionArtefactListAsync(userId, editionId, withMask);
                artefacts = ArtefactDTOTransformer.QueryArtefactListToArtefactListDTO(listings.ToList(), editionId);
            }
            
            return artefacts;
        }
        
        public async Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(uint? userId, uint editionId,
            bool withMask = false)
        {
            var artefactListings = await _artefactRepository.GetEditionArtefactListAsync(userId, editionId, withMask);
            var imagedObjectIds = artefactListings.Select(x => x.ImageCatalogId);
            
            
            return ArtefactDTOTransformer.QueryArtefactListToArtefactListDTO(artefactListings.ToList(), editionId);
        }

        public async Task<ArtefactDTO> UpdateArtefactAsync(UserInfo user, uint editionId, uint artefactId, string mask = null,
            string name = null, string position = null)
        {
            var withMask = false;
            var resultList = new List<AlteredRecord>();
            if (user.userId.HasValue)
            {
                if (!string.IsNullOrEmpty(mask))
                {
                    // UpdateArtefactShapeAsync will inform us if the WKT mask is in an invalid format
                    resultList.AddRange(await _artefactRepository.UpdateArtefactShapeAsync(user, artefactId, mask));
                    withMask = true;
                }

                if (!string.IsNullOrEmpty(name))
                    resultList.AddRange(await _artefactRepository.UpdateArtefactNameAsync(user, artefactId, name));
                
                if (!string.IsNullOrEmpty(position)) // TODO: we should allow for null here, which would mean we need to delete the position from the edition
                {
                    if (!GeometryValidation.ValidateTransformMatrix(position))
                        throw StandardErrors.ImproperInputData("artefact_position");
                    resultList.AddRange(
                        await _artefactRepository.UpdateArtefactPositionAsync(user, artefactId, position));
                }  
            }
            
            return await GetEditionArtefactAsync(user, artefactId, withMask);
        }
        
        public async Task<ArtefactDTO> CreateArtefactAsync(UserInfo user, uint editionId, uint masterImageId, string mask = null,
            string name = null, string position = null)
        {
            uint newArtefactId = 0;
            if (user.userId.HasValue)
            {
                newArtefactId = await _artefactRepository.CreateNewArtefactAsync(user, editionId, masterImageId, mask, name, position);
            }

            return newArtefactId != 0 
                ? await GetEditionArtefactAsync(user, newArtefactId, mask != null)
                : null;
        }

        public async Task DeleteArtefactAsync(UserInfo user, uint artefactId)
        {
            await _artefactRepository.DeleteArtefactAsync(user, artefactId);
        }
    }
}
