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
                                public Null(uint editionId, uint lineId) : base(editionId, lineId, null)
                                {
                                    requestVerb = HttpMethod.Get;
                                    requestPath = "v1/editions/{editionId}/lines/{lineId}";
                                }
                            }
                        }
                    }

                    public static partial class TextFragments
                    {
                        public class Null : EditionRequestObject<EmptyInput, TextFragmentDataListDTO>
                        {
                            /// <summary>
                            ///     Request a listing of all editions available to the user
                            /// </summary>
                            public Null(uint editionId) : base(editionId, null)
                            {
                                requestVerb = HttpMethod.Get;
                                requestPath = "v1/editions/{editionId}/text-fragments";
                            }
                        }

                        public static partial class TextFragmentId
                        {
                            public class Null : TextFragmentRequestObject<EmptyInput, TextEditionDTO>
                            {
                                /// <summary>
                                ///     Request a listing of all editions available to the user
                                /// </summary>
                                public Null(uint editionId, uint textFragmentId) : base(editionId, textFragmentId, null)
                                {
                                    requestVerb = HttpMethod.Get;
                                    requestPath = "v1/editions/{editionId}/text-fragments/{textFragmentId}";
                                }
                            }

                            public class Lines : TextFragmentRequestObject<EmptyInput, LineDataListDTO>
                            {
                                /// <summary>
                                ///     Request a listing of all editions available to the user
                                /// </summary>
                                public Lines(uint editionId, uint textFragmentId) : base(editionId, textFragmentId, null)
                                {
                                    requestVerb = HttpMethod.Get;
                                    requestPath = "v1/editions/{editionId}/text-fragments/{textFragmentId}/lines";
                                }
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
                            ///     Request a listing of all editions available to the user
                            /// </summary>
                            public Null(uint editionId, CreateTextFragmentDTO payload) : base(editionId, payload)
                            {
                                requestVerb = HttpMethod.Get;
                                requestPath = "/v1/editions/{editionId}/text-fragments";
                            }
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