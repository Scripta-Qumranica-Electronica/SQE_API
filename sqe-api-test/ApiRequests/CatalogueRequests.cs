using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class
            V1_Catalogue_ImagedObjects_ImagedObjectId_TextFragments : ImagedObjectRequestObject<EmptyInput,
                CatalogueMatchListDTO, EmptyOutput>
        {
            public V1_Catalogue_ImagedObjects_ImagedObjectId_TextFragments(string imagedObjectId) : base(imagedObjectId) { }
        }

        public class
                    V1_Catalogue_TextFragments_TextFragmentId_ImagedObjects : TextFragmentRequestObject<EmptyInput,
                        CatalogueMatchListDTO, EmptyOutput>
        {
            public V1_Catalogue_TextFragments_TextFragmentId_ImagedObjects(uint textFragmentId) : base(textFragmentId) { }
        }
    }

    public static partial class Post
    {

    }

    public static partial class Put
    {

    }

    public static partial class Delete
    {

    }
}