using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public static partial class V1
        {
            public static partial class Editions
            {
                public static partial class EditionId
                {
                    public static partial class Lines
                    {
                        public static partial class LineId
                        {
                            public class Null : LineRequestObject<EmptyInput, LineTextDTO>
                            {
                                /// <summary>
                                ///     Request a listing of all editions available to the user
                                /// </summary>
                                public Null(uint editionId, uint lineId) : base(editionId, lineId, null) { }
                            }
                        }
                    }

                    public static partial class TextFragments
                    {
                        public class Null : EditionRequestObject<EmptyInput, TextFragmentDataListDTO>
                        {
                            /// <summary>
                            ///     Request a listing of all text fragments belonging to an edition
                            /// </summary>
                            /// <param name="editionId">The edition to search for text fragments</param>
                            public Null(uint editionId) : base(editionId, null) { }
                        }

                        public static partial class TextFragmentId
                        {
                            public class Null : TextFragmentRequestObject<EmptyInput, TextEditionDTO>
                            {
                                /// <summary>
                                ///     Request a specific text fragment from a specific edition
                                /// </summary>
                                /// <param name="editionId">The edition to search for the text fragment</param>
                                /// <param name="textFragmentId">The desired text fragment</param>
                                public Null(uint editionId, uint textFragmentId) : base(editionId, textFragmentId, null) { }
                            }

                            public class Lines : TextFragmentRequestObject<EmptyInput, LineDataListDTO>
                            {
                                /// <summary>
                                ///     Request a listing of all lines in a text fragment of an edition
                                /// </summary>
                                /// <param name="editionId">The edition to search for the text fragment</param>
                                /// <param name="textFragmentId">The text fragment to search for lines</param>
                                public Lines(uint editionId, uint textFragmentId) : base(editionId, textFragmentId, null) { }
                            }
                        }
                    }
                }
            }
        }
    }

    public static partial class Post
    {
        public static partial class V1
        {
            public static partial class Editions
            {
                public static partial class EditionId
                {
                    public static partial class TextFragments
                    {
                        public class Null : EditionRequestObject<CreateTextFragmentDTO, TextFragmentDataDTO>
                        {
                            /// <summary>
                            ///     Add a new tet fragment to an edition
                            /// </summary>
                            /// <param name="editionId">The edition to add the text fragment to</param>
                            /// <param name="payload">The details of the new text fragment</param>
                            public Null(uint editionId, CreateTextFragmentDTO payload) : base(editionId, payload) { }
                        }
                    }
                }
            }
        }
    }

    public static partial class Put
    {
        public static partial class V1
        {
            public static class Text
            {
            }
        }
    }

    public static partial class Delete
    {
        public static partial class V1
        {
            public static class Text
            {
            }
        }
    }
}