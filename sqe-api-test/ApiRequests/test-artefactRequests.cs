
using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


            public static partial class POST
            {
        

                public class V1_Editions_EditionId_Artefacts
                : EditionRequestObject<CreateArtefactDTO, ArtefactDTO, ArtefactDTO>
                {
                    /// <summary>
        ///     Creates a new artefact with the provided data.
        ///     If no mask is provided, a placeholder mask will be created with the values:
        ///     "POLYGON((0 0,1 1,1 0,0 0))" (the system requires a valid WKT polygon mask for
        ///     every artefact). It is not recommended to leave the mask, name, or work status
        ///     blank or null. It will often be advantageous to leave the transformation null
        ///     when first creating a new artefact.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">A CreateArtefactDTO with the data for the new artefact</param>
                    public V1_Editions_EditionId_Artefacts(uint editionId,CreateArtefactDTO payload) 
                        : base(editionId, null, payload) { }
                }
        

                public class V1_Editions_EditionId_Artefacts_BatchTransformation
                : EditionRequestObject<BatchUpdateArtefactPlacementDTO, BatchUpdatedArtefactTransformDTO, BatchUpdatedArtefactTransformDTO>
                {
                    /// <summary>
        ///     Updates the positional data for a batch of artefacts
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">A BatchUpdateArtefactTransformDTO with a list of the desired updates</param>
        /// <returns></returns>
                    public V1_Editions_EditionId_Artefacts_BatchTransformation(uint editionId,BatchUpdateArtefactPlacementDTO payload) 
                        : base(editionId, null, payload) { }
                }
        

                public class V1_Editions_EditionId_ArtefactGroups
                : EditionRequestObject<CreateArtefactGroupDTO, ArtefactGroupDTO, ArtefactGroupDTO>
                {
                    /// <summary>
        ///     Creates a new artefact group with the submitted data.
        ///     The new artefact must have a list of artefacts that belong to the group.
        ///     It is not necessary to give the group a name.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">Parameters of the new artefact group</param>
        /// <returns></returns>
                    public V1_Editions_EditionId_ArtefactGroups(uint editionId,CreateArtefactGroupDTO payload) 
                        : base(editionId, null, payload) { }
                }
        
	}

            public static partial class DELETE
            {
        

                public class V1_Editions_EditionId_Artefacts_ArtefactId
                : EditionRequestObject<EmptyInput, EmptyOutput, DeleteDTO>
                {
                    /// <summary>
        ///     Deletes the specified artefact
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
                    public V1_Editions_EditionId_Artefacts_ArtefactId(uint editionId,uint artefactId) 
                        : base(editionId, null) { }
                }
        

                public class V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId
                : EditionRequestObject<EmptyInput, DeleteDTO, DeleteDTO>
                {
                    /// <summary>
        ///     Deletes the specified artefact group.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactGroupId">Unique Id of the artefact group to be deleted</param>
        /// <returns></returns>
                    public V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId(uint editionId,uint artefactGroupId) 
                        : base(editionId, null) { }
                }
        
	}

            public static partial class GET
            {
        

                public class V1_Editions_EditionId_Artefacts_ArtefactId
                : EditionRequestObject<EmptyInput, ArtefactDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
                    public V1_Editions_EditionId_Artefacts_ArtefactId(uint editionId,uint artefactId,List<string> optional = null) 
                        : base(editionId, optional) { }
                }
        

                public class V1_Editions_EditionId_Artefacts_ArtefactId_Rois
                : EditionRequestObject<EmptyInput, InterpretationRoiDTOList, EmptyOutput>
                {
                    /// <summary>
        ///     Provides a listing of all rois belonging to an artefact in the specified edition
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
                    public V1_Editions_EditionId_Artefacts_ArtefactId_Rois(uint editionId,uint artefactId) 
                        : base(editionId, null) { }
                }
        

                public class V1_Editions_EditionId_Artefacts
                : EditionRequestObject<EmptyInput, ArtefactListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides a listing of all artefacts that are part of the specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Add "masks" to include artefact polygons and "images" to include image data</param>
                    public V1_Editions_EditionId_Artefacts(uint editionId,List<string> optional = null) 
                        : base(editionId, optional) { }
                }
        

                public class V1_Editions_EditionId_Artefacts_ArtefactId_TextFragments
                : EditionRequestObject<EmptyInput, ArtefactTextFragmentMatchListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides a listing of text fragments that have text in the specified artefact.
        ///     With the optional query parameter "suggested", this endpoint will also return
        ///     any text fragment that the system suggests might have text in the artefact.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="optional">Add "suggested" to include possible matches suggested by the system</param>
                    public V1_Editions_EditionId_Artefacts_ArtefactId_TextFragments(uint editionId,uint artefactId,List<string> optional = null) 
                        : base(editionId, optional) { }
                }
        

                public class V1_Editions_EditionId_ArtefactGroups
                : EditionRequestObject<EmptyInput, ArtefactGroupListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Gets a listing of all artefact groups in the edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <returns></returns>
                    public V1_Editions_EditionId_ArtefactGroups(uint editionId) 
                        : base(editionId, null) { }
                }
        

                public class V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId
                : EditionRequestObject<EmptyInput, ArtefactGroupDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Gets the details of a specific artefact group in the edition
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactGroupId">Id of the desired artefact group</param>
        /// <returns></returns>
                    public V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId(uint editionId,uint artefactGroupId) 
                        : base(editionId, null) { }
                }
        
	}

            public static partial class PUT
            {
        

                public class V1_Editions_EditionId_Artefacts_ArtefactId
                : EditionRequestObject<UpdateArtefactDTO, ArtefactDTO, ArtefactDTO>
                {
                    /// <summary>
        ///     Updates the specified artefact.
        ///     There are many possible attributes that can be changed for
        ///     an artefact.  The caller should only input only those that
        ///     should be changed. Attributes with a null value will be ignored.
        ///     For instance, setting the mask to null or "" will result in
        ///     no changes to the current mask, and no value for the mask will
        ///     be returned (or broadcast). Likewise, the transformation, name,
        ///     or status message may be set to null and no change will be made
        ///     to those entities (though any unchanged values will be returned
        ///     along with the changed values and also broadcast to co-editors).
        /// </summary>
        /// <param name="artefactId">Unique Id of the desired artefact</param>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="payload">An UpdateArtefactDTO with the desired alterations to the artefact</param>
                    public V1_Editions_EditionId_Artefacts_ArtefactId(uint editionId,uint artefactId,UpdateArtefactDTO payload) 
                        : base(editionId, null, payload) { }
                }
        

                public class V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId
                : EditionRequestObject<UpdateArtefactGroupDTO, ArtefactGroupDTO, ArtefactGroupDTO>
                {
                    /// <summary>
        ///     Updates the details of an artefact group.
        ///     The artefact group will now only contain the artefacts listed in the JSON payload.
        ///     If the name is null, no change will be made, otherwise the name will also be updated.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="artefactGroupId">Id of the artefact group to be updated</param>
        /// <param name="payload">Parameters that the artefact group should be changed to</param>
        /// <returns></returns>
                    public V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId(uint editionId,uint artefactGroupId,UpdateArtefactGroupDTO payload) 
                        : base(editionId, null, payload) { }
                }
        
	}

}
