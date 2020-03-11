using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{

    public static partial class Post
    {
        public class V1_Utils_ValidateWkt : RequestObject<WktPolygonDTO, EmptyOutput>
        {
            /// <summary>
            /// Validate a WKT polygon
            /// </summary>
            /// <param name="payload">A DTO with a WKT polygon to be validated</param>
            public V1_Utils_ValidateWkt(WktPolygonDTO payload) : base(payload)
            {
            }
        }
    }
}