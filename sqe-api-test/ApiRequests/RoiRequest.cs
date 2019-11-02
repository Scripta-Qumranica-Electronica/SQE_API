using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_Editions_EditionId_Rois_RoiId : RoiRequestObject<EmptyInput, InterpretationRoiDTO>
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
        public class V1_Editions_EditionId_Rois : EditionRequestObject<SetInterpretationRoiDTO, InterpretationRoiDTO>
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
            }
        }

        public class V1_Editions_EditionId_Rois_Batch
            : EditionRequestObject<InterpretationRoiDTOList, InterpretationRoiDTO>
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
            }
        }
    }

    public static partial class Put
    {
        public class V1_Editions_EditionId_Rois_RoiId
            : EditionRequestObject<SetInterpretationRoiDTO, UpdatedInterpretationRoiDTO>
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
            }
        }

        public class V1_Editions_EditionId_Rois_Batch
            : EditionRequestObject<InterpretationRoiDTOList, UpdatedInterpretationRoiDTOList>
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
            }
        }
    }

    public static partial class Delete
    {
        public class V1_Editions_EditionId_Rois_RoiId : RoiRequestObject<SetInterpretationRoiDTO, InterpretationRoiDTO>
        {
            /// <summary>
            ///     Deletes a ROI from an edition
            /// </summary>
            /// <param name="editionId">The editionId to delete the ROI from</param>
            /// <param name="roiId">The id of the ROI to delete</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId, uint roiId) : base(editionId, roiId, null)
            {
            }
        }
    }
}