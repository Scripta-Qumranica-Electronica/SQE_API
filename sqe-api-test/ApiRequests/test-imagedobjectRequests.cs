
using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


            public static partial class GET
            {
        

                public class V1_ImagedObjects_ImagedObjectId
                : RequestObject<EmptyInput, SimpleImageListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides information for the specified imaged object.
        /// </summary>
        /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
                    public V1_ImagedObjects_ImagedObjectId(string imagedObjectId) 
                        : base() { }
                }
        

                public class V1_Editions_EditionId_ImagedObjects_ImagedObjectId
                : EditionRequestObject<EmptyInput, ImagedObjectDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides information for the specified imaged object related to the specified edition, can include images and also
        ///     their masks with optional.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="imagedObjectId">Unique Id of the desired object from the imaging Institution</param>
        /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
                    public V1_Editions_EditionId_ImagedObjects_ImagedObjectId(uint editionId,string imagedObjectId,List<string> optional = null) 
                        : base(editionId, optional) { }
                }
        

                public class V1_Editions_EditionId_ImagedObjects
                : EditionRequestObject<EmptyInput, ImagedObjectListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides a listing of imaged objects related to the specified edition, can include images and also their masks with
        ///     optional.
        /// </summary>
        /// <param name="editionId">Unique Id of the desired edition</param>
        /// <param name="optional">Set 'artefacts' to receive related artefact data and 'masks' to include the artefact masks</param>
                    public V1_Editions_EditionId_ImagedObjects(uint editionId,List<string> optional = null) 
                        : base(editionId, optional) { }
                }
        

                public class V1_ImagedObjects_Institutions
                : RequestObject<EmptyInput, ImageInstitutionListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides a list of all institutional image providers.
        /// </summary>
                    public V1_ImagedObjects_Institutions() 
                        : base() { }
                }
        

                public class V1_ImagedObjects_Institutions_Institution
                : RequestObject<EmptyInput, InstitutionalImageListDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides a list of all institutional image providers.
        /// </summary>
                    public V1_ImagedObjects_Institutions_Institution(string institution) 
                        : base() { }
                }
        

                public class V1_ImagedObjects_ImagedObjectId_TextFragments
                : RequestObject<EmptyInput, ImagedObjectTextFragmentMatchDTO, EmptyOutput>
                {
                    /// <summary>
        ///     Provides a list of all text fragments that should correspond to the imaged object.
        /// </summary>
        /// <param name="imagedObjectId">Id of the imaged object</param>
        /// <returns></returns>
                    public V1_ImagedObjects_ImagedObjectId_TextFragments(string imagedObjectId) 
                        : base() { }
                }
        
	}

}
