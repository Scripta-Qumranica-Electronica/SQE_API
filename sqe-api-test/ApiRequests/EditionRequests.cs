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


        public class V1_Editions_EditionId
        : RequestObject<EmptyInput, DeleteTokenDTO>
        {
            private readonly uint _editionId;
            private readonly List<string> _optional;
            private readonly string _token;

            public class Listeners
            {
                public ListenerMethods DeletedEdition = ListenerMethods.DeletedEdition;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Provides details about the specified edition and all accessible alternate editions
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="optional">Optional parameters: 'deleteForAllEditors'</param>
            /// <param name="token">token required when using optional 'deleteForAllEditors'</param>
            public V1_Editions_EditionId(uint editionId, List<string> optional = null, string token = null)

            {
                _editionId = editionId;
                _optional = optional;
                _token = token;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.DeletedEdition, (DeletedEditionIsNull, DeletedEditionListener));
            }

            public DeleteTokenDTO DeletedEdition { get; private set; }
            private void DeletedEditionListener(HubConnection signalrListener) => signalrListener.On<DeleteTokenDTO>("DeletedEdition", receivedData => DeletedEdition = receivedData);
            private bool DeletedEditionIsNull() => DeletedEdition == null;

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    + (_optional != null ? $"?optional={string.Join(",", _optional)}" : "")
                    + (_token != null ? $"&token={_token}" : "");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _optional, _token);
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


        public class V1_Editions_AdminShareRequests
        : RequestObject<EmptyInput, AdminEditorRequestListDTO>
        {




            /// <summary>
            ///     Get a list of requests issued by the current user for other users
            ///     to become editors of a shared edition
            /// </summary>
            /// <returns></returns>
            public V1_Editions_AdminShareRequests()

            {


            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString());
            }


        }

        public class V1_Editions_EditorInvitations
        : RequestObject<EmptyInput, EditorInvitationListDTO>
        {




            /// <summary>
            ///     Get a list of invitations issued to the current user to become an editor of a shared edition
            /// </summary>
            /// <returns></returns>
            public V1_Editions_EditorInvitations()

            {


            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString());
            }


        }

        public class V1_Editions_EditionId
        : RequestObject<EmptyInput, EditionGroupDTO>
        {
            private readonly uint _editionId;



            /// <summary>
            ///     Provides details about the specified edition and all accessible alternate editions
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            public V1_Editions_EditionId(uint editionId)

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

        public class V1_Editions
        : RequestObject<EmptyInput, EditionListDTO>
        {




            /// <summary>
            ///     Provides a listing of all editions accessible to the current user
            /// </summary>
            public V1_Editions()

            {


            }



            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString());
            }


        }

        public class V1_Editions_EditionId_ScriptCollection
        : RequestObject<EmptyInput, EditionScriptCollectionDTO>
        {
            private readonly uint _editionId;



            /// <summary>
            ///     Provides spatial data for all letters in the edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <returns></returns>
            public V1_Editions_EditionId_ScriptCollection(uint editionId)

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

        public class V1_Editions_EditionId_ScriptLines
        : RequestObject<EmptyInput, EditionScriptLinesDTO>
        {
            private readonly uint _editionId;



            /// <summary>
            ///     Provides spatial data for all letters in the edition organized and oriented
            ///     by lines.
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <returns></returns>
            public V1_Editions_EditionId_ScriptLines(uint editionId)

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
    }

    public static partial class Post
    {


        public class V1_Editions_EditionId_AddEditorRequest
        : RequestObject<InviteEditorDTO, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly InviteEditorDTO _payload;

            public class Listeners
            {
                public ListenerMethods RequestedEditor = ListenerMethods.RequestedEditor;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Adds an editor to the specified edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="payload">JSON object with the attributes of the new editor</param>
            public V1_Editions_EditionId_AddEditorRequest(uint editionId, InviteEditorDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.RequestedEditor, (RequestedEditorIsNull, RequestedEditorListener));
            }

            public EditorInvitationDTO RequestedEditor { get; private set; }
            private void RequestedEditorListener(HubConnection signalrListener) => signalrListener.On<EditorInvitationDTO>("RequestedEditor", receivedData => RequestedEditor = receivedData);
            private bool RequestedEditorIsNull() => RequestedEditor == null;

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

        public class V1_Editions_ConfirmEditorship_Token
        : RequestObject<EmptyInput, DetailedEditorRightsDTO>
        {
            private readonly string _token;

            public class Listeners
            {
                public ListenerMethods CreatedEditor = ListenerMethods.CreatedEditor;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Confirm addition of an editor to the specified edition
            /// </summary>
            /// <param name="token">JWT for verifying the request confirmation</param>
            public V1_Editions_ConfirmEditorship_Token(string token)

            {
                _token = token;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.CreatedEditor, (CreatedEditorIsNull, CreatedEditorListener));
            }

            public DetailedEditorRightsDTO CreatedEditor { get; private set; }
            private void CreatedEditorListener(HubConnection signalrListener) => signalrListener.On<DetailedEditorRightsDTO>("CreatedEditor", receivedData => CreatedEditor = receivedData);
            private bool CreatedEditorIsNull() => CreatedEditor == null;

            protected override string HttpPath()
            {
                return RequestPath.Replace("/token", $"/{_token.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _token);
            }


        }

        public class V1_Editions_EditionId
        : RequestObject<EditionCopyDTO, EditionDTO>
        {
            private readonly uint _editionId;
            private readonly EditionCopyDTO _payload;

            public class Listeners
            {
                public ListenerMethods CreatedEdition = ListenerMethods.CreatedEdition;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Creates a copy of the specified edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
            public V1_Editions_EditionId(uint editionId, EditionCopyDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.CreatedEdition, (CreatedEditionIsNull, CreatedEditionListener));
            }

            public EditionDTO CreatedEdition { get; private set; }
            private void CreatedEditionListener(HubConnection signalrListener) => signalrListener.On<EditionDTO>("CreatedEdition", receivedData => CreatedEdition = receivedData);
            private bool CreatedEditionIsNull() => CreatedEdition == null;

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


        public class V1_Editions_EditionId_Editors_EditorEmailId
        : RequestObject<UpdateEditorRightsDTO, DetailedEditorRightsDTO>
        {
            private readonly uint _editionId;
            private readonly string _editorEmailId;
            private readonly UpdateEditorRightsDTO _payload;

            public class Listeners
            {
                public ListenerMethods CreatedEditor = ListenerMethods.CreatedEditor;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Changes the rights for an editor of the specified edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="editorEmailId">Email address of the editor whose permissions are being changed</param>
            /// <param name="payload">JSON object with the attributes of the new editor</param>
            public V1_Editions_EditionId_Editors_EditorEmailId(uint editionId, string editorEmailId, UpdateEditorRightsDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _editorEmailId = editorEmailId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.CreatedEditor, (CreatedEditorIsNull, CreatedEditorListener));
            }

            public DetailedEditorRightsDTO CreatedEditor { get; private set; }
            private void CreatedEditorListener(HubConnection signalrListener) => signalrListener.On<DetailedEditorRightsDTO>("CreatedEditor", receivedData => CreatedEditor = receivedData);
            private bool CreatedEditorIsNull() => CreatedEditor == null;

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}").Replace("/editor-email-id", $"/{_editorEmailId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _editorEmailId, _payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId
        : RequestObject<EditionUpdateRequestDTO, EditionDTO>
        {
            private readonly uint _editionId;
            private readonly EditionUpdateRequestDTO _payload;

            public class Listeners
            {
                public ListenerMethods UpdatedEdition = ListenerMethods.UpdatedEdition;
            };
            public Listeners AvailableListeners { get; }

            /// <summary>
            ///     Updates data for the specified edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="request">JSON object with the attributes to be updated</param>
            public V1_Editions_EditionId(uint editionId, EditionUpdateRequestDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                AvailableListeners = new Listeners();
                _listenerDict.Add(ListenerMethods.UpdatedEdition, (UpdatedEditionIsNull, UpdatedEditionListener));
            }

            public EditionDTO UpdatedEdition { get; private set; }
            private void UpdatedEditionListener(HubConnection signalrListener) => signalrListener.On<EditionDTO>("UpdatedEdition", receivedData => UpdatedEdition = receivedData);
            private bool UpdatedEditionIsNull() => UpdatedEdition == null;

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
