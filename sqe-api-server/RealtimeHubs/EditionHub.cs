/*
 * Do not edit this file directly!
 * This hub class is autogenerated by the `sqe-realtime-hub-builder` project
 * based on the controllers in the `sqe-api-server` project. Changes made
 * there will automatically be incorporated here the next time the
 * `sqe-realtime-hub-builder` is run.
 */

using System.Collections.Generic;
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
		///  Adds an editor to the specified edition
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="payload">JSON object with the attributes of the new editor</param>
		[Authorize]
		public async Task PostV1EditionsEditionIdAddEditorRequest(
				uint              editionId
				, InviteEditorDTO payload)

		{
			try
			{
				await _editionService.RequestNewEditionEditor(
						await _userService.GetCurrentUserObjectAsync(editionId, admin: true)
						, payload);
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
		///  Get a list of requests issued by the current user for other users
		///  to become editors of a shared edition
		/// </summary>
		/// <returns></returns>
		[Authorize]
		public async Task<AdminEditorRequestListDTO> GetV1EditionsAdminShareRequests()

		{
			try
			{
				return await _editionService.GetAdminEditorRequests(
						_userService.GetCurrentUserId() ?? 0);
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
		///  Get a list of invitations issued to the current user to become an editor of a shared edition
		/// </summary>
		/// <returns></returns>
		[Authorize]
		public async Task<EditorInvitationListDTO> GetV1EditionsEditorInvitations()

		{
			try
			{
				return await _editionService.GetUserEditorInvitations(
						_userService.GetCurrentUserId() ?? 0);
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
		///  Confirm addition of an editor to the specified edition
		/// </summary>
		/// <param name="token">JWT for verifying the request confirmation</param>
		[Authorize]
		public async Task<DetailedEditorRightsDTO> PostV1EditionsConfirmEditorshipToken(
				string token)

		{
			try
			{
				return await _editionService.AddEditionEditor(
						_userService.GetCurrentUserId()
						, token);
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
		///  Changes the rights for an editor of the specified edition
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="editorEmailId">Email address of the editor whose permissions are being changed</param>
		/// <param name="payload">JSON object with the attributes of the new editor</param>
		[Authorize]
		public async Task<DetailedEditorRightsDTO> PutV1EditionsEditionIdEditorsEditorEmailId(
				uint                    editionId
				, string                editorEmailId
				, UpdateEditorRightsDTO payload)

		{
			try
			{
				return await _editionService.ChangeEditionEditorRights(
						await _userService.GetCurrentUserObjectAsync(editionId, admin: true)
						, editorEmailId
						, payload);
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
		///  Creates a copy of the specified edition
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
		[Authorize]
		public async Task<EditionDTO> PostV1EditionsEditionId(
				uint             editionId
				, EditionCopyDTO request)

		{
			try
			{
				return await _editionService.CopyEditionAsync(
						await _userService.GetCurrentUserObjectAsync(editionId)
						, request);
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
		///  Archives an edition so that in no longer appears in user data and searches. An admin
		///  may use the archiveForAllEditors optional parameter in order to archive the edition
		///  for all editors (must be confirmed with an archive token).
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition to be archived</param>
		/// <param name="optional">Optional parameters: 'archiveForAllEditors'</param>
		/// <param name="token">token required when using optional 'archiveForAllEditors'</param>
		[Authorize]
		public async Task<ArchiveTokenDTO> DeleteV1EditionsEditionId(
				uint           editionId
				, List<string> optional
				, string       token)

		{
			try
			{
				return await _editionService.ArchiveEditionAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, token
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
		///  Provides details about the specified edition and all accessible alternate editions
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		[AllowAnonymous]
		public async Task<EditionGroupDTO> GetV1EditionsEditionId(uint editionId)

		{
			try
			{
				return await _editionService.GetEditionAsync(
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
		///  Provides a listing of all editions accessible to the current user
		/// </summary>
		[AllowAnonymous]
		public async Task<EditionListDTO> GetV1Editions(bool? published, bool? personal)

		{
			try
			{
				return await _editionService.ListEditionsAsync(
						_userService.GetCurrentUserId()
						, published
						, personal);
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
		///  Updates data for the specified edition
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <param name="request">JSON object with the attributes to be updated</param>
		[Authorize]
		public async Task<EditionDTO> PutV1EditionsEditionId(
				uint                      editionId
				, EditionUpdateRequestDTO request)

		{
			try
			{
				return await _editionService.UpdateEditionAsync(
						await _userService.GetCurrentUserObjectAsync(editionId, true)
						, request);
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
		///  Provides spatial data for all letters in the edition
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <returns></returns>
		[AllowAnonymous]
		public async Task<EditionScriptCollectionDTO> GetV1EditionsEditionIdScriptCollection(
				uint editionId)

		{
			try
			{
				return await _editionService.GetEditionScriptCollection(
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
		///  Provides spatial data for all letters in the edition organized and oriented
		///  by lines.
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <returns></returns>
		[AllowAnonymous]
		public async Task<EditionScriptLinesDTO> GetV1EditionsEditionIdScriptLines(uint editionId)

		{
			try
			{
				return await _editionService.GetEditionScriptLines(
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
		///  Retrieve extra institutional metadata concerning the edition
		///  manuscript if available.
		/// </summary>
		/// <param name="editionId">Unique Id of the desired edition</param>
		/// <returns></returns>
		[AllowAnonymous]
		public async Task<EditionManuscriptMetadataDTO> GetV1EditionsEditionIdMetadata(
				uint editionId)

		{
			try
			{
				return await _editionService.GetEditionMetadata(
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

		[AllowAnonymous]
		public async Task<EditionListDTO> GetV1ManuscriptsManuscriptIdEditions(uint manuscriptId)

		{
			try
			{
				return await _editionService.GetManuscriptEditionsAsync(
						_userService.GetCurrentUserId()
						, manuscriptId);
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
