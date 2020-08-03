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
            var roi = await _roiRepository.GetSignInterpretationRoiByIdAsync(editionUser, roiId);

            return new InterpretationRoiDTO
            {
                artefactId = roi.ArtefactId.GetValueOrDefault(),
                editorId = roi.SignInterpretationRoiAuthor.GetValueOrDefault(),
                exceptional = roi.Exceptional.GetValueOrDefault(),
                interpretationRoiId = roi.SignInterpretationRoiId.GetValueOrDefault(),
                translate = new TranslateDTO
                {
                    x = roi.TranslateX.GetValueOrDefault(),
                    y = roi.TranslateY.GetValueOrDefault()
                },
                shape = roi.Shape,
                signInterpretationId = roi.SignInterpretationId,
                valuesSet = roi.ValuesSet.GetValueOrDefault()
            };
        }

        public async Task<InterpretationRoiDTOList> GetRoisByArtefactIdAsync(UserInfo editionUser,
            uint artefactId)
        {
            return new InterpretationRoiDTOList
            {
                rois = (await _roiRepository.GetSignInterpretationRoisByArtefactIdAsync(editionUser, artefactId))
                    .Select(
                        x => new InterpretationRoiDTO
                        {
                            artefactId = x.ArtefactId.GetValueOrDefault(),
                            editorId = x.SignInterpretationRoiAuthor.GetValueOrDefault(),
                            exceptional = x.Exceptional.GetValueOrDefault(),
                            interpretationRoiId = x.SignInterpretationRoiId.GetValueOrDefault(),
                            translate = new TranslateDTO
                            {
                                x = x.TranslateX.GetValueOrDefault(),
                                y = x.TranslateY.GetValueOrDefault()
                            },
                            shape = x.Shape,
                            signInterpretationId = x.SignInterpretationId,
                            valuesSet = x.ValuesSet.GetValueOrDefault()
                        }
                    )
                    .ToList()
            };
        }

        public async Task<InterpretationRoiDTO> CreateRoiAsync(UserInfo editionUser,
            SetInterpretationRoiDTO newRoi,
            string clientId = null)
        {
            newRoi.shape = await GeometryValidation.ValidatePolygonAsync(newRoi.shape, "roi");
            return (await CreateRoisAsync(
                editionUser,
                new SetInterpretationRoiDTOList { rois = new List<SetInterpretationRoiDTO> { newRoi } },
                clientId
            )).rois.FirstOrDefault();
        }

        public async Task<InterpretationRoiDTOList> CreateRoisAsync(UserInfo editionUser,
            SetInterpretationRoiDTOList newRois,
            string clientId = null)
        {
            var newRoisDTO = new InterpretationRoiDTOList
            {
                rois = (
                        await _roiRepository.CreateRoisAsync( // Write new rois
                            editionUser,
                            (await Task.WhenAll(newRois.rois
                                .Select( // Serialize the SetInterpretationRoiDTOList to a List of SetSignInterpretationROI
                                    _convertSignInterpretationDTOToSetSignInterpretationROI
                                )))
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
                .CreatedRoisBatch(newRoisDTO);

            return newRoisDTO;
        }

        public async Task<BatchEditRoiResponseDTO> BatchEditRoisAsync(UserInfo editionUser,
            BatchEditRoiDTO rois,
            string clientId = null)
        {
            var (createRois, updateRois, deleteRois) = _roiRepository.BatchEditRoisAsync(
                editionUser,
                (await Task.WhenAll(rois.createRois.Select(_convertSignInterpretationDTOToSetSignInterpretationROI)))
                .ToList(),
                (await Task.WhenAll(rois.updateRois.Select(_convertInterpretationRoiDTOToSignInterpretationROI)))
                .ToList(),
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
                .EditedRoisBatch(batchEditRoisDTO);

            return batchEditRoisDTO;
        }

        public async Task<UpdatedInterpretationRoiDTO> UpdateRoiAsync(UserInfo editionUser,
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
                shape = await GeometryValidation.ValidatePolygonAsync(updatedRoi.shape, "roi")
            };
            return (await UpdateRoisAsync(
                editionUser,
                new InterpretationRoiDTOList { rois = new List<InterpretationRoiDTO> { fullUpdatedRoi } },
                clientId
            )).rois.FirstOrDefault();
        }

        public async Task<UpdatedInterpretationRoiDTOList> UpdateRoisAsync(UserInfo editionUser,
            InterpretationRoiDTOList updatedRois,
            string clientId = null)
        {
            var updateRoisDTO = new UpdatedInterpretationRoiDTOList
            {
                rois = (
                        await _roiRepository.UpdateRoisAsync( // Write new rois
                            editionUser,
                            (await Task.WhenAll(updatedRois.rois
                                .Select( // Serialize the InterpretationRoiDTOList to a List of SignInterpretationROI
                                    _convertInterpretationRoiDTOToSignInterpretationROI
                                )))
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
                .UpdatedRoisBatch(updateRoisDTO);

            return updateRoisDTO;
        }

        public async Task<NoContentResult> DeleteRoiAsync(UserInfo editionUser,
            uint deleteRoi,
            string clientId = null)
        {
            await DeleteRoisAsync(editionUser, new List<uint> { deleteRoi }, clientId);
            return new NoContentResult();
        }

        public async Task<List<uint>> DeleteRoisAsync(UserInfo editionUser,
            List<uint> deleteRois,
            string clientId = null)
        {
            var resp = await _roiRepository.DeleteRoisAsync(editionUser, deleteRois);

            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .DeletedRoi(new DeleteDTO(EditionEntities.roi, resp));
            return resp;
        }

        private async Task<SignInterpretationRoiData> _convertSignInterpretationDTOToSetSignInterpretationROI(
            SetInterpretationRoiDTO x)
        {
            return new SignInterpretationRoiData
            {
                SignInterpretationId = x.signInterpretationId,
                ArtefactId = x.artefactId,
                Exceptional = x.exceptional,
                TranslateX = x.translate.x,
                TranslateY = x.translate.y,
                Shape = await GeometryValidation.ValidatePolygonAsync(x.shape, "roi"),
                ValuesSet = x.valuesSet
            };
        }

        private async Task<SignInterpretationRoiData> _convertInterpretationRoiDTOToSignInterpretationROI(
            InterpretationRoiDTO x)
        {
            return new SignInterpretationRoiData
            {
                SignInterpretationRoiId = x.interpretationRoiId,
                SignInterpretationId = x.signInterpretationId,
                ArtefactId = x.artefactId,
                Exceptional = x.exceptional,
                TranslateX = x.translate.x,
                TranslateY = x.translate.y,
                Shape = await GeometryValidation.ValidatePolygonAsync(x.shape, "roi"),
                ValuesSet = x.valuesSet
            };
        }

        private InterpretationRoiDTO _convertSignInterpretationROIToInterpretationRoiDTO(SignInterpretationRoiData x)
        {
            return new InterpretationRoiDTO
            {
                artefactId = x.ArtefactId.GetValueOrDefault(),
                editorId = x.SignInterpretationRoiAuthor.GetValueOrDefault(),
                exceptional = x.Exceptional.GetValueOrDefault(),
                interpretationRoiId = x.SignInterpretationRoiId.GetValueOrDefault(),
                signInterpretationId = x.SignInterpretationId.GetValueOrDefault(),
                translate = new TranslateDTO
                {
                    x = x.TranslateX.GetValueOrDefault(),
                    y = x.TranslateY.GetValueOrDefault()
                },
                shape = x.Shape,
                valuesSet = x.ValuesSet.GetValueOrDefault()
            };
        }

        private UpdatedInterpretationRoiDTO _convertUpdatedSignInterpretationROIToUpdatedInterpretationRoiDTO(
            SignInterpretationRoiData x)
        {
            return new UpdatedInterpretationRoiDTO
            {
                artefactId = x.ArtefactId.GetValueOrDefault(),
                editorId = x.SignInterpretationRoiAuthor.GetValueOrDefault(),
                exceptional = x.Exceptional.GetValueOrDefault(),
                interpretationRoiId = x.SignInterpretationRoiId.GetValueOrDefault(),
                oldInterpretationRoiId = x.OldSignInterpretationRoiId,
                signInterpretationId = x.SignInterpretationId,
                translate = new TranslateDTO
                {
                    x = x.TranslateX.GetValueOrDefault(),
                    y = x.TranslateY.GetValueOrDefault()
                },
                shape = x.Shape,
                valuesSet = x.ValuesSet.GetValueOrDefault()
            };
        }
    }
}