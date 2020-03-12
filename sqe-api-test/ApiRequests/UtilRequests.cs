using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{

    public static partial class Post
    {
        public class V1_Utils_RepairWktPolygon : RequestObject<WktPolygonDTO, WktPolygonDTO>
        {
            /// <summary>
            /// Validate a WKT polygon
            /// </summary>
            /// <param name="payload">A DTO with a WKT polygon to be validated</param>
            public V1_Utils_RepairWktPolygon(WktPolygonDTO payload) : base(payload)
            {
            }
        }
    }
}