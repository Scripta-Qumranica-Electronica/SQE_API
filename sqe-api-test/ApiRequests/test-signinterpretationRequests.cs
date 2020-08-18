
using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


            public static partial class GET
            {
        

                public class V1_Editions_EditionId_SignInterpretationsAttributes
                : EditionRequestObject<EmptyInput, AttributeListDTO, EmptyOutput>
                {
                    /// <summary>
        /// Retrieve a list of all possible attributes for an edition
        /// </summary>
        /// <param name="editionId">The ID of the edition being searched</param>
        /// <returns>A list of and edition's attributes and their details</returns>
                    public V1_Editions_EditionId_SignInterpretationsAttributes(uint editionId) 
                        : base(editionId, null) { }
                }
        

                public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId
                : EditionRequestObject<EmptyInput, SignInterpretationDTO, EmptyOutput>
                {
                    /// <summary>
        /// Retrieve the details of a sign interpretation in an edition
        /// </summary>
        /// <param name="editionId">The ID of the edition being searched</param>
        /// <param name="signInterpretationId">The desired sign interpretation id</param>
        /// <returns>The details of the desired sign interpretation</returns>
                    public V1_Editions_EditionId_SignInterpretations_SignInterpretationId(uint editionId) 
                        : base(editionId, null) { }
                }
        
	}

            public static partial class POST
            {
        

                public class V1_Editions_EditionId_SignInterpretationsAttributes
                : EditionRequestObject<CreateAttributeDTO, AttributeDTO, AttributeDTO>
                {
                    /// <summary>
        /// Create a new attribute for an edition
        /// </summary>
        /// <param name="editionId">The ID of the edition being edited</param>
        /// <param name="newAttribute">The details of the new attribute</param>
        /// <returns>The details of the newly created attribute</returns>
                    public V1_Editions_EditionId_SignInterpretationsAttributes(uint editionId,CreateAttributeDTO payload) 
                        : base(editionId, null, payload) { }
                }
        

                public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes
                : EditionRequestObject<InterpretationAttributeCreateDTO, SignInterpretationDTO, SignInterpretationDTO>
                {
                    /// <summary>
        /// This adds a new attribute to the specified sign interpretation.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation for adding a new attribute</param>
        /// <param name="newSignInterpretationAttributes">Details of the attribute to be added</param>
        /// <returns>The updated sign interpretation</returns>
                    public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(uint editionId,uint signInterpretationId,InterpretationAttributeCreateDTO payload) 
                        : base(editionId, null, payload) { }
                }
        
	}

            public static partial class DELETE
            {
        

                public class V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId
                : EditionRequestObject<EmptyInput, EmptyOutput, DeleteDTO>
                {
                    /// <summary>
        /// Delete an attribute from an edition
        /// </summary>
        /// <param name="editionId">The ID of the edition being edited</param>
        /// <param name="attributeId">The ID of the attribute to delete</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
                    public V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(uint editionId,uint attributeId) 
                        : base(editionId, null) { }
                }
        

                public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId
                : EditionRequestObject<EmptyInput, EmptyOutput, DeleteDTO>
                {
                    /// <summary>
        /// This deletes the specified attribute value from the specified sign interpretation.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
        /// <param name="attributeValueId">Id of the attribute being removed</param>
        /// <returns>Ok or Error</returns>
                    public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(uint editionId,uint signInterpretationId,uint attributeValueId) 
                        : base(editionId, null) { }
                }
        
	}

            public static partial class PUT
            {
        

                public class V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId
                : EditionRequestObject<UpdateAttributeDTO, AttributeDTO, AttributeDTO>
                {
                    /// <summary>
        /// Change the details of an attribute in an edition
        /// </summary>
        /// <param name="editionId">The ID of the edition being edited</param>
        /// <param name="attributeId">The ID of the attribute to update</param>
        /// <param name="updatedAttribute">The details of the updated attribute</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
                    public V1_Editions_EditionId_SignInterpretationsAttributes_AttributeId(uint editionId,uint attributeId,UpdateAttributeDTO payload) 
                        : base(editionId, null, payload) { }
                }
        

                public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary
                : EditionRequestObject<CommentaryCreateDTO, SignInterpretationDTO, SignInterpretationDTO>
                {
                    // /// <summary>
        // /// Creates a new sign interpretation 
        // /// </summary>
        // /// <param name="editionId">ID of the edition being changed</param>
        // /// <param name="newSignInterpretation">New sign interpretation data to be added</param>
        // /// <returns>The new sign interpretation</returns>
        // [HttpPost("v1/editions/{editionId}/sign-interpretations")]
        // public async Task<ActionResult<SignInterpretationDTO>> PostNewSignInterpretation([FromRoute] uint editionId,
        //     [FromBody] SignInterpretationCreateDTO newSignInterpretation)
        // {
        //     throw new NotImplementedException(); //Not Implemented
        // }
        //
        // /// <summary>
        // /// Deletes the sign interpretation in the route. The endpoint automatically manages the sign stream
        // /// by connecting all the deleted sign's next and previous nodes.
        // /// </summary>
        // /// <param name="editionId">ID of the edition being changed</param>
        // /// <param name="signInterpretationId">ID of the sign interpretation being deleted</param>
        // /// <returns>Ok or Error</returns>
        // [HttpDelete("v1/editions/{editionId}/sign-interpretations/{signInterpretationId}")]
        // public async Task<ActionResult> DeleteSignInterpretation([FromRoute] uint editionId,
        //     [FromRoute] uint signInterpretationId)
        // {
        //     throw new NotImplementedException(); //Not Implemented
        // }

        /// <summary>
        /// Updates the commentary of a sign interpretation
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation whose commentary is being changed</param>
        /// <param name="commentary">The new commentary for the sign interpretation</param>
        /// <returns>Ok or Error</returns>
                    public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary(uint editionId,uint signInterpretationId,CommentaryCreateDTO payload) 
                        : base(editionId, null, payload) { }
                }
        

                public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId
                : EditionRequestObject<InterpretationAttributeCreateDTO, SignInterpretationDTO, SignInterpretationDTO>
                {
                    /// <summary>
        /// This changes the values of the specified sign interpretation attribute,
        /// mainly used to change commentary.
        /// </summary>
        /// <param name="editionId">ID of the edition being changed</param>
        /// <param name="signInterpretationId">ID of the sign interpretation being altered</param>
        /// <param name="attributeValueId">Id of the attribute value to be altered</param>
        /// <param name="alteredSignInterpretationAttribute">New details of the attribute</param>
        /// <returns>The updated sign interpretation</returns>
                    public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeValueId(uint editionId,uint signInterpretationId,uint attributeValueId,InterpretationAttributeCreateDTO payload) 
                        : base(editionId, null, payload) { }
                }
        
	}

}
