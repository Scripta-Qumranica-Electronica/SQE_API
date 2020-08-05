using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.HttpControllers
{
    [Authorize]
    [ApiController]
    public class SignInterpretationController : ControllerBase
    {
        private readonly IUserService _userService;

        public SignInterpretationController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Replaces the sign interpretation in the route with the submitted sign interpretation DTO.
        /// This is the only way to change a sign interpretation's character. The endpoint will create
        /// a new sign interpretation id with the submitted information, remove the old sign interpretation
        /// id from the edition sign streams, and insert the new sign interpretation into its place in the stream. 
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being replaced</param>
        /// <param name="newSignInterpretation">New sign interpretation data to be added</param>
        /// <returns>The new sign interpretation</returns>
        [HttpPost("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}")]
        public async Task<ActionResult<SignInterpretationDTO>> PostReplaceSignInterpretation([FromRoute] uint editionId,
            [FromRoute] uint signInterpretationId,
            [FromBody] SignInterpretationCreateDTO newSignInterpretation)
        {
            return null; //Not Implemented
        }

        /// <summary>
        /// Deletes the sign interpretation in the route. The endpoint automatically manages the sign stream
        /// by connecting all the deleted sign's next and previous nodes.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being deleted</param>
        /// <returns>Ok or Error</returns>
        [HttpPost("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}")]
        public async Task<ActionResult> DeleteSignInterpretation([FromRoute] uint editionId,
            [FromRoute] uint signInterpretationId)
        {
            return null; //Not Implemented
        }

        /// <summary>
        /// Updates the commentary of a sign interpretation
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation whose commentary is being changed</param>
        /// <param name="string">The new commentary for the sign interpretation</param>
        /// <returns>Ok or Error</returns>
        [HttpPut("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/commentary")]
        public async Task<ActionResult> PutSignInterpretationCommentary([FromRoute] uint editionId,
            [FromRoute] uint signInterpretationId,
            [FromBody] string commentary)
        {
            return null;  //Not Implemented
        }

        /// <summary>
        /// This adds a new attribute to the specified sign interpretation.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation for adding a new attribute</param>
        /// <param name="newSignInterpretationAttributes">Details of the attribute to be added</param>
        /// <returns>The updated sign interpretation</returns>
        [HttpPost("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/attributes")]
        public async Task<ActionResult<SignInterpretationDTO>> PostSignInterpretationAttribute([FromRoute] uint editionId,
            [FromRoute] uint signInterpretationId,
            [FromBody] InterpretationAttributeListDTO newSignInterpretationAttributes)
        {
            return null;  //Not Implemented
        }

        /// <summary>
        /// This changes the values of the specified sign interpretation attribute,
        /// mainly used to change commentary.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
        /// <param name="attributeId">Id of the attribute to be altered</param>
        /// <param name="alteredSignInterpretationAttribute">New details of the attribute</param>
        /// <returns>The updated sign interpretation</returns>
        [HttpPut("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/attributes/{attributeId}")]
        public async Task<ActionResult<SignInterpretationDTO>> PutSignInterpretationAttribute([FromRoute] uint editionId,
            [FromRoute] uint signInterpretationId,
            [FromRoute] uint attributeId,
            [FromBody] InterpretationAttributeDTO alteredSignInterpretationAttribute)
        {
            return null;  //Not Implemented
        }

        /// <summary>
        /// This deletes the specified attribute from the specified sign interpretation.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being alteres</param>
        /// <param name="attributeId">Id of the attribute being removed</param>
        /// <returns>Ok or Error</returns>
        [HttpDelete("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/attributes/{attributeId}")]
        public async Task<ActionResult> DeleteSignInterpretationAttribute([FromRoute] uint editionId,
            [FromRoute] uint signInterpretationId,
            [FromRoute] uint attributeId)
        {
            return null;  //Not Implemented
        }
    }
}