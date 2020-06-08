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
        Task<ArtefactDTO> GetEditionArtefactAsync(EditionUserInfo editionUser, uint artefactId, List<string> optional);

        Task<ArtefactListDTO> GetEditionArtefactListingsAsync(EditionUserInfo editionUser,
            List<string> optional);

        Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(EditionUserInfo editionUser,
            bool withMask = false);

        Task<BatchUpdatedArtefactTransformDTO> BatchUpdateArtefactTransformAsync(EditionUserInfo editionUser,
            BatchUpdateArtefactPlacementDTO updates,
            string clientId = null);

        Task<ArtefactDTO> UpdateArtefactAsync(EditionUserInfo editionUser,
            uint artefactId,
            UpdateArtefactDTO updateArtefact,
            string clientId = null);

        Task<ArtefactDTO> CreateArtefactAsync(EditionUserInfo editionUser,
            CreateArtefactDTO createArtefact,
            string clientId = null);

        Task<NoContentResult> DeleteArtefactAsync(EditionUserInfo editionUser, uint artefactId, string clientId = null);

        Task<ArtefactTextFragmentMatchListDTO> ArtefactTextFragmentsAsync(EditionUserInfo editionUser,
            uint artefactId,
            List<string> optional);
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

        public async Task<ArtefactDTO> GetEditionArtefactAsync(EditionUserInfo editionUser,
            uint artefactId,
            List<string> optional)
        {
            ParseImageMaskOptionals(optional, out _, out var withMask);
            var artefact = await _artefactRepository.GetEditionArtefactAsync(editionUser, artefactId, withMask);
            return artefact.ToDTO(editionUser.EditionId);
        }

        public async Task<ArtefactListDTO> GetEditionArtefactListingsAsync(EditionUserInfo editionUser,
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
                    editionUser.EditionId
                );
            }
            return artefacts;
        }

        public async Task<ArtefactListDTO> GetEditionArtefactListingsWithImagesAsync(EditionUserInfo editionUser,
            bool withMask = false)
        {
            var artefactListings = await _artefactRepository.GetEditionArtefactListAsync(editionUser, withMask);
            //var imagedObjectIds = artefactListings.Select(x => x.ImageCatalogId);


            return ArtefactListSerializationDTO.QueryArtefactListToArtefactListDTO(
                artefactListings.ToList(),
                editionUser.EditionId
            );
        }

        public async Task<BatchUpdatedArtefactTransformDTO> BatchUpdateArtefactTransformAsync(EditionUserInfo editionUser,
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

            return new BatchUpdatedArtefactTransformDTO()
            {
                artefactPlacements = updatedArtefacts.Select(x => new UpdatedArtefactPlacementDTO()
                {
                    artefactId = x.id,
                    placementEditorId = x.artefactPlacementEditorId ?? 0,
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
        public async Task<ArtefactDTO> UpdateArtefactAsync(EditionUserInfo editionUser,
            uint artefactId,
            UpdateArtefactDTO updateArtefact,
            string clientId = null)
        {
            var withMask = false;
            var tasks = new List<Task<List<AlteredRecord>>>();
            if (!string.IsNullOrEmpty(updateArtefact.mask))
            {
                var cleanedPoly = await GeometryValidation.ValidatePolygonAsync(updateArtefact.mask, "artefact");
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

        public async Task<ArtefactDTO> CreateArtefactAsync(EditionUserInfo editionUser,
            CreateArtefactDTO createArtefact,
            string clientId = null)
        {
            var cleanedPoly = string.IsNullOrEmpty(createArtefact.mask)
                ? null
                : await GeometryValidation.ValidatePolygonAsync(createArtefact.mask, "artefact");

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

        public async Task<NoContentResult> DeleteArtefactAsync(EditionUserInfo editionUser,
            uint artefactId,
            string clientId = null)
        {
            await _artefactRepository.DeleteArtefactAsync(editionUser, artefactId);

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .DeletedArtefact(new DeleteDTO(EditionEntities.artefact, new List<uint>() { artefactId }));
            return new NoContentResult();
        }

        public async Task<ArtefactTextFragmentMatchListDTO> ArtefactTextFragmentsAsync(EditionUserInfo editionUser,
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

        private async Task<ArtefactTextFragmentMatchListDTO> _artefactSuggestedTextFragmentsAsync(
            EditionUserInfo editionUser,
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