
using System;
using System.Collections.Generic;
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
            public readonly uint EditionId;

            /// <summary>
            ///     Retrieves the ids of all Fragments of all fragments in the given edition of a scroll
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <returns>An array of the text fragment ids in correct sequence</returns>
            public V1_Editions_EditionId_TextFragments(uint editionId)
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

        public class V1_Editions_EditionId_TextFragments_TextFragmentId_Artefacts
        : RequestObject<EmptyInput, ArtefactDataListDTO, EmptyOutput>
        {
            public readonly uint EditionId;
            public readonly uint TextFragmentId;

            /// <summary>
            ///     Retrieves the ids of all Artefacts in the given textFragmentName
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="textFragmentId">Id of the text fragment</param>
            /// <returns>An array of the line ids in the proper sequence</returns>
            public V1_Editions_EditionId_TextFragments_TextFragmentId_Artefacts(uint editionId, uint textFragmentId)
                : base(null)
            {
                this.EditionId = editionId;
                this.TextFragmentId = textFragmentId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/text-fragment-id", $"/{TextFragmentId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, TextFragmentId);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_TextFragments_TextFragmentId_Lines
        : RequestObject<EmptyInput, LineDataListDTO, EmptyOutput>
        {
            public readonly uint EditionId;
            public readonly uint TextFragmentId;

            /// <summary>
            ///     Retrieves the ids of all lines in the given textFragmentName
            /// </summary>
            /// <param name="editionId">Id of the edition</param>
            /// <param name="textFragmentId">Id of the text fragment</param>
            /// <returns>An array of the line ids in the proper sequence</returns>
            public V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(uint editionId, uint textFragmentId)
                : base(null)
            {
                this.EditionId = editionId;
                this.TextFragmentId = textFragmentId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/text-fragment-id", $"/{TextFragmentId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, TextFragmentId);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_TextFragments_TextFragmentId
        : RequestObject<EmptyInput, TextEditionDTO, EmptyOutput>
        {
            public readonly uint EditionId;
            public readonly uint TextFragmentId;

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
                : base(null)
            {
                this.EditionId = editionId;
                this.TextFragmentId = textFragmentId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/text-fragment-id", $"/{TextFragmentId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, TextFragmentId);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_Lines_LineId
        : RequestObject<EmptyInput, LineTextDTO, EmptyOutput>
        {
            public readonly uint EditionId;
            public readonly uint LineId;

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
                : base(null)
            {
                this.EditionId = editionId;
                this.LineId = lineId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/line-id", $"/{LineId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, LineId);
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


        public class V1_Editions_EditionId_TextFragments
        : RequestObject<CreateTextFragmentDTO, TextFragmentDataDTO, TextFragmentDataDTO>
        {
            public readonly uint EditionId;
            public readonly CreateTextFragmentDTO Payload;

            /// <summary>
            ///     Creates a new text fragment in the given edition of a scroll
            /// </summary>
            /// <param name="createFragment">A JSON object with the details of the new text fragment to be created</param>
            /// <param name="editionId">Id of the edition</param>
            public V1_Editions_EditionId_TextFragments(uint editionId, CreateTextFragmentDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.Payload = payload;
                this.listenerMethod = "CreatedTextFragment";
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


        public class V1_Editions_EditionId_TextFragments_TextFragmentId
        : RequestObject<UpdateTextFragmentDTO, TextFragmentDataDTO, TextFragmentDataDTO>
        {
            public readonly uint EditionId;
            public readonly uint TextFragmentId;
            public readonly UpdateTextFragmentDTO Payload;

            /// <summary>
            ///     Updates the specified text fragment with the submitted properties
            /// </summary>
            /// <param name="editionId">Edition of the text fragment being updates</param>
            /// <param name="textFragmentId">Id of the text fragment being updates</param>
            /// <param name="updatedTextFragment">Details of the updated text fragment</param>
            /// <returns>The details of the updated text fragment</returns>
            public V1_Editions_EditionId_TextFragments_TextFragmentId(uint editionId, uint textFragmentId, UpdateTextFragmentDTO payload)
                : base(payload)
            {
                this.EditionId = editionId;
                this.TextFragmentId = textFragmentId;
                this.Payload = payload;
                this.listenerMethod = "CreatedTextFragment";
            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/text-fragment-id", $"/{TextFragmentId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, TextFragmentId, Payload);
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
