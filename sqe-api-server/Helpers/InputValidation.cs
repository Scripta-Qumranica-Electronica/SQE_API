using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetTopologySuite.IO;
using NetTopologySuite.Simplify;
using Newtonsoft.Json;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Helpers
{
    public static class GeometryValidation
    {
        // You must DllImport before each external function imported (beware the paths)
        [DllImport(
            "geo_repair_polygon",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl
        )]
        private static extern BinaryData repair_wkb(IntPtr data, UIntPtr len);

        [DllImport(
            "geo_repair_polygon",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl
        )]
        private static extern void c_bin_data_free(BinaryData bin_data);

        [DllImport(
            "geo_repair_polygon",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl
        )]
        private static extern IntPtr repair_wkt(IntPtr wkt);

        [DllImport(
            "geo_repair_polygon",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl
        )]
        private static extern void c_char_free(IntPtr ptr);

        /// <summary>
        ///     The validator checks that the transformMatrix is indeed valid JSON that can be successfully
        ///     parsed into the SQE.SqeHttpApi.DataAccess.Models.TransformMatrix class.
        /// </summary>
        /// <param name="transformMatrix">A JSON string with a transform matrix object.</param>
        /// <returns></returns>
        public static bool ValidateTransformMatrix(string transformMatrix)
        {
            try
            {
                // Test that the string is valid JSON that can be parsed into a valid instance of the TransformMatrix class.
                _ = JsonConvert.DeserializeObject<TransformMatrix>(transformMatrix);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Validate that the wkt polygon is indeed correct. If it is not, the method will
        ///     throw an error. When the fix parameter is set to true, it will try to repair it and
        ///     send the repaired version of the polygon back with the error.  If the polygon cannot be
        ///     repaired at all, then a descriptive error about the polygon is thrown.
        /// </summary>
        /// <param name="wktPolygon">A wkt polygon</param>
        /// <param name="entityName">The type of object being validated (used in formulating useful exception error messages)</param>
        /// <param name="fix">
        ///     Invalid polygons always throw an error, this flag determines whether to try to return a repaired
        ///     version of the polygon with that error
        /// </param>
        /// <returns>A WKT string with the cleaned polygon</returns>
        public static async Task<string> ValidatePolygonAsync(string wktPolygon, string entityName, bool fix = false)
        {
            // Wrap the private validation method in a Task so we can asynchronously call this non-trivial method
            // But see the docs: https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices?view=aspnetcore-3.1
            // Those docs explicitly warn against do `await Task.Run`.
            return await Task.Run(() => _validatePolygon(wktPolygon, entityName, fix));
        }

        /// <summary>
        ///     Private method to validate that the wkt polygon is indeed correct. If it is not, the method will
        ///     throw an error. When the fix parameter is set to true, it will try to repair it and
        ///     send the repaired version of the polygon back with the error.  If the polygon cannot be
        ///     repaired at all, then a descriptive error about the polygon is thrown.
        /// </summary>
        /// <param name="wktPolygon">A wkt polygon</param>
        /// <param name="entityName">The type of object being validated (used in formulating useful exception error messages)</param>
        /// <param name="fix">
        ///     Invalid polygons always throw an error, this flag determines whether to try to return a repaired
        ///     version of the polygon with that error
        /// </param>
        /// <returns>A WKT string with the cleaned polygon</returns>
        /// <exception cref="StandardExceptions.InputDataRuleViolationException"></exception>
        private static string _validatePolygon(string wktPolygon, string entityName, bool fix)
        {
            var wkr = new WKTReader();
            // Bail immediately on null/blank input
            if (string.IsNullOrEmpty(wktPolygon))
                throw new StandardExceptions.InputDataRuleViolationException("The submitted WKT POLYGON is empty");

            // Try loading the polygon
            try
            {
                var polygon = wkr.Read(wktPolygon);
                // If it is valid, return it
                if (polygon.IsValid)
                {
                    // Remove any completely unnecessary points
                    var simplifier = new DouglasPeuckerSimplifier(polygon) { DistanceTolerance = 0 };
                    return simplifier.GetResultGeometry().ToString();
                }

                // It is invalid, but could be repaired as a binary representation
                // Throw an error if no request to fix it has been made
                if (!fix)
                    throw new StandardExceptions.InputDataRuleViolationException(
                        "The submitted WKT POLYGON is invalid, try using the API's repair-wkt-polygon endpoint to fix it");

                // Try repairing the binary version of the polygon
                var wkb_in = polygon.AsBinary(); // Get the binary data
                // Instantiate the variable to process the response from the FFI
                var bin = new BinaryData();

                try
                {
                    var pinnedArray = GCHandle.Alloc(wkb_in, GCHandleType.Pinned);
                    var unmanagedWkbIn = pinnedArray.AddrOfPinnedObject();
                    bin = repair_wkb(unmanagedWkbIn, (UIntPtr)wkb_in.Length);
                    pinnedArray.Free();

                    // The FFI function repair_wkb returns a null pointer with a 0 length if it could not repair the poly.
                    // For safety throw on either.  In fact, check for a length less than 21, because 21 bytes is the
                    // length of the smallest possible valid WKB (a POINT geometry). 
                    if ((int)bin.len < 21 || bin.data == IntPtr.Zero)
                        throw new StandardExceptions.InputDataRuleViolationException(
                            "The submitted WKT POLYGON is invalid and cannot be repaired");

                    // Parse the returned binary data
                    var wkbData = new byte[(int)bin.len];
                    Marshal.Copy(bin.data, wkbData, 0, (int)bin.len);

                    // Dear possible future reader, we have decided to do this marshalling the safe way.
                    // Should you find that this is causing unacceptable memory pressure and/or latency, then
                    // the returned binary data can be read directly in an unsafe way.
                    // The procedure is as follows
                    /*
                    // Map the response of the FFI into a Span
                    Span<byte> wkbData;
                    unsafe 
                    {
                        wkbData = new Span<byte>(bin.data.ToPointer(), (int)bin.len);
                    }
                    // You will then need to cast the Span<byte> to a byte[] to read it as WKB data.
                    */

                    // Read the binary data into a Net Topology geometry and get the WKT representation
                    var wkbReader = new WKBReader();
                    // Completely unnecessary points
                    var simplifier = new DouglasPeuckerSimplifier(wkbReader.Read(wkbData)) { DistanceTolerance = 0 };

                    // Free the data allocated by Rust (we do it this way since we do not know the length of the response,
                    // so it would be just a guess if we passed Rust memory managed by C#)
                    c_bin_data_free(bin);


                    return simplifier.GetResultGeometry().ToString();
                }
                catch
                {
                    throw new StandardExceptions.InputDataRuleViolationException(
                        "The submitted WKT POLYGON is invalid and cannot be repaired");
                }
            }
            catch
            {
                // The polygon could not be loaded, so throw an error if no request to fix it has been made
                if (!fix)
                    throw new StandardExceptions.InputDataRuleViolationException(
                        "The submitted WKT POLYGON is invalid, try using the API's repair-wkt-polygon endpoint to fix it");
            }

            // Try repairing as a WKT (there may have been some text formatting errors)
            var repairedWkt = repair_wkt(Marshal.StringToHGlobalAnsi(_repairPolygon(wktPolygon)));
            var returnPoly = Marshal.PtrToStringAnsi(repairedWkt);
            c_char_free(repairedWkt); // We need to let Rust free the memory it was using
            if (string.IsNullOrEmpty(returnPoly) || returnPoly == "INVALIDGEOMETRY")
                throw new StandardExceptions.InputDataRuleViolationException(
                    "The submitted WKT POLYGON is invalid and cannot be repaired");

            // Remove any completely unnecessary points
            var simplified = new DouglasPeuckerSimplifier(wkr.Read(returnPoly)) { DistanceTolerance = 0 };
            return simplified.GetResultGeometry().ToString();
        }

        /// <summary>
        ///     This makes a quick pass of the geometry and fixes unclosed polygons as well
        ///     as trying to fix any simple text formatting errors.
        /// </summary>
        /// <param name="wkt">A Wkt Polygon string</param>
        /// <returns></returns>
        /// <exception cref="StandardExceptions.InputDataRuleViolationException"></exception>
        private static string _repairPolygon(string wkt)
        {
            var asymmetricNesting = new Regex(@"\),\s(?!\()");
            var wktSplit = new Regex(@"\)\s?,\s?\(");

            // Check for errors with the nesting of parentheses in the polygon geometry, we don't really know how to fix those
            if (asymmetricNesting.Matches(wkt).Count > 0)
                throw new StandardExceptions.InputDataRuleViolationException(
                    "The submitted POLYGON has an improperly nested ring");

            // We break the string into the individual paths the loop over them, repairing each path as we go.
            var polyStrings = wktSplit.Split(wkt).Select(z =>
            {
                // Strip extraneous characters
                var saniString = z.Replace("POLYGON", "")
                    .Replace("MULTI", "")
                    .Replace("(", "")
                    .Replace(")", "");
                var coords = saniString.Split(",").ToList();

                // Make sure the path is explicitly closed
                if (coords.First() != coords.Last())
                    coords.Add(coords.First());

                return $"({string.Join(",", coords)})";
            }).ToList();

            return $"POLYGON({string.Join(",", polyStrings)})";
        }

        // You must declare the layout of your C struct (this is for any array)
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct BinaryData
        {
            public UIntPtr len;
            public IntPtr data;
        }
    }
}