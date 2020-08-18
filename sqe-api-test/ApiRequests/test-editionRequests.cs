
using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


            public static partial class POST
            {
        

                public class V1_Editions_EditionId_AddEditorRequest
                : EditionRequestObject<InviteEditorDTO, EmptyOutput, EmptyOutput>
                {
                    /// <summary>
        ///     Adds an editor to the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
                    public V1_Editions_EditionId_AddEditorRequest(uint editionId,InviteEditorDTO payload) 
                        : base(editionId, null, payload) { }
                }
        

                public class V1_Editions_ConfirmEditorship_Token
                : RequestObject<EmptyInput, DetailedEditorRightsDTO, DetailedEditorRightsDTO>
                {
                    /// <summary>
        ///     Confirm addition of an editor to the specified edition
        /// </summary>
        /// <param name="token">JWT for verifying the request confirmation</param>
                    public V1_Editions_ConfirmEditorship_Token(string token) 
                        : base() { }
                }
        

                public class V1_Editions_EditionId
                : EditionRequestObject<EditionCopyDTO, EditionDTO, EditionDTO>
                {
                    /// <summary>
        ///     Creates a copy of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be changed in the copied edition</param>
                    public V1_Editions_EditionId(uint editionId,EditionCopyDTO payload) 
                        : base(editionId, null, payload) { }
                }
        
	}

            public static partial class GET
            {
        

                public class V1_Editions_AdminShareRequests
                : RequestObject<EmptyInput, AdminEditorRequestListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Get a list of requests issued by the current user for other users
        ///     to become editors of a shared edition
        /// </summary>
        /// <returns></returns>
                    public V1_Editions_AdminShareRequests() 
                        : base() { }
                }
        

                public class V1_Editions_EditorInvitations
                : RequestObject<EmptyInput, EditorInvitationListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Get a list of invitations issued to the current user to become an editor of a shared edition
        /// </summary>
        /// <returns></returns>
                    public V1_Editions_EditorInvitations() 
                        : base() { }
                }
        

                public class V1_Editions_EditionId
                : EditionRequestObject<EmptyInput, EditionGroupDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
                    public V1_Editions_EditionId(uint editionId) 
                        : base(editionId, null) { }
                }
        

                public class V1_Editions
                : RequestObject<EmptyInput, EditionListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides a listing of all editions accessible to the current user
        /// </summary>
                    public V1_Editions() 
                        : base() { }
                }
        

                public class _V1_Editions_EditionId_ScriptCollection
                : EditionRequestObject<EmptyInput, EditionScriptCollectionDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides spatial data for all letters in the edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <returns></returns>
                    public _V1_Editions_EditionId_ScriptCollection(uint editionId) 
                        : base(editionId, null) { }
                }
        

                public class _V1_Editions_EditionId_ScriptLines
                : EditionRequestObject<EmptyInput, EditionScriptLinesDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides spatial data for all letters in the edition organized and oriented
        ///     by lines.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <returns></returns>
                    public _V1_Editions_EditionId_ScriptLines(uint editionId) 
                        : base(editionId, null) { }
                }
        
	}

            public static partial class PUT
            {
        

                public class V1_Editions_EditionId_Editors_EditorEmailId
                : EditionRequestObject<UpdateEditorRightsDTO, DetailedEditorRightsDTO, DetailedEditorRightsDTO>
                {
                    /// <summary>
        ///     Changes the rights for an editor of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="editorEmailId">Email address of the editor whose permissions are being changed</param>
        /// <param name="payload">JSON object with the attributes of the new editor</param>
                    public V1_Editions_EditionId_Editors_EditorEmailId(uint editionId,string editorEmailId,UpdateEditorRightsDTO payload) 
                        : base(editionId, null, payload) { }
                }
        

                public class V1_Editions_EditionId
                : EditionRequestObject<EditionUpdateRequestDTO, EditionDTO, EditionDTO>
                {
                    /// <summary>
        ///     Updates data for the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="request">JSON object with the attributes to be updated</param>
                    public V1_Editions_EditionId(uint editionId,EditionUpdateRequestDTO payload) 
                        : base(editionId, null, payload) { }
                }
        
	}

            public static partial class DELETE
            {
        

                public class V1_Editions_EditionId
                : EditionRequestObject<EmptyInput, DeleteTokenDTO, DeleteDTO>
                {
                    /// <summary>
        ///     Provides details about the specified edition and all accessible alternate editions
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Optional parameters: 'deleteForAllEditors'</param>
        /// <param name="token">token required when using optional 'deleteForAllEditors'</param>
                    public V1_Editions_EditionId(uint editionId,List<string> optional = null,string optional = null) 
                        : base(editionId, optional) { }
                }
        
	}

}
