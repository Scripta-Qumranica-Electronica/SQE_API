
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


    public static partial class Delete
    {


        public class V1_Editions_EditionId_Rois_RoiId
        : RequestObject<EmptyInput, EmptyOutput, EmptyOutput>
        {
            public readonly uint EditionId;
            public readonly uint RoiId;

            /// <summary>
            ///     Deletes a sign ROI from the given edition of a scroll
            /// </summary>
            /// <param name="roiId">Id of the ROI to be deleted</param>
            /// <param name="editionId">Id of the edition</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId, uint roiId)
                : base(null)
            {
                this.EditionId = editionId;
                this.RoiId = roiId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/roi-id", $"/{RoiId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, RoiId);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }
    }

    public static partial class Get
    {


        public class V1_Editions_EditionId_Rois_RoiId
        : RequestObject<EmptyInput, InterpretationRoiDTO, EmptyOutput>
        {
            public readonly uint EditionId;
            public readonly uint RoiId;

            /// <summary>
            ///     Get the details for a ROI in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="roiId">A JSON object with the new ROI to be created</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId, uint roiId)
                : base(null)
            {
                this.EditionId = editionId;
                this.RoiId = roiId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/roi-id", $"/{RoiId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, RoiId);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }
    }

    public static partial class Post
    {


        public class V1_Editions_EditionId_Rois
        : RequestObject<SetInterpretationRoiDTO, InterpretationRoiDTO, EmptyOutput>
        {
            public readonly uint EditionId;
            public readonly SetInterpretationRoiDTO Payload;

            /// <summary>
            ///     Creates new sign ROI in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="newRoi">A JSON object with the new ROI to be created</param>
            public V1_Editions_EditionId_Rois(uint editionId, SetInterpretationRoiDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.Payload = payload;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, Payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_Rois_Batch
        : RequestObject<SetInterpretationRoiDTOList, InterpretationRoiDTOList, InterpretationRoiDTOList>
        {
            public readonly uint EditionId;
            public readonly SetInterpretationRoiDTOList Payload;

            /// <summary>
            ///     Creates new sign ROI's in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="newRois">A JSON object with an array of the new ROI's to be created</param>
            public V1_Editions_EditionId_Rois_Batch(uint editionId, SetInterpretationRoiDTOList payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.Payload = payload;
                this.listenerMethod = "CreatedRoisBatch";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, Payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_Rois_BatchEdit
        : RequestObject<BatchEditRoiDTO, BatchEditRoiResponseDTO, BatchEditRoiResponseDTO>
        {
            public readonly uint EditionId;
            public readonly BatchEditRoiDTO Payload;

            /// <summary>
            ///     Processes a series of create/update/delete ROI requests in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="rois">A JSON object with all the roi edits to be performed</param>
            public V1_Editions_EditionId_Rois_BatchEdit(uint editionId, BatchEditRoiDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.Payload = payload;
                this.listenerMethod = "EditedRoisBatch";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, Payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }
    }

    public static partial class Put
    {


        public class V1_Editions_EditionId_Rois_RoiId
        : RequestObject<SetInterpretationRoiDTO, UpdatedInterpretationRoiDTO, EmptyOutput>
        {
            public readonly uint EditionId;
            public readonly uint RoiId;
            public readonly SetInterpretationRoiDTO Payload;

            /// <summary>
            ///     Update an existing sign ROI in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="roiId">Id of the ROI to be updated</param>
            /// <param name="updateRoi">A JSON object with the updated ROI details</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId, uint roiId, SetInterpretationRoiDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.RoiId = roiId;
                this.Payload = payload;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/roi-id", $"/{RoiId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, RoiId, Payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_Rois_Batch
        : RequestObject<InterpretationRoiDTOList, UpdatedInterpretationRoiDTOList, UpdatedInterpretationRoiDTOList>
        {
            public readonly uint EditionId;
            public readonly InterpretationRoiDTOList Payload;

            /// <summary>
            ///     Update existing sign ROI's in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="updateRois">A JSON object with an array of the updated ROI details</param>
            public V1_Editions_EditionId_Rois_Batch(uint editionId, InterpretationRoiDTOList payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.Payload = payload;
                this.listenerMethod = "UpdatedRoisBatch";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, Payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }
    }

}
