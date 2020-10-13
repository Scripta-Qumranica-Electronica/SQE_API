/*
 * This file is automatically generated by the GenerateTestRequestObjects
 * project in the Utilities folder. Do not edit this file directly as
 * its contents may be overwritten at any point.
 *
 * Should a class here need to be altered for any reason, you should look
 * first to the auto generation program for possible updating to include
 * the needed special case. Otherwise, it is possible to create your own
 * manually written ApiRequest object, though this is generally discouraged.
 */


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
        : RequestObject<EmptyInput, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly uint _roiId;

            public class Listeners
            {
                public ListenerMethods DeletedRoi = ListenerMethods.DeletedRoi;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Deletes a sign ROI from the given edition of a scroll
            /// </summary>
            /// <param name="roiId">Id of the ROI to be deleted</param>
            /// <param name="editionId">Id of the edition</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId, uint roiId)

            {
                _editionId = editionId;
                _roiId = roiId;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.DeletedRoi, (DeletedRoiIsNull, DeletedRoiListener));
            }

            public DeleteDTO DeletedRoi { get; private set; }
            private void DeletedRoiListener(HubConnection signalrListener) => signalrListener.On<DeleteDTO>("DeletedRoi", receivedData => DeletedRoi = receivedData);
            private bool DeletedRoiIsNull() => DeletedRoi == null;

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/roi-id", $"/{_roiId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _roiId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }
    }

    public static partial class Get
    {


        public class V1_Editions_EditionId_Rois_RoiId
        : RequestObject<EmptyInput, InterpretationRoiDTO>
        {
            private readonly uint _editionId;
            private readonly uint _roiId;



            /// <summary>
            ///     Get the details for a ROI in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="roiId">A JSON object with the new ROI to be created</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId, uint roiId)

            {
                _editionId = editionId;
                _roiId = roiId;

            }



            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/roi-id", $"/{_roiId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _roiId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }
    }

    public static partial class Post
    {


        public class V1_Editions_EditionId_Rois
        : RequestObject<SetInterpretationRoiDTO, InterpretationRoiDTO>
        {
            private readonly uint _editionId;
            private readonly SetInterpretationRoiDTO _payload;

            public class Listeners
            {
                public ListenerMethods CreatedRoisBatch = ListenerMethods.CreatedRoisBatch;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Creates new sign ROI in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="newRoi">A JSON object with the new ROI to be created</param>
            public V1_Editions_EditionId_Rois(uint editionId, SetInterpretationRoiDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.CreatedRoisBatch, (CreatedRoisBatchIsNull, CreatedRoisBatchListener));
            }

            public InterpretationRoiDTOList CreatedRoisBatch { get; private set; }
            private void CreatedRoisBatchListener(HubConnection signalrListener) => signalrListener.On<InterpretationRoiDTOList>("CreatedRoisBatch", receivedData => CreatedRoisBatch = receivedData);
            private bool CreatedRoisBatchIsNull() => CreatedRoisBatch == null;

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_Rois_Batch
        : RequestObject<SetInterpretationRoiDTOList, InterpretationRoiDTOList>
        {
            private readonly uint _editionId;
            private readonly SetInterpretationRoiDTOList _payload;

            public class Listeners
            {
                public ListenerMethods EditedRoisBatch = ListenerMethods.EditedRoisBatch;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Creates new sign ROI's in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="newRois">A JSON object with an array of the new ROI's to be created</param>
            public V1_Editions_EditionId_Rois_Batch(uint editionId, SetInterpretationRoiDTOList payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.EditedRoisBatch, (EditedRoisBatchIsNull, EditedRoisBatchListener));
            }

            public BatchEditRoiResponseDTO EditedRoisBatch { get; private set; }
            private void EditedRoisBatchListener(HubConnection signalrListener) => signalrListener.On<BatchEditRoiResponseDTO>("EditedRoisBatch", receivedData => EditedRoisBatch = receivedData);
            private bool EditedRoisBatchIsNull() => EditedRoisBatch == null;

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_Rois_BatchEdit
        : RequestObject<BatchEditRoiDTO, BatchEditRoiResponseDTO>
        {
            private readonly uint _editionId;
            private readonly BatchEditRoiDTO _payload;

            public class Listeners
            {
                public ListenerMethods EditedRoisBatch = ListenerMethods.EditedRoisBatch;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Processes a series of create/update/delete ROI requests in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="rois">A JSON object with all the roi edits to be performed</param>
            public V1_Editions_EditionId_Rois_BatchEdit(uint editionId, BatchEditRoiDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.EditedRoisBatch, (EditedRoisBatchIsNull, EditedRoisBatchListener));
            }

            public BatchEditRoiResponseDTO EditedRoisBatch { get; private set; }
            private void EditedRoisBatchListener(HubConnection signalrListener) => signalrListener.On<BatchEditRoiResponseDTO>("EditedRoisBatch", receivedData => EditedRoisBatch = receivedData);
            private bool EditedRoisBatchIsNull() => EditedRoisBatch == null;

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }
    }

    public static partial class Put
    {


        public class V1_Editions_EditionId_Rois_RoiId
        : RequestObject<SetInterpretationRoiDTO, UpdatedInterpretationRoiDTO>
        {
            private readonly uint _editionId;
            private readonly uint _roiId;
            private readonly SetInterpretationRoiDTO _payload;

            public class Listeners
            {
                public ListenerMethods EditedRoisBatch = ListenerMethods.EditedRoisBatch;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Update an existing sign ROI in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="roiId">Id of the ROI to be updated</param>
            /// <param name="updateRoi">A JSON object with the updated ROI details</param>
            public V1_Editions_EditionId_Rois_RoiId(uint editionId, uint roiId, SetInterpretationRoiDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _roiId = roiId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.EditedRoisBatch, (EditedRoisBatchIsNull, EditedRoisBatchListener));
            }

            public BatchEditRoiResponseDTO EditedRoisBatch { get; private set; }
            private void EditedRoisBatchListener(HubConnection signalrListener) => signalrListener.On<BatchEditRoiResponseDTO>("EditedRoisBatch", receivedData => EditedRoisBatch = receivedData);
            private bool EditedRoisBatchIsNull() => EditedRoisBatch == null;

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/roi-id", $"/{_roiId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _roiId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_Rois_Batch
        : RequestObject<UpdateInterpretationRoiDTOList, UpdatedInterpretationRoiDTOList>
        {
            private readonly uint _editionId;
            private readonly UpdateInterpretationRoiDTOList _payload;

            public class Listeners
            {
                public ListenerMethods UpdatedRoisBatch = ListenerMethods.UpdatedRoisBatch;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Update existing sign ROI's in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="updateRois">A JSON object with an array of the updated ROI details</param>
            public V1_Editions_EditionId_Rois_Batch(uint editionId, UpdateInterpretationRoiDTOList payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.UpdatedRoisBatch, (UpdatedRoisBatchIsNull, UpdatedRoisBatchListener));
            }

            public UpdatedInterpretationRoiDTOList UpdatedRoisBatch { get; private set; }
            private void UpdatedRoisBatchListener(HubConnection signalrListener) => signalrListener.On<UpdatedInterpretationRoiDTOList>("UpdatedRoisBatch", receivedData => UpdatedRoisBatch = receivedData);
            private bool UpdatedRoisBatchIsNull() => UpdatedRoisBatch == null;

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }
    }

}
