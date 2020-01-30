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
	UpdateEditorRightsDTO,
	CreateEditorRightsDTO,
	TextEditionDTO,
	ShareDTO,
	DeleteTokenDTO,
	DeleteEditionEntityDTO,
	EditionUpdateRequestDTO,
	EditionCopyDTO,
	ImageDTO,
	ImageInstitutionDTO,
	ImageInstitutionListDTO,
	ImageStackDTO,
	ImagedObjectDTO,
	ImagedObjectListDTO,
	PolygonDTO,
	SetInterpretationRoiDTO,
	InterpretationRoiDTO,
	UpdatedInterpretationRoiDTO,
	SetInterpretationRoiDTOList,
	InterpretationRoiDTOList,
	UpdatedInterpretationRoiDTOList,
	BatchEditRoiDTO,
	BatchEditRoiResponseDTO,
	SignDTO,
	NextSignInterpretationDTO,
	SignInterpretationDTO,
	InterpretationAttributeDTO,
	TextFragmentDataDTO,
	ArtefactTextFragmentMatchDTO,
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
} from "./sqe-dtos"

import SignalR from "signalr";

interface SignalRSQE extends SignalR {  
    sqeHub: SQEHub;  
}  

interface SQEHub extends HubConnection {  
    client: {  // callbacks on client side through server
		CreateTextFragment(returnedData: TextFragmentDataDTO): void;
		UpdateTextFragment(returnedData: TextFragmentDataDTO): void;
		CreateEditor(returnedData: CreateEditorRightsDTO): void;
		UpdateEditorEmail(returnedData: CreateEditorRightsDTO): void;
		CreateEditionId(returnedData: EditionDTO): void;
		DeleteEdition(returnedData: DeleteTokenDTO): void;
		UpdateEdition(returnedData: EditionDTO): void;
		CreateLogin(returnedData: DetailedUserTokenDTO): void;
		CreateUser(returnedData: UserDTO): void;
		CreateRoi(returnedData: InterpretationRoiDTO): void;
		CreateRoisBatch(returnedData: InterpretationRoiDTOList): void;
		CreateRoisBatchEdit(returnedData: BatchEditRoiResponseDTO): void;
		UpdateRoi(returnedData: UpdatedInterpretationRoiDTO): void;
		UpdateRoisBatch(returnedData: UpdatedInterpretationRoiDTOList): void;
		DeleteRoi(returnedData: number): void;
		CreateArtefact(returnedData: ArtefactDTO): void;
		DeleteArtefact(returnedData: number): void;
		UpdateArtefact(returnedData: ArtefactDTO): void  
    };  
    server: {  // Calling of server side hubs
		PostV1EditionsEditionIdEditors(editionId: number, payload: CreateEditorRightsDTO): CreateEditorRightsDTO;
		PutV1EditionsEditionIdEditorsEditorEmailId(editionId: number, editorEmailId: string, payload: UpdateEditorRightsDTO): CreateEditorRightsDTO;
		PostV1EditionsEditionId(editionId: number, request: EditionCopyDTO): EditionDTO;
		DeleteV1EditionsEditionId(editionId: number, optional: string[], token: string): DeleteTokenDTO;
		GetV1EditionsEditionId(editionId: number): EditionGroupDTO;
		GetV1Editions(): EditionListDTO;
		PutV1EditionsEditionId(editionId: number, request: EditionUpdateRequestDTO): EditionDTO;
		PostV1UsersLogin(payload: LoginRequestDTO): DetailedUserTokenDTO;
		PostV1UsersChangeUnactivatedEmail(payload: UnactivatedEmailUpdateRequestDTO): void;
		PostV1UsersChangeForgottenPassword(payload: ResetForgottenUserPasswordRequestDTO): void;
		PostV1UsersChangePassword(payload: ResetLoggedInUserPasswordRequestDTO): void;
		PutV1Users(payload: UserUpdateRequestDTO): DetailedUserDTO;
		PostV1UsersConfirmRegistration(payload: AccountActivationRequestDTO): void;
		PostV1UsersForgotPassword(payload: ResetUserPasswordRequestDTO): void;
		GetV1Users(): UserDTO;
		PostV1Users(payload: NewUserRequestDTO): UserDTO;
		PostV1UsersResendActivationEmail(payload: ResendUserAccountActivationRequestDTO): void;
		PostV1EditionsEditionIdTextFragments(editionId: number, createFragment: CreateTextFragmentDTO): TextFragmentDataDTO;
		PutV1EditionsEditionIdTextFragmentsTextFragmentId(editionId: number, textFragmentId: number, updatedTextFragment: UpdateTextFragmentDTO): TextFragmentDataDTO;
		GetV1EditionsEditionIdTextFragments(editionId: number): TextFragmentDataListDTO;
		GetV1EditionsEditionIdTextFragmentsTextFragmentIdArtefacts(editionId: number, textFragmentId: number): ArtefactDataListDTO;
		GetV1EditionsEditionIdTextFragmentsTextFragmentIdLines(editionId: number, textFragmentId: number): LineDataListDTO;
		GetV1EditionsEditionIdTextFragmentsTextFragmentId(editionId: number, textFragmentId: number): TextEditionDTO;
		GetV1EditionsEditionIdLinesLineId(editionId: number, lineId: number): LineTextDTO;
		SubscribeToEdition(editionId: number): void;
		UnsubscribeToEdition(editionId: number): void;
		ListEditionSubscriptions(): number[];
		GetV1EditionsEditionIdImagedObjectsImagedObjectId(editionId: number, imagedObjectId: string, optional: string[]): ImagedObjectDTO;
		GetV1EditionsEditionIdImagedObjects(editionId: number, optional: string[]): ImagedObjectListDTO;
		GetV1ImagedObjectsInstitutions(): ImageInstitutionListDTO;
		GetV1EditionsEditionIdRoisRoiId(editionId: number, roiId: number): InterpretationRoiDTO;
		PostV1EditionsEditionIdRois(editionId: number, newRoi: SetInterpretationRoiDTO): InterpretationRoiDTO;
		PostV1EditionsEditionIdRoisBatch(editionId: number, newRois: SetInterpretationRoiDTOList): InterpretationRoiDTOList;
		PostV1EditionsEditionIdRoisBatchEdit(editionId: number, rois: BatchEditRoiDTO): BatchEditRoiResponseDTO;
		PutV1EditionsEditionIdRoisRoiId(editionId: number, roiId: number, updateRoi: SetInterpretationRoiDTO): UpdatedInterpretationRoiDTO;
		PutV1EditionsEditionIdRoisBatch(editionId: number, updateRois: InterpretationRoiDTOList): UpdatedInterpretationRoiDTOList;
		DeleteV1EditionsEditionIdRoisRoiId(editionId: number, roiId: number): void;
		PostV1EditionsEditionIdArtefacts(editionId: number, payload: CreateArtefactDTO): ArtefactDTO;
		DeleteV1EditionsEditionIdArtefactsArtefactId(editionId: number, artefactId: number): void;
		GetV1EditionsEditionIdArtefactsArtefactId(editionId: number, artefactId: number, optional: string[]): ArtefactDTO;
		GetV1EditionsEditionIdArtefactsArtefactIdRois(editionId: number, artefactId: number): InterpretationRoiDTOList;
		GetV1EditionsEditionIdArtefacts(editionId: number, optional: string[]): ArtefactListDTO;
		GetV1EditionsEditionIdArtefactsArtefactIdTextFragments(editionId: number, artefactId: number, optional: string[]): ArtefactTextFragmentMatchListDTO;
		PutV1EditionsEditionIdArtefactsArtefactId(editionId: number, artefactId: number, payload: UpdateArtefactDTO): ArtefactDTO  
    }  
} 