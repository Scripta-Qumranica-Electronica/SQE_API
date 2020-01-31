using System.Threading.Tasks;
using SQE.API.DTO;

namespace SQE.API.Server.RealtimeHubs
{
    public interface ISQEClient
    {
        Task CreateTextFragment(TextFragmentDataDTO returnedData);
        Task UpdateTextFragment(TextFragmentDataDTO returnedData);
        Task CreateEditor(CreateEditorRightsDTO returnedData);
        Task UpdateEditorEmail(CreateEditorRightsDTO returnedData);
        Task CreateEditionId(EditionDTO returnedData);
        Task DeleteEdition(DeleteTokenDTO returnedData);
        Task UpdateEdition(EditionDTO returnedData);
        Task CreateLogin(DetailedUserTokenDTO returnedData);
        Task CreateUser(UserDTO returnedData);
        Task CreateRoi(InterpretationRoiDTO returnedData);
        Task CreateRoisBatch(InterpretationRoiDTOList returnedData);
        Task CreateRoisBatchEdit(BatchEditRoiResponseDTO returnedData);
        Task UpdateRoi(UpdatedInterpretationRoiDTO returnedData);
        Task UpdateRoisBatch(UpdatedInterpretationRoiDTOList returnedData);
        Task DeleteRoi(uint returnedData);
        Task CreateArtefact(ArtefactDTO returnedData);
        Task DeleteArtefact(uint returnedData);
        Task UpdateArtefact(ArtefactDTO returnedData);
    }
}