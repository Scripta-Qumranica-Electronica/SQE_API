using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_Editions_EditionId_Lines_LineId : LineRequestObject<EmptyInput, LineTextDTO>
        {
            /// <summary>
            ///     Request a listing of all editions available to the user
            /// </summary>
            public V1_Editions_EditionId_Lines_LineId(uint editionId, uint lineId) : base(editionId, lineId, null)
            {
            }
        }

        public class V1_Editions_EditionId_TextFragments : EditionRequestObject<EmptyInput, TextFragmentDataListDTO>
        {
            /// <summary>
            ///     Request a listing of all text fragments belonging to an edition
            /// </summary>
            /// <param name="editionId">The edition to search for text fragments</param>
            public V1_Editions_EditionId_TextFragments(uint editionId) : base(editionId)
            {
            }
        }

        public class V1_Editions_EditionId_TextFragments_TextFragmentId
            : TextFragmentRequestObject<EmptyInput, TextEditionDTO>
        {
            /// <summary>
            ///     Request a specific text fragment from a specific edition
            /// </summary>
            /// <param name="editionId">The edition to search for the text fragment</param>
            /// <param name="textFragmentId">The desired text fragment</param>
            public V1_Editions_EditionId_TextFragments_TextFragmentId(uint editionId, uint textFragmentId) : base(
                editionId,
                textFragmentId,
                null
            )
            {
            }
        }

        public class V1_Editions_EditionId_TextFragments_TextFragmentId_Lines
            : TextFragmentRequestObject<EmptyInput, LineDataListDTO>
        {
            /// <summary>
            ///     Request a listing of all lines in a text fragment of an edition
            /// </summary>
            /// <param name="editionId">The edition to search for the text fragment</param>
            /// <param name="textFragmentId">The text fragment to search for lines</param>
            public V1_Editions_EditionId_TextFragments_TextFragmentId_Lines(uint editionId, uint textFragmentId) : base(
                editionId,
                textFragmentId,
                null
            )
            {
            }
        }
    }

    public static partial class Post
    {
        public class V1_Editions_EditionId_TextFragments
            : EditionRequestObject<CreateTextFragmentDTO, TextFragmentDataDTO>
        {
            /// <summary>
            ///     Add a new tet fragment to an edition
            /// </summary>
            /// <param name="editionId">The edition to add the text fragment to</param>
            /// <param name="payload">The details of the new text fragment</param>
            public V1_Editions_EditionId_TextFragments(uint editionId, CreateTextFragmentDTO payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("createTextFragment");
            }
        }
    }

    public static partial class Put
    {
        public class V1_Editions_EditionId_TextFragments_TextFragmentId
            : TextFragmentRequestObject<UpdateTextFragmentDTO, TextFragmentDataDTO>
        {
            /// <summary>
            ///     Alter a text fragment in an edition
            /// </summary>
            /// <param name="editionId">The edition to add the text fragment to</param>
            /// <param name="textFragmentId">The text fragment to alter</param>
            /// <param name="payload">The details of the new text fragment</param>
            public V1_Editions_EditionId_TextFragments_TextFragmentId(uint editionId, uint textFragmentId, UpdateTextFragmentDTO payload) : base(
                editionId,
                textFragmentId,
                payload
            )
            {
                listenerMethod.Add("updateTextFragment");
            }
        }
    }

    public static partial class Delete
    {
    }
}