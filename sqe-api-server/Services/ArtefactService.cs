using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Serialization;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
    public interface IArtefactService
    {
        Task<ArtefactDTO> GetEditionArtefactAsync(UserInfo editionUser, uint artefactId, List<string> optional);

        Task<ArtefactListDTO> GetEditionArtefactListingsAsync(UserInfo editionUser,
            List<string> optional);

        Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(UserInfo editionUser,
            bool withMask = false);

        Task<BatchUpdatedArtefactTransformDTO> BatchUpdateArtefactTransformAsync(UserInfo editionUser,
            BatchUpdateArtefactPlacementDTO updates,
            string clientId = null);

        Task<ArtefactDTO> UpdateArtefactAsync(UserInfo editionUser,
            uint artefactId,
            UpdateArtefactDTO updateArtefact,
            string clientId = null);

        Task<ArtefactDTO> CreateArtefactAsync(UserInfo editionUser,
            CreateArtefactDTO createArtefact,
            string clientId = null);

        Task<NoContentResult> DeleteArtefactAsync(UserInfo editionUser, uint artefactId, string clientId = null);

        Task<ArtefactTextFragmentMatchListDTO> ArtefactTextFragmentsAsync(UserInfo editionUser,
            uint artefactId,
            List<string> optional);

        Task<ArtefactGroupListDTO> ArtefactGroupsOfEditionAsync(UserInfo editionUser);

        Task<ArtefactGroupDTO> GetArtefactGroupDataAsync(UserInfo editionUser, uint artefactGroupId);

        Task<ArtefactGroupDTO> CreateArtefactGroupAsync(UserInfo editionUser,
            CreateArtefactGroupDTO artefactGroup, string clientId = null);

        Task<ArtefactGroupDTO> UpdateArtefactGroupAsync(UserInfo editionUser, uint artefactGroupId,
            UpdateArtefactGroupDTO artefactGroup, string clientId = null);

        Task<DeleteDTO> DeleteArtefactGroupAsync(UserInfo editionUser, uint artefactGroupId,
            string clientId = null);
    }

    public class ArtefactService : IArtefactService
    {
        private readonly IArtefactRepository _artefactRepository;
        private readonly IHubContext<MainHub, ISQEClient> _hubContext;

        public ArtefactService(IArtefactRepository artefactRepository, IHubContext<MainHub, ISQEClient> hubContext)
        {
            _artefactRepository = artefactRepository;
            _hubContext = hubContext;
        }

        public async Task<ArtefactDTO> GetEditionArtefactAsync(UserInfo editionUser,
            uint artefactId,
            List<string> optional)
        {
            ParseImageMaskOptionals(optional, out _, out var withMask);
            var artefact = await _artefactRepository.GetEditionArtefactAsync(editionUser, artefactId, withMask);
            return artefact.ToDTO(editionUser.EditionId.Value);
        }

        public async Task<ArtefactListDTO> GetEditionArtefactListingsAsync(UserInfo editionUser,
            List<string> optional)
        {
            ParseImageMaskOptionals(optional, out var withImages, out var withMask);
            ArtefactListDTO artefacts;
            if (withImages)
            {
                artefacts = await GetEditionArtefactListingsWithImagesAsync(editionUser, withMask);
            }
            else
            {
                var listings = await _artefactRepository.GetEditionArtefactListAsync(editionUser, withMask);
                artefacts = ArtefactListSerializationDTO.QueryArtefactListToArtefactListDTO(
                    listings.ToList(),
                    editionUser.EditionId.Value
                );
            }

            return artefacts;
        }

        public async Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(UserInfo editionUser,
            bool withMask = false)
        {
            var artefactListings = await _artefactRepository.GetEditionArtefactListAsync(editionUser, withMask);
            //var imagedObjectIds = artefactListings.Select(x => x.ImageCatalogId);


            return ArtefactListSerializationDTO.QueryArtefactListToArtefactListDTO(
                artefactListings.ToList(),
                editionUser.EditionId.Value
            );
        }

        public async Task<BatchUpdatedArtefactTransformDTO> BatchUpdateArtefactTransformAsync(UserInfo editionUser,
            BatchUpdateArtefactPlacementDTO updates,
            string clientId = null)
        {
            await _artefactRepository.BatchUpdateArtefactPositionAsync(editionUser, updates.artefactPlacements);

            // Collect the updated artefacts
            var updatedArtefacts = await Task.WhenAll(
                updates.artefactPlacements
                    .Select(async x => await GetEditionArtefactAsync(editionUser, x.artefactId, new List<string>()))
            );

            // Create the tasks to broadcast the change to all subscribers of the editionId.
            // Exclude the client (not the user), which made the request, that client directly received the response.
            var broadcastTasks = updatedArtefacts
                .Select(x =>
                    _hubContext.Clients
                        .GroupExcept(editionUser.EditionId.ToString(), clientId)
                        .UpdatedArtefact(x));

            // Wait for all tasks to finish before returning (otherwise the threads may get lost)
            await Task.WhenAll(broadcastTasks);

            return new BatchUpdatedArtefactTransformDTO
            {
                artefactPlacements = updatedArtefacts.Select(x => new UpdatedArtefactPlacementDTO
                {
                    artefactId = x.id,
                    placementEditorId = x.artefactPlacementEditorId ?? 0,
                    isPlaced = x.isPlaced,
                    placement = x.placement
                }).ToList()
            };
        }

        // NOTE: This function offers many possibilities for updating an artefact. It could
        // happen that this is abused, and, for example, people send the entire mask along when
        // they are only trying to change only the z-Index. Such a situation would result in a lot
        // of extra bandwidth usage and checking (the system does check to see if the mask has
        // actually changed). If such is the case, consider breaking up the artefact update
        // endpoint into several distinct endpoints, for example: one for name, another for 
        // position, and another for mask.
        public async Task<ArtefactDTO> UpdateArtefactAsync(UserInfo editionUser,
            uint artefactId,
            UpdateArtefactDTO updateArtefact,
            string clientId = null)
        {
            var withMask = false;
            var tasks = new List<Task<List<AlteredRecord>>>();
            if (!string.IsNullOrEmpty(updateArtefact.mask))
            {
                var cleanedPoly = GeometryValidation.ValidatePolygon(updateArtefact.mask, "artefact");
                tasks.Add(
                    _artefactRepository.UpdateArtefactShapeAsync(editionUser, artefactId, cleanedPoly)
                );
                withMask = true;
            }

            if (!string.IsNullOrEmpty(updateArtefact.name))
                tasks.Add(_artefactRepository.UpdateArtefactNameAsync(editionUser, artefactId, updateArtefact.name));

            if (updateArtefact.placement != null)
                tasks.Add(
                    _artefactRepository.UpdateArtefactPositionAsync(
                        editionUser,
                        artefactId,
                        updateArtefact.placement.scale,
                        updateArtefact.placement.rotate,
                        updateArtefact.placement.translate?.x,
                        updateArtefact.placement.translate?.y,
                        updateArtefact.placement.zIndex
                    )
                );

            if (!string.IsNullOrEmpty(updateArtefact.statusMessage))
                tasks.Add(
                    _artefactRepository.UpdateArtefactStatusAsync(editionUser, artefactId, updateArtefact.statusMessage)
                );

            await Task.WhenAll(tasks);
            var updatedArtefact = await GetEditionArtefactAsync(
                editionUser,
                artefactId,
                withMask ? new List<string> { "masks" } : null
            );

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .UpdatedArtefact(updatedArtefact);

            return updatedArtefact;
        }

        public async Task<ArtefactDTO> CreateArtefactAsync(UserInfo editionUser,
            CreateArtefactDTO createArtefact,
            string clientId = null)
        {
            var cleanedPoly = string.IsNullOrEmpty(createArtefact.mask)
                ? null
                : GeometryValidation.ValidatePolygon(createArtefact.mask, "artefact");

            var newArtefact = await _artefactRepository.CreateNewArtefactAsync(
                editionUser,
                createArtefact.masterImageId,
                cleanedPoly,
                createArtefact.name,
                createArtefact.placement?.scale,
                createArtefact.placement?.rotate,
                createArtefact.placement?.translate?.x,
                createArtefact.placement?.translate?.y,
                createArtefact.placement?.zIndex,
                createArtefact.statusMessage
            );

            var optional = string.IsNullOrEmpty(createArtefact.mask)
                ? new List<string>()
                : new List<string> { "masks" };

            var newlyCreatedArtefact = await GetEditionArtefactAsync(editionUser, newArtefact, optional);

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .CreatedArtefact(newlyCreatedArtefact);

            return newlyCreatedArtefact;
        }

        public async Task<NoContentResult> DeleteArtefactAsync(UserInfo editionUser,
            uint artefactId,
            string clientId = null)
        {
            await _artefactRepository.DeleteArtefactAsync(editionUser, artefactId);

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .DeletedArtefact(new DeleteDTO(EditionEntities.artefact, new List<uint> { artefactId }));
            return new NoContentResult();
        }

        public async Task<ArtefactTextFragmentMatchListDTO> ArtefactTextFragmentsAsync(UserInfo editionUser,
            uint artefactId,
            List<string> optional)
        {
            ParseTextFragmentOptionals(optional, out var suggestedResults);

            var realMatches = new ArtefactTextFragmentMatchListDTO(
                (await _artefactRepository.ArtefactTextFragmentsAsync(editionUser, artefactId))
                .Select(
                    x => new ArtefactTextFragmentMatchDTO(
                        x.TextFragmentId.GetValueOrDefault(),
                        x.TextFragmentName,
                        x.TextFragmentEditorId.GetValueOrDefault(),
                        false
                    )
                )
                .ToList()
            );
            if (!suggestedResults) return realMatches;

            var suggestedMatches = await _artefactSuggestedTextFragmentsAsync(editionUser, artefactId);
            realMatches.textFragments.AddRange(suggestedMatches.textFragments);
            return realMatches;
        }

        public async Task<ArtefactGroupListDTO> ArtefactGroupsOfEditionAsync(UserInfo editionUser)
        {
            return (await _artefactRepository.ArtefactGroupsOfEditionAsync(editionUser)).ToDTO();
        }

        public async Task<ArtefactGroupDTO> GetArtefactGroupDataAsync(UserInfo editionUser, uint artefactGroupId)
        {
            return (await _artefactRepository.GetArtefactGroupAsync(editionUser, artefactGroupId)).ToDTO();
        }

        public async Task<ArtefactGroupDTO> CreateArtefactGroupAsync(UserInfo editionUser,
            CreateArtefactGroupDTO artefactGroup,
            string clientId = null)
        {
            var results = (await _artefactRepository.CreateArtefactGroupAsync(editionUser, artefactGroup.name,
                artefactGroup.artefacts)).ToDTO();
            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .CreatedArtefactGroup(results);
            return results;
        }

        public async Task<ArtefactGroupDTO> UpdateArtefactGroupAsync(UserInfo editionUser, uint artefactGroupId,
            UpdateArtefactGroupDTO artefactGroup,
            string clientId = null)
        {
            var results = (await _artefactRepository.UpdateArtefactGroupAsync(editionUser, artefactGroupId,
                artefactGroup.name,
                artefactGroup.artefacts)).ToDTO();
            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .UpdatedArtefactGroup(results);
            return results;
        }

        public async Task<DeleteDTO> DeleteArtefactGroupAsync(UserInfo editionUser, uint artefactGroupId,
            string clientId = null)
        {
            await _artefactRepository.DeleteArtefactGroupAsync(editionUser, artefactGroupId);
            var results = new DeleteDTO(EditionEntities.artefactGroup, artefactGroupId);
            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .DeletedArtefactGroup(results);
            return results;
        }

        private async Task<ArtefactTextFragmentMatchListDTO> _artefactSuggestedTextFragmentsAsync(
            UserInfo editionUser,
            uint artefactId)
        {
            return new ArtefactTextFragmentMatchListDTO(
                (await _artefactRepository.ArtefactSuggestedTextFragmentsAsync(editionUser, artefactId))
                .Select(
                    x => new ArtefactTextFragmentMatchDTO(
                        x.TextFragmentId.GetValueOrDefault(),
                        x.TextFragmentName,
                        x.TextFragmentEditorId.GetValueOrDefault(),
                        true)
                )
                .ToList()
            );
        }

        private static void ParseImageMaskOptionals(List<string> optionals, out bool images, out bool masks)
        {
            images = masks = false;
            if (optionals == null)
                return;
            images = optionals.Contains("images");
            masks = optionals.Contains("masks");
        }

        private static void ParseTextFragmentOptionals(List<string> optionals, out bool suggestedResults)
        {
            suggestedResults = optionals.Contains("suggested");
        }
    }
}