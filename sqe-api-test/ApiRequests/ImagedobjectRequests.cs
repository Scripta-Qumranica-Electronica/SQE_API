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
    public static partial class Get
    {
        public class V1_ImagedObjects_ImagedObjectId
            : RequestObject<EmptyInput, SimpleImageListDTO>
        {
            private readonly string _imagedObjectId;


            /// <summary>
            ///     Provides information for the specified imaged object.
            /// </summary>
            /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
            public V1_ImagedObjects_ImagedObjectId(string imagedObjectId)

            {
                _imagedObjectId = imagedObjectId;
            }


            protected override string HttpPath()
            {
                return RequestPath.Replace("/imaged-object-id", $"/{_imagedObjectId}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _imagedObjectId);
            }
        }

        public class V1_Editions_EditionId_ImagedObjects_ImagedObjectId
            : RequestObject<EmptyInput, ImagedObjectDTO>
        {
            private readonly uint _editionId;
            private readonly string _imagedObjectId;
            private readonly List<string> _optional;


            /// <summary>
            ///     Provides information for the specified imaged object related to the specified edition, can include images and also
            ///     their masks with optional.
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
            /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
            public V1_Editions_EditionId_ImagedObjects_ImagedObjectId(uint editionId, string imagedObjectId,
                List<string> optional = null)

            {
                _editionId = editionId;
                _imagedObjectId = imagedObjectId;
                _optional = optional;
            }


            protected override string HttpPath()
            {
                return RequestPath.Replace("/edition-id", $"/{_editionId.ToString()}")
                           .Replace("/imaged-object-id", $"/{_imagedObjectId}")
                       + (_optional != null ? $"?optional={string.Join(",", _optional)}" : "");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR =>
                    signalR.InvokeAsync<T>(SignalrRequestString(), _editionId, _imagedObjectId, _optional);
            }

            public override uint? GetEditionId()
            {
                {
                    return _editionId;
                }
            }
        }

        public class V1_Editions_EditionId_ImagedObjects
            : RequestObject<EmptyInput, ImagedObjectListDTO>
        {
            private readonly uint _editionId;
            private readonly List<string> _optional;


            /// <summary>
            ///     Provides a listing of imaged objects related to the specified edition, can include images and also their masks with
            ///     optional.
            /// </summary>
            /// <param name="editionId">Unique Id of the desired edition</param>
            /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
            public V1_Editions_EditionId_ImagedObjects(uint editionId, List<string> optional = null)

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

        public class V1_ImagedObjects_Institutions
            : RequestObject<EmptyInput, ImageInstitutionListDTO>
        {
            protected override string HttpPath()
            {
                return RequestPath;
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString());
            }
        }

        public class V1_ImagedObjects_Institutions_InstitutionName
            : RequestObject<EmptyInput, InstitutionalImageListDTO>
        {
            private readonly string _institutionName;


            /// <summary>
            ///     Provides a list of all institutional image providers.
            /// </summary>
            public V1_ImagedObjects_Institutions_InstitutionName(string institutionName)

            {
                _institutionName = institutionName;
            }


            protected override string HttpPath()
            {
                return RequestPath.Replace("/institution-name", $"/{_institutionName}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _institutionName);
            }
        }

        public class V1_ImagedObjects_ImagedObjectId_TextFragments
            : RequestObject<EmptyInput, ImagedObjectTextFragmentMatchListDTO>
        {
            private readonly string _imagedObjectId;


            /// <summary>
            ///     Provides a list of all text fragments that should correspond to the imaged object.
            /// </summary>
            /// <param name="imagedObjectId">Id of the imaged object</param>
            /// <returns></returns>
            public V1_ImagedObjects_ImagedObjectId_TextFragments(string imagedObjectId)

            {
                _imagedObjectId = imagedObjectId;
            }


            protected override string HttpPath()
            {
                return RequestPath.Replace("/imaged-object-id", $"/{_imagedObjectId}");
            }

            public override Func<HubConnection, Task<T>> SignalrRequest<T>()
            {
                return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), _imagedObjectId);
            }
        }
    }
}