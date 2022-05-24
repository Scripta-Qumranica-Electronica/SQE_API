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
		private readonly IArtefactService _artefactService;
		private readonly IRoiService _roiService;
		private readonly ITextService _textService;
		private readonly IUserService _userService;
		private readonly ICatalogService _catalogueService;
		private readonly IEditionService _editionService;
		private readonly IImagedObjectService _imagedObjectService;
		private readonly IImageService _imageService;
		private readonly IWordService _wordService;
		private readonly IScriptService _scriptService;
		private readonly ISearchService _searchService;
		private readonly ISignInterpretationService _signInterpretationService;
		private readonly IUtilService _utilService;

        public MainHub(IArtefactService artefactService, IRoiService roiService, ITextService textService, IUserService userService, ICatalogService catalogueService, IEditionService editionService, IImagedObjectService imagedObjectService, IImageService imageService, IWordService wordService, IScriptService scriptService, ISearchService searchService, ISignInterpretationService signInterpretationService, IUtilService utilService)
        {
			_artefactService = artefactService;
			_roiService = roiService;
			_textService = textService;
			_userService = userService;
			_catalogueService = catalogueService;
			_editionService = editionService;
			_imagedObjectService = imagedObjectService;
			_imageService = imageService;
			_wordService = wordService;
			_scriptService = scriptService;
			_searchService = searchService;
			_signInterpretationService = signInterpretationService;
			_utilService = utilService;
        }
     }
}