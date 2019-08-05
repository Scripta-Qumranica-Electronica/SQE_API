﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SQE.SqeApi.DataAccess;
using SQE.SqeApi.DataAccess.Helpers;
using SQE.SqeApi.DataAccess.Models;
using SQE.SqeApi.Server.DTOs;
using SQE.SqeApi.Server.Helpers;
using SQE.SqeApi.Server.Hubs;

namespace SQE.SqeApi.Server.Services
{
    public interface IArtefactService
    {
        Task<ArtefactDTO> GetEditionArtefactAsync(UserInfo user, uint artefactId, List<string> optional);
        Task<ArtefactListDTO> GetEditionArtefactListingsAsync(uint? userId, uint editionId, List<string> optional);

        Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(uint? userId, uint editionId,
            bool withMask = false);

        Task<ArtefactDTO> UpdateArtefactAsync(UserInfo user, uint editionId, uint artefactId, string mask = null,
            string name = null, string position = null, string clientId = null);

        Task<ArtefactDTO> CreateArtefactAsync(UserInfo user, uint editionId, uint masterImageId, string mask = null,
            string name = null, string position = null, string clientId = null);

        Task<NoContentResult> DeleteArtefactAsync(UserInfo user, uint artefactId, string clientId = null);
    }

    public class ArtefactService : IArtefactService
    {
        private readonly IArtefactRepository _artefactRepository;
        private readonly IHubContext<MainHub> _hubContext;

        public ArtefactService(IArtefactRepository artefactRepository, IHubContext<MainHub> hubContext)
        {
            _artefactRepository = artefactRepository;
            _hubContext = hubContext;
        }

        public async Task<ArtefactDTO> GetEditionArtefactAsync(UserInfo user, uint artefactId, List<string> optional)
        {
            ParseOptionals(optional, out _, out var withMask);
            if (!user.editionId.HasValue) 
                return null;
            var artefact = await _artefactRepository.GetEditionArtefactAsync(user, artefactId, withMask);
            return ArtefactDTOTransformer.QueryArtefactToArtefactDTO(artefact, user.editionId.Value);
        }

        public async Task<ArtefactListDTO> GetEditionArtefactListingsAsync(uint? userId, uint editionId,
            List<string> optional)
        {
            ParseOptionals(optional, out var withImages, out var withMask);
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
            string name = null, string position = null, string clientId = null)
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
                        throw new StandardErrors.ImproperInputData("artefact position");
                    resultList.AddRange(
                        await _artefactRepository.UpdateArtefactPositionAsync(user, artefactId, position));
                }  
            }
            
            var updatedArtefact = await GetEditionArtefactAsync(user, artefactId, 
                withMask ? new List<string>(){"masks"} : null);

            await _hubContext.Clients.GroupExcept(editionId.ToString(), clientId)
                .SendAsync("updateArtefact", updatedArtefact);
            return updatedArtefact;
        }
        
        public async Task<ArtefactDTO> CreateArtefactAsync(UserInfo user, uint editionId, uint masterImageId, string mask = null,
            string name = null, string position = null, string clientId = null)
        {
            uint newArtefactId = 0;
            if (user.userId.HasValue)
            {
                if (!string.IsNullOrEmpty(position) && !GeometryValidation.ValidateTransformMatrix(position))
                    throw new StandardErrors.ImproperInputData("artefact position");
                newArtefactId = await _artefactRepository.CreateNewArtefactAsync(user, editionId, masterImageId, 
                        mask, name, position);
            }
            
            var optional = mask != null ? new List<string>(){"masks"} : new List<string>();

            var newArtefact = newArtefactId != 0 
                ? await GetEditionArtefactAsync(user, newArtefactId, optional)
                : null;
            await _hubContext.Clients.GroupExcept(editionId.ToString(), clientId)
                .SendAsync("createArtefact", newArtefact);
            return newArtefact;
        }

        public async Task<NoContentResult> DeleteArtefactAsync(UserInfo user, uint artefactId, string clientId = null)
        {
            await _artefactRepository.DeleteArtefactAsync(user, artefactId);
            await _hubContext.Clients.GroupExcept(user.editionId.Value.ToString(), clientId)
                .SendAsync("deleteArtefact", artefactId);
            return new NoContentResult();
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