using System.Threading.Tasks;
using SQE.API.DTO;

namespace SQE.API.Server.RealtimeHubs
{
    /// <summary>
    ///     This interface is used to define the methods that the SignalR Hub can call
    ///     on the client. It is manually created and will not be altered by any of
    ///     the automated SignalR tooling.
    /// </summary>
    public interface ISQEClient
    {
        /// <summary>
        ///     broadcasts a new text fragment has been created
        /// </summary>
        /// <param name="returnedData">Details of the newly created text fragment</param>
        /// <returns></returns>
        Task CreatedTextFragment(TextFragmentDataDTO returnedData);

        /// <summary>
        ///     broadcasts a text fragment has been updated
        /// </summary>
        /// <param name="returnedData">Details of the updated text fragment</param>
        /// <returns></returns>
        Task UpdatedTextFragment(TextFragmentDataDTO returnedData);

        /// <summary>
        ///     broadcasts a editor has been requested for the edition
        /// </summary>
        /// <param name="returnedData">Details of the new editor</param>
        /// <returns></returns>
        Task RequestedEditor(EditorInvitationDTO returnedData);

        /// <summary>
        ///     broadcasts a editor has been added to the edition
        /// </summary>
        /// <param name="returnedData">Details of the new editor</param>
        /// <returns></returns>
        Task CreatedEditor(DetailedEditorRightsDTO returnedData);

        /// <summary>
        ///     broadcasts an editor's permissions have been updated
        /// </summary>
        /// <param name="returnedData">Details of the editor's updated permissions</param>
        /// <returns></returns>
        Task UpdatedEditorEmail(DetailedEditorRightsDTO returnedData);

        /// <summary>
        ///     broadcasts a new text edition has been created
        /// </summary>
        /// <param name="returnedData">Details of the newly created edition</param>
        /// <returns></returns>
        Task CreatedEdition(EditionDTO returnedData);

        /// <summary>
        ///     broadcasts an edition has been deleted
        /// </summary>
        /// <param name="returnedData">Details of the deleted edition</param>
        /// <returns></returns>
        Task DeletedEdition(DeleteTokenDTO returnedData);

        /// <summary>
        ///     broadcasts an edition's details have been updated
        /// </summary>
        /// <param name="returnedData">Details of the updated edition</param>
        /// <returns></returns>
        Task UpdatedEdition(EditionDTO returnedData);

        /// <summary>
        ///     broadcasts one or more new ROI's have been created
        /// </summary>
        /// <param name="returnedData">Details of the newly created ROI's</param>
        /// <returns></returns>
        Task CreatedRoisBatch(InterpretationRoiDTOList returnedData);

        /// <summary>
        ///     broadcasts one or more new ROI's have been updated
        /// </summary>
        /// <param name="returnedData">Details of the updated ROI's</param>
        /// <returns></returns>
        Task EditedRoisBatch(BatchEditRoiResponseDTO returnedData);

        /// <summary>
        ///     broadcasts one or more new ROI's have been updated
        /// </summary>
        /// <param name="returnedData">Details of the updated ROI's</param>
        /// <returns></returns>
        Task UpdatedRoisBatch(UpdatedInterpretationRoiDTOList returnedData);

        /// <summary>
        ///     broadcasts a ROI has been deleted
        /// </summary>
        /// <param name="returnedData">Details of the deleted ROI</param>
        /// <returns></returns>
        Task DeletedRoi(DeleteDTO returnedData);

        /// <summary>
        ///     broadcasts an artefact has been created
        /// </summary>
        /// <param name="returnedData">Details of the newly created artefact</param>
        /// <returns></returns>
        Task CreatedArtefact(ArtefactDTO returnedData);

        /// <summary>
        ///     broadcasts an artefact has been deleted
        /// </summary>
        /// <param name="returnedData">Details of the deleted artefact</param>
        /// <returns></returns>
        Task DeletedArtefact(DeleteDTO returnedData);

        /// <summary>
        ///     broadcasts an artefact has been updated
        /// </summary>
        /// <param name="returnedData">Details of the updated artefact</param>
        /// <returns></returns>
        Task UpdatedArtefact(ArtefactDTO returnedData);

        /// <summary>
        ///     broadcasts an artefact group has been created
        /// </summary>
        /// <param name="returnedData">Details of the newly created artefact group</param>
        /// <returns></returns>
        Task CreatedArtefactGroup(ArtefactGroupDTO returnedData);

        /// <summary>
        ///     broadcasts and artefact group has been updated
        /// </summary>
        /// <param name="returnedData">Details of the updated artefact group</param>
        /// <returns></returns>
        Task UpdatedArtefactGroup(ArtefactGroupDTO returnedData);

        /// <summary>
        ///     broadcasts an artefact group has been deleted
        /// </summary>
        /// <param name="returnedData">Delete DTO with the id of the deleted artefact group</param>
        /// <returns></returns>
        Task DeletedArtefactGroup(DeleteDTO returnedData);

        /// <summary>
        ///     broadcasts the transform details for a batch of artefacts has been updated
        /// </summary>
        /// <param name="returnedData">Details of the updated artefact transforms</param>
        /// <returns></returns>
        Task BatchUpdatedArtefactTransform(BatchUpdatedArtefactTransformDTO returnedData);

        /// <summary>
        ///     broadcasts the create of a sign interpretation
        /// </summary>
        /// <param name="returnedData"></param>
        /// <returns></returns>
        Task CreatedSignInterpretation(SignInterpretationListDTO returnedData);

        /// <summary>
        ///     broadcasts the update of a sign interpretation
        /// </summary>
        /// <param name="returnedData"></param>
        /// <returns></returns>
        Task UpdatedSignInterpretation(SignInterpretationDTO returnedData);

        /// <summary>
        ///     broadcasts the delete of a sign interpretation
        /// </summary>
        /// <param name="returnedData"></param>
        /// <returns></returns>
        Task DeletedSignInterpretation(DeleteDTO returnedData);

        /// <summary>
        ///     broadcasts the creation of an attribute
        /// </summary>
        /// <param name="returnedData"></param>
        /// <returns></returns>
        Task CreatedAttribute(AttributeDTO returnedData);

        /// <summary>
        ///     broadcasts the update of an attribute
        /// </summary>
        /// <param name="returnedData"></param>
        /// <returns></returns>
        Task UpdatedAttribute(AttributeDTO returnedData);

        /// <summary>
        ///     broadcasts the delete of an attribute
        /// </summary>
        /// <param name="returnedData"></param>
        /// <returns></returns>
        Task DeletedAttribute(DeleteDTO returnedData);
    }
}