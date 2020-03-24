using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_Editions_AdminShareRequests : RequestObject<EmptyInput, AdminEditorRequestListDTO, EmptyOutput>
        {
            /// <summary>
            /// Requests a list of all outstanding editor requests made by the current user
            /// </summary>
            public V1_Editions_AdminShareRequests() : base(null)
            {
            }
        }
        
        public class V1_Editions_EditorInvitations : RequestObject<EmptyInput, AdminEditorRequestListDTO, EmptyOutput>
        {
            /// <summary>
            /// Requests a list of all outstanding editor requests made by the current user
            /// </summary>
            public V1_Editions_EditorInvitations() : base(null)
            {
            }
        }
        
        public class V1_Editions : RequestObject<EmptyInput, EditionListDTO, EmptyOutput>
        {
            /// <summary>
            ///     Request a listing of all editions available to the user
            /// </summary>
            public V1_Editions() : base(null)
            {
            }
        }

        public class V1_Editions_EditionId : EditionRequestObject<EmptyInput, EditionGroupDTO, EmptyOutput>
        {
            /// <summary>
            ///     Request information about a specific edition
            /// </summary>
            /// <param name="editionId">The editionId for the desired edition</param>
            public V1_Editions_EditionId(uint editionId) : base(editionId)
            {
            }
        }
    }

    public static partial class Post
    {
        public class V1_Editions_EditionId : EditionRequestObject<EditionCopyDTO, EditionDTO, EditionDTO>
        {
            /// <summary>
            ///     Request to create a copy of an edition
            /// </summary>
            /// <param name="editionId">Id of the edition to be copied</param>
            public V1_Editions_EditionId(uint editionId, EditionCopyDTO payload) : base(editionId, null, payload)
            {
            }
        }

        public class V1_Editions_EditionId_AddEditorRequest : EditionRequestObject<DetailedEditorRightsDTO, EmptyOutput, EditorInvitationDTO>
        {
            /// <summary>
            ///     Request to add an editor to an edition
            /// </summary>
            /// <param name="editionId">The editionId for the desired edition</param>
            /// <param name="payload">An object containing the settings for the editor and editor rights</param>
            public V1_Editions_EditionId_AddEditorRequest(uint editionId, DetailedEditorRightsDTO payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("RequestedEditor");
            }
        }

        public class V1_Editions_ConfirmEditorship_Token
            : EditionEditorConfirmationObject<string, DetailedEditorRightsDTO, DetailedEditorRightsDTO>
        {
            public V1_Editions_ConfirmEditorship_Token(Guid token, uint editionId) : base(token, editionId)
            {
                listenerMethod.Add("CreatedEditor");
            }
        }
    }

    public static partial class Put
    {
        public class V1_Editions_EditionId_Editors_EditorEmailId
            : EditionEditorRequestObject<UpdateEditorRightsDTO, DetailedEditorRightsDTO, DetailedEditorRightsDTO>
        {
            /// <summary>
            ///     Request to change the access rights of an editor to an edition
            /// </summary>
            /// <param name="editionId">The editionId for the desired edition</param>
            /// <param name="payload">An object containing the settings for the editor and editor rights</param>
            public V1_Editions_EditionId_Editors_EditorEmailId(uint editionId,
                string editorEmail,
                UpdateEditorRightsDTO payload) : base(
                editionId,
                editorEmail,
                null,
                payload
            )
            {
                listenerMethod.Add("updateEditionEditor");
            }
        }

        public class V1_Editions_EditionId : EditionRequestObject<EditionUpdateRequestDTO, EditionDTO, EditionDTO>
        {
            /// <summary>
            ///     Request to update data for the specified edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="payload">JSON object with the attributes to be updated</param>
            public V1_Editions_EditionId(uint editionId, EditionUpdateRequestDTO payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("updateEdition");
            }
        }
    }

    public static partial class Delete
    {
        public class V1_Editions_EditionId : EditionRequestObject<EmptyInput, DeleteTokenDTO, DeleteTokenDTO>
        {
            private readonly List<string> _optional;
            private readonly string _token;

            /// <summary>
            ///     Request to delete an edition
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="optional">Optional parameters: 'deleteForAllEditors'</param>
            /// <param name="token">token required when using optional 'deleteForAllEditors'</param>
            public V1_Editions_EditionId(uint editionId, List<string> optional, string token) : base(editionId)
            {
                _optional = optional;
                _token = token;
                listenerMethod.Add("deleteEdition");
            }

            protected override string HttpPath()
            {
                var http = requestPath.Replace("/edition-id", $"/{editionId.ToString()}");
                if (_optional.Count > 0)
                    http += "?optional[]=" + string.Join("&", _optional);
                if (!string.IsNullOrEmpty(_token))
                    http += $"{(_optional.Count > 0 ? "&" : "?")}token={_token}";
                return http;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), editionId, _optional, _token);
            }
        }
    }
}

// /*ApiRequests Template*/
// public static partial class Get
// {
//     public static partial class V1
//     {
//         public static class Editions
//         {
//             
//         }
//     }
// }
//
// public static partial class Post
// {
//     public static partial class V1
//     {
//         public static class Editions
//         {
//         }
//     }
// }
//
// public static partial class Put
// {
//     public static partial class V1
//     {
//         public static class Editions
//         {
//         }
//     }
// }
//
// public static partial class Delete
// {
//     public static partial class V1
//     {
//         public static class Editions
//         {
//         }
//     }
// }