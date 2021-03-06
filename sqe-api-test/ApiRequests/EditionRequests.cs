using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_Editions : RequestObject<EmptyInput, EditionListDTO>
        {
            /// <summary>
            ///     Request a listing of all editions available to the user
            /// </summary>
            public V1_Editions() : base(null)
            {
            }
        }

        public class V1_Editions_EditionId : EditionRequestObject<EmptyInput, EditionGroupDTO>
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
        public class V1_Editions_EditionId : EditionRequestObject<EditionCopyDTO, EditionDTO>
        {
            /// <summary>
            ///     Request to create a copy of an edition
            /// </summary>
            /// <param name="editionId">Id of the edition to be copied</param>
            public V1_Editions_EditionId(uint editionId, EditionCopyDTO payload) : base(editionId, null, payload)
            {
            }
        }

        public class V1_Editions_EditionId_Editors : EditionRequestObject<EditorRightsDTO, EditorRightsDTO>
        {
            /// <summary>
            ///     Request to add an editor to an edition
            /// </summary>
            /// <param name="editionId">The editionId for the desired edition</param>
            /// <param name="payload">An object containing the settings for the editor and editor rights</param>
            public V1_Editions_EditionId_Editors(uint editionId, EditorRightsDTO payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("addEditionEditor");
            }
        }
    }

    public static partial class Put
    {
        public class V1_Editions_EditionId_Editors : EditionRequestObject<EditorRightsDTO, EditorRightsDTO>
        {
            /// <summary>
            ///     Request to change the access rights of an editor to an edition
            /// </summary>
            /// <param name="editionId">The editionId for the desired edition</param>
            /// <param name="payload">An object containing the settings for the editor and editor rights</param>
            public V1_Editions_EditionId_Editors(uint editionId, EditorRightsDTO payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("updateEditionEditor");
            }
        }

        public class V1_Editions_EditionId : EditionRequestObject<EditionUpdateRequestDTO, EditionDTO>
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
        public class V1_Editions_EditionId : EditionRequestObject<string, DeleteTokenDTO>
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