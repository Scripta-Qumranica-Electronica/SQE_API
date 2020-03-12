using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;
using SQE.API.Server.Helpers;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Precision;
using NetTopologySuite.Utilities;
using Matrix = System.Drawing.Drawing2D.Matrix;

namespace SQE.API.Server.Services
{
    public interface IEditionService
    {
        Task<EditionGroupDTO> GetEditionAsync(EditionUserInfo editionUser,
            bool artefacts = false,
            bool fragments = false);

        Task<EditionListDTO> ListEditionsAsync(uint? userId);

        Task<EditionDTO> UpdateEditionAsync(EditionUserInfo editionUser,
            EditionUpdateRequestDTO updatedEdition,
            string clientId = null);

        Task<EditionDTO> CopyEditionAsync(EditionUserInfo editionUser,
            EditionCopyDTO editionInfo,
            string clientId = null);

        Task<DeleteTokenDTO> DeleteEditionAsync(EditionUserInfo editionUser,
            string token,
            List<string> optional,
            string clientId = null);

        Task<NoContentResult> RequestNewEditionEditor(EditionUserInfo editionUser,
            CreateEditorRightsDTO newEditor,
            string clientId = null);

        Task<CreateEditorRightsDTO> AddEditionEditor(uint? userId,
            string token,
            string clientId = null);

        Task<CreateEditorRightsDTO> ChangeEditionEditorRights(EditionUserInfo editionUser,
            string editorEmail,
            UpdateEditorRightsDTO updatedEditor,
            string clientId = null);

        Task<EditionScriptCollectionDTO> GetEditionScriptCollection(EditionUserInfo editionUser);
    }

    public class EditionService : IEditionService
    {
        private readonly IEditionRepository _editionRepo;
        private readonly IEmailSender _emailSender;
        private readonly IHubContext<MainHub, ISQEClient> _hubContext;
        private readonly IUserRepository _userRepo;
        private readonly IUserService _userService;
        private readonly string webServer;

        public EditionService(IEditionRepository editionRepo,
            IUserRepository userRepo,
            IUserService userService,
            IHubContext<MainHub, ISQEClient> hubContext,
            IEmailSender emailSender,
            IConfiguration config)
        {
            _editionRepo = editionRepo;
            _userRepo = userRepo;
            _userService = userService;
            _hubContext = hubContext;
            _emailSender = emailSender;
            webServer = config.GetConnectionString("WebsiteHost");
        }

        public async Task<EditionGroupDTO> GetEditionAsync(EditionUserInfo editionUser,
            bool artefacts = false,
            bool fragments = false)
        {
            var scrollModels = await _editionRepo.ListEditionsAsync(editionUser.userId, editionUser.EditionId);

            var primaryModel = scrollModels.FirstOrDefault(sv => sv.EditionId == editionUser.EditionId);
            if (primaryModel == null) // User is not allowed to see this scroll version
                return null;
            var otherModels = scrollModels.Where(sv => sv.EditionId != editionUser.EditionId)
                .OrderBy(sv => sv.EditionId);

            var editionGroup = new EditionGroupDTO
            {
                primary = EditionModelToDTO(primaryModel),
                others = otherModels.Select(EditionModelToDTO)
            };

            return editionGroup;
        }

        public async Task<EditionListDTO> ListEditionsAsync(uint? userId)
        {
            return new EditionListDTO
            {
                editions = (await _editionRepo.ListEditionsAsync(userId, null))
                    .GroupBy(x => x.ScrollId) // Group the edition listings by scroll_id
                    .Select(x => x.Select(EditionModelToDTO)) // Format each entry as an EditionDTO
                    .Select(x => x.ToList()) // Convert the groups from IEnumerable to List
                    .ToList() // Convert the list of groups from IEnumerable to List so we now have List<List<EditionDTO>>
            };
        }

        public async Task<EditionDTO> UpdateEditionAsync(EditionUserInfo editionUser,
            EditionUpdateRequestDTO updatedEditionData,
            string clientId = null)
        {
            var editionBeforeChanges =
                (await _editionRepo.ListEditionsAsync(editionUser.userId, editionUser.EditionId)).First();

            if (updatedEditionData.copyrightHolder != null
                || editionBeforeChanges.Collaborators != updatedEditionData.collaborators)
                await _editionRepo.ChangeEditionCopyrightAsync(
                    editionUser,
                    updatedEditionData.copyrightHolder,
                    updatedEditionData.collaborators
                );

            if (!string.IsNullOrEmpty(updatedEditionData.name))
                await _editionRepo.ChangeEditionNameAsync(editionUser, updatedEditionData.name);

            var editions = await _editionRepo.ListEditionsAsync(
                editionUser.userId,
                editionUser.EditionId
            ); //get wanted edition by edition Id

            var updatedEdition = EditionModelToDTO(editions.First(x => x.EditionId == editionUser.EditionId));
            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            var editionUsers = await _editionRepo.GetEditionEditorUserIds(editionUser);
            foreach (var userId in editionUsers)
                await _hubContext.Clients.GroupExcept($"user-{userId.ToString()}", clientId)
                    .UpdatedEdition(updatedEdition);

            return updatedEdition;
        }

        public async Task<EditionDTO> CopyEditionAsync(EditionUserInfo editionUser,
            EditionCopyDTO editionInfo,
            string clientId = null)
        {
            EditionDTO edition;
            // Clone edition
            var copyToEditionId = await _editionRepo.CopyEditionAsync(
                editionUser,
                editionInfo.copyrightHolder,
                editionInfo.collaborators
            );
            if (editionUser.EditionId == copyToEditionId)
                // Check if is success is true, else throw error.
                throw new Exception($"Failed to clone {editionUser.EditionId}.");
            await editionUser.SetEditionId(copyToEditionId); // Update user object for the new editionId

            //Change the Name, if a Name has been passed
            if (!string.IsNullOrEmpty(editionInfo.name))
            {
                edition = await UpdateEditionAsync(editionUser, editionInfo, clientId); // Change the Name.
            }
            else
            {
                var editions = await _editionRepo.ListEditionsAsync(
                    editionUser.userId,
                    editionUser.EditionId
                ); //get wanted scroll by Id
                var unformattedEdition = editions.First(x => x.EditionId == editionUser.EditionId);
                //I think we do not get this far if no records were found, `First` will, I think throw an error.
                //Maybe we should more often make use of try/catch.
                if (unformattedEdition == null)
                    throw new StandardExceptions.DataNotFoundException("edition", editionUser.EditionId);
                edition = EditionModelToDTO(unformattedEdition);
            }

            // Broadcast edition creation notification to all connections of this user
            await _hubContext.Clients.GroupExcept($"user-{editionUser.userId.ToString()}", clientId)
                .CreatedEdition(edition);

            return edition; //need to return the updated scroll
        }

        /// <summary>
        ///     Delete all data from the edition that the user is currently subscribed to. The user must be admin and
        ///     provide a valid delete token.
        /// </summary>
        /// <param name="editionUser">User object requesting the delete</param>
        /// <param name="token">token required for optional "deleteForAllEditors"</param>
        /// <param name="optional">optional parameters: "deleteForAllEditors"</param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<DeleteTokenDTO> DeleteEditionAsync(EditionUserInfo editionUser,
            string token,
            List<string> optional,
            string clientId = null)
        {
            _parseOptional(optional, out var deleteForAllEditors);

            var deleteResponse = new DeleteTokenDTO
            {
                editionId = editionUser.EditionId,
                token = null
            };

            // Check if the edition should be deleted for all users
            if (deleteForAllEditors)
            {
                var editionUsers = await _editionRepo.GetEditionEditorUserIds(editionUser);

                // Try to delete the edition fully for all editors
                var newToken = await _editionRepo.DeleteAllEditionDataAsync(editionUser, token);

                // End the request with null for successful delete or a proper token for requests without a confirmation token
                if (string.IsNullOrEmpty(newToken))
                {
                    // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
                    // made the request, that client directly received the response.
                    foreach (var userId in editionUsers)
                        await _hubContext.Clients.GroupExcept($"user-{userId.ToString()}", clientId)
                            .DeletedEdition(deleteResponse);
                    return null;
                }

                deleteResponse.token = newToken;
                return deleteResponse;
            }

            // The edition should only be made inaccessible for the current user
            var userInfo = await _userRepo.GetDetailedUserByIdAsync(editionUser.userId);

            // Setting all permission to false is how we delete a user's access to an edition.
            await _editionRepo.ChangeEditionEditorRights(
                editionUser,
                userInfo.Email,
                false,
                false,
                false,
                false
            );

            // Broadcast edition deletion notification to all connections of this user
            await _hubContext.Clients.GroupExcept($"user-{editionUser.userId.ToString()}", clientId)
                .DeletedEdition(deleteResponse);

            return deleteResponse;
        }

        /// <summary>
        ///     Sends a request to a user to become an editor of the editionUser edition
        ///     with the specified permissions
        /// </summary>
        /// <param name="editionUser">User object making the request</param>
        /// <param name="newEditor">Details of the new editor to be added</param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<NoContentResult> RequestNewEditionEditor(EditionUserInfo editionUser,
            CreateEditorRightsDTO newEditor,
            string clientId = null)
        {
            var requestingUser = await _userRepo.GetDetailedUserByIdAsync(editionUser.userId);
            var newUserToken = await _editionRepo.RequestAddEditionEditor(
                editionUser,
                newEditor.email,
                newEditor.mayRead,
                newEditor.mayWrite,
                newEditor.mayLock,
                newEditor.isAdmin
            );

            var editions = await _editionRepo.ListEditionsAsync(
                editionUser.userId,
                editionUser.EditionId
            ); //get wanted edition by edition Id
            var edition = EditionModelToDTO(editions.First(x => x.EditionId == editionUser.EditionId));

            const string emailBody = @"
<html><body>Dear $User,<br>
<br>
$Admin has invited you to become an editor on $EditionName. To accept the invitation, please click 
<a href=""$WebServer/acceptEditor/token/$Token"">here</a>. If you do not wish to accept this invitation, no action is necessary on your 
part and the offer will expire in $ExpirationPeriod.<br>
<br>
Best wishes,<br>
The Scripta Qumranica Electronica team</body></html>";
            const string emailSubject = "Invitation to edit $EditionName in Scripta Qumranica Electronica";
            var newEditorName = !string.IsNullOrEmpty(newUserToken.Forename)
                                || !string.IsNullOrEmpty(newUserToken.Surname)
                ? (newUserToken.Forename + " " + newUserToken.Surname).Trim()
                : newUserToken.Email;
            var adminName = !string.IsNullOrEmpty(requestingUser.Forename)
                            || !string.IsNullOrEmpty(requestingUser.Surname)
                ? (requestingUser.Forename + " " + requestingUser.Surname).Trim()
                : requestingUser.Email;
            await _emailSender.SendEmailAsync(
                newEditor.email,
                emailSubject.Replace("$EditionName", edition.name),
                emailBody.Replace("$User", newEditorName)
                    .Replace("$Admin", adminName)
                    .Replace("$WebServer", webServer)
                    .Replace("$Token", newUserToken.Token)
                    .Replace("$EditionName", edition.name)
                    .Replace("$ExpirationPeriod", "no current expiration period set")
            );

            // Broadcast the request to the potential editor.
            await _hubContext.Clients.Group($"user-{newUserToken.UserId.ToString()}")
                .RequestedEditor(edition);

            return new NoContentResult();
        }

        /// <summary>
        ///     Adds a new editor to an edition with the requested access rights
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="token">JWT to verify the request</param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<CreateEditorRightsDTO> AddEditionEditor(uint? userId,
            string token,
            string clientId = null)
        {
            if (!userId.HasValue)
                throw new StandardExceptions.NoAuthorizationException();
            var newUserPermissions = await _editionRepo.AddEditionEditor(
                token,
                userId.Value
            );

            var newEditorDTO = _permissionsToEditorRightsDTO(
                newUserPermissions.email,
                newUserPermissions.mayRead,
                newUserPermissions.mayWrite,
                newUserPermissions.mayLock,
                newUserPermissions.isAdmin
            );
            // Broadcast the change to all editors of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            var editionUser = await _userService.GetCurrentUserObjectAsync(newUserPermissions.editionId);
            var editionUsers = await _editionRepo.GetEditionEditorUserIds(editionUser);
            foreach (var editionUserId in editionUsers)
                await _hubContext.Clients.GroupExcept($"user-{editionUserId.ToString()}", clientId)
                    .CreatedEditor(newEditorDTO);
            return newEditorDTO;
        }

        /// <summary>
        ///     Changes the access rights of an editor
        /// </summary>
        /// <param name="editionUser">User object making the request</param>
        /// <param name="editorEmail"></param>
        /// <param name="updatedEditor">Details of the editor and the desired access rights</param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<CreateEditorRightsDTO> ChangeEditionEditorRights(EditionUserInfo editionUser,
            string editorEmail,
            UpdateEditorRightsDTO updatedEditor,
            string clientId = null)
        {
            var updatedUserPermissions = await _editionRepo.ChangeEditionEditorRights(
                editionUser,
                editorEmail,
                updatedEditor.mayRead,
                updatedEditor.mayWrite,
                updatedEditor.mayLock,
                updatedEditor.isAdmin
            );
            var updatedEditorDTO = _permissionsToEditorRightsDTO(
                editorEmail,
                updatedUserPermissions.MayRead,
                updatedUserPermissions.MayWrite,
                updatedUserPermissions.MayLock,
                updatedUserPermissions.IsAdmin
            );
            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            var editionUsers = await _editionRepo.GetEditionEditorUserIds(editionUser);
            foreach (var editionUserId in editionUsers)
                await _hubContext.Clients.GroupExcept($"user-{editionUserId.ToString()}", clientId)
                    .CreatedEditor(updatedEditorDTO);

            return updatedEditorDTO;
        }

        public async Task<EditionScriptCollectionDTO> GetEditionScriptCollection(EditionUserInfo editionUser)
        {
            var letters = await _editionRepo.GetEditionScriptCollection(editionUser);
            var lettersSorted = letters.GroupBy(x => x.Id).ToList();
            var wkbr = new WKBReader();
            var wkr = new WKTReader();
            var wkw = new WKTWriter();
            return new EditionScriptCollectionDTO()
            {
                letters = lettersSorted.Select(
                        x =>
                        {
                            var polys = x.Select(
                                y =>
                                {
                                    //var poly = wkbr.Read(Encoding.ASCII.GetBytes(await GeometryValidation.CleanPolygonAsync(Encoding.ASCII.GetString(y.Polygon), "ROI")));
                                    var poly = wkbr.Read(y.Polygon);
                                    var tr = new AffineTransformation();
                                    var rotation = y.LetterRotation;
                                    tr.Rotate(rotation, poly.Centroid.X, poly.Centroid.Y);
                                    tr.Translate(y.TranslateX, y.TranslateY);
                                    poly = tr.Transform(poly);
                                    return poly;
                                }).Where(z => z.IsValid && !z.IsEmpty).ToList();
                            var cpu = new CascadedPolygonUnion(polys);
                            var combinedPoly = cpu.Union();
                            var envelope = polys.Any() ? combinedPoly.EnvelopeInternal : new Envelope(0, 0, 0, 0);
                            if (polys.Any())
                            {
                                // var tr = new AffineTransformation();
                                // tr.Rotate(Degrees.ToRadians(x.First().ImageRotation));
                                // var rotatedPoly = tr.Transform(combinedPoly);
                                // var envelope1 = rotatedPoly.EnvelopeInternal;

                                var tr = new AffineTransformation();
                                tr.Translate(-envelope.MinX, -envelope.MinY);
                                var translatedPoly = tr.Transform(combinedPoly);

                                var envelope2 = translatedPoly.EnvelopeInternal;

                                combinedPoly = translatedPoly;
                            }

                            return new LetterDTO()
                            {
                                id = x.First().Id,
                                letter = x.First().Letter,
                                rotation = x.First().ImageRotation,
                                imageURL = x.First().ImageURL
                                           + $"/{envelope.MinX},{envelope.MinY},{envelope.Width},{envelope.Height}/pct:99/0/"
                                           + x.First().ImageSuffix,
                                polygon = polys.Any() ? wkw.Write(combinedPoly) : null
                            };
                        }
                    ).ToList()
            };
        }

        private static EditionDTO EditionModelToDTO(Edition model)
        {
            return new EditionDTO
            {
                id = model.EditionId,
                name = model.Name,
                editionDataEditorId = model.EditionDataEditorId,
                permission = PermissionModelToDTO(model.Permission),
                owner = UserService.UserModelToDto(model.Owner),
                thumbnailUrl = model.Thumbnail,
                locked = model.Locked,
                isPublic = model.IsPublic,
                lastEdit = model.LastEdit,
                copyright = model.Copyright
            };
        }

        private static PermissionDTO PermissionModelToDTO(Permission model)
        {
            return new PermissionDTO
            {
                isAdmin = model.IsAdmin,
                mayWrite = model.MayWrite
            };
        }

        internal static UserToken OwnerToModel(UserDTO user)
        {
            return new UserToken
            {
                UserId = user.userId,
                Email = user.email
            };
        }

        internal static Permission PermissionDtoTOModel(PermissionDTO permission)
        {
            return new Permission
            {
                IsAdmin = permission.isAdmin,
                MayWrite = permission.mayWrite
            };
        }

        private static CreateEditorRightsDTO _permissionsToEditorRightsDTO(string editorEmail,
            bool mayRead,
            bool mayWrite,
            bool mayLock,
            bool isAdmin)
        {
            return new CreateEditorRightsDTO
            {
                email = editorEmail,
                mayRead = mayRead,
                mayWrite = mayWrite,
                mayLock = mayLock,
                isAdmin = isAdmin
            };
        }

        private static void _parseOptional(List<string> optional, out bool deleteForAllEditors)
        {
            deleteForAllEditors = optional.Contains("deleteForAllEditors");
        }
    }
}