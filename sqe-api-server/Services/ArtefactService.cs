using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Simplify;
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

            // var wkr = new WKTReader();
            // var wkw = new WKTWriter();
            // artefacts.artefacts = artefacts.artefacts.Select(x =>
            //     {
            //         x.mask.mask = wkw.Write(DouglasPeuckerSimplifier.Simplify(wkr.Read(x.mask.mask), 10)).Replace(", ", ",").Replace("POLYGON (", "POLYGON(");
            //         return x;
            //     }
            // ).ToList();
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
                var cleanedPoly = await GeometryValidation.ValidatePolygonAsync(updateArtefact.polygon.mask, "artefact");
                tasks.Add(
                    _artefactRepository.UpdateArtefactShapeAsync(editionUser, artefactId, cleanedPoly)
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
                .UpdatedArtefact(updatedArtefact);

            return updatedArtefact;
        }

        public async Task<ArtefactDTO> CreateArtefactAsync(EditionUserInfo editionUser,
            CreateArtefactDTO createArtefact,
            string clientId = null)
        {
            var cleanedPoly = string.IsNullOrEmpty(createArtefact.polygon.mask)
                ? null
                : await GeometryValidation.ValidatePolygonAsync(createArtefact.polygon.mask, "artefact");

            var newArtefact = await _artefactRepository.CreateNewArtefactAsync(
                editionUser,
                createArtefact.masterImageId,
                cleanedPoly,
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