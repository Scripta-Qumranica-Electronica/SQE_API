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

    }

    public static partial class Put
    {

    }

    public static partial class Delete
    {

    }
}