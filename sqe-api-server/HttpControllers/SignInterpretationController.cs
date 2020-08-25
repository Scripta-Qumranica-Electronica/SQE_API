using System;
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
        private readonly ISignInterpretationService _signInterpretationService;

        public SignInterpretationController(IUserService userService, ISignInterpretationService signInterpretationService)
        {
            _userService = userService;
            _signInterpretationService = signInterpretationService;
        }

        /// <summary>
        /// Retrieve a list of all possible attributes for an edition
        /// </summary>
        /// <param name="editionId">The ID of the edition being searched</param>
        /// <returns>A list of and edition's attributes and their details</returns>
        [AllowAnonymous]
        [HttpGet("v1/editions/{editionId}/sign-interpretations-attributes")]
        public async Task<ActionResult<AttributeListDTO>> GetAllEditionSignInterpretationAttributes([FromRoute] uint editionId)
        {
            return await _signInterpretationService.GetEditionSignInterpretationAttributesAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, false));
        }

        /// <summary>
        /// Retrieve the details of a sign interpretation in an edition
        /// </summary>
        /// <param name="editionId">The ID of the edition being searched</param>
        /// <param name="signInterpretationId">The desired sign interpretation id</param>
        /// <returns>The details of the desired sign interpretation</returns>
        [AllowAnonymous]
        [HttpGet("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}")]
        public async Task<ActionResult<SignInterpretationDTO>> GetEditionSignInterpretationDetails([FromRoute] uint editionId, [FromRoute] uint signInterpretationId)
        {
            return await _signInterpretationService.GetEditionSignInterpretationAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, false),
                signInterpretationId);
        }

        /// <summary>
        /// Create a new attribute for an edition
        /// </summary>
        /// <param name="editionId">The ID of the edition being edited</param>
        /// <param name="newAttribute">The details of the new attribute</param>
        /// <returns>The details of the newly created attribute</returns>
        [HttpPost("v1/editions/{editionId}/sign-interpretations-attributes")]
        public async Task<ActionResult<AttributeDTO>> CreateEditionSignInterpretationAttributes([FromRoute] uint editionId, [FromBody] CreateAttributeDTO newAttribute)
        {
            return await _signInterpretationService.CreateEditionAttributeAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                newAttribute);
        }

        /// <summary>
        /// Delete an attribute from an edition
        /// </summary>
        /// <param name="editionId">The ID of the edition being edited</param>
        /// <param name="attributeId">The ID of the attribute to delete</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpDelete("v1/editions/{editionId}/sign-interpretations-attributes/{attributeId}")]
        public async Task<ActionResult> DeleteEditionSignInterpretationAttributes([FromRoute] uint editionId,
            [FromRoute] uint attributeId)
        {
            return await _signInterpretationService.DeleteEditionAttributeAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                attributeId);
        }

        /// <summary>
        /// Change the details of an attribute in an edition
        /// </summary>
        /// <param name="editionId">The ID of the edition being edited</param>
        /// <param name="attributeId">The ID of the attribute to update</param>
        /// <param name="updatedAttribute">The details of the updated attribute</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPut("v1/editions/{editionId}/sign-interpretations-attributes/{attributeId}")]
        public async Task<ActionResult<AttributeDTO>> UpdateEditionSignInterpretationAttributes([FromRoute] uint editionId,
            [FromRoute] uint attributeId, [FromBody] UpdateAttributeDTO updatedAttribute)
        {
            return await _signInterpretationService.UpdateEditionAttributeAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                attributeId,
                updatedAttribute);
        }

        /// <summary>
        /// Creates a new sign interpretation 
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="newSignInterpretation">New sign interpretation data to be added</param>
        /// <returns>The new sign interpretation</returns>
        [HttpPost("v1/editions/{editionId}/sign-interpretations")]
        public async Task<ActionResult<SignInterpretationListDTO>> PostNewSignInterpretation([FromRoute] uint editionId,
            [FromBody] SignInterpretationCreateDTO newSignInterpretation)
        {
            return await _signInterpretationService.CreateSignInterpretationAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                newSignInterpretation);
        }
        //
        // /// <summary>
        // /// Deletes the sign interpretation in the route. The endpoint automatically manages the sign stream
        // /// by connecting all the deleted sign's next and previous nodes.
        // /// </summary>
        // /// <param name="editionId">ID of the edition being changed</param>
        // /// <param name="signInterpretationId">ID of the sign interpretation being deleted</param>
        // /// <returns>Ok or Error</returns>
        // [HttpDelete("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}")]
        // public async Task<ActionResult> DeleteSignInterpretation([FromRoute] uint editionId,
        //     [FromRoute] uint signInterpretationId)
        // {
        //     throw new NotImplementedException(); //Not Implemented
        // }

        /// <summary>
        /// Updates the commentary of a sign interpretation
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation whose commentary is being changed</param>
        /// <param name="commentary">The new commentary for the sign interpretation</param>
        /// <returns>Ok or Error</returns>
        [HttpPut("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/commentary")]
        public async Task<ActionResult<SignInterpretationDTO>> PutSignInterpretationCommentary([FromRoute] uint editionId,
            [FromRoute] uint signInterpretationId,
            [FromBody] CommentaryCreateDTO commentary)
        {
            return await _signInterpretationService.CreateOrUpdateSignInterpretationCommentaryAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                signInterpretationId,
                commentary);
        }

        /// <summary>
        /// This adds a new attribute to the specified sign interpretation.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation for adding a new attribute</param>
        /// <param name="newSignInterpretationAttributes">Details of the attribute to be added</param>
        /// <returns>The updated sign interpretation</returns>
        [HttpPost("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/attributes")]
        public async Task<ActionResult<SignInterpretationDTO>> PostSignInterpretationAttribute(
            [FromRoute] uint editionId,
            [FromRoute] uint signInterpretationId,
            [FromBody] InterpretationAttributeCreateDTO newSignInterpretationAttributes)
        {
            return await _signInterpretationService.CreateSignInterpretationAttributeAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                signInterpretationId,
                newSignInterpretationAttributes);
        }

        /// <summary>
        /// This changes the values of the specified sign interpretation attribute,
        /// mainly used to change commentary.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
        /// <param name="attributeValueId">Id of the attribute value to be altered</param>
        /// <param name="alteredSignInterpretationAttribute">New details of the attribute</param>
        /// <returns>The updated sign interpretation</returns>
        [HttpPut("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/attributes/{attributeValueId}")]
        public async Task<ActionResult<SignInterpretationDTO>> PutSignInterpretationAttribute(
            [FromRoute] uint editionId,
            [FromRoute] uint signInterpretationId,
            [FromRoute] uint attributeValueId,
            [FromBody] InterpretationAttributeCreateDTO alteredSignInterpretationAttribute)
        {
            return await _signInterpretationService.UpdateSignInterpretationAttributeAsync(
                await _userService.GetCurrentUserObjectAsync(editionId, true),
                signInterpretationId,
                attributeValueId,
                alteredSignInterpretationAttribute);
        }

        /// <summary>
        /// This deletes the specified attribute value from the specified sign interpretation.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
        /// <param name="attributeValueId">Id of the attribute being removed</param>
        /// <returns>Ok or Error</returns>
        [HttpDelete("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/attributes/{attributeValueId}")]
        public async Task<ActionResult> DeleteSignInterpretationAttribute(
            [FromRoute] uint editionId,
            [FromRoute] uint signInterpretationId,
            [FromRoute] uint attributeValueId)
        {
            return await _signInterpretationService.DeleteSignInterpretationAttributeAsync(await _userService.GetCurrentUserObjectAsync(editionId, true),
                signInterpretationId,
                attributeValueId);
        }
    }
}