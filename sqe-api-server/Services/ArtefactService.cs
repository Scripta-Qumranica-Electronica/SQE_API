using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.API.Server.RealtimeHubs;
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
        private readonly IHubContext<MainHub> _hubContext;

        public ArtefactService(IArtefactRepository artefactRepository, IHubContext<MainHub> hubContext)
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
            return ArtefactDTOTransformer.QueryArtefactToArtefactDTO(artefact, editionUser.EditionId);
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
                artefacts = ArtefactDTOTransformer.QueryArtefactListToArtefactListDTO(
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


            return ArtefactDTOTransformer.QueryArtefactListToArtefactListDTO(
                artefactListings.ToList(),
                editionUser.EditionId
            );
        }

        public async Task<ArtefactDTO> UpdateArtefactAsync(EditionUserInfo editionUser,
            uint artefactId,
            UpdateArtefactDTO updateArtefact,
            string clientId = null)
        {
            var withMask = false;
            var tasks = new List<Task<List<AlteredRecord>>>();
            if (!string.IsNullOrEmpty(updateArtefact.polygon.mask))
            {
                // UpdateArtefactShapeAsync will inform us if the WKT mask is in an invalid format
                tasks.Add(
                    _artefactRepository.UpdateArtefactShapeAsync(editionUser, artefactId, updateArtefact.polygon.mask)
                );
                withMask = true;
            }

            if (!string.IsNullOrEmpty(updateArtefact.name))
                tasks.Add(_artefactRepository.UpdateArtefactNameAsync(editionUser, artefactId, updateArtefact.name));

            if (updateArtefact.polygon != null)
                tasks.Add(
                    _artefactRepository.UpdateArtefactPositionAsync(
                        editionUser,
                        artefactId,
                        updateArtefact.polygon.transformation.scale,
                        updateArtefact.polygon.transformation.rotate,
                        updateArtefact.polygon.transformation.translate?.x,
                        updateArtefact.polygon.transformation.translate?.y
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
                .SendAsync("updateArtefact", updatedArtefact);

            return updatedArtefact;
        }

        public async Task<ArtefactDTO> CreateArtefactAsync(EditionUserInfo editionUser,
            CreateArtefactDTO createArtefact,
            string clientId = null)
        {
            uint newArtefactId = 0;
            if (editionUser.userId.HasValue)
                newArtefactId = await _artefactRepository.CreateNewArtefactAsync(
                    editionUser,
                    createArtefact.masterImageId,
                    createArtefact.polygon.mask,
                    createArtefact.name,
                    createArtefact.polygon.transformation?.scale,
                    createArtefact.polygon.transformation?.rotate,
                    createArtefact.polygon.transformation?.translate?.x,
                    createArtefact.polygon.transformation?.translate?.y,
                    createArtefact.statusMessage
                );

            var optional = string.IsNullOrEmpty(createArtefact.polygon.mask)
                ? new List<string>()
                : new List<string> { "masks" };

            var createArtefactnewArtefact = newArtefactId != 0
                ? await GetEditionArtefactAsync(editionUser, newArtefactId, optional)
                : null;

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .SendAsync("createArtefact", createArtefactnewArtefact);

            return createArtefactnewArtefact;
        }

        public async Task<NoContentResult> DeleteArtefactAsync(EditionUserInfo editionUser,
            uint artefactId,
            string clientId = null)
        {
            await _artefactRepository.DeleteArtefactAsync(editionUser, artefactId);

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .SendAsync(
                    "deleteArtefact",
                    new DeleteEditionEntityDTO { entityId = artefactId, editorId = editionUser.EditionEditorId.Value }
                );
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
                        x.EditionEditorId.GetValueOrDefault(),
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
                        x.EditionEditorId.GetValueOrDefault(),
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