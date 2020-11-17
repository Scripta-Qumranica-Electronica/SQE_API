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
		private readonly ISignInterpretationService _signInterpretationService;
		private readonly IUserService               _userService;

		public SignInterpretationController(
				IUserService                 userService
				, ISignInterpretationService signInterpretationService)
		{
			_userService = userService;
			_signInterpretationService = signInterpretationService;
		}

		/// <summary>
		///  Retrieve a list of all possible attributes for an edition
		/// </summary>
		/// <param name="editionId">The ID of the edition being searched</param>
		/// <returns>A list of and edition's attributes and their details</returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/sign-interpretations-attributes")]
		public async Task<ActionResult<AttributeListDTO>>
				GetAllEditionSignInterpretationAttributes([FromRoute] uint editionId)
			=> await _signInterpretationService.GetEditionSignInterpretationAttributesAsync(
					await _userService.GetCurrentUserObjectAsync(editionId));

		/// <summary>
		///  Retrieve the details of a sign interpretation in an edition
		/// </summary>
		/// <param name="editionId">The ID of the edition being searched</param>
		/// <param name="signInterpretationId">The desired sign interpretation id</param>
		/// <returns>The details of the desired sign interpretation</returns>
		[AllowAnonymous]
		[HttpGet("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}")]
		public async Task<ActionResult<SignInterpretationDTO>> GetEditionSignInterpretationDetails(
				[FromRoute]   uint editionId
				, [FromRoute] uint signInterpretationId)
			=> await _signInterpretationService.GetEditionSignInterpretationAsync(
					await _userService.GetCurrentUserObjectAsync(editionId)
					, signInterpretationId);

		/// <summary>
		///  Create a new attribute for an edition
		/// </summary>
		/// <param name="editionId">The ID of the edition being edited</param>
		/// <param name="newAttribute">The details of the new attribute</param>
		/// <returns>The details of the newly created attribute</returns>
		[HttpPost("v1/editions/{editionId}/sign-interpretations-attributes")]
		public async Task<ActionResult<AttributeDTO>> CreateEditionSignInterpretationAttributes(
				[FromRoute]  uint               editionId
				, [FromBody] CreateAttributeDTO newAttribute)
			=> await _signInterpretationService.CreateEditionAttributeAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, newAttribute);

		/// <summary>
		///  Delete an attribute from an edition
		/// </summary>
		/// <param name="editionId">The ID of the edition being edited</param>
		/// <param name="attributeId">The ID of the attribute to delete</param>
		/// <returns></returns>
		[HttpDelete("v1/editions/{editionId}/sign-interpretations-attributes/{attributeId}")]
		public async Task<ActionResult> DeleteEditionSignInterpretationAttributes(
				[FromRoute]   uint editionId
				, [FromRoute] uint attributeId)
			=> await _signInterpretationService.DeleteEditionAttributeAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, attributeId);

		/// <summary>
		///  Change the details of an attribute in an edition
		/// </summary>
		/// <param name="editionId">The ID of the edition being edited</param>
		/// <param name="attributeId">The ID of the attribute to update</param>
		/// <param name="updatedAttribute">The details of the updated attribute</param>
		/// <returns></returns>
		[HttpPut("v1/editions/{editionId}/sign-interpretations-attributes/{attributeId}")]
		public async Task<ActionResult<AttributeDTO>> UpdateEditionSignInterpretationAttributes(
				[FromRoute]   uint               editionId
				, [FromRoute] uint               attributeId
				, [FromBody]  UpdateAttributeDTO updatedAttribute)
			=> await _signInterpretationService.UpdateEditionAttributeAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, attributeId
					, updatedAttribute);

		/// <summary>
		///  Creates a new sign interpretation.  This creates a new sign entity for the submitted
		///  interpretation. This also takes care of inserting the sign interpretation into the
		///  sign stream following the specifications in the newSignInterpretation.
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="newSignInterpretation">New sign interpretation data to be added</param>
		/// <returns>The new sign interpretation</returns>
		[HttpPost("v1/editions/{editionId}/sign-interpretations")]
		public async Task<ActionResult<SignInterpretationCreatedDTO>> PostNewSignInterpretation(
				[FromRoute]  uint                        editionId
				, [FromBody] SignInterpretationCreateDTO newSignInterpretation)
			=> await _signInterpretationService.CreateSignInterpretationAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, null
					, newSignInterpretation);

		/// <summary>
		///  Creates a variant sign interpretation to the submitted sign interpretation id using
		///  the character and attribute settings of the newSignInterpretation payload. It will
		///  copy the ROIs from the original sign interpretation to the new one, but it will not
		///  copy the attributes (or any commentaries associated with the attributes).
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">
		///  Id of the sign interpretation for which this variant
		///  will be created
		/// </param>
		/// <param name="newSignInterpretation">New sign interpretation data to be added</param>
		/// <returns>The new sign interpretation</returns>
		[HttpPost("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}")]
		public async Task<ActionResult<SignInterpretationCreatedDTO>>
				PostAlternateSignInterpretation(
						[FromRoute]   uint                         editionId
						, [FromRoute] uint                         signInterpretationId
						, [FromBody]  SignInterpretationVariantDTO newSignInterpretation)
			=> await _signInterpretationService.CreateVariantSignInterpretationAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, signInterpretationId
					, newSignInterpretation);

		/// <summary>
		///  Deletes the sign interpretation in the route. The endpoint automatically manages the
		///  sign stream by connecting all the deleted sign's next and previous nodes.  Adding
		///  "delete-all-variants" to the optional query parameter will cause all variant sign
		///  interpretations to be deleted as well.
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">ID of the sign interpretation being deleted</param>
		/// <param name="optional">
		///  If the string "delete-all-variants" is submitted here, then
		///  all variant readings to the submitted sign interpretation id will be deleted as well
		/// </param>
		/// <returns>
		///  A list of all the sign interpretations that were deleted and changed as a result of
		///  the deletion operation
		/// </returns>
		[HttpDelete("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}")]
		public async Task<ActionResult<SignInterpretationDeleteDTO>> DeleteSignInterpretation(
				[FromRoute]   uint     editionId
				, [FromRoute] uint     signInterpretationId
				, [FromQuery] string[] optional)
			=> await _signInterpretationService.DeleteSignInterpretationAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, signInterpretationId
					, optional);

		/// <summary>
		///  Links two sign interpretations together in the edition's sign stream
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">The sign interpretation to be linked to the nextSignInterpretationId</param>
		/// <param name="nextSignInterpretationId">The sign interpretation to become the new next sign interpretation</param>
		/// <returns>The updated sign interpretation</returns>
		[HttpPost(
				"v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/link-to/{nextSignInterpretationId}")]
		public async Task<ActionResult<SignInterpretationDTO>> PostLinkSignInterpretations(
				[FromRoute]   uint editionId
				, [FromRoute] uint signInterpretationId
				, [FromRoute] uint nextSignInterpretationId)
			=> await _signInterpretationService.LinkSignInterpretationsAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, signInterpretationId
					, nextSignInterpretationId);

		/// <summary>
		///  Links two sign interpretations in the edition's sign stream
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">The sign interpretation to be unlinked from the nextSignInterpretationId</param>
		/// <param name="nextSignInterpretationId">The sign interpretation to removed as next sign interpretation</param>
		/// <returns>The updated sign interpretation</returns>
		[HttpPost(
				"v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/unlink-from/{nextSignInterpretationId}")]
		public async Task<ActionResult<SignInterpretationDTO>> PostUnlinkSignInterpretations(
				[FromRoute]   uint editionId
				, [FromRoute] uint signInterpretationId
				, [FromRoute] uint nextSignInterpretationId)
			=> await _signInterpretationService.UnlinkSignInterpretationsAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, signInterpretationId
					, nextSignInterpretationId);

		/// <summary>
		///  Updates the commentary of a sign interpretation
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">ID of the sign interpretation whose commentary is being changed</param>
		/// <param name="commentary">The new commentary for the sign interpretation</param>
		/// <returns>Ok or Error</returns>
		[HttpPut("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/commentary")]
		public async Task<ActionResult<SignInterpretationDTO>> PutSignInterpretationCommentary(
				[FromRoute]   uint                editionId
				, [FromRoute] uint                signInterpretationId
				, [FromBody]  CommentaryCreateDTO commentary)
			=> await _signInterpretationService.CreateOrUpdateSignInterpretationCommentaryAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, signInterpretationId
					, commentary);

		/// <summary>
		///  This adds a new attribute to the specified sign interpretation.
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">ID of the sign interpretation for adding a new attribute</param>
		/// <param name="newSignInterpretationAttributes">Details of the attribute to be added</param>
		/// <returns>The updated sign interpretation</returns>
		[HttpPost("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/attributes")]
		public async Task<ActionResult<SignInterpretationDTO>> PostSignInterpretationAttribute(
				[FromRoute]   uint                             editionId
				, [FromRoute] uint                             signInterpretationId
				, [FromBody]  InterpretationAttributeCreateDTO newSignInterpretationAttributes)
			=> await _signInterpretationService.CreateSignInterpretationAttributeAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, signInterpretationId
					, newSignInterpretationAttributes);

		/// <summary>
		///  This changes the values of the specified sign interpretation attribute,
		///  mainly used to change commentary.
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
		/// <param name="attributeValueId">Id of the attribute value to be altered</param>
		/// <param name="alteredSignInterpretationAttribute">New details of the attribute</param>
		/// <returns>The updated sign interpretation</returns>
		[HttpPut(
				"v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/attributes/{attributeValueId}")]
		public async Task<ActionResult<SignInterpretationDTO>> PutSignInterpretationAttribute(
				[FromRoute]   uint                             editionId
				, [FromRoute] uint                             signInterpretationId
				, [FromRoute] uint                             attributeValueId
				, [FromBody]  InterpretationAttributeCreateDTO alteredSignInterpretationAttribute)
			=> await _signInterpretationService.UpdateSignInterpretationAttributeAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, signInterpretationId
					, attributeValueId
					, alteredSignInterpretationAttribute);

		/// <summary>
		///  This deletes the specified attribute value from the specified sign interpretation.
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
		/// <param name="attributeValueId">Id of the attribute being removed</param>
		/// <returns>Ok or Error</returns>
		[HttpDelete(
				"v1/editions/{editionId}/sign-interpretations/{signInterpretationId}/attributes/{attributeValueId}")]
		public async Task<ActionResult> DeleteSignInterpretationAttribute(
				[FromRoute]   uint editionId
				, [FromRoute] uint signInterpretationId
				, [FromRoute] uint attributeValueId)
			=> await _signInterpretationService.DeleteSignInterpretationAttributeAsync(
					await _userService.GetCurrentUserObjectAsync(editionId, true)
					, signInterpretationId
					, attributeValueId);

		/// <summary>
		///  This is an admin endpoint used to trigger the generation of materialized sign streams.
		///  These streams are generated on demand by the API, but it can happen that some do not
		///  complete (a record in the database exists when a materialization was started but
		///  never finished).
		/// </summary>
		/// <param name="editionIds">
		///  A list of edition IDs for which to generate materialized
		///  sign streams.  If the list is empty, then the system will look for any unfinished
		///  jobs and complete those.
		/// </param>
		/// <returns></returns>
		[ApiExplorerSettings(IgnoreApi = true)]
		[HttpPost("v1/materialize-sign-streams")]
		public async Task<ActionResult> MaterializeSignStream([FromBody] uint[] editionIds)
			=> await _signInterpretationService.MaterializeSignStreams(
					await _userService.GetCurrentUserObjectAsync(null)
					, editionIds);
	}
}
