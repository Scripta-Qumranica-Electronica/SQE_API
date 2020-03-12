/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-api-server` project. Changes made
 * there will automatically be incorporated here the next time the 
 * `sqe-realtime-hub-builder` is run.
 */

using Microsoft.AspNetCore.SignalR;
using SQE.API.Server.Services;
using SQE.DatabaseAccess.Helpers;
using System.Text.Json;

namespace SQE.API.Server.RealtimeHubs
{
    public partial class MainHub : Hub<ISQEClient>
    {
        private readonly ITextService _textService;
        private readonly IUserService _userService;
        private readonly IEditionService _editionService;
        private readonly IImagedObjectService _imagedObjectService;
        private readonly IImageService _imageService;
        private readonly IRoiService _roiService;
        private readonly IArtefactService _artefactService;
        private readonly IUtilService _utilService;

        public MainHub(ITextService textService, IUserService userService, IEditionService editionService, IImagedObjectService imagedObjectService, IImageService imageService, IRoiService roiService, IArtefactService artefactService, IUtilService utilService)
        {
            _textService = textService;
            _userService = userService;
            _editionService = editionService;
            _imagedObjectService = imagedObjectService;
            _imageService = imageService;
            _roiService = roiService;
            _artefactService = artefactService;
            _utilService = utilService;
        }
    }
}