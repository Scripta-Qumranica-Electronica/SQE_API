using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
    public interface IRoiService
    {
        Task<InterpretationRoiDTO> GetRoiAsync(EditionUserInfo editionUser, uint roiId);

        Task<InterpretationRoiDTOList> GetRoisByArtefactIdAsync(EditionUserInfo editionUser, uint artefactId);

        Task<InterpretationRoiDTO> CreateRoiAsync(EditionUserInfo editionUser,
            SetInterpretationRoiDTO newRois,
            string clientId = null);

        Task<InterpretationRoiDTOList> CreateRoisAsync(EditionUserInfo editionUser,
            SetInterpretationRoiDTOList newRois,
            string clientId = null);

        Task<BatchEditRoiResponseDTO> BatchEditRoisAsync(EditionUserInfo editionUser,
            BatchEditRoiDTO rois,
            string clientId = null);

        Task<UpdatedInterpretationRoiDTO> UpdateRoiAsync(EditionUserInfo editionUser,
            uint roiId,
            SetInterpretationRoiDTO updatedRoi,
            string clientId = null);

        Task<UpdatedInterpretationRoiDTOList> UpdateRoisAsync(EditionUserInfo editionUser,
            InterpretationRoiDTOList updatedRois,
            string clientId = null);

        Task<NoContentResult> DeleteRoisAsync(EditionUserInfo editionUser,
            List<uint> deleteRois,
            string clientId = null);

        Task<NoContentResult> DeleteRoiAsync(EditionUserInfo editionUser,
            uint deleteRoi,
            string clientId = null);
    }

    public class RoiService : IRoiService
    {
        private readonly IHubContext<MainHub> _hubContext;
        private readonly IRoiRepository _roiRepository;

        public RoiService(IRoiRepository roiRepository, IHubContext<MainHub> hubContext)
        {
            _roiRepository = roiRepository;
            _hubContext = hubContext;
        }

        public async Task<InterpretationRoiDTO> GetRoiAsync(EditionUserInfo editionUser, uint roiId)
        {
            var roi = await _roiRepository.GetSignInterpretationRoiByIdAsync(editionUser, roiId);
            return new InterpretationRoiDTO
            {
                artefactId = roi.ArtefactId,
                editorId = roi.SignInterpretationRoiAuthor,
                exceptional = roi.Exceptional,
                interpretationRoiId = roi.SignInterpretationRoiId,
                translate = new TranslateDTO
                {
                    x = roi.TranslateX,
                    y = roi.TranslateY
                },
                shape = roi.Shape,
                signInterpretationId = roi.SignInterpretationId,
                valuesSet = roi.ValuesSet
            };
        }

        public async Task<InterpretationRoiDTOList> GetRoisByArtefactIdAsync(EditionUserInfo editionUser,
            uint artefactId)
        {
            return new InterpretationRoiDTOList
            {
                rois = (await _roiRepository.GetSignInterpretationRoisByArtefactIdAsync(editionUser, artefactId))
                    .Select(
                        x => new InterpretationRoiDTO
                        {
                            artefactId = x.ArtefactId,
                            editorId = x.SignInterpretationRoiAuthor,
                            exceptional = x.Exceptional,
                            interpretationRoiId = x.SignInterpretationRoiId,
                            translate = new TranslateDTO
                            {
                                x = x.TranslateX,
                                y = x.TranslateY
                            },
                            shape = x.Shape,
                            signInterpretationId = x.SignInterpretationId,
                            valuesSet = x.ValuesSet
                        }
                    )
                    .ToList()
            };
        }

        public async Task<InterpretationRoiDTO> CreateRoiAsync(EditionUserInfo editionUser,
            SetInterpretationRoiDTO newRois,
            string clientId = null)
        {
            return (await CreateRoisAsync(
                editionUser,
                new SetInterpretationRoiDTOList { rois = new List<SetInterpretationRoiDTO> { newRois } },
                clientId
            )).rois.FirstOrDefault();
        }

        public async Task<InterpretationRoiDTOList> CreateRoisAsync(EditionUserInfo editionUser,
            SetInterpretationRoiDTOList newRois,
            string clientId = null)
        {
            var newRoisDTO = new InterpretationRoiDTOList
            {
                rois = (
                        await _roiRepository.CreateRoisAsync( // Write new rois
                            editionUser,
                            newRois.rois
                                .Select( // Serialize the SetInterpretationRoiDTOList to a List of SetSignInterpretationROI
                                    _convertSignInterpretationDTOToSetSignInterpretationROI
                                )
                                .ToList()
                        )
                    )
                    .Select( // Serialize the ROI Repository response to a List of InterpretationRoiDTO
                        _convertSignInterpretationROIToInterpretationRoiDTO
                    )
                    .ToList()
            };

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            // TODO: make a DTO for the delete object.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .SendAsync("createRois", newRoisDTO);

            return newRoisDTO;
        }

        public async Task<BatchEditRoiResponseDTO> BatchEditRoisAsync(EditionUserInfo editionUser,
            BatchEditRoiDTO rois,
            string clientId = null)
        {
            var (createRois, updateRois, deleteRois) = _roiRepository.BatchEditRoisAsync(
                editionUser,
                rois.createRois.Select(_convertSignInterpretationDTOToSetSignInterpretationROI).ToList(),
                rois.updateRois.Select(_convertInterpretationRoiDTOToSignInterpretationROI).ToList(),
                rois.deleteRois
            );
            await Task.WhenAll(createRois, updateRois, deleteRois);

            var batchEditRoisDTO = new BatchEditRoiResponseDTO
            {
                createRois = (await createRois).Select(_convertSignInterpretationROIToInterpretationRoiDTO).ToList(),
                updateRois = (await updateRois)
                    .Select(_convertUpdatedSignInterpretationROIToUpdatedInterpretationRoiDTO)
                    .ToList(),
                deleteRois = await deleteRois
            };

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            // TODO: make a DTO for the delete object.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .SendAsync("createRois", batchEditRoisDTO.createRois);
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .SendAsync("updateRois", batchEditRoisDTO.updateRois);
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .SendAsync("deleteRois", batchEditRoisDTO.deleteRois);

            return batchEditRoisDTO;
        }

        public async Task<UpdatedInterpretationRoiDTO> UpdateRoiAsync(EditionUserInfo editionUser,
            uint roiId,
            SetInterpretationRoiDTO updatedRoi,
            string clientId = null)
        {
            var fullUpdatedRoi = new InterpretationRoiDTO
            {
                artefactId = updatedRoi.artefactId,
                interpretationRoiId = roiId,
                signInterpretationId = updatedRoi.signInterpretationId,
                exceptional = updatedRoi.exceptional,
                valuesSet = updatedRoi.valuesSet,
                translate = updatedRoi.translate,
                shape = updatedRoi.shape
            };
            return (await UpdateRoisAsync(
                editionUser,
                new InterpretationRoiDTOList { rois = new List<InterpretationRoiDTO> { fullUpdatedRoi } },
                clientId
            )).rois.FirstOrDefault();
        }

        public async Task<UpdatedInterpretationRoiDTOList> UpdateRoisAsync(EditionUserInfo editionUser,
            InterpretationRoiDTOList updatedRois,
            string clientId = null)
        {
            var updateRoisDTO = new UpdatedInterpretationRoiDTOList
            {
                rois = (
                        await _roiRepository.UpdateRoisAsync( // Write new rois
                            editionUser,
                            updatedRois.rois
                                .Select( // Serialize the InterpretationRoiDTOList to a List of SignInterpretationROI
                                    _convertInterpretationRoiDTOToSignInterpretationROI
                                )
                                .ToList()
                        )
                    )
                    .Select( // Serialize the ROI Repository response to a List of InterpretationRoiDTO
                        _convertUpdatedSignInterpretationROIToUpdatedInterpretationRoiDTO
                    )
                    .ToList()
            };

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            // TODO: make a DTO for the delete object.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .SendAsync("updateRois", updateRoisDTO);

            return updateRoisDTO;
        }

        public async Task<NoContentResult> DeleteRoiAsync(EditionUserInfo editionUser,
            uint deleteRoi,
            string clientId = null)
        {
            return await DeleteRoisAsync(editionUser, new List<uint> { deleteRoi }, clientId);
        }

        public async Task<NoContentResult> DeleteRoisAsync(EditionUserInfo editionUser,
            List<uint> deleteRois,
            string clientId = null)
        {
            var deleteRoisDTO = await _roiRepository.DeletRoisAsync(editionUser, deleteRois);

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .SendAsync(
                    "deleteRois",
                    deleteRoisDTO.Select(
                            x => new DeleteEditionEntityDTO { entityId = x, editorId = editionUser.EditionEditorId.Value }
                        )
                        .ToList()
                );

            return new NoContentResult();
        }

        private SetSignInterpretationROI _convertSignInterpretationDTOToSetSignInterpretationROI(
            SetInterpretationRoiDTO x)
        {
            return new SetSignInterpretationROI
            {
                SignInterpretationId = x.signInterpretationId,
                ArtefactId = x.artefactId,
                Exceptional = x.exceptional,
                TranslateX = x.translate.x,
                TranslateY = x.translate.y,
                Shape = x.shape,
                ValuesSet = x.valuesSet
            };
        }

        private SignInterpretationROI _convertInterpretationRoiDTOToSignInterpretationROI(InterpretationRoiDTO x)
        {
            return new SignInterpretationROI
            {
                SignInterpretationRoiId = x.interpretationRoiId,
                SignInterpretationId = x.signInterpretationId,
                ArtefactId = x.artefactId,
                Exceptional = x.exceptional,
                TranslateX = x.translate.x,
                TranslateY = x.translate.y,
                Shape = x.shape,
                ValuesSet = x.valuesSet
            };
        }

        private InterpretationRoiDTO _convertSignInterpretationROIToInterpretationRoiDTO(SignInterpretationROI x)
        {
            return new InterpretationRoiDTO
            {
                artefactId = x.ArtefactId,
                editorId = x.SignInterpretationRoiAuthor,
                exceptional = x.Exceptional,
                interpretationRoiId = x.SignInterpretationRoiId,
                signInterpretationId = x.SignInterpretationId,
                translate = new TranslateDTO
                {
                    x = x.TranslateX,
                    y = x.TranslateY
                },
                shape = x.Shape,
                valuesSet = x.ValuesSet
            };
        }

        private UpdatedInterpretationRoiDTO _convertUpdatedSignInterpretationROIToUpdatedInterpretationRoiDTO(
            UpdatedSignInterpretationROI x)
        {
            return new UpdatedInterpretationRoiDTO
            {
                artefactId = x.ArtefactId,
                editorId = x.SignInterpretationRoiAuthor,
                exceptional = x.Exceptional,
                interpretationRoiId = x.SignInterpretationRoiId,
                oldInterpretationRoiId = x.OldSignInterpretationRoiId,
                signInterpretationId = x.SignInterpretationId,
                translate = new TranslateDTO
                {
                    x = x.TranslateX,
                    y = x.TranslateY
                },
                shape = x.Shape,
                valuesSet = x.ValuesSet
            };
        }
    }
}