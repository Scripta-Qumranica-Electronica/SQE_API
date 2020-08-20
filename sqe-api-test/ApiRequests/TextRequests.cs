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
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_Editions_EditionId_TextFragments
            : RequestObject<EmptyInput, TextFragmentDataListDTO, EmptyOutput>
        {
            private readonly uint _editionId;

            /// <summary>
            ///     Retrieves the ids of all Fragments of all fragments in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <returns>An array of the text fragment ids in correct sequence</returns>
            public V1_Editions_EditionId_TextFragments(uint editionId)

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

        public class V1_Editions_EditionId_TextFragments_TextFragmentId_Artefacts
            : RequestObject<EmptyInput, ArtefactDataListDTO, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly uint _textFragmentId;

            /// <summary>
            ///     Retrieves the ids of all Artefacts in the given textFragmentName
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="textFragmentId">Id of the text fragment</param>
            /// <returns>An array of the line ids in the proper sequence</returns>
            public V1_Editions_EditionId_TextFragments_TextFragmentId_Artefacts(uint editionId, uint textFragmentId)

            {
                _editionId = editionId;
                _textFragmentId = textFragmentId;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/text-fragment-id", $"/{_textFragmentId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _textFragmentId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_TextFragments_TextFragmentId_Lines
            : RequestObject<EmptyInput, LineDataListDTO, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly uint _textFragmentId;

            /// <summary>
            ///     Retrieves the ids of all lines in the given textFragmentName
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="textFragmentId">Id of the text fragment</param>
            /// <returns>An array of the line ids in the proper sequence</returns>
            public V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(uint editionId, uint textFragmentId)

            {
                _editionId = editionId;
                _textFragmentId = textFragmentId;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/text-fragment-id", $"/{_textFragmentId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _textFragmentId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_TextFragments_TextFragmentId
            : RequestObject<EmptyInput, TextEditionDTO, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly uint _textFragmentId;

            /// <summary>
            ///     Retrieves all signs and their data from the given textFragmentName
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="textFragmentId">Id of the text fragment</param>
            /// <returns>
            ///     A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
            ///     sequence
            /// </returns>
            public V1_Editions_EditionId_TextFragments_TextFragmentId(uint editionId, uint textFragmentId)

            {
                _editionId = editionId;
                _textFragmentId = textFragmentId;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/text-fragment-id", $"/{_textFragmentId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _textFragmentId);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_Lines_LineId
            : RequestObject<EmptyInput, LineTextDTO, EmptyOutput>
        {
            private readonly uint _editionId;
            private readonly uint _lineId;

            /// <summary>
            ///     Retrieves all signs and their data from the given line
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="lineId">Id of the line</param>
            /// <returns>
            ///     A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
            ///     sequence
            /// </returns>
            public V1_Editions_EditionId_Lines_LineId(uint editionId, uint lineId)

            {
                _editionId = editionId;
                _lineId = lineId;
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/line-id", $"/{_lineId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _lineId);
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
        public class V1_Editions_EditionId_TextFragments
            : RequestObject<CreateTextFragmentDTO, TextFragmentDataDTO, TextFragmentDataDTO>
        {
            private readonly uint _editionId;
            private readonly CreateTextFragmentDTO _payload;

            /// <summary>
            ///     Creates a new text fragment in the given edition of a scroll
            /// </summary>
            /// <param name="createFragment">A JSON object with the details of the new text fragment to be created</param>
            /// <param name="editionId">Id of the edition</param>
            public V1_Editions_EditionId_TextFragments(uint editionId, CreateTextFragmentDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _payload = payload;
                ListenerMethod = "CreatedTextFragment";
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
        public class V1_Editions_EditionId_TextFragments_TextFragmentId
            : RequestObject<UpdateTextFragmentDTO, TextFragmentDataDTO, TextFragmentDataDTO>
        {
            private readonly uint _editionId;
            private readonly UpdateTextFragmentDTO _payload;
            private readonly uint _textFragmentId;

            /// <summary>
            ///     Updates the specified text fragment with the submitted properties
            /// </summary>
            /// <param name="editionId">Edition of the text fragment being updates</param>
            /// <param name="textFragmentId">Id of the text fragment being updates</param>
            /// <param name="updatedTextFragment">Details of the updated text fragment</param>
            /// <returns>The details of the updated text fragment</returns>
            public V1_Editions_EditionId_TextFragments_TextFragmentId(uint editionId, uint textFragmentId,
                UpdateTextFragmentDTO payload)
                : base(payload)
            {
                _editionId = editionId;
                _textFragmentId = textFragmentId;
                _payload = payload;
                ListenerMethod = "CreatedTextFragment";
            }

            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                    .Replace("/text-fragment-id", $"/{_textFragmentId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _textFragmentId, _payload);
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