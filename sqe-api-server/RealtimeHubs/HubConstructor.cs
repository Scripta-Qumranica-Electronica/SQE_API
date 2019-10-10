using Microsoft.AspNetCore.SignalR;
using SQE.API.Server.Services;

namespace SQE.API.Server.RealtimeHubs
{
    public partial class MainHub : Hub
    {
        private readonly IArtefactService _artefactService;
        private readonly IRoiService _roiService;
        private readonly IUserService _userService;
        private readonly IEditionService _editionService;
        private readonly IImagedObjectService _imagedObjectService;
        private readonly IImageService _imageService;
        private readonly ITextService _textService;

        public MainHub(IArtefactService artefactService, IRoiService roiService, IUserService userService, IEditionService editionService, IImagedObjectService imagedObjectService, IImageService imageService, ITextService textService)
        {
            _artefactService = artefactService;
            _roiService = roiService;
            _userService = userService;
            _editionService = editionService;
            _imagedObjectService = imagedObjectService;
            _imageService = imageService;
            _textService = textService;
        }
    }
}