using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.IO;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Serialization;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
    public interface IRoiService
    {
        Task<InterpretationRoiDTO> GetRoiAsync(UserInfo editionUser, uint roiId);

        Task<InterpretationRoiDTOList> GetRoisByArtefactIdAsync(UserInfo editionUser, uint artefactId);

        Task<InterpretationRoiDTO> CreateRoiAsync(UserInfo editionUser,
            SetInterpretationRoiDTO newRois,
            string clientId = null);

        Task<InterpretationRoiDTOList> CreateRoisAsync(UserInfo editionUser,
            SetInterpretationRoiDTOList newRois,
            string clientId = null);

        Task<BatchEditRoiResponseDTO> BatchEditRoisAsync(UserInfo editionUser,
            BatchEditRoiDTO rois,
            string clientId = null);

        Task<UpdatedInterpretationRoiDTO> UpdateRoiAsync(UserInfo editionUser,
            uint roiId,
            SetInterpretationRoiDTO updatedRoi,
            string clientId = null);

        Task<UpdatedInterpretationRoiDTOList> UpdateRoisAsync(UserInfo editionUser,
            InterpretationRoiDTOList updatedRois,
            string clientId = null);

        Task<List<uint>> DeleteRoisAsync(UserInfo editionUser,
            List<uint> deleteRois,
            string clientId = null);

        Task<NoContentResult> DeleteRoiAsync(UserInfo editionUser,
            uint deleteRoi,
            string clientId = null);
    }

    public class RoiService : IRoiService
    {
        private readonly IHubContext<MainHub, ISQEClient> _hubContext;
        private readonly Regex _removeDecimals = new Regex(@"\.\d+");
        private readonly IRoiRepository _roiRepository;
        private readonly WKTReader _wkr = new WKTReader();
        private readonly WKTWriter _wkw = new WKTWriter();

        public RoiService(IRoiRepository roiRepository, IHubContext<MainHub, ISQEClient> hubContext)
        {
            _roiRepository = roiRepository;
            _hubContext = hubContext;
        }

        public async Task<InterpretationRoiDTO> GetRoiAsync(UserInfo editionUser, uint roiId)
        {
            return (await _roiRepository.GetSignInterpretationRoiByIdAsync(editionUser, roiId)).ToDTO();
        }

        public async Task<InterpretationRoiDTOList> GetRoisByArtefactIdAsync(UserInfo editionUser,
            uint artefactId)
        {
            return new InterpretationRoiDTOList
            {
                rois = (await _roiRepository.GetSignInterpretationRoisByArtefactIdAsync(editionUser, artefactId))
                    .ToDTO()
                    .ToList()
            };
        }

        public async Task<InterpretationRoiDTO> CreateRoiAsync(UserInfo editionUser,
            SetInterpretationRoiDTO newRoi,
            string clientId = null)
        {
            var response = (await CreateRoisInternalAsync(
                editionUser,
                new SetInterpretationRoiDTOList { rois = new List<SetInterpretationRoiDTO> { newRoi } }
            )).rois;

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients
                .GroupExcept(editionUser.EditionId.ToString(), clientId)
                .CreatedRoisBatch(new InterpretationRoiDTOList() { rois = response });

            return response.First();
        }

        public async Task<InterpretationRoiDTOList> CreateRoisAsync(UserInfo editionUser,
            SetInterpretationRoiDTOList newRois,
            string clientId = null)
        {
            var response = await CreateRoisInternalAsync(editionUser, newRois);

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            // TODO: make a DTO for the delete object.
            await _hubContext.Clients
                .GroupExcept(editionUser.EditionId.ToString(), clientId)
                .EditedRoisBatch(new BatchEditRoiResponseDTO() { createRois = response.rois });

            return response;
        }

        private async Task<InterpretationRoiDTOList> CreateRoisInternalAsync(UserInfo editionUser,
            SetInterpretationRoiDTOList newRois)
        {
            var newRoisDTO = new InterpretationRoiDTOList
            {
                rois = (
                        await _roiRepository.CreateRoisAsync( // Write new rois
                            editionUser,
                            // Serialize the SetInterpretationRoiDTOList to a List of SetSignInterpretationROI
                            newRois.rois.ToSignInterpretationRoiData().ToList()
                        )
                    )
                    .ToDTO()
                    .ToList()
            };

            return newRoisDTO;
        }

        public async Task<BatchEditRoiResponseDTO> BatchEditRoisAsync(UserInfo editionUser,
            BatchEditRoiDTO rois,
            string clientId = null)
        {
            var (createRois, updateRois, deleteRois) = _roiRepository.BatchEditRoisAsync(
                editionUser,
                rois.createRois.ToSignInterpretationRoiData().ToList(),
                rois.updateRois.ToSignInterpretationRoiData().ToList(),
                rois.deleteRois
            );
            await Task.WhenAll(createRois, updateRois, deleteRois);

            var batchEditRoisDTO = new BatchEditRoiResponseDTO
            {
                createRois = (await createRois).ToDTO().ToList(),
                updateRois = (await updateRois).ToUpdateDTO().ToList(),
                deleteRois = await deleteRois
            };

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            // TODO: make a DTO for the delete object.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .EditedRoisBatch(batchEditRoisDTO);

            return batchEditRoisDTO;
        }

        public async Task<UpdatedInterpretationRoiDTO> UpdateRoiAsync(UserInfo editionUser,
            uint roiId,
            SetInterpretationRoiDTO updatedRoi,
            string clientId = null)
        {
            var fullUpdatedRoi = updatedRoi.ToInterpretationRoiDTO(roiId);

            var updateRoisDTO = (await UpdateRoisInternalAsync(editionUser, new InterpretationRoiDTOList { rois = new List<InterpretationRoiDTO> { fullUpdatedRoi } }))
                .rois;

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            // TODO: make a DTO for the delete object.
            await _hubContext.Clients
                .GroupExcept(editionUser.EditionId.ToString(), clientId)
                .EditedRoisBatch(new BatchEditRoiResponseDTO() { updateRois = updateRoisDTO });

            return updateRoisDTO.First();
        }

        public async Task<UpdatedInterpretationRoiDTOList> UpdateRoisAsync(UserInfo editionUser,
            InterpretationRoiDTOList updatedRois,
            string clientId = null)
        {
            var updateRoisDTO = await UpdateRoisInternalAsync(editionUser, updatedRois);

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            // TODO: make a DTO for the delete object.
            await _hubContext.Clients
                .GroupExcept(editionUser.EditionId.ToString(), clientId)
                .UpdatedRoisBatch(updateRoisDTO);

            return updateRoisDTO;
        }

        private async Task<UpdatedInterpretationRoiDTOList> UpdateRoisInternalAsync(UserInfo editionUser,
            InterpretationRoiDTOList updatedRois)
        {
            return new UpdatedInterpretationRoiDTOList
            {
                rois = (
                        await _roiRepository.UpdateRoisAsync( // Write new rois
                            editionUser,
                            updatedRois.rois.ToSignInterpretationRoiData().ToList()
                        )
                    )
                    .ToUpdateDTO()
                    .ToList()
            };
        }

        public async Task<NoContentResult> DeleteRoiAsync(UserInfo editionUser,
            uint deleteRoi,
            string clientId = null)
        {
            await DeleteRoisInternalAsync(editionUser, new List<uint> { deleteRoi });

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients
                .GroupExcept(editionUser.EditionId.ToString(), clientId)
                .DeletedRoi(new DeleteDTO(EditionEntities.roi, deleteRoi));

            return new NoContentResult();
        }

        public async Task<List<uint>> DeleteRoisAsync(UserInfo editionUser,
            List<uint> deleteRois,
            string clientId = null)
        {
            var response = await DeleteRoisInternalAsync(editionUser, deleteRois);

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients
                .GroupExcept(editionUser.EditionId.ToString(), clientId)
                .DeletedRoi(new DeleteDTO(EditionEntities.roi, response));

            return response;
        }

        private async Task<List<uint>> DeleteRoisInternalAsync(UserInfo editionUser,
            List<uint> deleteRois)
        {
            return await _roiRepository.DeleteRoisAsync(editionUser, deleteRois);
        }
    }
}