using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_Editions_EditionId_Rois_RoiId : RoiRequestObject<EmptyInput, InterpretationRoiDTO, EmptyOutput>
        {
            /// <summary>
            ///     Request information about a ROI
            /// </summary>
            /// <param name="editionId">The editionId for the desired roi</param>
            /// <param name="roiId">The ROI id for the desired roi</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId, uint roiId) : base(editionId, roiId, null)
            {
            }
        }
    }

    public static partial class Post
    {
        public class V1_Editions_EditionId_Rois : EditionRequestObject<SetInterpretationRoiDTO, InterpretationRoiDTO,
            InterpretationRoiDTO>
        {
            /// <summary>
            ///     Create a new ROI in an edition
            /// </summary>
            /// <param name="editionId">The editionId to create the new ROI</param>
            /// <param name="payload">The details of the new ROI</param>
            public V1_Editions_EditionId_Rois(uint editionId, SetInterpretationRoiDTO payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("CreatedRoi");
            }
        }

        public class V1_Editions_EditionId_Rois_Batch
            : EditionRequestObject<InterpretationRoiDTOList, InterpretationRoiDTO, InterpretationRoiDTO>
        {
            /// <summary>
            ///     Create one or more new ROIs in an edition
            /// </summary>
            /// <param name="editionId">The editionId to create the new ROIs</param>
            /// <param name="payload">The details of the new ROIs</param>
            public V1_Editions_EditionId_Rois_Batch(uint editionId, InterpretationRoiDTOList payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("CreatedRoisBatch");
            }
        }

        public class V1_Editions_EditionId_Rois_BatchEdit
            : EditionRequestObject<BatchEditRoiDTO, BatchEditRoiResponseDTO, BatchEditRoiResponseDTO>
        {
            /// <summary>
            ///     Create one or more new ROIs in an edition
            /// </summary>
            /// <param name="editionId">The editionId to create the new ROIs</param>
            /// <param name="payload">The details of the new ROIs</param>
            public V1_Editions_EditionId_Rois_BatchEdit(uint editionId, BatchEditRoiDTO payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("CreatedRoisBatch");
                listenerMethod.Add("UpdatedRoisBatch");
                listenerMethod.Add("DeletedRoisBatch");
            }
        }
    }

    public static partial class Put
    {
        public class V1_Editions_EditionId_Rois_RoiId
            : EditionRequestObject<SetInterpretationRoiDTO, UpdatedInterpretationRoiDTO, UpdatedInterpretationRoiDTO>
        {
            /// <summary>
            ///     Update ROI in an edition
            /// </summary>
            /// <param name="editionId">The editionId to update ROI</param>
            /// <param name="payload">The details of the updated ROI</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId, SetInterpretationRoiDTO payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("UpdatedRoi");
            }
        }

        public class V1_Editions_EditionId_Rois_Batch
            : EditionRequestObject<InterpretationRoiDTOList, UpdatedInterpretationRoiDTOList,
                UpdatedInterpretationRoiDTOList>
        {
            /// <summary>
            ///     Updates one or more new ROIs in an edition
            /// </summary>
            /// <param name="editionId">The editionId to update the ROIs</param>
            /// <param name="payload">The details of the updates ROIs</param>
            public V1_Editions_EditionId_Rois_Batch(uint editionId, InterpretationRoiDTOList payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("UpdatedRoisBatch");
            }
        }
    }

    public static partial class Delete
    {
        public class V1_Editions_EditionId_Rois_RoiId : RoiRequestObject<EmptyInput, EmptyOutput, EmptyOutput>
        {
            /// <summary>
            ///     Deletes a ROI from an edition
            /// </summary>
            /// <param name="editionId">The editionId to delete the ROI from</param>
            /// <param name="roiId">The id of the ROI to delete</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId, uint roiId) : base(editionId, roiId, null)
            {
                listenerMethod.Add("DeletedRoi");
            }
        }
    }
}