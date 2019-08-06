using Microsoft.AspNetCore.SignalR;
using SQE.SqeApi.Server.Services;

namespace SQE.SqeApi.Server.Hubs
{
    public partial class MainHub : Hub
    {
        private readonly IArtefactService _artefactService;
        private readonly IUserService _userService;
        private readonly IEditionService _editionService;
        private readonly IImagedObjectService _imagedObjectService;
        private readonly IImageService _imageService;
        private readonly ITextService _textService;

        public MainHub(IArtefactService artefactService, IUserService userService, IEditionService editionService,
            IImagedObjectService imagedObjectService, IImageService imageService, ITextService textService)
        {
            _artefactService = artefactService;
            _userService = userService;
            _editionService = editionService;
            _imagedObjectService = imagedObjectService;
            _imageService = imageService;
            _textService = textService;
        }
    }
}