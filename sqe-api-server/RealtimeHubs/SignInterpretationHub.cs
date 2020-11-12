/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-api-server` project. Changes made
 * there will automatically be incorporated here the next time the
 * `sqe-realtime-hub-builder` is run.
 */

using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.DatabaseAccess.Helpers;

namespace SQE.API.Server.RealtimeHubs
{
	public partial class MainHub
	{
		/// <summary>
		///  Retrieve a list of all possible attributes for an edition
		/// </summary>
		/// <param name="editionId">The ID of the edition being searched</param>
		/// <returns>A list of and edition's attributes and their details</returns>
		[AllowAnonymous]
		public async Task<AttributeListDTO> GetV1EditionsEditionIdSignInterpretationsAttributes(
				uint editionId)

		{
			try
			{
				return await _signInterpretationService.GetEditionSignInterpretationAttributesAsync(
						await _userService.GetCurrentUserObjectAsync(editionId));
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Retrieve the details of a sign interpretation in an edition
		/// </summary>
		/// <param name="editionId">The ID of the edition being searched</param>
		/// <param name="signInterpretationId">The desired sign interpretation id</param>
		/// <returns>The details of the desired sign interpretation</returns>
		[AllowAnonymous]
		public async Task<SignInterpretationDTO>
				GetV1EditionsEditionIdSignInterpretationsSignInterpretationId(
						uint   editionId
						, uint signInterpretationId)

		{
			try
			{
				return await _signInterpretationService.GetEditionSignInterpretationAsync(
						await _userService.GetCurrentUserObjectAsync(editionId)
						, signInterpretationId);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Create a new attribute for an edition
		/// </summary>
		/// <param name="editionId">The ID of the edition being edited</param>
		/// <param name="newAttribute">The details of the new attribute</param>
		/// <returns>The details of the newly created attribute</returns>
		[Authorize]
		public async Task<AttributeDTO> PostV1EditionsEditionIdSignInterpretationsAttributes(
				uint                 editionId
				, CreateAttributeDTO newAttribute)

		{
			try
			{
				return await _signInterpretationService.CreateEditionAttributeAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, newAttribute);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Delete an attribute from an edition
		/// </summary>
		/// <param name="editionId">The ID of the edition being edited</param>
		/// <param name="attributeId">The ID of the attribute to delete</param>
		/// <returns></returns>
		[Authorize]
		public async Task DeleteV1EditionsEditionIdSignInterpretationsAttributesAttributeId(
				uint   editionId
				, uint attributeId)

		{
			try
			{
				await _signInterpretationService.DeleteEditionAttributeAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, attributeId);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Change the details of an attribute in an edition
		/// </summary>
		/// <param name="editionId">The ID of the edition being edited</param>
		/// <param name="attributeId">The ID of the attribute to update</param>
		/// <param name="updatedAttribute">The details of the updated attribute</param>
		/// <returns></returns>
		[Authorize]
		public async Task<AttributeDTO>
				PutV1EditionsEditionIdSignInterpretationsAttributesAttributeId(
						uint                 editionId
						, uint               attributeId
						, UpdateAttributeDTO updatedAttribute)

		{
			try
			{
				return await _signInterpretationService.UpdateEditionAttributeAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, attributeId
						, updatedAttribute);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Creates a new sign interpretation.  This creates a new sign entity for the submitted
		///  interpretation. This also takes care of inserting the sign interpretation into the
		///  sign stream following the specifications in the newSignInterpretation.
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="newSignInterpretation">New sign interpretation data to be added</param>
		/// <returns>The new sign interpretation</returns>
		[Authorize]
		public async Task<SignInterpretationListDTO> PostV1EditionsEditionIdSignInterpretations(
				uint                          editionId
				, SignInterpretationCreateDTO newSignInterpretation)

		{
			try
			{
				return await _signInterpretationService.CreateSignInterpretationAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, null
						, newSignInterpretation);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Creates a variant sign interpretation to the submitted sign interpretation id.
		///  This variant will be inserted into the sign stream following the specifications
		///  in the newSignInterpretation. If the properties for `attributes`, `rois`, or
		///  `commentary`
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">
		///  Id of the sign interpretation for which this variant
		///  will be created
		/// </param>
		/// <param name="newSignInterpretation">New sign interpretation data to be added</param>
		/// <returns>The new sign interpretation</returns>
		[Authorize]
		public async Task<SignInterpretationListDTO>
				PostV1EditionsEditionIdSignInterpretationsSignInterpretationId(
						uint                          editionId
						, uint                        signInterpretationId
						, SignInterpretationCreateDTO newSignInterpretation)

		{
			try
			{
				return await _signInterpretationService.CreateSignInterpretationAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, signInterpretationId
						, newSignInterpretation);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

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
		[Authorize]
		public async Task<SignInterpretationDeleteDTO>
				DeleteV1EditionsEditionIdSignInterpretationsSignInterpretationId(
						uint       editionId
						, uint     signInterpretationId
						, string[] optional)

		{
			try
			{
				return await _signInterpretationService.DeleteSignInterpretationAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, signInterpretationId
						, optional);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Links two sign interpretations together in the edition's sign stream
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">The sign interpretation to be linked to the nextSignInterpretationId</param>
		/// <param name="nextSignInterpretationId">The sign interpretation to become the new next sign interpretation</param>
		/// <returns>The updated sign interpretation</returns>
		[Authorize]
		public async Task<SignInterpretationDTO>
				PostV1EditionsEditionIdSignInterpretationsSignInterpretationIdLinkToNextSignInterpretationId(
						uint   editionId
						, uint signInterpretationId
						, uint nextSignInterpretationId)

		{
			try
			{
				return await _signInterpretationService.LinkSignInterpretationsAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, signInterpretationId
						, nextSignInterpretationId);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Links two sign interpretations in the edition's sign stream
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">The sign interpretation to be unlinked from the nextSignInterpretationId</param>
		/// <param name="nextSignInterpretationId">The sign interpretation to removed as next sign interpretation</param>
		/// <returns>The updated sign interpretation</returns>
		[Authorize]
		public async Task<SignInterpretationDTO>
				PostV1EditionsEditionIdSignInterpretationsSignInterpretationIdUnlinkFromNextSignInterpretationId(
						uint   editionId
						, uint signInterpretationId
						, uint nextSignInterpretationId)

		{
			try
			{
				return await _signInterpretationService.UnlinkSignInterpretationsAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, signInterpretationId
						, nextSignInterpretationId);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  Updates the commentary of a sign interpretation
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">ID of the sign interpretation whose commentary is being changed</param>
		/// <param name="commentary">The new commentary for the sign interpretation</param>
		/// <returns>Ok or Error</returns>
		[Authorize]
		public async Task<SignInterpretationDTO>
				PutV1EditionsEditionIdSignInterpretationsSignInterpretationIdCommentary(
						uint                  editionId
						, uint                signInterpretationId
						, CommentaryCreateDTO commentary)

		{
			try
			{
				return await _signInterpretationService
						.CreateOrUpdateSignInterpretationCommentaryAsync(
								await _userService.GetCurrentUserObjectAsync(editionId, true)
								, signInterpretationId
								, commentary);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  This adds a new attribute to the specified sign interpretation.
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">ID of the sign interpretation for adding a new attribute</param>
		/// <param name="newSignInterpretationAttributes">Details of the attribute to be added</param>
		/// <returns>The updated sign interpretation</returns>
		[Authorize]
		public async Task<SignInterpretationDTO>
				PostV1EditionsEditionIdSignInterpretationsSignInterpretationIdAttributes(
						uint                               editionId
						, uint                             signInterpretationId
						, InterpretationAttributeCreateDTO newSignInterpretationAttributes)

		{
			try
			{
				return await _signInterpretationService.CreateSignInterpretationAttributeAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, signInterpretationId
						, newSignInterpretationAttributes);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  This changes the values of the specified sign interpretation attribute,
		///  mainly used to change commentary.
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
		/// <param name="attributeValueId">Id of the attribute value to be altered</param>
		/// <param name="alteredSignInterpretationAttribute">New details of the attribute</param>
		/// <returns>The updated sign interpretation</returns>
		[Authorize]
		public async Task<SignInterpretationDTO>
				PutV1EditionsEditionIdSignInterpretationsSignInterpretationIdAttributesAttributeValueId(
						uint                               editionId
						, uint                             signInterpretationId
						, uint                             attributeValueId
						, InterpretationAttributeCreateDTO alteredSignInterpretationAttribute)

		{
			try
			{
				return await _signInterpretationService.UpdateSignInterpretationAttributeAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, signInterpretationId
						, attributeValueId
						, alteredSignInterpretationAttribute);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

		/// <summary>
		///  This deletes the specified attribute value from the specified sign interpretation.
		/// </summary>
		/// <param name="editionId">ID of the edition being changed</param>
		/// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
		/// <param name="attributeValueId">Id of the attribute being removed</param>
		/// <returns>Ok or Error</returns>
		[Authorize]
		public async Task
				DeleteV1EditionsEditionIdSignInterpretationsSignInterpretationIdAttributesAttributeValueId(
						uint   editionId
						, uint signInterpretationId
						, uint attributeValueId)

		{
			try
			{
				await _signInterpretationService.DeleteSignInterpretationAttributeAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, signInterpretationId
						, attributeValueId);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}

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
		[Authorize]
		public async Task PostV1MaterializeSignStreams(uint[] editionIds)

		{
			try
			{
				await _signInterpretationService.MaterializeSignStreams(
						await _userService.GetCurrentUserObjectAsync(null)
						, editionIds);
			}
			catch (ApiException err)
			{
				throw new HubException(
						JsonSerializer.Serialize(
								new HttpExceptionMiddleware.ApiExceptionError(
										nameof(err)
										, err.Error
										, err is IExceptionWithData exceptionWithData
												? exceptionWithData.CustomReturnedData
												: null)));
			}
		}
	}
}
