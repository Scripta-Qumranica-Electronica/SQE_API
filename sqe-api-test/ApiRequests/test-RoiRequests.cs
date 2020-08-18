/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-api-server` project. Changes made
 * there will automatically be incorporated here the next time the 
 * `sqe-realtime-hub-builder` is run.
 */


using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


    public static partial class GET
    {


        public class V1_Editions_EditionId_Rois_RoiId
        : EditionRequestObject<EmptyInput, InterpretationRoiDTO, EmptyOutput>
        {
            /// <summary>
        ///     Get the details for a ROI in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="roiId">A JSON object with the new ROI to be created</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId,uint roiId) 
                : base(editionId, null) { }
        }

	}

    public static partial class POST
    {


        public class V1_Editions_EditionId_Rois
        : EditionRequestObject<SetInterpretationRoiDTO, InterpretationRoiDTO, InterpretationRoiDTO>
        {
            /// <summary>
        ///     Creates new sign ROI in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="newRoi">A JSON object with the new ROI to be created</param>
            public V1_Editions_EditionId_Rois(uint editionId,SetInterpretationRoiDTO payload) 
                : base(editionId, null, payload) { }
        }


        public class V1_Editions_EditionId_Rois_Batch
        : EditionRequestObject<SetInterpretationRoiDTOList, InterpretationRoiDTOList, InterpretationRoiDTOList>
        {
            /// <summary>
        ///     Creates new sign ROI's in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="newRois">A JSON object with an array of the new ROI's to be created</param>
            public V1_Editions_EditionId_Rois_Batch(uint editionId,SetInterpretationRoiDTOList payload) 
                : base(editionId, null, payload) { }
        }


        public class V1_Editions_EditionId_Rois_BatchEdit
        : EditionRequestObject<BatchEditRoiDTO, BatchEditRoiResponseDTO, BatchEditRoiResponseDTO>
        {
            /// <summary>
        ///     Processes a series of create/update/delete ROI requests in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="rois">A JSON object with all the roi edits to be performed</param>
            public V1_Editions_EditionId_Rois_BatchEdit(uint editionId,BatchEditRoiDTO payload) 
                : base(editionId, null, payload) { }
        }

	}

    public static partial class PUT
    {


        public class V1_Editions_EditionId_Rois_RoiId
        : EditionRequestObject<SetInterpretationRoiDTO, UpdatedInterpretationRoiDTO, UpdatedInterpretationRoiDTO>
        {
            /// <summary>
        ///     Update an existing sign ROI in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="roiId">Id of the ROI to be updated</param>
        /// <param name="updateRoi">A JSON object with the updated ROI details</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId,uint roiId,SetInterpretationRoiDTO payload) 
                : base(editionId, null, payload) { }
        }


        public class V1_Editions_EditionId_Rois_Batch
        : EditionRequestObject<InterpretationRoiDTOList, UpdatedInterpretationRoiDTOList, UpdatedInterpretationRoiDTOList>
        {
            /// <summary>
        ///     Update existing sign ROI's in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="updateRois">A JSON object with an array of the updated ROI details</param>
            public V1_Editions_EditionId_Rois_Batch(uint editionId,InterpretationRoiDTOList payload) 
                : base(editionId, null, payload) { }
        }

	}

    public static partial class DELETE
    {


        public class V1_Editions_EditionId_Rois_RoiId
        : EditionRequestObject<EmptyInput, EmptyOutput, DeleteDTO>
        {
            /// <summary>
        ///     Deletes a sign ROI from the given edition of a scroll
        /// </summary>
        /// <param name="roiId">Id of the ROI to be deleted</param>
        /// <param name="editionId">Id of the edition</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId,uint roiId) 
                : base(editionId, null) { }
        }

	}

}
