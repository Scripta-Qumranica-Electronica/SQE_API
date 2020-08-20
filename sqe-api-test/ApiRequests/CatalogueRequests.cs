
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


    public static partial class Delete
    {


        public class V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId
        : RequestObject<EmptyInput, EmptyOutput, EmptyOutput>
        {
            public readonly uint IaaEditionCatalogToTextFragmentId;

            /// <summary>
            ///     Remove an existing imaged object and text fragment match, which is not correct
            /// </summary>
            /// <param name="iaaEditionCatalogToTextFragmentId">The unique id of the match to confirm</param>
            /// <returns></returns>
            public V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId(uint iaaEditionCatalogToTextFragmentId)
                : base(null)
            {
                this.IaaEditionCatalogToTextFragmentId = iaaEditionCatalogToTextFragmentId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/iaa-edition-catalog-to-text-fragment-id", $"/{IaaEditionCatalogToTextFragmentId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), IaaEditionCatalogToTextFragmentId);
            }


        }
    }

    public static partial class Get
    {


        public class V1_Catalogue_ImagedObjects_ImagedObjectId_TextFragments
        : RequestObject<EmptyInput, CatalogueMatchListDTO, EmptyOutput>
        {
            public readonly string ImagedObjectId;

            /// <summary>
            ///     Get a listing of all text fragments matches that correspond to an imaged object
            /// </summary>
            /// <param name="imagedObjectId">Id of imaged object to search for transcription matches</param>
            public V1_Catalogue_ImagedObjects_ImagedObjectId_TextFragments(string imagedObjectId)
                : base(null)
            {
                this.ImagedObjectId = imagedObjectId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/imaged-object-id", $"/{ImagedObjectId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), ImagedObjectId);
            }


        }

        public class V1_Catalogue_TextFragments_TextFragmentId_ImagedObjects
        : RequestObject<EmptyInput, CatalogueMatchListDTO, EmptyOutput>
        {
            public readonly uint TextFragmentId;

            /// <summary>
            ///     Get a listing of all imaged objects that matches that correspond to a transcribed text fragment
            /// </summary>
            /// <param name="textFragmentId">Unique Id of the text fragment to search for imaged object matches</param>
            public V1_Catalogue_TextFragments_TextFragmentId_ImagedObjects(uint textFragmentId)
                : base(null)
            {
                this.TextFragmentId = textFragmentId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/text-fragment-id", $"/{TextFragmentId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), TextFragmentId);
            }


        }

        public class V1_Catalogue_Editions_EditionId_ImagedObjectTextFragmentMatches
        : RequestObject<EmptyInput, CatalogueMatchListDTO, EmptyOutput>
        {
            public readonly uint EditionId;

            /// <summary>
            ///     Get a listing of all corresponding imaged objects and transcribed text fragment in a specified edition
            /// </summary>
            /// <param name="editionId">Unique Id of the edition to search for imaged objects to text fragment matches</param>
            public V1_Catalogue_Editions_EditionId_ImagedObjectTextFragmentMatches(uint editionId)
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

        public class V1_Catalogue_Manuscript_ManuscriptId_ImagedObjectTextFragmentMatches
        : RequestObject<EmptyInput, CatalogueMatchListDTO, EmptyOutput>
        {
            public readonly uint ManuscriptId;

            /// <summary>
            ///     Get a listing of all corresponding imaged objects and transcribed text fragment in a specified edition
            /// </summary>
            /// <param name="manuscriptId">Unique Id of the edition to search for imaged objects to text fragment matches</param>
            public V1_Catalogue_Manuscript_ManuscriptId_ImagedObjectTextFragmentMatches(uint manuscriptId)
                : base(null)
            {
                this.ManuscriptId = manuscriptId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/manuscript-id", $"/{ManuscriptId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), ManuscriptId);
            }


        }
    }

    public static partial class Post
    {


        public class V1_Catalogue
        : RequestObject<CatalogueMatchInputDTO, EmptyOutput, EmptyOutput>
        {
            public readonly CatalogueMatchInputDTO Payload;

            /// <summary>
            ///     Create a new matched pair for an imaged object and a text fragment along with the edition princeps information
            /// </summary>
            /// <param name="newMatch">The details of the new match</param>
            /// <returns></returns>
            public V1_Catalogue(CatalogueMatchInputDTO payload)
                : base(payload)
            {
                this.Payload = payload;

            }

            protected override string HttpPath()
            {
                return requestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), Payload);
            }


        }

        public class V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId
        : RequestObject<EmptyInput, EmptyOutput, EmptyOutput>
        {
            public readonly uint IaaEditionCatalogToTextFragmentId;

            /// <summary>
            ///     Confirm the correctness of an existing imaged object and text fragment match
            /// </summary>
            /// <param name="iaaEditionCatalogToTextFragmentId">The unique id of the match to confirm</param>
            /// <returns></returns>
            public V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId(uint iaaEditionCatalogToTextFragmentId)
                : base(null)
            {
                this.IaaEditionCatalogToTextFragmentId = iaaEditionCatalogToTextFragmentId;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/iaa-edition-catalog-to-text-fragment-id", $"/{IaaEditionCatalogToTextFragmentId.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), IaaEditionCatalogToTextFragmentId);
            }


        }
    }

}
