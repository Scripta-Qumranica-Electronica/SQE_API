using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers
{
    [Authorize]
    [ApiController]
    public class CatalogueController : ControllerBase
    {
        private readonly ICatalogService _catalogueService;
        private readonly IUserService _userService;

        public CatalogueController(ICatalogService catalogueService, IUserService userService)
        {
            _catalogueService = catalogueService;
            _userService = userService;
        }

        /// <summary>
        ///     Get a listing of all text fragments matches that correspond to an imaged object
        /// </summary>
        /// <param name="imagedObjectId">Id of imaged object to search for transcription matches</param>
        [AllowAnonymous]
        [HttpGet("v1/catalogue/imaged-objects/{imagedObjectId}/text-fragments")]
        public async Task<ActionResult<CatalogueMatchListDTO>> GetTextFragmentsOfImagedObject(
            [FromRoute] string imagedObjectId)
        {
            return await _catalogueService.GetTextFragmentsOfImagedObject(imagedObjectId);
        }

        /// <summary>
        ///     Get a listing of all imaged objects that matches that correspond to a transcribed text fragment
        /// </summary>
        /// <param name="textFragmentId">Unique Id of the text fragment to search for imaged object matches</param>
        [AllowAnonymous]
        [HttpGet("v1/catalogue/text-fragments/{textFragmentId}/imaged-objects")]
        public async Task<ActionResult<CatalogueMatchListDTO>> GetImagedObjectsOfTextFragments(
            [FromRoute] uint textFragmentId)
        {
            return await _catalogueService.GetImagedObjectsOfTextFragment(textFragmentId);
        }

        /// <summary>
        ///     Get a listing of all corresponding imaged objects and transcribed text fragment in a specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the edition to search for imaged objects to text fragment matches</param>
        [AllowAnonymous]
        [HttpGet("v1/catalogue/editions/{editionId}/imaged-object-text-fragment-matches")]
        public async Task<ActionResult<CatalogueMatchListDTO>> GetImagedObjectsAndTextFragmentsOfEdition(
            [FromRoute] uint editionId)
        {
            return await _catalogueService.GetTextFragmentsAndImagedObjectsOfEdition(editionId);
        }

        /// <summary>
        ///     Get a listing of all corresponding imaged objects and transcribed text fragment in a specified manuscript
        /// </summary>
        /// <param name="manuscriptId">Unique Id of the manuscript to search for imaged objects to text fragment matches</param>
        [AllowAnonymous]
        [HttpGet("v1/catalogue/manuscripts/{manuscriptId}/imaged-object-text-fragment-matches")]
        public async Task<ActionResult<CatalogueMatchListDTO>> GetImagedObjectsAndTextFragmentsOfManuscript(
            [FromRoute] uint manuscriptId)
        {
            return await _catalogueService.GetTextFragmentsAndImagedObjectsOfManuscript(manuscriptId);
        }

        /// <summary>
        ///     Create a new matched pair for an imaged object and a text fragment along with the edition princeps information
        /// </summary>
        /// <param name="newMatch">The details of the new match</param>
        /// <returns></returns>
        [HttpPost("v1/catalogue")]
        public async Task<ActionResult> PostNewImagedObjectTextFragmentMatch([FromBody] CatalogueMatchInputDTO newMatch)
        {
            return await _catalogueService.CreateTextFragmentImagedObjectMatch(
                await _userService.GetCurrentUserObjectAsync(null, true),
                newMatch);
        }

        /// <summary>
        ///     Confirm the correctness of an existing imaged object and text fragment match
        /// </summary>
        /// <param name="iaaEditionCatalogToTextFragmentId">The unique id of the match to confirm</param>
        /// <returns></returns>
        [HttpPost("v1/catalogue/confirm-match/{iaaEditionCatalogToTextFragmentId}")]
        public async Task<ActionResult> ConfirmImagedObjectTextFragmentMatch(
            [FromRoute] uint iaaEditionCatalogToTextFragmentId)
        {
            return await _catalogueService.ConfirmTextFragmentImagedObjectMatch(
                await _userService.GetCurrentUserObjectAsync(null, true),
                iaaEditionCatalogToTextFragmentId, true);
        }

        /// <summary>
        ///     Remove an existing imaged object and text fragment match, which is not correct
        /// </summary>
        /// <param name="iaaEditionCatalogToTextFragmentId">The unique id of the match to confirm</param>
        /// <returns></returns>
        [HttpDelete("v1/catalogue/confirm-match/{iaaEditionCatalogToTextFragmentId}")]
        public async Task<ActionResult> RejectImagedObjectTextFragmentMatch(
            [FromRoute] uint iaaEditionCatalogToTextFragmentId)
        {
            return await _catalogueService.ConfirmTextFragmentImagedObjectMatch(
                await _userService.GetCurrentUserObjectAsync(null, true),
                iaaEditionCatalogToTextFragmentId, false);
        }
    }
}