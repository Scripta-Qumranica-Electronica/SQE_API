using SQE.API.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQE.API.Server.RealtimeHubs
{
    interface IHubEvents
    {
        Task UpdatedArtefact(ArtefactDTO artefact);
        Task CreatedArtefact(ArtefactDTO artefact);
        Task DeletedArtefact(DeleteEditionEntityDTO deletedEntity);

        Task UpdatedEdition(EditionDTO edition);
        Task DeletedEdition(DeleteEditionEntityDTO deletedntity);
        Task AddedEditionEditor(EditorRightsDTO editor);
        Task UpdatedEditionEditor(EditorRightsDTO editor);

        Task CreatedROIs(InterpretationRoiDTOList rois);
        Task UpdatedROIs(UpdatedInterpretationRoiDTOList rois);
        Task DeletedROIs(List<uint> rois);
    }
}
