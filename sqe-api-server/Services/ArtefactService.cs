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
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services;

public interface IArtefactService
{
	Task<ArtefactDTO> GetEditionArtefactAsync(
			UserInfo       editionUser
			, uint         artefactId
			, List<string> optional);

	Task<ExtendedArtefactListDTO> GetEditionArtefactListingsAsync(
			UserInfo       editionUser
			, List<string> optional);

	Task<BatchUpdatedArtefactTransformDTO> BatchUpdateArtefactTransformAsync(
			UserInfo                          editionUser
			, BatchUpdateArtefactPlacementDTO updates
			, string                          clientId = null);

	Task<ArtefactDTO> UpdateArtefactAsync(
			UserInfo            editionUser
			, uint              artefactId
			, UpdateArtefactDTO updateArtefact
			, string            clientId = null
			, string            operationId = null);

	Task<ArtefactDTO> CreateArtefactAsync(
			UserInfo            editionUser
			, CreateArtefactDTO createArtefact
			, string            clientId = null);

	Task<NoContentResult> DeleteArtefactAsync(
			UserInfo editionUser
			, uint   artefactId
			, string clientId = null);

	Task<ArtefactTextFragmentMatchListDTO> ArtefactTextFragmentsAsync(
			UserInfo       editionUser
			, uint         artefactId
			, List<string> optional);

	Task<ArtefactGroupListDTO> ArtefactGroupsOfEditionAsync(UserInfo editionUser);

	Task<ArtefactGroupDTO> GetArtefactGroupDataAsync(UserInfo editionUser, uint artefactGroupId);

	Task<ArtefactGroupDTO> CreateArtefactGroupAsync(
			UserInfo                 editionUser
			, CreateArtefactGroupDTO artefactGroup
			, string                 clientId = null);

	Task<ArtefactGroupDTO> UpdateArtefactGroupAsync(
			UserInfo                 editionUser
			, uint                   artefactGroupId
			, UpdateArtefactGroupDTO artefactGroup
			, string                 clientId = null);

	Task<DeleteIntIdDTO> DeleteArtefactGroupAsync(
			UserInfo editionUser
			, uint   artefactGroupId
			, string clientId = null);
}

public class ArtefactService : IArtefactService
{
	private readonly IArtefactRepository              _artefactRepository;
	private readonly IHubContext<MainHub, ISQEClient> _hubContext;
	private readonly IImagedObjectRepository          _imagedObjectRepository;
	private readonly IImageRepository                 _imageRepository;

	public ArtefactService(
			IImagedObjectRepository            imagedObjectRepository
			, IArtefactRepository              artefactRepository
			, IImageRepository                 imageRepository
			, IHubContext<MainHub, ISQEClient> hubContext)
	{
		_imagedObjectRepository = imagedObjectRepository;
		_artefactRepository = artefactRepository;
		_imageRepository = imageRepository;
		_hubContext = hubContext;
	}

	public async Task<ArtefactDTO> GetEditionArtefactAsync(
			UserInfo       editionUser
			, uint         artefactId
			, List<string> optional)
	{
		ParseImageMaskOptionals(optional, out _, out var withMask);

		var artefact =
				await _artefactRepository.GetEditionArtefactAsync(
						editionUser
						, artefactId
						, withMask);

		return artefact.ToDTO(editionUser.EditionId.Value);
	}

	public async Task<ExtendedArtefactListDTO> GetEditionArtefactListingsAsync(
			UserInfo       editionUser
			, List<string> optional)
	{
		ParseImageMaskOptionals(optional, out var withImages, out var withMask);

		var listings =
				(await _artefactRepository.GetEditionArtefactListAsync(editionUser, withMask))
				.ToList();

		var baseList = ArtefactListSerializationDTO.QueryArtefactListToArtefactListDTO(
				listings
				, editionUser.EditionId.Value);

		// When images are requested, fetch every edition image once and keep only
		// the master per (imaged object, side) so each artefact can carry its
		// master image URL + IIIF manifest (letting the client render the
		// artefacts view without loading the edition's imaged objects).
		Dictionary<(string, string), Image> masterImages = null;

		if (withImages)
			masterImages =
					(await _imageRepository.GetImagesAsync(editionUser, null))
					.Where(i => i.Master)
					.GroupBy(i => (i.ObjectId, i.Side))
					.ToDictionary(g => g.Key, g => g.First());

		var artefacts = baseList.artefacts.Select(a =>
		{
			var dto = ToExtendedArtefactDTO(a);

			if (masterImages != null)
			{
				var side = a.side == SideDesignation.recto
						? "recto"
						: "verso";

				if (masterImages.TryGetValue((a.imagedObjectId, side), out var image))
				{
					dto.url = image.URL;
					dto.imageManifest = image.ImageManifest;
					dto.ppi = image.PPI;
				}
			}

			return dto;
		}).ToList();

		return new ExtendedArtefactListDTO { artefacts = artefacts };
	}

	private static ExtendedArtefactDTO ToExtendedArtefactDTO(ArtefactDTO a) =>
			new ExtendedArtefactDTO
			{
					id = a.id
					, name = a.name
					, editionId = a.editionId
					, imagedObjectId = a.imagedObjectId
					, imageId = a.imageId
					, artefactDataEditorId = a.artefactDataEditorId
					, mask = a.mask
					, artefactMaskEditorId = a.artefactMaskEditorId
					, isPlaced = a.isPlaced
					, placement = a.placement
					, artefactPlacementEditorId = a.artefactPlacementEditorId
					, side = a.side
					, statusMessage = a.statusMessage
					,
			};

	public async Task<BatchUpdatedArtefactTransformDTO> BatchUpdateArtefactTransformAsync(
			UserInfo                          editionUser
			, BatchUpdateArtefactPlacementDTO updates
			, string                          clientId = null)
	{
		await _artefactRepository.BatchUpdateArtefactPositionAsync(
				editionUser
				, updates.artefactPlacements);

		// Collect the updated artefacts
		var updatedArtefacts = new List<ArtefactDTO>(updates.artefactPlacements.Count);

		foreach (var artefactPlacement in updates.artefactPlacements)
		{
			updatedArtefacts.Add(
					await GetEditionArtefactAsync(editionUser, artefactPlacement.artefactId, []));
		}

		// Create the tasks to broadcast the change to all subscribers of the editionId.
		// Exclude the client (not the user), which made the request, that client directly received the response.
		var broadcastTasks =
				updatedArtefacts.Select(x => _hubContext.Clients
														.GroupExcept(
																editionUser.EditionId.ToString()
																, clientId)
														.UpdatedArtefact(x));

		// Wait for all tasks to finish before returning (otherwise the threads may get lost)
		await Task.WhenAll(broadcastTasks);

		return new BatchUpdatedArtefactTransformDTO
		{
				artefactPlacements = updatedArtefacts
									 .Select(x => new UpdatedArtefactPlacementDTO
									 {
											 artefactId = x.id
											 , placementEditorId =
													 x.artefactPlacementEditorId
													 ?? 0
											 , isPlaced = x.isPlaced
											 , placement = x.placement
											 ,
									 })
									 .ToList()
				,
		};
	}

	// NOTE: This function offers many possibilities for updating an artefact. It could
	// happen that this is abused, and, for example, people send the entire mask along when
	// they are only trying to change only the z-Index. Such a situation would result in a lot
	// of extra bandwidth usage and checking (the system does check to see if the mask has
	// actually changed). If such is the case, consider breaking up the artefact update
	// endpoint into several distinct endpoints, for example: one for name, another for
	// position, and another for mask.
	public async Task<ArtefactDTO> UpdateArtefactAsync(
			UserInfo            editionUser
			, uint              artefactId
			, UpdateArtefactDTO updateArtefact
			, string            clientId = null
			, string            operationId = null)
	{
		var cleanedPoly = string.IsNullOrEmpty(updateArtefact.mask)
				? null
				: GeometryValidation.ValidatePolygon(updateArtefact.mask, "artefact");

		await _artefactRepository.UpdateArtefactAllAsync(
				editionUser
				, artefactId
				, cleanedPoly
				, updateArtefact.masterImageId
				, updateArtefact.statusMessage
				, updateArtefact.name
				, updateArtefact.placement?.scale
				, updateArtefact.placement?.rotate
				, updateArtefact.placement?.translate.x
				, updateArtefact.placement?.translate.y
				, updateArtefact.placement?.zIndex
				, updateArtefact.placement?.mirrored
				  ?? false);

		var updatedArtefact = await GetEditionArtefactAsync(
				editionUser
				, artefactId
				, !string.IsNullOrEmpty(cleanedPoly)
						? new List<string> { "masks" }
						: null);

		// Echo the client-supplied operation id on the broadcast so the originating
		// client can recognise its own change (opId reconciliation) rather than
		// treating the echo as a foreign edit.
		updatedArtefact.operationId = operationId;

		// Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
		// made the request, that client directly received the response.
		await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
						 .UpdatedArtefact(updatedArtefact);

		return updatedArtefact;
	}

	public async Task<ArtefactDTO> CreateArtefactAsync(
			UserInfo            editionUser
			, CreateArtefactDTO createArtefact
			, string            clientId = null)
	{
		if (createArtefact.masterImageId.HasValue)
		{
			var _ = await _imagedObjectRepository.CreateEditionImagedObjectsBySqeImageIdAsync(
					editionUser
					, createArtefact.masterImageId.Value);
		}

		var cleanedPoly = string.IsNullOrEmpty(createArtefact.mask)
				? null
				: GeometryValidation.ValidatePolygon(createArtefact.mask, "artefact");

		var newArtefact = await _artefactRepository.CreateNewArtefactAsync(
				editionUser
				, createArtefact.masterImageId
				, cleanedPoly
				, createArtefact.name
				, createArtefact.placement?.scale
				, createArtefact.placement?.rotate
				, createArtefact.placement?.translate?.x
				, createArtefact.placement?.translate?.y
				, createArtefact.placement?.zIndex
				, createArtefact.statusMessage
				, createArtefact.placement?.mirrored ?? false);

		var optional = string.IsNullOrEmpty(createArtefact.mask)
				? new List<string>()
				: new List<string> { "masks" };

		var newlyCreatedArtefact =
				await GetEditionArtefactAsync(editionUser, newArtefact, optional);

		// Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
		// made the request, that client directly received the response.
		await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
						 .CreatedArtefact(newlyCreatedArtefact);

		return newlyCreatedArtefact;
	}

	public async Task<NoContentResult> DeleteArtefactAsync(
			UserInfo editionUser
			, uint   artefactId
			, string clientId = null)
	{
		await _artefactRepository.DeleteArtefactAsync(editionUser, artefactId);

		// Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
		// made the request, that client directly received the response.
		await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
						 .DeletedArtefact(
								 new DeleteIntIdDTO(
										 EditionEntities.artefact
										 , new List<uint> { artefactId }));

		return new NoContentResult();
	}

	public async Task<ArtefactTextFragmentMatchListDTO> ArtefactTextFragmentsAsync(
			UserInfo       editionUser
			, uint         artefactId
			, List<string> optional)
	{
		ParseTextFragmentOptionals(optional, out var suggestedResults);

		var realMatches = new ArtefactTextFragmentMatchListDTO(
				(await _artefactRepository.ArtefactTextFragmentsAsync(editionUser, artefactId))
				.Select(x => new ArtefactTextFragmentMatchDTO(
								x.TextFragmentId.GetValueOrDefault()
								, x.TextFragmentName
								, x.TextFragmentEditorId.GetValueOrDefault()
								, false))
				.ToList());

		if (!suggestedResults)
			return realMatches;

		var suggestedMatches = await _artefactSuggestedTextFragmentsAsync(editionUser, artefactId);

		realMatches.textFragments.AddRange(suggestedMatches.textFragments);

		return realMatches;
	}

	public async Task<ArtefactGroupListDTO> ArtefactGroupsOfEditionAsync(UserInfo editionUser)
		=> (await _artefactRepository.ArtefactGroupsOfEditionAsync(editionUser)).ToDTO();

	public async Task<ArtefactGroupDTO> GetArtefactGroupDataAsync(
			UserInfo editionUser
			, uint   artefactGroupId)
		=> (await _artefactRepository.GetArtefactGroupAsync(editionUser, artefactGroupId)).ToDTO();

	public async Task<ArtefactGroupDTO> CreateArtefactGroupAsync(
			UserInfo                 editionUser
			, CreateArtefactGroupDTO artefactGroup
			, string                 clientId = null)
	{
		var results = (await _artefactRepository.CreateArtefactGroupAsync(
				editionUser
				, artefactGroup.name
				, artefactGroup.artefacts)).ToDTO();

		// Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
		// made the request, that client directly received the response.
		await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
						 .CreatedArtefactGroup(results);

		return results;
	}

	public async Task<ArtefactGroupDTO> UpdateArtefactGroupAsync(
			UserInfo                 editionUser
			, uint                   artefactGroupId
			, UpdateArtefactGroupDTO artefactGroup
			, string                 clientId = null)
	{
		var results = (await _artefactRepository.UpdateArtefactGroupAsync(
				editionUser
				, artefactGroupId
				, artefactGroup.name
				, artefactGroup.artefacts)).ToDTO();

		// Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
		// made the request, that client directly received the response.
		await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
						 .UpdatedArtefactGroup(results);

		return results;
	}

	public async Task<DeleteIntIdDTO> DeleteArtefactGroupAsync(
			UserInfo editionUser
			, uint   artefactGroupId
			, string clientId = null)
	{
		await _artefactRepository.DeleteArtefactGroupAsync(editionUser, artefactGroupId);

		var results = new DeleteIntIdDTO(EditionEntities.artefactGroup, artefactGroupId);

		// Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
		// made the request, that client directly received the response.
		await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
						 .DeletedArtefactGroup(results);

		return results;
	}

	private async Task<ArtefactTextFragmentMatchListDTO> _artefactSuggestedTextFragmentsAsync(
			UserInfo editionUser
			, uint   artefactId)
	{
		return new ArtefactTextFragmentMatchListDTO(
				(await _artefactRepository.ArtefactSuggestedTextFragmentsAsync(
						editionUser
						, artefactId)).Select(x => new ArtefactTextFragmentMatchDTO(
													  x.TextFragmentId.GetValueOrDefault()
													  , x.TextFragmentName
													  , x.TextFragmentEditorId.GetValueOrDefault()
													  , true))
									  .ToList());
	}

	private static void ParseImageMaskOptionals(
			List<string> optionals
			, out bool   images
			, out bool   masks)
	{
		images = masks = false;

		if (optionals == null)
			return;

		images = optionals.Contains("images");
		masks = optionals.Contains("masks");
	}

	private static void ParseTextFragmentOptionals(
			List<string> optionals
			, out bool   suggestedResults)
	{
		suggestedResults = optionals.Contains("suggested");
	}
}
