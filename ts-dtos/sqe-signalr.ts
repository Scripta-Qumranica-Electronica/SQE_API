/*
 * Do not edit this file directly!
 * This SignalRSQE class is autogenerated by the `GenerateTypescriptInterfaces` 
 * in the project https://github.com/Scripta-Qumranica-Electronica/SQE_API.
 * Changes made there are used to automatically create this file at {ROOT}/ts-dtos
 * whenever the GenerateTypescriptInterfaces program is run.
 */

/* tslint:disable */

import { 
	ArtefactDataDTO,
	ArtefactDTO,
	ArtefactListDTO,
	ArtefactDataListDTO,
	UpdateArtefactDTO,
	CreateArtefactDTO,
	EditionDTO,
	EditionGroupDTO,
	EditionListDTO,
	PermissionDTO,
	MinimalEditorRights,
	UpdateEditorRightsDTO,
	InviteEditorDTO,
	DetailedEditorRightsDTO,
	DetailedUpdateEditorRightsDTO,
	AdminEditorRequestDTO,
	EditorInvitationDTO,
	EditorInvitationListDTO,
	AdminEditorRequestListDTO,
	TextEditionDTO,
	DeleteTokenDTO,
	DeleteEditionEntityDTO,
	EditionScriptCollectionDTO,
	DeleteDTO,
	EditionUpdateRequestDTO,
	EditionCopyDTO,
	ImageDTO,
	ImageInstitutionDTO,
	ImageInstitutionListDTO,
	ImageStackDTO,
	ImagedObjectDTO,
	ImagedObjectListDTO,
	PolygonDTO,
	WktPolygonDTO,
	SetInterpretationRoiDTO,
	InterpretationRoiDTO,
	UpdatedInterpretationRoiDTO,
	SetInterpretationRoiDTOList,
	InterpretationRoiDTOList,
	UpdatedInterpretationRoiDTOList,
	BatchEditRoiDTO,
	BatchEditRoiResponseDTO,
	LetterDTO,
	SignDTO,
	NextSignInterpretationDTO,
	SignInterpretationDTO,
	InterpretationAttributeDTO,
	TextFragmentDataDTO,
	ArtefactTextFragmentMatchDTO,
	ImagedObjectTextFragmentMatchDTO,
	TextFragmentDataListDTO,
	ArtefactTextFragmentMatchListDTO,
	TextFragmentDTO,
	LineDataDTO,
	LineDataListDTO,
	LineDTO,
	LineTextDTO,
	UpdateTextFragmentDTO,
	CreateTextFragmentDTO,
	TransformationDTO,
	TranslateDTO,
	LoginRequestDTO,
	UserUpdateRequestDTO,
	NewUserRequestDTO,
	AccountActivationRequestDTO,
	ResendUserAccountActivationRequestDTO,
	UnactivatedEmailUpdateRequestDTO,
	ResetUserPasswordRequestDTO,
	ResetForgottenUserPasswordRequestDTO,
	ResetLoggedInUserPasswordRequestDTO,
	UserDTO,
	DetailedUserDTO,
	DetailedUserTokenDTO,
	EditorDTO,
	ArtefactSide,
} from "@/dtos/sqe-dtos"

import { HubConnection } from '@microsoft/signalr'; 

export class SignalRUtilities {  
    private _connection: HubConnection;
    
    public constructor(connection: HubConnection) {
        this._connection = connection;
    }

    /*
     * Server methods.
     */

    /**
	 * Get the details for a ROI in the given edition of a scroll
	 *
	 * @param editionId - Id of the edition
	 * @param roiId - A JSON object with the new ROI to be created
	 *
	 */
    public async getV1EditionsEditionIdRoisRoiId(editionId: number, roiId: number): Promise<InterpretationRoiDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdRoisRoiId', editionId, roiId);
    }

    /**
	 * Creates new sign ROI in the given edition of a scroll
	 *
	 * @param editionId - Id of the edition
	 * @param newRoi - A JSON object with the new ROI to be created
	 *
	 */
    public async postV1EditionsEditionIdRois(editionId: number, newRoi: SetInterpretationRoiDTO): Promise<InterpretationRoiDTO> {
        return await this._connection.invoke('PostV1EditionsEditionIdRois', editionId, newRoi);
    }

    /**
	 * Creates new sign ROI's in the given edition of a scroll
	 *
	 * @param editionId - Id of the edition
	 * @param newRois - A JSON object with an array of the new ROI's to be created
	 *
	 */
    public async postV1EditionsEditionIdRoisBatch(editionId: number, newRois: SetInterpretationRoiDTOList): Promise<InterpretationRoiDTOList> {
        return await this._connection.invoke('PostV1EditionsEditionIdRoisBatch', editionId, newRois);
    }

    /**
	 * Processes a series of create/update/delete ROI requests in the given edition of a scroll
	 *
	 * @param editionId - Id of the edition
	 * @param rois - A JSON object with all the roi edits to be performed
	 *
	 */
    public async postV1EditionsEditionIdRoisBatchEdit(editionId: number, rois: BatchEditRoiDTO): Promise<BatchEditRoiResponseDTO> {
        return await this._connection.invoke('PostV1EditionsEditionIdRoisBatchEdit', editionId, rois);
    }

    /**
	 * Update an existing sign ROI in the given edition of a scroll
	 *
	 * @param editionId - Id of the edition
	 * @param roiId - Id of the ROI to be updated
	 * @param updateRoi - A JSON object with the updated ROI details
	 *
	 */
    public async putV1EditionsEditionIdRoisRoiId(editionId: number, roiId: number, updateRoi: SetInterpretationRoiDTO): Promise<UpdatedInterpretationRoiDTO> {
        return await this._connection.invoke('PutV1EditionsEditionIdRoisRoiId', editionId, roiId, updateRoi);
    }

    /**
	 * Update existing sign ROI's in the given edition of a scroll
	 *
	 * @param editionId - Id of the edition
	 * @param updateRois - A JSON object with an array of the updated ROI details
	 *
	 */
    public async putV1EditionsEditionIdRoisBatch(editionId: number, updateRois: InterpretationRoiDTOList): Promise<UpdatedInterpretationRoiDTOList> {
        return await this._connection.invoke('PutV1EditionsEditionIdRoisBatch', editionId, updateRois);
    }

    /**
	 * Deletes a sign ROI from the given edition of a scroll
	 *
	 * @param roiId - Id of the ROI to be deleted
	 * @param editionId - Id of the edition
	 *
	 */
    public async deleteV1EditionsEditionIdRoisRoiId(editionId: number, roiId: number): Promise<void> {
        return await this._connection.invoke('DeleteV1EditionsEditionIdRoisRoiId', editionId, roiId);
    }

    /**
	 * Checks a WKT polygon to ensure validity. If the polygon is invalid,
	 * it attempts to construct a valid polygon that matches the original
	 * as closely as possible.
	 *
	 * @param payload - JSON object with the WKT polygon to validate
	 *
	 */
    public async postV1UtilsRepairWktPolygon(payload: WktPolygonDTO): Promise<WktPolygonDTO> {
        return await this._connection.invoke('PostV1UtilsRepairWktPolygon', payload);
    }

    /**
	 * Override the default OnConnectedAsync to add the connection to the user's user_id
	 * group if the user is authenticated. The user_id group is used for messages that
	 * are above the level of a single edition.
	 *
	 *
	 *
	 */
    public async onConnectedAsync(): Promise<void> {
        return await this._connection.invoke('OnConnectedAsync');
    }

    /**
	 * The client subscribes to all changes for the specified editionId.
	 *
	 * @param editionId - The ID of the edition to receive updates
	 *
	 */
    public async subscribeToEdition(editionId: number): Promise<void> {
        return await this._connection.invoke('SubscribeToEdition', editionId);
    }

    /**
	 * The client unsubscribes to all changes for the specified editionId.
	 *
	 * @param editionId - The ID of the edition to stop receiving updates
	 *
	 */
    public async unsubscribeToEdition(editionId: number): Promise<void> {
        return await this._connection.invoke('UnsubscribeToEdition', editionId);
    }

    /**
	 * Get a list of all editions the client is currently subscribed to.
	 *
	 *
	 * @returns - A list of every editionId for which the client receives update
	 */
    public async listEditionSubscriptions(): Promise<number[]> {
        return await this._connection.invoke('ListEditionSubscriptions');
    }

    /**
	 * Adds an editor to the specified edition
	 *
	 * @param editionId - Unique Id of the desired edition
	 * @param payload - JSON object with the attributes of the new editor
	 *
	 */
    public async postV1EditionsEditionIdAddEditorRequest(editionId: number, payload: InviteEditorDTO): Promise<void> {
        return await this._connection.invoke('PostV1EditionsEditionIdAddEditorRequest', editionId, payload);
    }

    /**
	 * Get a list of requests issued by the current user for other users
	 * to become editors of a shared edition
	 *
	 *
	 *
	 */
    public async getV1EditionsAdminShareRequests(): Promise<AdminEditorRequestListDTO> {
        return await this._connection.invoke('GetV1EditionsAdminShareRequests');
    }

    /**
	 * Get a list of invitations issued to the current user to become an editor of a shared edition
	 *
	 *
	 *
	 */
    public async getV1EditionsEditorInvitations(): Promise<EditorInvitationListDTO> {
        return await this._connection.invoke('GetV1EditionsEditorInvitations');
    }

    /**
	 * Confirm addition of an editor to the specified edition
	 *
	 * @param token - JWT for verifying the request confirmation
	 *
	 */
    public async postV1EditionsConfirmEditorshipToken(token: string): Promise<DetailedEditorRightsDTO> {
        return await this._connection.invoke('PostV1EditionsConfirmEditorshipToken', token);
    }

    /**
	 * Changes the rights for an editor of the specified edition
	 *
	 * @param editionId - Unique Id of the desired edition
	 * @param editorEmailId - Email address of the editor whose permissions are being changed
	 * @param payload - JSON object with the attributes of the new editor
	 *
	 */
    public async putV1EditionsEditionIdEditorsEditorEmailId(editionId: number, editorEmailId: string, payload: UpdateEditorRightsDTO): Promise<DetailedEditorRightsDTO> {
        return await this._connection.invoke('PutV1EditionsEditionIdEditorsEditorEmailId', editionId, editorEmailId, payload);
    }

    /**
	 * Creates a copy of the specified edition
	 *
	 * @param editionId - Unique Id of the desired edition
	 * @param request - JSON object with the attributes to be changed in the copied edition
	 *
	 */
    public async postV1EditionsEditionId(editionId: number, request: EditionCopyDTO): Promise<EditionDTO> {
        return await this._connection.invoke('PostV1EditionsEditionId', editionId, request);
    }

    /**
	 * Provides details about the specified edition and all accessible alternate editions
	 *
	 * @param editionId - Unique Id of the desired edition
	 * @param optional - Optional parameters: 'deleteForAllEditors'
	 * @param token - token required when using optional 'deleteForAllEditors'
	 *
	 */
    public async deleteV1EditionsEditionId(editionId: number, optional: string[], token: string): Promise<DeleteTokenDTO> {
        return await this._connection.invoke('DeleteV1EditionsEditionId', editionId, optional, token);
    }

    /**
	 * Provides details about the specified edition and all accessible alternate editions
	 *
	 * @param editionId - Unique Id of the desired edition
	 *
	 */
    public async getV1EditionsEditionId(editionId: number): Promise<EditionGroupDTO> {
        return await this._connection.invoke('GetV1EditionsEditionId', editionId);
    }

    /**
	 * Provides a listing of all editions accessible to the current user
	 *
	 *
	 *
	 */
    public async getV1Editions(): Promise<EditionListDTO> {
        return await this._connection.invoke('GetV1Editions');
    }

    /**
	 * Updates data for the specified edition
	 *
	 * @param editionId - Unique Id of the desired edition
	 * @param request - JSON object with the attributes to be updated
	 *
	 */
    public async putV1EditionsEditionId(editionId: number, request: EditionUpdateRequestDTO): Promise<EditionDTO> {
        return await this._connection.invoke('PutV1EditionsEditionId', editionId, request);
    }

    /**
	 * Provides spatial data for all letters in the edition
	 *
	 * @param editionId - Unique Id of the desired edition
	 *
	 */
    public async getV1EditionsEditionIdScriptCollection(editionId: number): Promise<EditionScriptCollectionDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdScriptCollection', editionId);
    }

    /**
	 * Creates a new text fragment in the given edition of a scroll
	 *
	 * @param createFragment - A JSON object with the details of the new text fragment to be created
	 * @param editionId - Id of the edition
	 *
	 */
    public async postV1EditionsEditionIdTextFragments(editionId: number, createFragment: CreateTextFragmentDTO): Promise<TextFragmentDataDTO> {
        return await this._connection.invoke('PostV1EditionsEditionIdTextFragments', editionId, createFragment);
    }

    /**
	 * Updates the specified text fragment with the submitted properties
	 *
	 * @param editionId - Edition of the text fragment being updates
	 * @param textFragmentId - Id of the text fragment being updates
	 * @param updatedTextFragment - Details of the updated text fragment
	 * @returns - The details of the updated text fragment
	 */
    public async putV1EditionsEditionIdTextFragmentsTextFragmentId(editionId: number, textFragmentId: number, updatedTextFragment: UpdateTextFragmentDTO): Promise<TextFragmentDataDTO> {
        return await this._connection.invoke('PutV1EditionsEditionIdTextFragmentsTextFragmentId', editionId, textFragmentId, updatedTextFragment);
    }

    /**
	 * Retrieves the ids of all Fragments of all fragments in the given edition of a scroll
	 *
	 * @param editionId - Id of the edition
	 * @returns - An array of the text fragment ids in correct sequence
	 */
    public async getV1EditionsEditionIdTextFragments(editionId: number): Promise<TextFragmentDataListDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdTextFragments', editionId);
    }

    /**
	 * Retrieves the ids of all Artefacts in the given textFragmentName
	 *
	 * @param editionId - Id of the edition
	 * @param textFragmentId - Id of the text fragment
	 * @returns - An array of the line ids in the proper sequence
	 */
    public async getV1EditionsEditionIdTextFragmentsTextFragmentIdArtefacts(editionId: number, textFragmentId: number): Promise<ArtefactDataListDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdTextFragmentsTextFragmentIdArtefacts', editionId, textFragmentId);
    }

    /**
	 * Retrieves the ids of all lines in the given textFragmentName
	 *
	 * @param editionId - Id of the edition
	 * @param textFragmentId - Id of the text fragment
	 * @returns - An array of the line ids in the proper sequence
	 */
    public async getV1EditionsEditionIdTextFragmentsTextFragmentIdLines(editionId: number, textFragmentId: number): Promise<LineDataListDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdTextFragmentsTextFragmentIdLines', editionId, textFragmentId);
    }

    /**
	 * Retrieves all signs and their data from the given textFragmentName
	 *
	 * @param editionId - Id of the edition
	 * @param textFragmentId - Id of the text fragment
	 * @returns - 
	 *             A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
	 *             sequence
	 *         
	 */
    public async getV1EditionsEditionIdTextFragmentsTextFragmentId(editionId: number, textFragmentId: number): Promise<TextEditionDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdTextFragmentsTextFragmentId', editionId, textFragmentId);
    }

    /**
	 * Retrieves all signs and their data from the given line
	 *
	 * @param editionId - Id of the edition
	 * @param lineId - Id of the line
	 * @returns - 
	 *             A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
	 *             sequence
	 *         
	 */
    public async getV1EditionsEditionIdLinesLineId(editionId: number, lineId: number): Promise<LineTextDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdLinesLineId', editionId, lineId);
    }

    /**
	 * Provides a JWT bearer token for valid email and password
	 *
	 * @param payload - JSON object with an email and password parameter
	 * @returns - 
	 *             A DetailedUserTokenDTO with a JWT for activated user accounts, or the email address of an unactivated user
	 *             account
	 *         
	 */
    public async postV1UsersLogin(payload: LoginRequestDTO): Promise<DetailedUserTokenDTO> {
        return await this._connection.invoke('PostV1UsersLogin', payload);
    }

    /**
	 * Allows a user who has not yet activated their account to change their email address. This will not work if the user
	 * account associated with the email address has already been activated
	 *
	 * @param payload - JSON object with the current email address and the new desired email address
	 *
	 */
    public async postV1UsersChangeUnactivatedEmail(payload: UnactivatedEmailUpdateRequestDTO): Promise<void> {
        return await this._connection.invoke('PostV1UsersChangeUnactivatedEmail', payload);
    }

    /**
	 * Uses the secret token from /users/forgot-password to validate a reset of the user's password
	 *
	 * @param payload - A JSON object with the secret token and the new password
	 *
	 */
    public async postV1UsersChangeForgottenPassword(payload: ResetForgottenUserPasswordRequestDTO): Promise<void> {
        return await this._connection.invoke('PostV1UsersChangeForgottenPassword', payload);
    }

    /**
	 * Changes the password for the currently logged in user
	 *
	 * @param payload - A JSON object with the old password and the new password
	 *
	 */
    public async postV1UsersChangePassword(payload: ResetLoggedInUserPasswordRequestDTO): Promise<void> {
        return await this._connection.invoke('PostV1UsersChangePassword', payload);
    }

    /**
	 * Updates a user's registration details. Note that the if the email address has changed, the account will be set to
	 * inactive until the account is activated with the secret token.
	 *
	 * @param payload - 
	 *             A JSON object with all data necessary to update a user account.  Null fields (but not empty
	 *             strings!) will be populated with existing user data
	 *         
	 * @returns - Returns a DetailedUserDTO with the updated user account details
	 */
    public async putV1Users(payload: UserUpdateRequestDTO): Promise<DetailedUserDTO> {
        return await this._connection.invoke('PutV1Users', payload);
    }

    /**
	 * Confirms registration of new user account.
	 *
	 * @param payload - JSON object with token from user registration email
	 * @returns - Returns a DetailedUserDTO for the confirmed account
	 */
    public async postV1UsersConfirmRegistration(payload: AccountActivationRequestDTO): Promise<void> {
        return await this._connection.invoke('PostV1UsersConfirmRegistration', payload);
    }

    /**
	 * Sends a secret token to the user's email to allow password reset.
	 *
	 * @param payload - JSON object with the email address for the user who wants to reset a lost password
	 *
	 */
    public async postV1UsersForgotPassword(payload: ResetUserPasswordRequestDTO): Promise<void> {
        return await this._connection.invoke('PostV1UsersForgotPassword', payload);
    }

    /**
	 * Provides the user details for a user with valid JWT in the Authorize header
	 *
	 *
	 * @returns - A UserDTO for user account.
	 */
    public async getV1Users(): Promise<UserDTO> {
        return await this._connection.invoke('GetV1Users');
    }

    /**
	 * Creates a new user with the submitted data.
	 *
	 * @param payload - A JSON object with all data necessary to create a new user account
	 * @returns - Returns a UserDTO for the newly created account
	 */
    public async postV1Users(payload: NewUserRequestDTO): Promise<UserDTO> {
        return await this._connection.invoke('PostV1Users', payload);
    }

    /**
	 * Sends a new activation email for the user's account. This will not work if the user account associated with the
	 * email address has already been activated.
	 *
	 * @param payload - JSON object with the current email address and the new desired email address
	 *
	 */
    public async postV1UsersResendActivationEmail(payload: ResendUserAccountActivationRequestDTO): Promise<void> {
        return await this._connection.invoke('PostV1UsersResendActivationEmail', payload);
    }

    /**
	 * Provides information for the specified imaged object related to the specified edition, can include images and also
	 * their masks with optional.
	 *
	 * @param editionId - Unique Id of the desired edition
	 * @param imagedObjectId - Unique Id of the desired object from the imaging Institution
	 * @param optional - Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks
	 *
	 */
    public async getV1EditionsEditionIdImagedObjectsImagedObjectId(editionId: number, imagedObjectId: string, optional: string[]): Promise<ImagedObjectDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdImagedObjectsImagedObjectId', editionId, imagedObjectId, optional);
    }

    /**
	 * Provides a listing of imaged objects related to the specified edition, can include images and also their masks with
	 * optional.
	 *
	 * @param editionId - Unique Id of the desired edition
	 * @param optional - Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks
	 *
	 */
    public async getV1EditionsEditionIdImagedObjects(editionId: number, optional: string[]): Promise<ImagedObjectListDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdImagedObjects', editionId, optional);
    }

    /**
	 * Provides a list of all institutional image providers.
	 *
	 *
	 *
	 */
    public async getV1ImagedObjectsInstitutions(): Promise<ImageInstitutionListDTO> {
        return await this._connection.invoke('GetV1ImagedObjectsInstitutions');
    }

    /**
	 * Provides a list of all text fragments that should correspond to the imaged object.
	 *
	 * @param imagedObjectId - Id of the imaged object
	 *
	 */
    public async getV1ImagedObjectsImagedObjectIdTextFragments(imagedObjectId: string): Promise<ImagedObjectTextFragmentMatchDTO[]> {
        return await this._connection.invoke('GetV1ImagedObjectsImagedObjectIdTextFragments', imagedObjectId);
    }

    /**
	 * Creates a new artefact with the provided data.
	 *
	 * @param editionId - Unique Id of the desired edition
	 * @param payload - A CreateArtefactDTO with the data for the new artefact
	 *
	 */
    public async postV1EditionsEditionIdArtefacts(editionId: number, payload: CreateArtefactDTO): Promise<ArtefactDTO> {
        return await this._connection.invoke('PostV1EditionsEditionIdArtefacts', editionId, payload);
    }

    /**
	 * Deletes the specified artefact
	 *
	 * @param artefactId - Unique Id of the desired artefact
	 * @param editionId - Unique Id of the desired edition
	 *
	 */
    public async deleteV1EditionsEditionIdArtefactsArtefactId(editionId: number, artefactId: number): Promise<void> {
        return await this._connection.invoke('DeleteV1EditionsEditionIdArtefactsArtefactId', editionId, artefactId);
    }

    /**
	 * Provides a listing of all artefacts that are part of the specified edition
	 *
	 * @param artefactId - Unique Id of the desired artefact
	 * @param editionId - Unique Id of the desired edition
	 * @param optional - Add "masks" to include artefact polygons and "images" to include image data
	 *
	 */
    public async getV1EditionsEditionIdArtefactsArtefactId(editionId: number, artefactId: number, optional: string[]): Promise<ArtefactDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdArtefactsArtefactId', editionId, artefactId, optional);
    }

    /**
	 * Provides a listing of all rois belonging to an artefact in the specified edition
	 *
	 * @param artefactId - Unique Id of the desired artefact
	 * @param editionId - Unique Id of the desired edition
	 *
	 */
    public async getV1EditionsEditionIdArtefactsArtefactIdRois(editionId: number, artefactId: number): Promise<InterpretationRoiDTOList> {
        return await this._connection.invoke('GetV1EditionsEditionIdArtefactsArtefactIdRois', editionId, artefactId);
    }

    /**
	 * Provides a listing of all artefacts that are part of the specified edition
	 *
	 * @param editionId - Unique Id of the desired edition
	 * @param optional - Add "masks" to include artefact polygons and "images" to include image data
	 *
	 */
    public async getV1EditionsEditionIdArtefacts(editionId: number, optional: string[]): Promise<ArtefactListDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdArtefacts', editionId, optional);
    }

    /**
	 * Provides a listing of text fragments that have text in the specified artefact.
	 * With the optional query parameter "suggested", this endpoint will also return
	 * any text fragment that the system suggests might have text in the artefact.
	 *
	 * @param editionId - Unique Id of the desired edition
	 * @param artefactId - Unique Id of the desired artefact
	 * @param optional - Add "suggested" to include possible matches suggested by the system
	 *
	 */
    public async getV1EditionsEditionIdArtefactsArtefactIdTextFragments(editionId: number, artefactId: number, optional: string[]): Promise<ArtefactTextFragmentMatchListDTO> {
        return await this._connection.invoke('GetV1EditionsEditionIdArtefactsArtefactIdTextFragments', editionId, artefactId, optional);
    }

    /**
	 * Updates the specified artefact
	 *
	 * @param artefactId - Unique Id of the desired artefact
	 * @param editionId - Unique Id of the desired edition
	 * @param payload - An UpdateArtefactDTO with the desired alterations to the artefact
	 *
	 */
    public async putV1EditionsEditionIdArtefactsArtefactId(editionId: number, artefactId: number, payload: UpdateArtefactDTO): Promise<ArtefactDTO> {
        return await this._connection.invoke('PutV1EditionsEditionIdArtefactsArtefactId', editionId, artefactId, payload);
    }

    /*
     * Client methods.
     */

    /**
	 * Add a listener for when the server broadcasts a new text fragment has been created
	 *
	 */
    public connectCreatedTextFragment(handler: (msg: TextFragmentDataDTO) => void): void {
        this._connection.on('CreatedTextFragment', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts a new text fragment has been created
	 *
	 */
    public disconnectCreatedTextFragment(handler: (msg: TextFragmentDataDTO) => void): void {
        this._connection.off('CreatedTextFragment', handler)
    }


    /**
	 * Add a listener for when the server broadcasts a text fragment has been updated
	 *
	 */
    public connectUpdatedTextFragment(handler: (msg: TextFragmentDataDTO) => void): void {
        this._connection.on('UpdatedTextFragment', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts a text fragment has been updated
	 *
	 */
    public disconnectUpdatedTextFragment(handler: (msg: TextFragmentDataDTO) => void): void {
        this._connection.off('UpdatedTextFragment', handler)
    }


    /**
	 * Add a listener for when the server broadcasts a editor has been requested for the edition
	 *
	 */
    public connectRequestedEditor(handler: (msg: EditorInvitationDTO) => void): void {
        this._connection.on('RequestedEditor', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts a editor has been requested for the edition
	 *
	 */
    public disconnectRequestedEditor(handler: (msg: EditorInvitationDTO) => void): void {
        this._connection.off('RequestedEditor', handler)
    }


    /**
	 * Add a listener for when the server broadcasts a editor has been added to the edition
	 *
	 */
    public connectCreatedEditor(handler: (msg: DetailedEditorRightsDTO) => void): void {
        this._connection.on('CreatedEditor', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts a editor has been added to the edition
	 *
	 */
    public disconnectCreatedEditor(handler: (msg: DetailedEditorRightsDTO) => void): void {
        this._connection.off('CreatedEditor', handler)
    }


    /**
	 * Add a listener for when the server broadcasts an editor's permissions have been updated
	 *
	 */
    public connectUpdatedEditorEmail(handler: (msg: DetailedEditorRightsDTO) => void): void {
        this._connection.on('UpdatedEditorEmail', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts an editor's permissions have been updated
	 *
	 */
    public disconnectUpdatedEditorEmail(handler: (msg: DetailedEditorRightsDTO) => void): void {
        this._connection.off('UpdatedEditorEmail', handler)
    }


    /**
	 * Add a listener for when the server broadcasts a new text edition has been created
	 *
	 */
    public connectCreatedEdition(handler: (msg: EditionDTO) => void): void {
        this._connection.on('CreatedEdition', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts a new text edition has been created
	 *
	 */
    public disconnectCreatedEdition(handler: (msg: EditionDTO) => void): void {
        this._connection.off('CreatedEdition', handler)
    }


    /**
	 * Add a listener for when the server broadcasts an edition has been deleted
	 *
	 */
    public connectDeletedEdition(handler: (msg: DeleteTokenDTO) => void): void {
        this._connection.on('DeletedEdition', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts an edition has been deleted
	 *
	 */
    public disconnectDeletedEdition(handler: (msg: DeleteTokenDTO) => void): void {
        this._connection.off('DeletedEdition', handler)
    }


    /**
	 * Add a listener for when the server broadcasts an edition's details have been updated
	 *
	 */
    public connectUpdatedEdition(handler: (msg: EditionDTO) => void): void {
        this._connection.on('UpdatedEdition', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts an edition's details have been updated
	 *
	 */
    public disconnectUpdatedEdition(handler: (msg: EditionDTO) => void): void {
        this._connection.off('UpdatedEdition', handler)
    }


    /**
	 * Add a listener for when the server broadcasts a new ROI has been created
	 *
	 */
    public connectCreatedRoi(handler: (msg: InterpretationRoiDTO) => void): void {
        this._connection.on('CreatedRoi', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts a new ROI has been created
	 *
	 */
    public disconnectCreatedRoi(handler: (msg: InterpretationRoiDTO) => void): void {
        this._connection.off('CreatedRoi', handler)
    }


    /**
	 * Add a listener for when the server broadcasts one or more new ROI's have been created
	 *
	 */
    public connectCreatedRoisBatch(handler: (msg: InterpretationRoiDTOList) => void): void {
        this._connection.on('CreatedRoisBatch', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts one or more new ROI's have been created
	 *
	 */
    public disconnectCreatedRoisBatch(handler: (msg: InterpretationRoiDTOList) => void): void {
        this._connection.off('CreatedRoisBatch', handler)
    }


    /**
	 * Add a listener for when the server broadcasts one or more new ROI's have been updated
	 *
	 */
    public connectEditedRoisBatch(handler: (msg: BatchEditRoiResponseDTO) => void): void {
        this._connection.on('EditedRoisBatch', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts one or more new ROI's have been updated
	 *
	 */
    public disconnectEditedRoisBatch(handler: (msg: BatchEditRoiResponseDTO) => void): void {
        this._connection.off('EditedRoisBatch', handler)
    }


    /**
	 * Add a listener for when the server broadcasts a ROI has been updated
	 *
	 */
    public connectUpdatedRoi(handler: (msg: UpdatedInterpretationRoiDTO) => void): void {
        this._connection.on('UpdatedRoi', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts a ROI has been updated
	 *
	 */
    public disconnectUpdatedRoi(handler: (msg: UpdatedInterpretationRoiDTO) => void): void {
        this._connection.off('UpdatedRoi', handler)
    }


    /**
	 * Add a listener for when the server broadcasts one or more new ROI's have been updated
	 *
	 */
    public connectUpdatedRoisBatch(handler: (msg: UpdatedInterpretationRoiDTOList) => void): void {
        this._connection.on('UpdatedRoisBatch', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts one or more new ROI's have been updated
	 *
	 */
    public disconnectUpdatedRoisBatch(handler: (msg: UpdatedInterpretationRoiDTOList) => void): void {
        this._connection.off('UpdatedRoisBatch', handler)
    }


    /**
	 * Add a listener for when the server broadcasts a ROI has been deleted
	 *
	 */
    public connectDeletedRoi(handler: (msg: DeleteDTO) => void): void {
        this._connection.on('DeletedRoi', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts a ROI has been deleted
	 *
	 */
    public disconnectDeletedRoi(handler: (msg: DeleteDTO) => void): void {
        this._connection.off('DeletedRoi', handler)
    }


    /**
	 * Add a listener for when the server broadcasts an artefact has been created
	 *
	 */
    public connectCreatedArtefact(handler: (msg: ArtefactDTO) => void): void {
        this._connection.on('CreatedArtefact', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts an artefact has been created
	 *
	 */
    public disconnectCreatedArtefact(handler: (msg: ArtefactDTO) => void): void {
        this._connection.off('CreatedArtefact', handler)
    }


    /**
	 * Add a listener for when the server broadcasts an artefact has been deleted
	 *
	 */
    public connectDeletedArtefact(handler: (msg: DeleteDTO) => void): void {
        this._connection.on('DeletedArtefact', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts an artefact has been deleted
	 *
	 */
    public disconnectDeletedArtefact(handler: (msg: DeleteDTO) => void): void {
        this._connection.off('DeletedArtefact', handler)
    }


    /**
	 * Add a listener for when the server broadcasts an artefact has been updated
	 *
	 */
    public connectUpdatedArtefact(handler: (msg: ArtefactDTO) => void): void {
        this._connection.on('UpdatedArtefact', handler)
    }

    /**
	 * Remove an existing listener that triggers when the server broadcasts an artefact has been updated
	 *
	 */
    public disconnectUpdatedArtefact(handler: (msg: ArtefactDTO) => void): void {
        this._connection.off('UpdatedArtefact', handler)
    }

} 