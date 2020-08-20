
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
        : RequestObject<EmptyInput, DeleteTokenDTO, DeleteTokenDTO>
        {
            public readonly uint EditionId;
            public readonly List<string> Optional;
            public readonly string Token;

            /// <summary>
            ///     Provides details about the specified edition and all accessible alternate editions
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="optional">Optional parameters: 'deleteForAllEditors'</param>
            /// <param name="token">token required when using optional 'deleteForAllEditors'</param>
            public V1_Editions_EditionId(uint editionId, List<string> optional = null, string token = null)
                : base(null)
            {
                this.EditionId = editionId;
                this.Optional = optional;
                this.Token = token;
                this.listenerMethod = "DeletedEdition";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}")
                    + (Optional != null ? $"?optional={string.Join(",", Optional)}" : "")
                    + (Token != null ? $"&token={Token}" : "");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, Optional, Token);
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


        public class V1_Editions_AdminShareRequests
        : RequestObject<EmptyInput, AdminEditorRequestListDTO, EmptyOutput>
        {


            /// <summary>
            ///     Get a list of requests issued by the current user for other users
            ///     to become editors of a shared edition
            /// </summary>
            /// <returns></returns>
            public V1_Editions_AdminShareRequests()
                : base(null)
            {


            }

            protected override string HttpPath()
            {
                return requestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString());
            }


        }

        public class V1_Editions_EditorInvitations
        : RequestObject<EmptyInput, EditorInvitationListDTO, EmptyOutput>
        {


            /// <summary>
            ///     Get a list of invitations issued to the current user to become an editor of a shared edition
            /// </summary>
            /// <returns></returns>
            public V1_Editions_EditorInvitations()
                : base(null)
            {


            }

            protected override string HttpPath()
            {
                return requestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString());
            }


        }

        public class V1_Editions_EditionId
        : RequestObject<EmptyInput, EditionGroupDTO, EmptyOutput>
        {
            public readonly uint EditionId;

            /// <summary>
            ///     Provides details about the specified edition and all accessible alternate editions
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            public V1_Editions_EditionId(uint editionId)
                : base(null)
            {
                this.EditionId = editionId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions
        : RequestObject<EmptyInput, EditionListDTO, EmptyOutput>
        {


            /// <summary>
            ///     Provides a listing of all editions accessible to the current user
            /// </summary>
            public V1_Editions()
                : base(null)
            {


            }

            protected override string HttpPath()
            {
                return requestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString());
            }


        }

        public class _V1_Editions_EditionId_ScriptCollection
        : RequestObject<EmptyInput, EditionScriptCollectionDTO, EmptyOutput>
        {
            public readonly uint EditionId;

            /// <summary>
            ///     Provides spatial data for all letters in the edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <returns></returns>
            public _V1_Editions_EditionId_ScriptCollection(uint editionId)
                : base(null)
            {
                this.EditionId = editionId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class _V1_Editions_EditionId_ScriptLines
        : RequestObject<EmptyInput, EditionScriptLinesDTO, EmptyOutput>
        {
            public readonly uint EditionId;

            /// <summary>
            ///     Provides spatial data for all letters in the edition organized and oriented
            ///     by lines.
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <returns></returns>
            public _V1_Editions_EditionId_ScriptLines(uint editionId)
                : base(null)
            {
                this.EditionId = editionId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId);
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


        public class V1_Editions_EditionId_AddEditorRequest
        : RequestObject<InviteEditorDTO, EmptyOutput, EditorInvitationDTO>
        {
            public readonly uint EditionId;
            public readonly InviteEditorDTO Payload;

            /// <summary>
            ///     Adds an editor to the specified edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="payload">JSON object with the attributes of the new editor</param>
            public V1_Editions_EditionId_AddEditorRequest(uint editionId, InviteEditorDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.Payload = payload;
                this.listenerMethod = "RequestedEditor";
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

        public class V1_Editions_ConfirmEditorship_Token
        : RequestObject<EmptyInput, DetailedEditorRightsDTO, DetailedEditorRightsDTO>
        {
            public readonly string Token;

            /// <summary>
            ///     Confirm addition of an editor to the specified edition
            /// </summary>
            /// <param name="token">JWT for verifying the request confirmation</param>
            public V1_Editions_ConfirmEditorship_Token(string token)
                : base(null)
            {
                this.Token = token;
                this.listenerMethod = "CreatedEditor";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/token", $"/{Token.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), Token);
            }


        }

        public class V1_Editions_EditionId
        : RequestObject<EditionCopyDTO, EditionDTO, EditionDTO>
        {
            public readonly uint EditionId;
            public readonly EditionCopyDTO Payload;

            /// <summary>
            ///     Creates a copy of the specified edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
            public V1_Editions_EditionId(uint editionId, EditionCopyDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.Payload = payload;
                this.listenerMethod = "CreatedEdition";
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


        public class V1_Editions_EditionId_Editors_EditorEmailId
        : RequestObject<UpdateEditorRightsDTO, DetailedEditorRightsDTO, DetailedEditorRightsDTO>
        {
            public readonly uint EditionId;
            public readonly string EditorEmailId;
            public readonly UpdateEditorRightsDTO Payload;

            /// <summary>
            ///     Changes the rights for an editor of the specified edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="editorEmailId">Email address of the editor whose permissions are being changed</param>
            /// <param name="payload">JSON object with the attributes of the new editor</param>
            public V1_Editions_EditionId_Editors_EditorEmailId(uint editionId, string editorEmailId, UpdateEditorRightsDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.EditorEmailId = editorEmailId;
                this.Payload = payload;
                this.listenerMethod = "CreatedEditor";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/editor-email-id", $"/{EditorEmailId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, EditorEmailId, Payload);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId
        : RequestObject<EditionUpdateRequestDTO, EditionDTO, EditionDTO>
        {
            public readonly uint EditionId;
            public readonly EditionUpdateRequestDTO Payload;

            /// <summary>
            ///     Updates data for the specified edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="request">JSON object with the attributes to be updated</param>
            public V1_Editions_EditionId(uint editionId, EditionUpdateRequestDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.Payload = payload;
                this.listenerMethod = "UpdatedEdition";
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
