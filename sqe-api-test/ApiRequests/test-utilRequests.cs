
using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{


            public static partial class POST
            {
        

                public class V1_Utils_RepairWktPolygon
                : RequestObject<WktPolygonDTO, WktPolygonDTO, WktPolygonDTO>
                {
                    /// <summary>
        ///     Checks a WKT polygon to ensure validity. If the polygon is invalid,
        ///     it attempts to construct a valid polygon that matches the original
        ///     as closely as possible.
        /// </summary>
        /// <param name="payload">JSON object with the WKT polygon to validate</param>
                    public V1_Utils_RepairWktPolygon(WktPolygonDTO payload) 
                        : base(payload) { }
                }
        
	}

}
