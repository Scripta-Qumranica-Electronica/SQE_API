
using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


            public static partial class GET
            {
        

                public class V1_Catalogue_ImagedObjects_ImagedObjectId_TextFragments
                : RequestObject<EmptyInput, CatalogueMatchListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Get a listing of all text fragments matches that correspond to an imaged object
        /// </summary>
        /// <param name="imagedObjectId">Id of imaged object to search for transcription matches</param>
                    public V1_Catalogue_ImagedObjects_ImagedObjectId_TextFragments(string imagedObjectId) 
                        : base() { }
                }
        

                public class V1_Catalogue_TextFragments_TextFragmentId_ImagedObjects
                : RequestObject<EmptyInput, CatalogueMatchListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Get a listing of all imaged objects that matches that correspond to a transcribed text fragment
        /// </summary>
        /// <param name="textFragmentId">Unique Id of the text fragment to search for imaged object matches</param>
                    public V1_Catalogue_TextFragments_TextFragmentId_ImagedObjects(uint textFragmentId) 
                        : base() { }
                }
        

                public class V1_Catalogue_Editions_EditionId_ImagedObjectTextFragmentMatches
                : EditionRequestObject<EmptyInput, CatalogueMatchListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Get a listing of all corresponding imaged objects and transcribed text fragment in a specified edition
        /// </summary>
        /// <param name="editionId">Unique Id of the edition to search for imaged objects to text fragment matches</param>
                    public V1_Catalogue_Editions_EditionId_ImagedObjectTextFragmentMatches(uint editionId) 
                        : base(editionId, null) { }
                }
        

                public class V1_Catalogue_Manuscript_ManuscriptId_ImagedObjectTextFragmentMatches
                : RequestObject<EmptyInput, CatalogueMatchListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Get a listing of all corresponding imaged objects and transcribed text fragment in a specified edition
        /// </summary>
        /// <param name="manuscriptId">Unique Id of the edition to search for imaged objects to text fragment matches</param>
                    public V1_Catalogue_Manuscript_ManuscriptId_ImagedObjectTextFragmentMatches(uint manuscriptId) 
                        : base() { }
                }
        
	}

            public static partial class POST
            {
        

                public class V1_Catalogue
                : RequestObject<CatalogueMatchInputDTO, EmptyOutput, EmptyOutput>
                {
                    /// <summary>
        ///     Create a new matched pair for an imaged object and a text fragment along with the edition princeps information
        /// </summary>
        /// <param name="newMatch">The details of the new match</param>
        /// <returns></returns>
                    public V1_Catalogue(CatalogueMatchInputDTO payload) 
                        : base(payload) { }
                }
        

                public class V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId
                : RequestObject<EmptyInput, EmptyOutput, EmptyOutput>
                {
                    /// <summary>
        ///     Confirm the correctness of an existing imaged object and text fragment match
        /// </summary>
        /// <param name="iaaEditionCatalogToTextFragmentId">The unique id of the match to confirm</param>
        /// <returns></returns>
                    public V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId(uint iaaEditionCatalogToTextFragmentId) 
                        : base() { }
                }
        
	}

            public static partial class DELETE
            {
        

                public class V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId
                : RequestObject<EmptyInput, EmptyOutput, DeleteDTO>
                {
                    /// <summary>
        ///     Remove an existing imaged object and text fragment match, which is not correct
        /// </summary>
        /// <param name="iaaEditionCatalogToTextFragmentId">The unique id of the match to confirm</param>
        /// <returns></returns>
                    public V1_Catalogue_ConfirmMatch_IaaEditionCatalogToTextFragmentId(uint iaaEditionCatalogToTextFragmentId) 
                        : base() { }
                }
        
	}

}
