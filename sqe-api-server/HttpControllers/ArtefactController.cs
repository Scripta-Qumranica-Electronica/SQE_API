using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers
{
    [Authorize]
    [ApiController]
    public class ArtefactController : ControllerBase
    {
        private readonly IArtefactService _artefactService;
        private readonly IRoiService _roiService;
        private readonly IUserService _userService;

        public ArtefactController(IArtefactService artefactService, IRoiService roiService, IUserService userService)
        {
            _artefactService = artefactService;
            _roiService = roiService;
            _userService = userService;
        }

        /// <summary>
        ///     Creates a new artefact with the provided data.
        ///
        ///     If no mask is provided, a placeholder mask will be created with the values:
        ///     "POLYGON((0 0,1 1,1 0,0 0))" (the system requires a valid WKT polygon mask for
        ///     every artefact). It is not recommended to leave the mask, name, or work status
        ///     blank or null. It will often be advantageous to leave the transformation null
        ///     when first creating a new artefact.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">A CreateArtefactDTO with the data for the new artefact</param>
        [HttpPost("v1/editions/{editionId}/[controller]s")]
        public async Task<ActionResult<ArtefactDTO>> CreateArtefact([FromRoute] uint editionId,
            [FromBody] CreateArtefactDTO payload)
        {
            return await _artefactService.CreateArtefactAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                payload
            );
        }

        /// <summary>
        ///     Deletes the specified artefact
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [HttpDelete("v1/editions/{editionId}/[controller]s/{artefactId}")]
        public async Task<ActionResult> DeleteArtefact([FromRoute] uint editionId, [FromRoute] uint artefactId)
        {
            return await _artefactService.DeleteArtefactAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                artefactId
            );
        }

        /// <summary>
        ///     Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        [HttpGet("v1/editions/{editionId}/[controller]s/{artefactId}")]
        public async Task<ActionResult<ArtefactDTO>> GetArtefact([FromRoute] uint editionId,
            [FromRoute] uint artefactId,
            [FromQuery] List<string> optional)
        {
            return await _artefactService.GetEditionArtefactAsync(
                await _userService.GetCurrentUserObjectAsync(editionId),
                artefactId,
                optional
            );
        }

        /// <summary>
        ///     Provides a listing of all rois belonging to an artefact in the specified edition
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [AllowAnonymous]
        [HttpGet("v1/editions/{editionId}/[controller]s/{artefactId}/rois")]
        public async Task<ActionResult<InterpretationRoiDTOList>> GetArtefactRois([FromRoute] uint editionId,
            [FromRoute] uint artefactId)
        {
            return await _roiService.GetRoisByArtefactIdAsync(
                await _userService.GetCurrentUserObjectAsync(editionId),
                artefactId
            );
        }

        /// <summary>
        ///     Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        [HttpGet("v1/editions/{editionId}/[controller]s")]
        public async Task<ActionResult<ArtefactListDTO>> GetArtefacts([FromRoute] uint editionId,
            [FromQuery] List<string> optional)
        {
            return await _artefactService.GetEditionArtefactListingsAsync(
                await _userService.GetCurrentUserObjectAsync(editionId),
                optional
            );
        }

        /// <summary>
        ///     Provides a listing of text fragments that have text in the specified artefact.
        ///     With the optional query parameter "suggested", this endpoint will also return
        ///     any text fragment that the system suggests might have text in the artefact.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="optional">Add "suggested" to include possible matches suggested by the system</param>
        [AllowAnonymous]
        [HttpGet("v1/editions/{editionId}/[controller]s/{artefactId}/text-fragments")]
        public async Task<ActionResult<ArtefactTextFragmentMatchListDTO>> GetArtefactTextFragments(
            [FromRoute] uint editionId,
            [FromRoute] uint artefactId,
            [FromQuery] List<string> optional)
        {
            return await _artefactService.ArtefactTextFragmentsAsync(
                await _userService.GetCurrentUserObjectAsync(editionId),
                artefactId,
                optional
            );
        }

        /// <summary>
        ///     Updates the specified artefact.
        /// 
        ///     There are many possible attributes that can be changed for
        ///     an artefact.  The caller should only input only those that
        ///     should be changed. Attributes with a null value will be ignored.
        ///     For instance, setting the mask to null or "" will result in
        ///     no changes to the current mask, and no value for the mask will
        ///     be returned (or broadcast). Likewise, the transformation, name,
        ///     or status message may be set to null and no change will be made
        ///     to those entities (though any unchanged values will be returned
        ///     along with the changed values and also broadcast to co-editors).
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">An UpdateArtefactDTO with the desired alterations to the artefact</param>
        [HttpPut("v1/editions/{editionId}/[controller]s/{artefactId}")]
        public async Task<ActionResult<ArtefactDTO>> UpdateArtefact([FromRoute] uint editionId,
            [FromRoute] uint artefactId,
            [FromBody] UpdateArtefactDTO payload)
        {
            return await _artefactService.UpdateArtefactAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                artefactId,
                payload
            );
        }

        /// <summary>
        ///     Updates the positional data for a batch of artefacts
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">A BatchUpdateArtefactTransformDTO with a list of the desired updates</param>
        /// <returns></returns>
        [HttpPost("v1/editions/{editionId}/[controller]s/batch-transformation")]
        public async Task<ActionResult<BatchUpdatedArtefactTransformDTO>> BatchUpdateArtefactTransform(
            [FromRoute] uint editionId,
            [FromBody] BatchUpdateArtefactTransformDTO payload)
        {
            return await _artefactService.BatchUpdateArtefactTransformAsync(await _userService.GetCurrentUserObjectAsync(editionId, true),
                payload);
        }
    }
}