using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SQE.SqeApi.Server.DTOs;
using SQE.SqeApi.Server.Services;

namespace SQE.SqeApi.Server.Hubs
{
    public class MainHub : Hub
    {
        private readonly IArtefactService _artefactService;
        private readonly IUserService _userService;
        private readonly IEditionService _editionService;
        private readonly IImagedObjectService _imagedObjectService;
        private readonly IImageService _imageService;
        private readonly ITextService _textService;

        public MainHub(IArtefactService artefactService, IUserService userService, IEditionService editionService,
            IImagedObjectService imagedObjectService, IImageService imageService, ITextService textService)
        {
            _artefactService = artefactService;
            _userService = userService;
            _editionService = editionService;
            _imagedObjectService = imagedObjectService;
            _imageService = imageService;
            _textService = textService;
        }

        #region artefact

        /// <summary>
        /// Creates a new artefact with the provided data.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">A CreateArtefactDTO with the data for the new artefact</param>
        [Authorize]
        public async Task<ArtefactDTO> PostV1EditionsEditionIdArtefacts(uint editionId, CreateArtefactDTO payload)
        {
            return await _artefactService.CreateArtefactAsync(
                _userService.GetCurrentUserObject(editionId),
                editionId,
                payload.masterImageId,
                payload.mask,
                payload.name,
                payload.position);
        }


        /// <summary>
        /// Deletes the specified artefact
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [Authorize]
        public async Task DeleteV1EditionsEditionIdArtefactsArtefactId(uint artefactId, uint editionId)
        {
            await _artefactService.DeleteArtefactAsync(
                _userService.GetCurrentUserObject(editionId),
                artefactId);
        }


        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        public async Task<ArtefactDTO> GetV1EditionsEditionIdArtefactsArtefactId(uint artefactId, uint editionId,
            List<string> optional)
        {
            return await _artefactService.GetEditionArtefactAsync(
                _userService.GetCurrentUserObject(editionId),
                artefactId,
                optional);
        }


        /// <summary>
        /// Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
        [AllowAnonymous]
        public async Task<ArtefactListDTO> GetV1EditionsEditionIdArtefacts(uint editionId, List<string> optional)
        {
            return await _artefactService.GetEditionArtefactListingsAsync(
                _userService.GetCurrentUserId(),
                editionId,
                optional);
        }


        /// <summary>
        /// Updates the specified artefact
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">An UpdateArtefactDTO with the desired alterations to the artefact</param>
        [Authorize]
        public async Task<ArtefactDTO> PutV1EditionsEditionIdArtefactsArtefactId(uint artefactId, uint editionId,
            UpdateArtefactDTO payload)
        {
            return await _artefactService.UpdateArtefactAsync(
                _userService.GetCurrentUserObject(editionId),
                editionId,
                artefactId,
                payload.mask,
                payload.name,
                payload.position);
        }

        #endregion artefact

        #region edition

        /// <summary>
        /// Adds an editor to the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        [Authorize]
        public async Task<EditorRightsDTO> PostV1EditionsEditionIdEditors(uint editionId, EditorRightsDTO payload)
        {
            return await _editionService.AddEditionEditor(
                _userService.GetCurrentUserObject(editionId),
                payload);
        }


        /// <summary>
        /// Changes the rights for an editor of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
        [Authorize]
        public async Task<EditorRightsDTO> PutV1EditionsEditionIdEditors(uint editionId, EditorRightsDTO payload)
        {
            return await _editionService.ChangeEditionEditorRights(
                _userService.GetCurrentUserObject(editionId),
                payload);
        }


        /// <summary>
        /// Creates a copy of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
        [Authorize]
        public async Task<EditionDTO> PostV1EditionsEditionId(uint editionId, EditionCopyDTO request)
        {
            return await _editionService.CopyEditionAsync(
                _userService.GetCurrentUserObject(editionId),
                request);
        }


        /// <summary>
        /// Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Optional parameters: 'deleteForAllEditors'</param>
        /// <param name="token">token required when using optional 'deleteForAllEditors'</param>
        [Authorize]
        public async Task<DeleteTokenDTO> DeleteV1EditionsEditionId(uint editionId, List<string> optional, string token)
        {
            return await _editionService.DeleteEditionAsync(
                _userService.GetCurrentUserObject(editionId),
                token,
                optional);
        }


        /// <summary>
        /// Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        [AllowAnonymous]
        public async Task<EditionGroupDTO> GetV1EditionsEditionId(uint editionId)
        {
            return await _editionService.GetEditionAsync(_userService.GetCurrentUserObject(editionId));
        }


        /// <summary>
        /// Provides a listing of all editions accessible to the current user
        /// </summary>
        [AllowAnonymous]
        public async Task<EditionListDTO> GetV1Editions()
        {
            return await _editionService.ListEditionsAsync(_userService.GetCurrentUserId());
        }


        /// <summary>
        /// Updates data for the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be updated</param>
        [Authorize]
        public async Task<EditionDTO> PutV1EditionsEditionId(uint editionId, EditionUpdateRequestDTO request)
        {
            return await _editionService.UpdateEditionAsync(
                _userService.GetCurrentUserObject(editionId),
                request.name,
                request.copyrightHolder,
                request.collaborators);
        }

        #endregion edition

        #region imagedObject

        /// <summary>
        /// Provides information for the specified imaged object related to the specified edition, can include images and also their masks with optional.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
        /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
        [AllowAnonymous]
        public async Task<ImagedObjectDTO> GetV1EditionsEditionIdImagedObjectsImagedObjectId(uint editionId,
            string imagedObjectId, List<string> optional)
        {
            return await _imagedObjectService.GetImagedObjectAsync(
                _userService.GetCurrentUserId(),
                editionId,
                imagedObjectId,
                optional);
        }


        /// <summary>
        /// Provides a listing of imaged objects related to the specified edition, can include images and also their masks with optional.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
        [AllowAnonymous]
        public async Task<ImagedObjectListDTO> GetV1EditionsEditionIdImagedObjects(uint editionId,
            List<string> optional)
        {
            return await _imagedObjectService.GetImagedObjectsAsync(
                _userService.GetCurrentUserId(),
                editionId,
                optional);
        }


        /// <summary>
        /// Provides a list of all institutional image providers.
        /// </summary>
        [AllowAnonymous]
        public async Task<ImageInstitutionListDTO> GetV1ImagedObjectsInstitutions()
        {
            return await _imageService.GetImageInstitutionsAsync();
        }

        #endregion imagedObject

        #region text

        /// <summary>
        /// Creates a new text fragment in the given edition of a scroll
        /// </summary>
        /// <param name="createFragment">A JSON object with the details of the new text fragment to be created</param>
        /// <param name="editionId">Id of the edition</param>
        [Authorize]
        public async Task<TextFragmentDataDTO> PostV1EditionsEditionIdTextFragments(
            CreateTextFragmentDTO createFragment, uint editionId)
        {
            return await _textService.CreateTextFragmentAsync(
                _userService.GetCurrentUserObject(editionId),
                createFragment);
        }


        /// <summary>
        /// Retrieves the ids of all fragments in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <returns>An array of the text fregment ids in correct sequence</returns>
        [AllowAnonymous]
        public async Task<TextFragmentDataListDTO> GetV1EditionsEditionIdTextFragments(uint editionId)
        {
            return await _textService.GetFragmentDataAsync(_userService.GetCurrentUserObject(editionId));
        }


        /// <summary>
        /// Retrieves the ids of all lines in the given textFragmentName
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>An array of the line ids in the proper sequence</returns>
        [AllowAnonymous]
        public async Task<LineDataListDTO> GetV1EditionsEditionIdTextFragmentsTextFragmentIdLines(uint editionId,
            uint textFragmentId)
        {
            return await _textService.GetLineIdsAsync(
                _userService.GetCurrentUserObject(editionId),
                textFragmentId);
        }


        /// <summary>
        /// Retrieves all signs and their data from the given textFragmentName
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>A manuscript edition object including the fragments and their lines in a hierarchical order and in correct sequence</returns>
        [Authorize]
        public async Task<TextEditionDTO> GetV1EditionsEditionIdTextFragmentsTextFragmentId(uint editionId,
            uint textFragmentId)
        {
            return await _textService.GetFragmentByIdAsync(
                _userService.GetCurrentUserObject(editionId),
                textFragmentId);
        }


        /// <summary>
        /// Retrieves all signs and their data from the given line
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="lineId">Id of the line</param>
        /// <returns>A manuscript edition object including the fragments and their lines in a hierarchical order and in correct sequence</returns>
        [AllowAnonymous]
        public async Task<LineTextDTO> GetV1EditionsEditionIdLinesLineId(uint editionId, uint lineId)
        {
            return await _textService.GetLineByIdAsync(
                _userService.GetCurrentUserObject(editionId),
                lineId);
        }

        #endregion text

        #region user

        /// <summary>
        /// Provides a JWT bearer token for valid email and password
        /// </summary>
        /// <param name="payload">JSON object with an email and password parameter</param>
        /// <returns>A DetailedUserTokenDTO with a JWT for activated user accounts, or the email address of an unactivated user account</returns>
        [AllowAnonymous]
        public async Task<DetailedUserTokenDTO> PostV1UsersLogin(LoginRequestDTO payload)
        {
            return await _userService.AuthenticateAsync(payload.email, payload.password);
        }


        /// <summary>
        /// Allows a user who has not yet activated their account to change their email address. This will not work if the user account associated with the email address has already been activated
        /// </summary>
        /// <param name="payload">JSON object with the current email address and the new desired email address</param>
        [AllowAnonymous]
        public async Task PostV1UsersChangeUnactivatedEmail(UnactivatedEmailUpdateRequestDTO payload)
        {
            await _userService.UpdateUnactivatedAccountEmailAsync(
                payload.email,
                payload.newEmail);
        }


        /// <summary>
        /// Uses the secret token from /users/forgot-password to validate a reset of the user's password
        /// </summary>
        /// <param name="payload">A JSON object with the secret token and the new password</param>
        [AllowAnonymous]
        public async Task PostV1UsersChangeForgottenPassword(ResetForgottenUserPasswordRequestDto payload)
        {
            await _userService.ResetLostPasswordAsync(
                payload.token,
                payload.password);
        }


        /// <summary>
        /// Changes the password for the currently logged in user
        /// </summary>
        /// <param name="payload">A JSON object with the old password and the new password</param>
        [Authorize]
        public async Task PostV1UsersChangePassword(ResetLoggedInUserPasswordRequestDTO payload)
        {
            await _userService.ChangePasswordAsync(
                _userService.GetCurrentUserObject(),
                payload.oldPassword,
                payload.newPassword);
        }


        /// <summary>
        /// Updates a user's registration details.  Note that the if the email address has changed, the account will be set to inactive until the account is activated with the secret token.
        /// </summary>
        /// <param name="payload">A JSON object with all data necessary to update a user account.  Null fields (but not empty strings!) will be populated with existing user data</param>
        /// <returns>Returns a DetailedUserDTO with the updated user account details</returns>
        [Authorize]
        public async Task<DetailedUserDTO> PutV1Users(UserUpdateRequestDTO payload)
        {
            return await _userService.UpdateUserAsync(
                _userService.GetCurrentUserObject(),
                payload);
        }


        /// <summary>
        /// Confirms registration of new user account.
        /// </summary>
        /// <param name="payload">JSON object with token from user registration email</param>
        /// <returns>Returns a DetailedUserDTO for the confirmed account</returns>
        [AllowAnonymous]
        public async Task<DetailedUserDTO> PostV1UsersConfirmRegistration(AccountActivationRequestDTO payload)
        {
            return await _userService.ConfirmUserRegistrationAsync(payload.token);
        }


        /// <summary>
        /// Creates a new user with the submitted data.
        /// </summary>
        /// <param name="payload">A JSON object with all data necessary to create a new user account</param>
        /// <returns>Returns a UserDTO for the newly created account</returns>
        [AllowAnonymous]
        public async Task<UserDTO> PostV1Users(NewUserRequestDTO payload)
        {
            return await _userService.CreateNewUserAsync(payload);
        }


        /// <summary>
        /// Provides the user details for a user with valid JWT in the Authorize header
        /// </summary>
        /// <returns>A DetailedUserDTO for user account.</returns>
        [Authorize]
        public async Task<DetailedUserDTO> GetV1Users()
        {
            return await _userService.GetCurrentUser();
        }


        /// <summary>
        /// Sends a secret token to the user's email to allow password reset.
        /// </summary>
        /// <param name="payload">JSON object with the email address for the user who wants to reset a lost password</param>
        [AllowAnonymous]
        public async Task PostV1UsersForgotPassword(ResetUserPasswordRequestDTO payload)
        {
            await _userService.RequestResetLostPasswordAsync(payload.email);
        }


        /// <summary>
        /// Sends a new activation email for the user's account. This will not work if the user account associated with the email address has already been activated.
        /// </summary>
        /// <param name="payload">JSON object with the current email address and the new desired email address</param>
        [AllowAnonymous]
        public async Task PostV1UsersResendActivationEmail(ResendUserAccountActivationRequestDTO payload)
        {
            await _userService.ResendActivationEmail(payload.email);
        }

        #endregion user


//        /// <summary>
//        /// This is used to authorize the client.  The bearer token stays with the client for the life of the connection.
//        /// I believe it is even remains over a reconnect (probably the load balancer needs sticky sessions for that
//        /// to work).
//        /// </summary>
//        /// <param name="payload">Stringified JSON with credentials: {email: string, password: string}</param>
//        /// <returns></returns>
//        public async Task Auth(string payload)
//        {
//            var user = await _userService.AuthenticateAsync(payload.email, payload.password);
//
//            // Call the broadcastMessage method to update clients.
//            return Clients.Caller.SendAsync("returnedRequest", response.status.ToString(), "auth", user);
//        }
    }
}