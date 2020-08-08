using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_Editions_EditionId_SignInterpretationsAttributes : EditionRequestObject<EmptyInput, AttributeListDTO, EmptyOutput>
        {
            /// <summary>
            /// Get all sign interpretation attributes for an edition
            /// </summary>
            /// <param name="editionId"></param>
            public V1_Editions_EditionId_SignInterpretationsAttributes(uint editionId) : base(editionId)
            {
            }
        }
    }

    public static partial class Post
    {
        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes : SignInterpretationRequestObject<InterpretationAttributeCreateDTO, SignInterpretationDTO, SignInterpretationDTO>
        {
            /// <summary>
            /// Create a new attribute on the specified sign interpretation
            /// </summary>
            /// <param name="editionId"></param>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes(uint editionId, uint signInterpretationId, InterpretationAttributeCreateDTO payload) : base(editionId, signInterpretationId, payload)
            {
                listenerMethod.Add("UpdatedSignInterpretation");
            }
        }

    }

    public static partial class Put
    {
        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary : SignInterpretationRequestObject<CommentaryCreateDTO, SignInterpretationDTO, SignInterpretationDTO>
        {
            /// <summary>
            /// Create a new attribute on the specified sign interpretation
            /// </summary>
            /// <param name="editionId"></param>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Commentary(uint editionId, uint signInterpretationId, CommentaryCreateDTO payload) : base(editionId, signInterpretationId, payload)
            {
                listenerMethod.Add("UpdatedSignInterpretation");
            }
        }

        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeId : SignInterpretationAttributeRequestObject<InterpretationAttributeCreateDTO, SignInterpretationDTO, SignInterpretationDTO>
        {
            /// <summary>
            /// Updates an attribute of the specified sign interpretation
            /// </summary>
            /// <param name="editionId"></param>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeId(uint editionId, uint signInterpretationId, uint attributeValueId, InterpretationAttributeCreateDTO payload) : base(editionId, signInterpretationId, attributeValueId, payload)
            {
                listenerMethod.Add("UpdatedSignInterpretation");
            }
        }

    }

    public static partial class Delete
    {
        public class V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeId : SignInterpretationAttributeRequestObject<EmptyInput, EmptyOutput, SignInterpretationDTO>
        {
            /// <summary>
            /// Deletes an attribute from the specified sign interpretation
            /// </summary>
            /// <param name="editionId"></param>
            public V1_Editions_EditionId_SignInterpretations_SignInterpretationId_Attributes_AttributeId(uint editionId, uint signInterpretationId, uint attributeValueId) : base(editionId, signInterpretationId, attributeValueId, null)
            {
                listenerMethod.Add("UpdatedSignInterpretation");
            }
        }
    }
}