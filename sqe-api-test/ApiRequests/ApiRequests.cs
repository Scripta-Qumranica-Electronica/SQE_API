using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CaseExtensions;
using Microsoft.AspNetCore.SignalR.Client;

namespace SQE.ApiTest.ApiRequests
{
    /// <summary>
    ///     An class used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public abstract class RequestObject<Tinput, Toutput, TListener>
    {
        public List<string> listenerMethod = new List<string>();
        protected Tinput payload;
        protected string requestPath;
        public HttpMethod requestVerb;

        /// <summary>
        ///     Provides a RequestObject used by the Request Class in SQE.ApiTest.Helpers to access an API endpoint
        /// </summary>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        protected RequestObject(Tinput payload)
        {
            this.payload = payload;
            var pathElements = GetType().ToString().Split(".").Last().Split('+', '_');
            requestPath =
                "/" + string.Join("/", pathElements.Skip(1).Select(x => x.ToKebabCase()).Where(x => x != "null"));
            var verb = pathElements.First();
            switch (verb)
            {
                case "Get":
                    requestVerb = HttpMethod.Get;
                    break;
                case "Post":
                    requestVerb = HttpMethod.Post;
                    break;
                case "Put":
                    requestVerb = HttpMethod.Put;
                    break;
                case "Delete":
                    requestVerb = HttpMethod.Delete;
                    break;
            }
        }

        /// <summary>
        ///     Returns an HttpRequestObject with the information needed to make a request to the HTTP server for this API endpoint
        /// </summary>
        /// <returns>May return null if HTTP requests are not possible with this endpoint</returns>
        public virtual HttpRequestObject GetHttpRequestObject()
        {
            return new HttpRequestObject
            {
                requestVerb = requestVerb,
                requestString = HttpPath(),
                payload = payload
            };
        }

        protected virtual string HttpPath()
        {
            return requestPath;
        }

        /// <summary>
        ///     Returns a function that can make a SignalR request to this API endpoint
        ///     The function that is returned requires a HubConnection as its only argument
        /// </summary>
        /// <typeparam name="T">The compiler checks to make sure T == Toutput</typeparam>
        /// <returns></returns>
        public virtual Func<HubConnection, Task<T>> SignalrRequest<T>()
            where T : Toutput
        {
            return signalR => payload == null
                ? signalR.InvokeAsync<T>(SignalrRequestString())
                : signalR.InvokeAsync<T>(SignalrRequestString(), payload);
        }

        /// <summary>
        ///     Formats the API endpoint method name for the SignalR server
        /// </summary>
        /// <returns></returns>
        protected string SignalrRequestString()
        {
            return requestVerb.ToString().First().ToString().ToUpper()
                   + requestVerb.ToString().Substring(1).ToLower()
                   + requestPath.Replace("/", "_").ToPascalCase();
        }

        /// <summary>
        ///     An object containing the necessary data to make an HTTP request to this API endpoint
        /// </summary>
        public class HttpRequestObject
        {
            public HttpMethod requestVerb { get; set; }
            public string requestString { get; set; }
            public Tinput payload { get; set; }
        }
    }

    /// <summary>
    ///     Subclass of RequestObject for all requests made on an edition
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class EditionRequestObject<Tinput, Toutput, TListener> : RequestObject<Tinput, Toutput, TListener>
    {
        public readonly uint editionId;
        public readonly List<string> optional;

        /// <summary>
        ///     Provides an EditionRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public EditionRequestObject(uint editionId, List<string> optional = null, Tinput payload = default) :
            base(payload)
        {
            this.editionId = editionId;
            this.optional = optional;
        }

        protected override string HttpPath()
        {
            return requestPath.Replace("/edition-id", $"/{editionId.ToString()}")
                   + (optional != null && optional.Any() ? $"?optional={string.Join(",", optional)}" : "");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => payload == null
                ? optional == null ? signalR.InvokeAsync<T>(SignalrRequestString(), editionId)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, optional)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, payload);
        }
    }

    /// <summary>
    ///     Subclass of RequestObject for all requests made on an edition
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class EditionEditorRequestObject<Tinput, Toutput, TListener> : EditionRequestObject<Tinput, Toutput, TListener>
    {
        public readonly string editorEmail;

        /// <summary>
        ///     Provides an EditionRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public EditionEditorRequestObject(uint editionId,
            string editorEmail,
            List<string> optional = null,
            Tinput payload = default) :
            base(editionId, optional, payload)
        {
            this.editorEmail = editorEmail;
        }

        protected override string HttpPath()
        {
            return requestPath.Replace("/edition-id", $"/{editionId.ToString()}")
                       .Replace("/editor-email-id", $"/{editorEmail}")
                   + (optional != null && optional.Any() ? $"?optional={string.Join(",", optional)}" : "");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => payload == null
                ? optional == null ? signalR.InvokeAsync<T>(SignalrRequestString(), editionId)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, optional)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, payload);
        }
    }

    /// <summary>
    ///     Subclass of RequestObject for all requests made on an edition
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class EditionEditorConfirmationObject<Tinput, Toutput, TListener> : EditionRequestObject<Tinput, Toutput, TListener>
    {
        public readonly Guid token;

        /// <summary>
        ///     Provides an EditionRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public EditionEditorConfirmationObject(Guid token, uint editionId, List<string> optional = null,
            Tinput payload = default) :
            base(editionId, optional, payload)
        {
            this.token = token;
        }

        protected override string HttpPath()
        {
            return requestPath.Replace("/token", $"/{token.ToString()}");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => signalR.InvokeAsync<T>(SignalrRequestString(), token.ToString());
        }
    }

    /// <summary>
    ///     Subclass of EditionRequestObject for all requests made on an imaged object
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class EditionImagedObjectRequestObject<Tinput, Toutput, TListener> : EditionRequestObject<Tinput, Toutput, TListener>
    {
        public readonly string imagedObjectId;

        /// <summary>
        ///     Provides an ImagedObjectRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="imagedObjectId">The id of the imaged object to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public EditionImagedObjectRequestObject(uint editionId,
            string imagedObjectId,
            List<string> optional = null,
            Tinput payload = default) : base(editionId, optional, payload)
        {
            this.imagedObjectId = imagedObjectId;
        }

        protected override string HttpPath()
        {
            return base.HttpPath().Replace("/imaged-object-id", $"/{imagedObjectId.ToString()}");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => payload == null
                ? optional == null ? signalR.InvokeAsync<T>(SignalrRequestString(), editionId, imagedObjectId)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, imagedObjectId, optional)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, imagedObjectId, payload);
        }
    }

    /// <summary>
    ///     Subclass of EditionRequestObject for all requests made on an text fragment
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class TextFragmentRequestObject<Tinput, Toutput, TListener> : EditionRequestObject<Tinput, Toutput, TListener>
    {
        public readonly uint textFragmentId;

        /// <summary>
        ///     Provides an TextFragmentRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="textFragmentId">The id of the text fragment to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public TextFragmentRequestObject(uint editionId, uint textFragmentId, Tinput payload) : base(
            editionId,
            null,
            payload
        )
        {
            this.textFragmentId = textFragmentId;
        }

        protected override string HttpPath()
        {
            return base.HttpPath().Replace("/text-fragment-id", $"/{textFragmentId.ToString()}");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => payload == null
                ? signalR.InvokeAsync<T>(SignalrRequestString(), editionId, textFragmentId)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, textFragmentId, payload);
        }
    }

    /// <summary>
    ///     Subclass of EditionRequestObject for all requests made on a line
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class LineRequestObject<Tinput, Toutput, TListener> : EditionRequestObject<Tinput, Toutput, TListener>
    {
        public readonly uint lineId;

        /// <summary>
        ///     Provides an TextFragmentRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="lineId">The id of the line to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public LineRequestObject(uint editionId, uint lineId, Tinput payload) : base(editionId, null, payload)
        {
            this.lineId = lineId;
        }

        protected override string HttpPath()
        {
            return base.HttpPath().Replace("/line-id", $"/{lineId.ToString()}");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => payload == null
                ? signalR.InvokeAsync<T>(SignalrRequestString(), editionId, lineId)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, lineId, payload);
        }
    }

    /// <summary>
    ///     Subclass of EditionRequestObject for all requests made on an artefact
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class ArtefactRequestObject<Tinput, Toutput, TListener> : EditionRequestObject<Tinput, Toutput, TListener>
    {
        public readonly uint artefactId;

        /// <summary>
        ///     Provides an ArtefactRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="artefactId">The id of the artefact to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public ArtefactRequestObject(uint editionId, uint artefactId, Tinput payload) : base(editionId, null, payload)
        {
            this.artefactId = artefactId;
        }

        protected override string HttpPath()
        {
            return base.HttpPath().Replace("/artefact-id", $"/{artefactId.ToString()}");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => payload == null
                ? signalR.InvokeAsync<T>(SignalrRequestString(), editionId, artefactId)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, artefactId, payload);
        }
    }

    /// <summary>
    ///     Subclass of EditionRequestObject for all requests made on an artefact group
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class ArtefactGroupRequestObject<Tinput, Toutput, TListener> : EditionRequestObject<Tinput, Toutput, TListener>
    {
        public readonly uint artefactGroupId;

        /// <summary>
        ///     Provides an ArtefactGroupRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="artefactId">The id of the artefact group to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public ArtefactGroupRequestObject(uint editionId, uint artefactGroupId, Tinput payload) : base(editionId, null, payload)
        {
            this.artefactGroupId = artefactGroupId;
        }

        protected override string HttpPath()
        {
            return base.HttpPath().Replace("/artefact-group-id", $"/{artefactGroupId.ToString()}");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => payload == null
                ? signalR.InvokeAsync<T>(SignalrRequestString(), editionId, artefactGroupId)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, artefactGroupId, payload);
        }
    }

    /// <summary>
    ///     Subclass of EditionRequestObject for all requests made on a roi
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    public class RoiRequestObject<Tinput, Toutput, TListener> : EditionRequestObject<Tinput, Toutput, TListener>
    {
        public readonly uint roiId;

        /// <summary>
        ///     Provides an RoiRequestObject for all API requests made on an edition
        /// </summary>
        /// <param name="editionId">The id of the edition to perform the request on</param>
        /// <param name="roiId">The id of the roi to perform the request on</param>
        /// <param name="payload">Payload to be sent to the API endpoint</param>
        public RoiRequestObject(uint editionId, uint roiId, Tinput payload) : base(editionId, null, payload)
        {
            this.roiId = roiId;
        }

        protected override string HttpPath()
        {
            return base.HttpPath().Replace("/roi-id", $"/{roiId.ToString()}");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR => payload == null
                ? signalR.InvokeAsync<T>(SignalrRequestString(), editionId, roiId)
                : signalR.InvokeAsync<T>(SignalrRequestString(), editionId, roiId, payload);
        }
    }

    /// <summary>
    ///     Subclass of EditionRequestObject for all requests made on an imaged object
    /// </summary>
    /// <typeparam name="Tinput">The type of the request payload</typeparam>
    /// <typeparam name="Toutput">The API endpoint return type</typeparam>
    /// <typeparam name="TListener">The API endpoint listener return type</typeparam>
    public class ImagedObjectRequestObject<Tinput, Toutput, TListener> : RequestObject<Tinput, Toutput, TListener>
    {
        public readonly string imagedObjectId;

        /// <summary>
        ///     Provides an ImagedObjectRequestObject for all API requests made on an imaged object
        /// </summary>
        /// <param name="imagedObjectId">The id of the imaged object to perform the request on</param>
        public ImagedObjectRequestObject(string imagedObjectId) : base(default(Tinput))
        {
            this.imagedObjectId = imagedObjectId;
        }

        protected override string HttpPath()
        {
            return base.HttpPath().Replace("/imaged-object-id", $"/{imagedObjectId}");
        }

        public override Func<HubConnection, Task<T>> SignalrRequest<T>()
        {
            return signalR =>
                signalR.InvokeAsync<T>(SignalrRequestString(), imagedObjectId, payload);
        }
    }

    /// <summary>
    ///     An empty request payload object
    /// </summary>
    public abstract class EmptyInput
    {
    }

    /// <summary>
    ///     An empty request response object
    /// </summary>
    public class EmptyOutput
    {
    }
}