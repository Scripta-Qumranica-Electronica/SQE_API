
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


    public static partial class Get
    {


        public class V1_ImagedObjects_ImagedObjectId
        : RequestObject<EmptyInput, SimpleImageListDTO, EmptyOutput>
        {
            public readonly string ImagedObjectId;

            /// <summary>
            ///     Provides information for the specified imaged object.
            /// </summary>
            /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
            public V1_ImagedObjects_ImagedObjectId(string imagedObjectId)
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

        public class V1_Editions_EditionId_ImagedObjects_ImagedObjectId
        : RequestObject<EmptyInput, ImagedObjectDTO, EmptyOutput>
        {
            public readonly uint EditionId;
            public readonly string ImagedObjectId;
            public readonly List<string> Optional;

            /// <summary>
            ///     Provides information for the specified imaged object related to the specified edition, can include images and also
            ///     their masks with optional.
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
            /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
            public V1_Editions_EditionId_ImagedObjects_ImagedObjectId(uint editionId, string imagedObjectId, List<string> optional = null)
                : base(null)
            {
                this.EditionId = editionId;
                this.ImagedObjectId = imagedObjectId;
                this.Optional = optional;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}").Replace("/imaged-object-id", $"/{ImagedObjectId.ToString()}")
                    + (Optional != null ? $"?optional={string.Join(",", Optional)}" : "");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, ImagedObjectId, Optional);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_Editions_EditionId_ImagedObjects
        : RequestObject<EmptyInput, ImagedObjectListDTO, EmptyOutput>
        {
            public readonly uint EditionId;
            public readonly List<string> Optional;

            /// <summary>
            ///     Provides a listing of imaged objects related to the specified edition, can include images and also their masks with
            ///     optional.
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
            public V1_Editions_EditionId_ImagedObjects(uint editionId, List<string> optional = null)
                : base(null)
            {
                this.EditionId = editionId;
                this.Optional = optional;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/edition-id", $"/{EditionId.ToString()}")
                    + (Optional != null ? $"?optional={string.Join(",", Optional)}" : "");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), EditionId, Optional);
            }

            public override uint? GetEditionId()
            {
                {
                    return EditionId;
                }
            }
        }

        public class V1_ImagedObjects_Institutions
        : RequestObject<EmptyInput, ImageInstitutionListDTO, EmptyOutput>
        {


            /// <summary>
            ///     Provides a list of all institutional image providers.
            /// </summary>
            public V1_ImagedObjects_Institutions()
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

        public class V1_ImagedObjects_Institutions_Institution
        : RequestObject<EmptyInput, InstitutionalImageListDTO, EmptyOutput>
        {
            public readonly string Institution;

            /// <summary>
            ///     Provides a list of all institutional image providers.
            /// </summary>
            public V1_ImagedObjects_Institutions_Institution(string institution)
                : base(null)
            {
                this.Institution = institution;

            }

            protected override string HttpPath()
            {
                return requestPath.Replace("/institution", $"/{Institution.ToString()}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), Institution);
            }


        }

        public class V1_ImagedObjects_ImagedObjectId_TextFragments
        : RequestObject<EmptyInput, ImagedObjectTextFragmentMatchListDTO, EmptyOutput>
        {
            public readonly string ImagedObjectId;

            /// <summary>
            ///     Provides a list of all text fragments that should correspond to the imaged object.
            /// </summary>
            /// <param name="imagedObjectId">Id of the imaged object</param>
            /// <returns></returns>
            public V1_ImagedObjects_ImagedObjectId_TextFragments(string imagedObjectId)
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
    }

}
