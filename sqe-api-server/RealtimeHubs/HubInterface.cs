using System.Threading.Tasks;
using SQE.API.DTO;

namespace SQE.API.Server.RealtimeHubs
{
    public interface ISQEClient
    {
        Task CreatedTextFragment(TextFragmentDataDTO returnedData);
        Task UpdateTextFragment(TextFragmentDataDTO returnedData);
        Task CreatedEditor(CreateEditorRightsDTO returnedData);
        Task UpdatedEditorEmail(CreateEditorRightsDTO returnedData);
        Task CreateEditionId(EditionDTO returnedData);
        Task DeletedEdition(DeleteTokenDTO returnedData);
        Task UpdatedEdition(EditionDTO returnedData);
        Task CreatedLogin(DetailedUserTokenDTO returnedData);
        Task CreatedUser(UserDTO returnedData);
        Task CreatedRoi(InterpretationRoiDTO returnedData);
        Task CreatedRoisBatch(InterpretationRoiDTOList returnedData);
        Task CreatedRoisBatchEdit(BatchEditRoiResponseDTO returnedData);
        Task UpdatedRoi(UpdatedInterpretationRoiDTO returnedData);
        Task UpdatedRoisBatch(UpdatedInterpretationRoiDTOList returnedData);
        Task DeletedRoi(uint returnedData);
        Task CreatedArtefact(ArtefactDTO returnedData);
        Task DeletedArtefact(uint returnedData);
        Task UpdatedArtefact(ArtefactDTO returnedData);
    }
}