/* tslint:disable */

/*
 * Do not edit this file directly!
 * This set of interfaces and enums is autogenerated by `GenerateTypescriptDTOs` 
 * in the project https://github.com/Scripta-Qumranica-Electronica/SQE_API.
 * Changes made there are used to automatically create this file at {ROOT}/ts-dtos
 * whenever the GenerateTypescriptDTOs program is run.
 */


export interface EditionScriptCollectionDTO {
    letters: CharacterShapeDTO[] | null;
}

export interface EditionScriptLinesDTO {
    textFragments: ScriptTextFragmentDTO[] | null;
}

export interface CharacterShapeDTO {
    id: number;
    character: char | null;
    polygon: string | null;
    imageURL: string | null;
    rotation: float | null;
    attributes: string[] | null;
}

export interface ScriptTextFragmentDTO {
    textFragmentName: string | null;
    textFragmentId: number;
    lines: ScriptLineDTO[] | null;
}

export interface ScriptLineDTO {
    lineName: string | null;
    lineId: number;
    artefacts: ScriptArtefactCharactersDTO[] | null;
}

export interface ScriptArtefactCharactersDTO {
    artefactName: string | null;
    artefactId: number;
    placement: PlacementDTO | null;
    characters: SignInterpretationDTO[] | null;
}

export interface TextFragmentDataDTO {
    id: number;
    name: string | null;
    editorId: number;
}

export interface ArtefactTextFragmentMatchDTO {
    suggested: boolean;
}

export interface ImagedObjectTextFragmentMatchDTO {
    editionId: number;
    manuscriptName: string | null;
    textFragmentId: number;
    textFragmentName: string | null;
    side: string | null;
}

export interface TextFragmentDataListDTO {
    textFragments: TextFragmentDataDTO[] | null;
}

export interface ArtefactTextFragmentMatchListDTO {
    textFragments: ArtefactTextFragmentMatchDTO[] | null;
}

export interface TextFragmentDTO {
    textFragmentId: number;
    textFragmentName: string | null;
    editorId: number;
    lines: LineDTO[] | null;
}

export interface LineDataDTO {
    lineId: number;
    lineName: string | null;
}

export interface LineDataListDTO {
    lines: LineDataDTO[] | null;
}

export interface LineDTO {
    lineId: number;
    lineName: string | null;
    editorId: number;
    signs: SignDTO[] | null;
}

export interface LineTextDTO {
    licence: string | null;
    editors: { [key: string] : EditorDTO } | null;
}

export interface UpdateTextFragmentDTO {
    name: string | null;
    previousTextFragmentId: number | null;
    nextTextFragmentId: number | null;
}

export interface CreateTextFragmentDTO {
    name: string;
}

export interface SignDTO {
    signInterpretations: SignInterpretationDTO[] | null;
}

export interface NextSignInterpretationDTO {
    nextSignInterpretationId: number;
    editorId: number;
}

export interface SignInterpretationDTO {
    signInterpretationId: number;
    character: string | null;
    attributes: InterpretationAttributeDTO[] | null;
    rois: InterpretationRoiDTO[] | null;
    nextSignInterpretations: NextSignInterpretationDTO[] | null;
}

export interface InterpretationAttributeDTO {
    interpretationAttributeId: number;
    sequence: number;
    attributeValueId: number;
    attributeValueString: string | null;
    editorId: number;
    value: float | null;
}

export interface PlacementDTO {
    scale: number;
    rotate: number;
    zIndex: number;
    translate: TranslateDTO | null;
}

export interface TranslateDTO {
    x: number;
    y: number;
}

export interface SimpleImageDTO {
    id: number;
    url: string | null;
    lightingType: Lighting | null;
    lightingDirection: Direction | null;
    waveLength: string[] | null;
    type: string | null;
    side: string | null;
    ppi: number;
    master: boolean;
    catalogNumber: number;
}

export interface ImageDTO {
    imageToImageMapEditorId: number | null;
    regionInMasterImage: string | null;
    regionInImage: string | null;
    transformToMaster: string | null;
}

export interface SimpleImageListDTO {
    images: SimpleImageDTO[] | null;
}

export interface ImageInstitutionDTO {
    name: string | null;
}

export interface ImageInstitutionListDTO {
    institutions: ImageInstitutionDTO[] | null;
}

export interface InstitutionalImageDTO {
    id: string | null;
    thumbnailUrl: string | null;
    license: string | null;
}

export interface InstitutionalImageListDTO {
    institutionalImages: InstitutionalImageDTO[] | null;
}

export interface ArtefactDataDTO {
    id: number;
    name: string;
}

export interface ArtefactDTO {
    editionId: number;
    imagedObjectId: string;
    imageId: number;
    artefactDataEditorId: number;
    mask: string;
    artefactMaskEditorId: number;
    isPlaced: boolean;
    placement: PlacementDTO;
    artefactPlacementEditorId: number | null;
    side: string;
    statusMessage: string | null;
}

export interface ArtefactSide {
    
}

export interface ArtefactListDTO {
    artefacts: ArtefactDTO[];
}

export interface ArtefactDataListDTO {
    artefacts: ArtefactDataDTO[];
}

export interface ArtefactGroupDTO {
    id: number;
}

export interface ArtefactGroupListDTO {
    artefactGroups: ArtefactGroupDTO[] | null;
}

export interface UpdateArtefactDTO {
    mask: string | null;
    placement: PlacementDTO | null;
    name: string | null;
    statusMessage: string | null;
}

export interface UpdateArtefactPlacementDTO {
    artefactId: number;
    isPlaced: boolean;
    placement: PlacementDTO | null;
}

export interface BatchUpdateArtefactPlacementDTO {
    artefactPlacements: UpdateArtefactPlacementDTO[];
}

export interface UpdatedArtefactPlacementDTO {
    placementEditorId: number;
}

export interface BatchUpdatedArtefactTransformDTO {
    artefactPlacements: UpdatedArtefactPlacementDTO[];
}

export interface UpdateArtefactGroupDTO {
    
}

export interface CreateArtefactDTO {
    masterImageId: number;
    mask: string;
}

export interface CreateArtefactGroupDTO {
    name: string | null;
    artefacts: number[];
}

export interface EditionDTO {
    id: number;
    name: string | null;
    editionDataEditorId: number;
    permission: PermissionDTO | null;
    owner: UserDTO | null;
    thumbnailUrl: string | null;
    shares: DetailedEditorRightsDTO[] | null;
    metrics: EditionManuscriptMetricsDTO | null;
    locked: boolean;
    isPublic: boolean;
    lastEdit: Date | null;
    copyright: string | null;
}

export interface EditionGroupDTO {
    primary: EditionDTO | null;
    others: EditionDTO[] | null;
}

export interface EditionListDTO {
    editions: List<EditionDTO>[] | null;
}

export interface PermissionDTO {
    mayRead: boolean;
    mayWrite: boolean;
    isAdmin: boolean;
}

export interface UpdateEditorRightsDTO {
    mayLock: boolean;
}

export interface InviteEditorDTO {
    email: string;
}

export interface DetailedEditorRightsDTO {
    email: string;
    editionId: number;
}

export interface DetailedUpdateEditorRightsDTO {
    editionId: number;
    editionName: string | null;
    date: Date | null;
}

export interface AdminEditorRequestDTO {
    editorName: string | null;
    editorEmail: string | null;
}

export interface EditorInvitationDTO {
    token: Guid | null;
    requestingAdminName: string | null;
    requestingAdminEmail: string | null;
}

export interface EditorInvitationListDTO {
    editorInvitations: EditorInvitationDTO[] | null;
}

export interface AdminEditorRequestListDTO {
    editorRequests: AdminEditorRequestDTO[] | null;
}

export interface TextEditionDTO {
    manuscriptId: number;
    editionName: string | null;
    editorId: number;
    licence: string | null;
    editors: { [key: string] : EditorDTO } | null;
    textFragments: TextFragmentDTO[] | null;
}

export interface DeleteTokenDTO {
    editionId: number;
    token: string | null;
}

export interface DeleteEditionEntityDTO {
    entityId: number;
    editorId: number;
}

export interface DeleteDTO {
    entity: EditionEntities | null;
    ids: number[] | null;
}

export interface EditionUpdateRequestDTO {
    metrics: UpdateEditionManuscriptMetricsDTO | null;
}

export interface EditionCopyDTO {
    name: string | null;
    copyrightHolder: string | null;
    collaborators: string | null;
}

export interface UpdateEditionManuscriptMetricsDTO {
    width: number;
    height: number;
    xOrigin: number;
    yOrigin: number;
}

export interface EditionManuscriptMetricsDTO {
    ppi: number;
    editorId: number;
}

export interface LoginRequestDTO {
    email: string;
    password: string;
}

export interface UserUpdateRequestDTO {
    password: string | null;
    email: string | null;
    organization: string | null;
    forename: string | null;
    surname: string | null;
}

export interface NewUserRequestDTO {
    email: string;
    password: string;
}

export interface AccountActivationRequestDTO {
    token: string;
}

export interface ResendUserAccountActivationRequestDTO {
    email: string;
}

export interface UnactivatedEmailUpdateRequestDTO {
    newEmail: string;
}

export interface ResetUserPasswordRequestDTO {
    email: string;
}

export interface ResetForgottenUserPasswordRequestDTO {
    password: string;
}

export interface ResetLoggedInUserPasswordRequestDTO {
    oldPassword: string;
    newPassword: string;
}

export interface UserDTO {
    userId: number;
    email: string | null;
}

export interface DetailedUserDTO {
    forename: string | null;
    surname: string | null;
    organization: string | null;
    activated: boolean;
}

export interface DetailedUserTokenDTO {
    token: string | null;
}

export interface EditorDTO {
    email: string | null;
    forename: string | null;
    surname: string | null;
    organization: string | null;
}

export interface ImageStackDTO {
    id: number | null;
    images: ImageDTO[] | null;
    masterIndex: number | null;
}

export interface ImagedObjectDTO {
    id: string | null;
    recto: ImageStackDTO | null;
    verso: ImageStackDTO | null;
    artefacts: ArtefactDTO[] | null;
}

export interface ImagedObjectListDTO {
    imagedObjects: ImagedObjectDTO[] | null;
}

export interface WktPolygonDTO {
    wktPolygon: string | null;
}

export interface SetInterpretationRoiDTO {
    artefactId: number;
    signInterpretationId: number | null;
    shape: string;
    translate: TranslateDTO;
    stanceRotation: ushort | null;
    exceptional: boolean;
    valuesSet: boolean;
}

export interface InterpretationRoiDTO {
    interpretationRoiId: number;
    editorId: number;
}

export interface UpdatedInterpretationRoiDTO {
    oldInterpretationRoiId: number;
}

export interface SetInterpretationRoiDTOList {
    rois: SetInterpretationRoiDTO[];
}

export interface InterpretationRoiDTOList {
    rois: InterpretationRoiDTO[];
}

export interface UpdatedInterpretationRoiDTOList {
    rois: UpdatedInterpretationRoiDTO[];
}

export interface BatchEditRoiDTO {
    createRois: InterpretationRoiDTO[] | null;
    updateRois: UpdatedInterpretationRoiDTO[] | null;
    deleteRois: number[] | null;
}

export interface BatchEditRoiResponseDTO {
    createRois: InterpretationRoiDTO[];
    updateRois: UpdatedInterpretationRoiDTO[];
    deleteRois: number[];
}

export interface CatalogueMatchInputDTO {
    catalogSide: SideDesignation | null;
    imagedObjectId: string;
    manuscriptId: number;
    editionName: string | null;
    editionVolume: string | null;
    editionLocation1: string | null;
    editionLocation2: string | null;
    editionSide: SideDesignation | null;
    comment: string | null;
    textFragmentId: number;
    editionId: number;
    confirmed: boolean | null;
}

export interface CatalogueMatchDTO {
    imageCatalogId: number;
    institution: string | null;
    catalogueNumber1: string | null;
    catalogueNumber2: string | null;
    proxy: string | null;
    url: string | null;
    filename: string | null;
    suffix: string | null;
    thumbnail: string | null;
    license: string | null;
    iaaEditionCatalogueId: number;
    manuscriptName: string | null;
    name: string | null;
    matchAuthor: string | null;
    matchConfirmationAuthor: string | null;
    matchId: number;
    dateOfMatch: Date | null;
    dateOfConfirmation: Date | null;
}

export interface CatalogueMatchListDTO {
    matches: CatalogueMatchDTO[] | null;
}

export enum Direction {
    left = 0,
    right = 1,
    top = 2,
}

export enum Lighting {
    direct = 0,
    raking = 1,
}

export enum EditionEntities {
    edition = 0,
    artefact = 1,
    artefactGroup = 2,
    textFragment = 3,
    line = 4,
    signInterpretation = 5,
    roi = 6,
}

export enum SideDesignation {
    recto = 0,
    verso = 1,
}
