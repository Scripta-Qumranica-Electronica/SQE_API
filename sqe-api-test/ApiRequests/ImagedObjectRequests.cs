using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_ImagedObjects_Institutions : RequestObject<EmptyInput, ImageInstitutionListDTO, EmptyOutput>
        {
            private V1_ImagedObjects_Institutions() : base(null)
            {
            }
        }

        public class V1_ImagedObjects_ImagedObjectId_TextFragments
            : ImagedObjectRequestObject<EmptyInput, List<ImagedObjectTextFragmentMatchDTO>, EmptyOutput>
        {
            public V1_ImagedObjects_ImagedObjectId_TextFragments(string imagedObjectId) : base(imagedObjectId)
            {
            }
        }

        public class V1_Editions_EditionId_ImagedObjects
            : EditionRequestObject<EmptyInput, ImagedObjectListDTO, EmptyOutput>
        {
            public V1_Editions_EditionId_ImagedObjects(uint editionId,
                string imagedObjectId,
                List<string> optional = null)
                : base(editionId, optional)
            {
            }
        }

        public class V1_Editions_EditionId_ImagedObjects_ImagedObjectId
            : EditionImagedObjectRequestObject<EmptyInput, ImagedObjectDTO, EmptyOutput>
        {
            public V1_Editions_EditionId_ImagedObjects_ImagedObjectId(uint editionId,
                string imagedObjectId,
                List<string> optional = null)
                : base(editionId, imagedObjectId, optional)
            {
            }
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