
using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


            public static partial class POST
            {
        

                public class V1_Editions_EditionId_TextFragments
                : EditionRequestObject<CreateTextFragmentDTO, TextFragmentDataDTO, TextFragmentDataDTO>
                {
                    /// <summary>
        ///     Creates a new text fragment in the given edition of a scroll
        /// </summary>
        /// <param name="createFragment">A JSON object with the details of the new text fragment to be created</param>
        /// <param name="editionId">Id of the edition</param>
                    public V1_Editions_EditionId_TextFragments(uint editionId,CreateTextFragmentDTO payload) 
                        : base(editionId, null, payload) { }
                }
        
	}

            public static partial class PUT
            {
        

                public class V1_Editions_EditionId_TextFragments_TextFragmentId
                : EditionRequestObject<UpdateTextFragmentDTO, TextFragmentDataDTO, TextFragmentDataDTO>
                {
                    /// <summary>
        ///     Updates the specified text fragment with the submitted properties
        /// </summary>
        /// <param name="editionId">Edition of the text fragment being updates</param>
        /// <param name="textFragmentId">Id of the text fragment being updates</param>
        /// <param name="updatedTextFragment">Details of the updated text fragment</param>
        /// <returns>The details of the updated text fragment</returns>
                    public V1_Editions_EditionId_TextFragments_TextFragmentId(uint editionId,uint textFragmentId,UpdateTextFragmentDTO payload) 
                        : base(editionId, null, payload) { }
                }
        
	}

            public static partial class GET
            {
        

                public class V1_Editions_EditionId_TextFragments
                : EditionRequestObject<EmptyInput, TextFragmentDataListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Retrieves the ids of all Fragments of all fragments in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <returns>An array of the text fragment ids in correct sequence</returns>
                    public V1_Editions_EditionId_TextFragments(uint editionId) 
                        : base(editionId, null) { }
                }
        

                public class V1_Editions_EditionId_TextFragments_TextFragmentId_Artefacts
                : EditionRequestObject<EmptyInput, ArtefactDataListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Retrieves the ids of all Artefacts in the given textFragmentName
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>An array of the line ids in the proper sequence</returns>
                    public V1_Editions_EditionId_TextFragments_TextFragmentId_Artefacts(uint editionId,uint textFragmentId) 
                        : base(editionId, null) { }
                }
        

                public class V1_Editions_EditionId_TextFragments_TextFragmentId_Lines
                : EditionRequestObject<EmptyInput, LineDataListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Retrieves the ids of all lines in the given textFragmentName
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>An array of the line ids in the proper sequence</returns>
                    public V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(uint editionId,uint textFragmentId) 
                        : base(editionId, null) { }
                }
        

                public class V1_Editions_EditionId_TextFragments_TextFragmentId
                : EditionRequestObject<EmptyInput, TextEditionDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Retrieves all signs and their data from the given textFragmentName
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>
        ///     A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
        ///     sequence
        /// </returns>
                    public V1_Editions_EditionId_TextFragments_TextFragmentId(uint editionId,uint textFragmentId) 
                        : base(editionId, null) { }
                }
        

                public class V1_Editions_EditionId_Lines_LineId
                : EditionRequestObject<EmptyInput, LineTextDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Retrieves all signs and their data from the given line
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="lineId">Id of the line</param>
        /// <returns>
        ///     A manuscript edition object including the fragments and their lines in a hierarchical order and in correct
        ///     sequence
        /// </returns>
                    public V1_Editions_EditionId_Lines_LineId(uint editionId,uint lineId) 
                        : base(editionId, null) { }
                }
        
	}

}
