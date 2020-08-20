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


        public class V1_Editions_EditionId_Artefacts_ArtefactId
        : RequestObject<EmptyInput, EmptyOutput, DeleteDTO>
        {
            private readonly uint _editionId;
            private readonly uint _artefactId;

            /// <summary>
            ///     Deletes the specified artefact
            /// </summary>
            /// <param name="artefactId">Unique Id of the desired artefact</param>
            /// <param name="editionId">Unique Id of the desired edition</param>
            public V1_Editions_EditionId_Artefacts_ArtefactId(uint editionId, uint artefactId)

            {
                _editionId = editionId;
                _artefactId = artefactId;
                ListenerMethod = "DeletedArtefact";
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/artefact-id", $"/{_artefactId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _artefactId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId
        : RequestObject<EmptyInput, DeleteDTO, DeleteDTO>
        {
            private readonly uint _editionId;
            private readonly uint _artefactGroupId;

            /// <summary>
            ///     Deletes the specified artefact group.
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="artefactGroupId">Unique Id of the artefact group to be deleted</param>
            /// <returns></returns>
            public V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId(uint editionId, uint artefactGroupId)

            {
                _editionId = editionId;
                _artefactGroupId = artefactGroupId;
                ListenerMethod = "DeletedArtefactGroup";
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/artefact-group-id", $"/{_artefactGroupId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _artefactGroupId);
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


        public class V1_Editions_EditionId_Artefacts_ArtefactId
        : RequestObject<EmptyInput, ArtefactDTO, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly uint _artefactId;
            private readonly List<string> _optional;

            /// <summary>
            ///     Provides a listing of all artefacts that are part of the specified edition
            /// </summary>
            /// <param name="artefactId">Unique Id of the desired artefact</param>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
            public V1_Editions_EditionId_Artefacts_ArtefactId(uint editionId, uint artefactId, List<string> optional = null)

            {
                _editionId = editionId;
                _artefactId = artefactId;
                _optional = optional;

            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/artefact-id", $"/{_artefactId.ToString()}")
                    + (_optional != null ? $"?optional={string.Join(",", _optional)}" : "");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _artefactId, _optional);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_Artefacts_ArtefactId_Rois
        : RequestObject<EmptyInput, InterpretationRoiDTOList, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly uint _artefactId;

            /// <summary>
            ///     Provides a listing of all rois belonging to an artefact in the specified edition
            /// </summary>
            /// <param name="artefactId">Unique Id of the desired artefact</param>
            /// <param name="editionId">Unique Id of the desired edition</param>
            public V1_Editions_EditionId_Artefacts_ArtefactId_Rois(uint editionId, uint artefactId)

            {
                _editionId = editionId;
                _artefactId = artefactId;

            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/artefact-id", $"/{_artefactId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _artefactId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_Artefacts
        : RequestObject<EmptyInput, ArtefactListDTO, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly List<string> _optional;

            /// <summary>
            ///     Provides a listing of all artefacts that are part of the specified edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
            public V1_Editions_EditionId_Artefacts(uint editionId, List<string> optional = null)

            {
                _editionId = editionId;
                _optional = optional;

            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    + (_optional != null ? $"?optional={string.Join(",", _optional)}" : "");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _optional);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_Artefacts_ArtefactId_TextFragments
        : RequestObject<EmptyInput, ArtefactTextFragmentMatchListDTO, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly uint _artefactId;
            private readonly List<string> _optional;

            /// <summary>
            ///     Provides a listing of text fragments that have text in the specified artefact.
            ///     With the optional query parameter "suggested", this endpoint will also return
            ///     any text fragment that the system suggests might have text in the artefact.
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="artefactId">Unique Id of the desired artefact</param>
            /// <param name="optional">Add "suggested" to include possible matches suggested by the system</param>
            public V1_Editions_EditionId_Artefacts_ArtefactId_TextFragments(uint editionId, uint artefactId, List<string> optional = null)

            {
                _editionId = editionId;
                _artefactId = artefactId;
                _optional = optional;

            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/artefact-id", $"/{_artefactId.ToString()}")
                    + (_optional != null ? $"?optional={string.Join(",", _optional)}" : "");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _artefactId, _optional);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_ArtefactGroups
        : RequestObject<EmptyInput, ArtefactGroupListDTO, EmptyOutput>
        {
            private readonly uint _editionId;

            /// <summary>
            ///     Gets a listing of all artefact groups in the edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <returns></returns>
            public V1_Editions_EditionId_ArtefactGroups(uint editionId)

            {
                _editionId = editionId;

            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId
        : RequestObject<EmptyInput, ArtefactGroupDTO, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly uint _artefactGroupId;

            /// <summary>
            ///     Gets the details of a specific artefact group in the edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="artefactGroupId">Id of the desired artefact group</param>
            /// <returns></returns>
            public V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId(uint editionId, uint artefactGroupId)

            {
                _editionId = editionId;
                _artefactGroupId = artefactGroupId;

            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/artefact-group-id", $"/{_artefactGroupId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _artefactGroupId);
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


        public class V1_Editions_EditionId_Artefacts
        : RequestObject<CreateArtefactDTO, ArtefactDTO, ArtefactDTO>
        {
            private readonly uint _editionId;
            private readonly CreateArtefactDTO _payload;

            /// <summary>
            ///     Creates a new artefact with the provided data.
            ///     If no mask is provided, a placeholder mask will be created with the values:
            ///     "POLYGON((0 0,1 1,1 0,0 0))" (the system requires a valid WKT polygon mask for
            ///     every artefact). It is not recommended to leave the mask, name, or work status
            ///     blank or null. It will often be advantageous to leave the transformation null
            ///     when first creating a new artefact.
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="payload">A CreateArtefactDTO with the data for the new artefact</param>
            public V1_Editions_EditionId_Artefacts(uint editionId, CreateArtefactDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                ListenerMethod = "CreatedArtefact";
            }

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

        public class V1_Editions_EditionId_Artefacts_BatchTransformation
        : RequestObject<BatchUpdateArtefactPlacementDTO, BatchUpdatedArtefactTransformDTO, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly BatchUpdateArtefactPlacementDTO _payload;

            /// <summary>
            ///     Updates the positional data for a batch of artefacts
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="payload">A BatchUpdateArtefactTransformDTO with a list of the desired updates</param>
            /// <returns></returns>
            public V1_Editions_EditionId_Artefacts_BatchTransformation(uint editionId, BatchUpdateArtefactPlacementDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;

            }

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

        public class V1_Editions_EditionId_ArtefactGroups
        : RequestObject<CreateArtefactGroupDTO, ArtefactGroupDTO, ArtefactGroupDTO>
        {
            private readonly uint _editionId;
            private readonly CreateArtefactGroupDTO _payload;

            /// <summary>
            ///     Creates a new artefact group with the submitted data.
            ///     The new artefact must have a list of artefacts that belong to the group.
            ///     It is not necessary to give the group a name.
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="payload">Parameters of the new artefact group</param>
            /// <returns></returns>
            public V1_Editions_EditionId_ArtefactGroups(uint editionId, CreateArtefactGroupDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                ListenerMethod = "CreatedArtefactGroup";
            }

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


        public class V1_Editions_EditionId_Artefacts_ArtefactId
        : RequestObject<UpdateArtefactDTO, ArtefactDTO, ArtefactDTO>
        {
            private readonly uint _editionId;
            private readonly uint _artefactId;
            private readonly UpdateArtefactDTO _payload;

            /// <summary>
            ///     Updates the specified artefact.
            ///     There are many possible attributes that can be changed for
            ///     an artefact.  The caller should only input only those that
            ///     should be changed. Attributes with a null value will be ignored.
            ///     For instance, setting the mask to null or "" will result in
            ///     no changes to the current mask, and no value for the mask will
            ///     be returned (or broadcast). Likewise, the transformation, name,
            ///     or status message may be set to null and no change will be made
            ///     to those entities (though any unchanged values will be returned
            ///     along with the changed values and also broadcast to co-editors).
            /// </summary>
            /// <param name="artefactId">Unique Id of the desired artefact</param>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="payload">An UpdateArtefactDTO with the desired alterations to the artefact</param>
            public V1_Editions_EditionId_Artefacts_ArtefactId(uint editionId, uint artefactId, UpdateArtefactDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _artefactId = artefactId;
                _payload = payload;
                ListenerMethod = "UpdatedArtefact";
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/artefact-id", $"/{_artefactId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _artefactId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId
        : RequestObject<UpdateArtefactGroupDTO, ArtefactGroupDTO, ArtefactGroupDTO>
        {
            private readonly uint _editionId;
            private readonly uint _artefactGroupId;
            private readonly UpdateArtefactGroupDTO _payload;

            /// <summary>
            ///     Updates the details of an artefact group.
            ///     The artefact group will now only contain the artefacts listed in the JSON payload.
            ///     If the name is null, no change will be made, otherwise the name will also be updated.
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="artefactGroupId">Id of the artefact group to be updated</param>
            /// <param name="payload">Parameters that the artefact group should be changed to</param>
            /// <returns></returns>
            public V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId(uint editionId, uint artefactGroupId, UpdateArtefactGroupDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _artefactGroupId = artefactGroupId;
                _payload = payload;
                ListenerMethod = "UpdatedArtefactGroup";
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/artefact-group-id", $"/{_artefactGroupId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _artefactGroupId, _payload);
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
