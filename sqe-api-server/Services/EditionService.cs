using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NaturalSort.Extension;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Union;
using SQE.API.DTO;
using SQE.API.Server.Helpers;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Serialization;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.API.Server.Services
{
	public interface IEditionService
	{
		Task<EditionGroupDTO> GetEditionAsync(
				UserInfo editionUser
				, bool   artefacts = false
				, bool   fragments = false);

		Task<EditionListDTO> ListEditionsAsync(
				uint?   userId
				, bool? published = null
				, bool? personal  = null);

		Task<EditionListDTO> GetManuscriptEditionsAsync(uint? userId, uint manuscriptId);

		Task<EditionDTO> UpdateEditionAsync(
				UserInfo                  editionUser
				, EditionUpdateRequestDTO updatedEdition
				, string                  clientId = null);

		Task<EditionDTO> CopyEditionAsync(
				UserInfo         editionUser
				, EditionCopyDTO editionInfo
				, string         clientId = null);

		Task<ArchiveTokenDTO> ArchiveEditionAsync(
				UserInfo       editionUser
				, string       token
				, List<string> optional
				, string       clientId = null);

		Task<NoContentResult> RequestNewEditionEditor(
				UserInfo          editionUser
				, InviteEditorDTO newEditor
				, string          clientId = null);

		Task<AdminEditorRequestListDTO> GetAdminEditorRequests(uint?   userId);
		Task<EditorInvitationListDTO>   GetUserEditorInvitations(uint? userId);

		Task<DetailedEditorRightsDTO> AddEditionEditor(
				uint?    userId
				, string token
				, string clientId = null);

		Task<DetailedEditorRightsDTO> ChangeEditionEditorRights(
				UserInfo                editionUser
				, string                editorEmail
				, UpdateEditorRightsDTO updatedEditor
				, string                clientId = null);

		Task<EditionScriptCollectionDTO> GetEditionScriptCollection(UserInfo editionUser);

		Task<EditionScriptLinesDTO> GetEditionScriptLines(UserInfo editionUser);
	}

	public class EditionService : IEditionService
	{
		private readonly AppSettings                      _appSettings;
		private readonly IEditionRepository               _editionRepo;
		private readonly IEmailSender                     _emailSender;
		private readonly IHubContext<MainHub, ISQEClient> _hubContext;
		private readonly IUserRepository                  _userRepo;
		private readonly IUserService                     _userService;
		private readonly string                           webServer;

		public EditionService(
				IEditionRepository                 editionRepo
				, IUserRepository                  userRepo
				, IUserService                     userService
				, IHubContext<MainHub, ISQEClient> hubContext
				, IEmailSender                     emailSender
				, IConfiguration                   config
				, IOptions<AppSettings>            appSettings)
		{
			_editionRepo = editionRepo;
			_userRepo = userRepo;
			_userService = userService;
			_hubContext = hubContext;
			_emailSender = emailSender;
			webServer = config.GetConnectionString("WebsiteHost");
			_appSettings = appSettings.Value;
		}

		public async Task<EditionGroupDTO> GetEditionAsync(
				UserInfo editionUser
				, bool   artefacts = false
				, bool   fragments = false)
		{
			var scrollModels = await _editionRepo.ListEditionsAsync(
					editionUser.userId
					, editionUser.EditionId);

			var primaryModel =
					scrollModels.FirstOrDefault(sv => sv.EditionId == editionUser.EditionId);

			if (primaryModel == null) // User is not allowed to see this scroll version
				return null;

			var otherModels = scrollModels.Where(sv => sv.EditionId != editionUser.EditionId)
										  .OrderBy(sv => sv.EditionId);

			var editionGroup = new EditionGroupDTO
			{
					primary = primaryModel.ToDTO()
					, others = otherModels.Select(x => x.ToDTO())
					,
			};

			return editionGroup;
		}

		public async Task<EditionListDTO> ListEditionsAsync(
				uint?   userId
				, bool? published = null
				, bool? personal  = null)
		{
			// Check if both published and personal are null (if so assume true)
			if (!published.HasValue
				&& !personal.HasValue)
			{
				published = true;
				personal = true;
			}

			// If either published or personal are null assume false
			published ??= false;
			personal ??= false;

			return new EditionListDTO
			{
					editions = (await _editionRepo.ListEditionsAsync(
									   userId
									   , null
									   , published.Value
									   , personal.Value))
							   .OrderBy(
									   x => x.Name
									   , StringComparison.OrdinalIgnoreCase.WithNaturalSort())
							   .Select(x => new List<EditionDTO> { x.ToDTO() })
							   .ToList()
					, // Convert the list of groups from IEnumerable to List so we now have List<List<EditionDTO>>
			};
		}

		public async Task<EditionListDTO> GetManuscriptEditionsAsync(
				uint?  userId
				, uint manuscriptId)
		{
			return new EditionListDTO
			{
					editions = (await _editionRepo.GetManuscriptEditions(userId, manuscriptId))
							   .Select(x => new List<EditionDTO> { x.ToDTO() })
							   .ToList()
					, // Convert the list of groups from IEnumerable to List so we now have List<List<EditionDTO>>
			};
		}

		public async Task<EditionDTO> UpdateEditionAsync(
				UserInfo                  editionUser
				, EditionUpdateRequestDTO updatedEditionData
				, string                  clientId = null)
		{
			var editionBeforeChanges =
					(await _editionRepo.ListEditionsAsync(
							editionUser.userId
							, editionUser.EditionId)).First();

			if ((updatedEditionData.copyrightHolder != null)
				|| ((updatedEditionData.collaborators != null)
					&& (editionBeforeChanges.Collaborators != updatedEditionData.collaborators)))
			{
				await _editionRepo.ChangeEditionCopyrightAsync(
						editionUser
						, updatedEditionData.copyrightHolder
						, updatedEditionData.collaborators);
			}

			if (!string.IsNullOrEmpty(updatedEditionData.name))
				await _editionRepo.ChangeEditionNameAsync(editionUser, updatedEditionData.name);

			if ((updatedEditionData.metrics != null)
				&& ((updatedEditionData.metrics.xOrigin != editionBeforeChanges.XOrigin)
					|| (updatedEditionData.metrics.yOrigin != editionBeforeChanges.YOrigin)
					|| (updatedEditionData.metrics.width != editionBeforeChanges.Width)
					|| (updatedEditionData.metrics.height != editionBeforeChanges.Height)))
			{
				await _editionRepo.UpdateEditionMetricsAsync(
						editionUser
						, updatedEditionData.metrics.width
						, updatedEditionData.metrics.height
						, updatedEditionData.metrics.xOrigin
						, updatedEditionData.metrics.yOrigin);
			}

			var editions =
					await _editionRepo.ListEditionsAsync(
							editionUser.userId
							, editionUser.EditionId); //get wanted edition by edition Id

			var updatedEdition = editions.First(x => x.EditionId == editionUser.EditionId).ToDTO();

			// Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
			// made the request, that client directly received the response.
			var editionUsers = await _editionRepo.GetEditionEditorUserIdsAsync(editionUser);

			foreach (var userId in editionUsers)
			{
				await _hubContext.Clients.GroupExcept($"user-{userId.ToString()}", clientId)
								 .UpdatedEdition(updatedEdition);
			}

			return updatedEdition;
		}

		public async Task<EditionDTO> CopyEditionAsync(
				UserInfo         editionUser
				, EditionCopyDTO editionInfo
				, string         clientId = null)
		{
			EditionDTO edition;

			// Clone edition
			var copyToEditionId = await _editionRepo.CopyEditionAsync(
					editionUser
					, editionInfo.name
					, editionInfo.copyrightHolder
					, editionInfo.collaborators);

			// Check if is success is true, else throw error.
			if (editionUser.EditionId == copyToEditionId)
				throw new Exception($"Failed to clone {editionUser.EditionId}.");

			await editionUser.SetEditionId(
					copyToEditionId); // Update user object for the new editionId

			var editions =
					await _editionRepo.ListEditionsAsync(
							editionUser.userId
							, editionUser.EditionId); //get wanted scroll by Id

			var unformattedEdition = editions.First(x => x.EditionId == editionUser.EditionId);

			//I think we do not get this far if no records were found, `First` will, I think throw an error.
			//Maybe we should more often make use of try/catch.
			if (unformattedEdition == null)
			{
				throw new StandardExceptions.DataNotFoundException(
						"edition"
						, editionUser.EditionId.Value);
			}

			edition = unformattedEdition.ToDTO();

			// Broadcast edition creation notification to all connections of this user
			await _hubContext.Clients.GroupExcept($"user-{editionUser.userId.ToString()}", clientId)
							 .CreatedEdition(edition);

			return edition; //need to return the updated scroll
		}

		/// <summary>
		///  Archive an edition that the user is currently subscribed to. The user must be admin and
		///  provide a valid archive token to archive the edition for all editors.
		/// </summary>
		/// <param name="editionUser">User object requesting the archive</param>
		/// <param name="token">token required for optional "archiveForAllEditors"</param>
		/// <param name="optional">optional parameters: "archiveForAllEditors"</param>
		/// <param name="clientId"></param>
		/// <returns></returns>
		public async Task<ArchiveTokenDTO> ArchiveEditionAsync(
				UserInfo       editionUser
				, string       token
				, List<string> optional
				, string       clientId = null)
		{
			_parseOptional(optional, out var archiveForAllEditors);

			var archiveResponse = new ArchiveTokenDTO
			{
					editionId = editionUser.EditionId.Value
					, token = null
					,
			};

			// Check if the edition should be deleted for all users
			if (archiveForAllEditors && editionUser.IsAdmin)
			{
				var editionUsers = await _editionRepo.GetEditionEditorUserIdsAsync(editionUser);

				// Try to delete the edition fully for all editors
				var newToken = await _editionRepo.ArchiveEditionAsync(editionUser, token);

				// End the request with null for successful delete or a proper token for requests without a confirmation token
				if (string.IsNullOrEmpty(newToken))
				{
					// Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
					// made the request, that client directly received the response.
					foreach (var userId in editionUsers)
					{
						await _hubContext.Clients.GroupExcept($"user-{userId.ToString()}", clientId)
										 .DeletedEdition(archiveResponse);
					}

					return null;
				}

				// Return the token that an admin can use to confirm the archive request
				// and complete it
				archiveResponse.token = newToken;

				return archiveResponse;
			}

			// The edition should only be made inaccessible only for the current user
			var userInfo = await _userRepo.GetDetailedUserByIdAsync(editionUser.userId);

			// Setting all permission to false is how we delete a user's access to an edition.
			await _editionRepo.ChangeEditionEditorRightsAsync(
					editionUser
					, userInfo.Email
					, false
					, false
					, false
					, false);

			// Broadcast edition deletion notification to all connections of this user
			await _hubContext.Clients.GroupExcept($"user-{editionUser.userId.ToString()}", clientId)
							 .DeletedEdition(archiveResponse);

			return archiveResponse;
		}

		/// <summary>
		///  Sends a request to a user to become an editor of the editionUser edition
		///  with the specified permissions
		/// </summary>
		/// <param name="editionUser">User object making the request</param>
		/// <param name="newEditor">Details of the new editor to be added</param>
		/// <param name="clientId"></param>
		/// <returns></returns>
		public async Task<NoContentResult> RequestNewEditionEditor(
				UserInfo          editionUser
				, InviteEditorDTO newEditor
				, string          clientId = null)
		{
			var requestingUser = await _userRepo.GetDetailedUserByIdAsync(editionUser.userId);

			var newUserToken = await _editionRepo.RequestAddEditionEditorAsync(
					editionUser
					, newEditor.email
					, true
					, // New editors can always read (otherwise there is no point)
					newEditor.mayWrite
					, newEditor.mayLock
					, newEditor.isAdmin);

			var editions =
					await _editionRepo.ListEditionsAsync(
							editionUser.userId
							, editionUser.EditionId); //get wanted edition by edition Id

			var edition = editions.First(x => x.EditionId == editionUser.EditionId).ToDTO();

			const string emailBody = @"
<html><body>Dear $User,<br>
<br>
$Admin has invited you to become an editor on $EditionName. To accept the invitation, please <a href=""$WebServer/accept-invitation/token/$Token"">click here</a>. If you do not wish to accept this invitation, no action is necessary on your
part and the invitation will expire in $ExpirationPeriod.<br>
<br>
Best wishes,<br>
The Scripta Qumranica Electronica team</body></html>";

			const string emailSubject =
					"Invitation to edit $EditionName in Scripta Qumranica Electronica";

			var newEditorName = !string.IsNullOrEmpty(newUserToken.Forename)
								|| !string.IsNullOrEmpty(newUserToken.Surname)
					? (newUserToken.Forename + " " + newUserToken.Surname).Trim()
					: newUserToken.Email;

			var adminName = !string.IsNullOrEmpty(requestingUser.Forename)
							|| !string.IsNullOrEmpty(requestingUser.Surname)
					? (requestingUser.Forename + " " + requestingUser.Surname).Trim()
					: requestingUser.Email;

			await _emailSender.SendEmailAsync(
					newEditor.email
					, emailSubject.Replace("$EditionName", edition.name)
					, emailBody.Replace("$User", newEditorName)
							   .Replace("$Admin", adminName)
							   .Replace("$WebServer", webServer)
							   .Replace("$Token", newUserToken.Token.ToString())
							   .Replace("$EditionName", edition.name)
							   .Replace("$ExpirationPeriod", _appSettings.EmailTokenDaysValid));

			// Broadcast the request to the potential editor.
			var editorBroadcastObject = new EditorInvitationDTO
			{
					editionId = editionUser.EditionId.Value
					, editionName = edition.name
					, requestingAdminName = adminName
					, requestingAdminEmail = requestingUser.Email
					, isAdmin = newEditor.isAdmin
					, mayLock = newEditor.mayLock
					, mayWrite = newEditor.mayWrite
					, mayRead = true
					, // New editors can always read (otherwise there is no point)
					token = newUserToken.Token
					, date = newUserToken.Date
					,
			};

			await _hubContext.Clients.Group($"user-{newUserToken.UserId.ToString()}")
							 .RequestedEditor(editorBroadcastObject);

			return new NoContentResult();
		}

		/// <summary>
		///  Request a list of requests that a user has sent other users to become editors
		/// </summary>
		/// <param name="userId">Id of the user who has issued the requests for others to becom editors</param>
		/// <returns></returns>
		/// <exception cref="StandardExceptions.NoAuthorizationException"></exception>
		public async Task<AdminEditorRequestListDTO> GetAdminEditorRequests(uint? userId)
		{
			if (!userId.HasValue)
				throw new StandardExceptions.NoAuthorizationException();

			return new AdminEditorRequestListDTO
			{
					editorRequests =
							(await _editionRepo.GetOutstandingEditionEditorRequestsAsync(
									userId.Value)).Select(
														  x => new AdminEditorRequestDTO
														  {
																  date = x.Date
																  , editionId = x.EditionId
																  , editionName = x.EditionName
																  , editorEmail = x.Email
																  , editorName =
																		  $"{x.EditorForename} {x.EditorSurname}, {x.EditorOrganization}"
																  , isAdmin = x.IsAdmin
																  , mayLock = x.MayLock
																  , mayRead = x.MayRead
																  , mayWrite = x.MayWrite
																  ,
														  })
												  .ToList()
					,
			};
		}

		/// <summary>
		///  Request a list of a user's outstanding invitations to become editor
		/// </summary>
		/// <param name="userId">User id to check for outstanding editor invitations</param>
		/// <returns></returns>
		/// <exception cref="StandardExceptions.NoAuthorizationException"></exception>
		public async Task<EditorInvitationListDTO> GetUserEditorInvitations(uint? userId)
		{
			if (!userId.HasValue)
				throw new StandardExceptions.NoAuthorizationException();

			return new EditorInvitationListDTO
			{
					editorInvitations =
							(await _editionRepo.GetOutstandingEditionEditorInvitationsAsync(
									userId.Value)).Select(
														  x => new EditorInvitationDTO
														  {
																  date = x.Date
																  , editionId = x.EditionId
																  , editionName = x.EditionName
																  , token = x.Token
																  , requestingAdminEmail = x.Email
																  , requestingAdminName =
																		  $"{x.AdminForename} {x.AdminSurname}, {x.AdminOrganization}"
																  , isAdmin = x.IsAdmin
																  , mayLock = x.MayLock
																  , mayRead = x.MayRead
																  , mayWrite = x.MayWrite
																  ,
														  })
												  .ToList()
					,
			};
		}

		/// <summary>
		///  Adds a new editor to an edition with the requested access rights
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="token">JWT to verify the request</param>
		/// <param name="clientId"></param>
		/// <returns></returns>
		public async Task<DetailedEditorRightsDTO> AddEditionEditor(
				uint?    userId
				, string token
				, string clientId = null)
		{
			if (!userId.HasValue)
				throw new StandardExceptions.NoAuthorizationException();

			var newUserPermissions = await _editionRepo.AddEditionEditorAsync(token, userId.Value);

			var newEditorDTO = _permissionsToEditorRightsDTO(
					newUserPermissions.Email
					, newUserPermissions.MayRead
					, newUserPermissions.MayWrite
					, newUserPermissions.MayLock
					, newUserPermissions.IsAdmin
					, newUserPermissions.EditionId);

			// Broadcast the change to all editors of the editionId. Exclude the client (not the user), which
			// made the request, that client directly received the response.
			var editionUser =
					await _userService.GetCurrentUserObjectAsync(newUserPermissions.EditionId);

			var editionUsers = await _editionRepo.GetEditionEditorUserIdsAsync(editionUser);

			foreach (var editionUserId in editionUsers)
			{
				await _hubContext.Clients.GroupExcept($"user-{editionUserId.ToString()}", clientId)
								 .CreatedEditor(newEditorDTO);
			}

			return newEditorDTO;
		}

		/// <summary>
		///  Changes the access rights of an editor
		/// </summary>
		/// <param name="editionUser">User object making the request</param>
		/// <param name="editorEmail"></param>
		/// <param name="updatedEditor">Details of the editor and the desired access rights</param>
		/// <param name="clientId"></param>
		/// <returns></returns>
		public async Task<DetailedEditorRightsDTO> ChangeEditionEditorRights(
				UserInfo                editionUser
				, string                editorEmail
				, UpdateEditorRightsDTO updatedEditor
				, string                clientId = null)
		{
			var updatedUserPermissions = await _editionRepo.ChangeEditionEditorRightsAsync(
					editionUser
					, editorEmail
					, updatedEditor.mayRead
					, updatedEditor.mayWrite
					, updatedEditor.mayLock
					, updatedEditor.isAdmin);

			var updatedEditorDTO = _permissionsToEditorRightsDTO(
					editorEmail
					, updatedUserPermissions.MayRead
					, updatedUserPermissions.MayWrite
					, updatedUserPermissions.MayLock
					, updatedUserPermissions.IsAdmin
					, editionUser.EditionId.Value);

			// Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
			// made the request, that client directly received the response.
			var editionUsers = await _editionRepo.GetEditionEditorUserIdsAsync(editionUser);

			foreach (var editionUserId in editionUsers)
			{
				await _hubContext.Clients.GroupExcept($"user-{editionUserId.ToString()}", clientId)
								 .CreatedEditor(updatedEditorDTO);
			}

			return updatedEditorDTO;
		}

		public async Task<EditionScriptCollectionDTO> GetEditionScriptCollection(
				UserInfo editionUser)
		{
			var letters = await _editionRepo.GetEditionScriptCollectionAsync(editionUser);

			var lettersSorted = letters.GroupBy(x => x.Id).ToList();
			var wkbr = new WKBReader();
			var wkw = new WKTWriter();

			return new EditionScriptCollectionDTO
			{
					letters = lettersSorted.Select(
												   x =>
												   {
													   var polys = x.Select(
																			y =>
																			{
																				var poly =
																						wkbr.Read(
																								y.Polygon);

																				var tr =
																						new
																								AffineTransformation();

																				var rotation =
																						y.LetterRotation;

																				tr.Rotate(
																						rotation
																						, poly
																						  .Centroid
																						  .X
																						, poly
																						  .Centroid
																						  .Y);

																				tr.Translate(
																						y.TranslateX
																						, y
																								.TranslateY);

																				poly = tr.Transform(
																						poly);

																				return poly;
																			})
																	.Where(
																			z => z.IsValid
																				 && !z.IsEmpty)
																	.ToList();

													   var cpu = new CascadedPolygonUnion(polys);

													   var combinedPoly = cpu.Union();

													   var envelope = polys.Any()
															   ? combinedPoly.EnvelopeInternal
															   : new Envelope(
																	   0
																	   , 0
																	   , 0
																	   , 0);

													   if (polys.Any())
													   {
														   var tr = new AffineTransformation();

														   tr.Translate(
																   -envelope.MinX
																   , -envelope.MinY);

														   var translatedPoly =
																   tr.Transform(combinedPoly);

														   combinedPoly = translatedPoly;
													   }

													   return new CharacterShapeDTO
													   {
															   id = x.First().Id
															   , character = x.First().Letter
															   , rotation = x.First().ImageRotation
															   , imageURL =
																	   x.First().ImageURL
																	   + $"/{envelope.MinX},{envelope.MinY},{envelope.Width},{envelope.Height}/full/0/"
																	   + x.First().ImageSuffix
															   , polygon = polys.Any()
																	   ? wkw.Write(combinedPoly)
																	   : null
															   , attributes =
																	   x.First()
																		.Attributes.Split(",")
																		.ToList()
															   ,
													   };
												   })
										   .ToList()
					,
			};
		}

		// TODO: we need to gather also the editor ID's
		public async Task<EditionScriptLinesDTO> GetEditionScriptLines(UserInfo editionUser)
		{
			var results = await _editionRepo.GetEditionScriptLines(editionUser);

			return new EditionScriptLinesDTO
			{
					textFragments = results.Select(a => a.ToDTO()).ToList(),
			};
		}

		private static DetailedEditorRightsDTO _permissionsToEditorRightsDTO(
				string editorEmail
				, bool mayRead
				, bool mayWrite
				, bool mayLock
				, bool isAdmin
				, uint editionId) => new DetailedEditorRightsDTO
		{
				email = editorEmail
				, mayRead = mayRead
				, mayWrite = mayWrite
				, mayLock = mayLock
				, isAdmin = isAdmin
				, editionId = editionId
				,
		};

		private static void _parseOptional(
				ICollection<string> optional
				, out bool          deleteForAllEditors)
		{
			deleteForAllEditors = optional.Contains("archiveForAllEditors");
		}
	}
}
