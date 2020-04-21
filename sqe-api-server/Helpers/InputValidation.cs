using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Helpers
{
    public static class GeometryValidation
    {
        // You must declare the layout of your C struct (this is for any array)
        [StructLayout(LayoutKind.Sequential)]
        public struct BinaryData
        {
            public UIntPtr data;
            public UIntPtr len;
        }

        // You must DllImport before each external function imported (beware the paths)
        [DllImport(
            "/home/bronson/workspace/SQE_API/sqe-api-server/bin/Debug/netcoreapp3.1/libgeo_repair_polygon.so",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl
        )]
        private static extern unsafe BinaryData repair_wkb(UIntPtr data, UIntPtr len);

        [DllImport(
            "/home/bronson/workspace/SQE_API/sqe-api-server/bin/Debug/netcoreapp3.1/libgeo_repair_polygon.so",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl
        )]
        private static extern unsafe void c_bin_data_free(BinaryData bin_data);

        [DllImport(
            "/home/bronson/workspace/SQE_API/sqe-api-server/bin/Debug/netcoreapp3.1/libgeo_repair_polygon.so",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl
        )]
        private static extern unsafe IntPtr repair_wkt(IntPtr wkt);

        [DllImport(
            "/home/bronson/workspace/SQE_API/sqe-api-server/bin/Debug/netcoreapp3.1/libgeo_repair_polygon.so",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl
        )]
        private static extern unsafe void c_char_free(IntPtr ptr);

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
        /// Validate that the wkt polygon is indeed correct. If it is not, the method will
        /// throw an error. When the fix parameter is set to true, it will try to repair it and
        /// send the repaired version of the polygon back with the error.  If the polygon cannot be
        /// repaired at all, then a descriptive error about the polygon is thrown.
        /// </summary>
        /// <param name="wktPolygon">A wkt polygon</param>
        /// <param name="entityName">The type of object being validated (used in formulating useful exception error messages)</param>
        /// <param name="fix">Invalid polygons always throw an error, this flag determines whether to try to return a repaired
        /// version of the polygon with that error</param>
        /// <returns>A WKT string with the cleaned polygon</returns>
        public static async Task<string> ValidatePolygonAsync(string wktPolygon, string entityName, bool fix = false)
        {
            // Wrap the private validation method in a Task so we can asynchronously call this non-trivial method
            return await Task.Run(() => _validatePolygon(wktPolygon, entityName, fix));
        }

        /// <summary>
        /// Private method to validate that the wkt polygon is indeed correct. If it is not, the method will
        /// throw an error. When the fix parameter is set to true, it will try to repair it and
        /// send the repaired version of the polygon back with the error.  If the polygon cannot be
        /// repaired at all, then a descriptive error about the polygon is thrown.
        /// </summary>
        /// <param name="wktPolygon">A wkt polygon</param>
        /// <param name="entityName">The type of object being validated (used in formulating useful exception error messages)</param>
        /// <param name="fix">Invalid polygons always throw an error, this flag determines whether to try to return a repaired
        /// version of the polygon with that error</param>
        /// <returns>A WKT string with the cleaned polygon</returns>
        /// <exception cref="StandardExceptions.InputDataRuleViolationException"></exception>
        private static string _validatePolygon(string wktPolygon, string entityName, bool fix)
        {
            var wkr = new WKTReader();
            // Bail immediately on null/blank input
            if (string.IsNullOrEmpty(wktPolygon))
                throw new StandardExceptions.InputDataRuleViolationException("The submitted WKT POLYGON is empty");

            var polygon = default(Geometry);
            // Try loading the polygon
            try
            {
                polygon = wkr.Read(wktPolygon);
                // If it is valid, return it
                if (polygon.IsValid)
                    return polygon.Normalized().ToString(); // Always normalize the polygon

                // It is invalid, but could be repaired as a binary representation
                // Throw an error if no request to fix it has been made
                if (!fix)
                    throw new StandardExceptions.InputDataRuleViolationException("The submitted WKT POLYGON is invalid, try using the API's repair-wkt-polygon endpoint to fix it");

                // Try repairing the binary version of the polygon
                var wkb_in = polygon.AsBinary(); // Get the binary data
                // Instantiate the variable to process the response from the FFI
                var bin = new BinaryData();
                var wkbData = new Span<byte>();

                // Reading and writing pointers is unsafe
                try
                {
                    unsafe
                    {
                        // Create a pointer to the binary data (it only needs to live till the FFI function returns)
                        fixed (byte* data = &wkb_in[0])
                        {
                            bin = repair_wkb((UIntPtr)data, (UIntPtr)wkb_in.Length);
                        }

                        // The FFI function repair_wkb returns a null pointer with a 0 length if it could not repair the poly.
                        // For safety throw on either.  In fact, check for a length less than 21, because 21 bytes is the
                        // length of the smallest possible valid WKB (a POINT geometry). 
                        if ((int)bin.len < 21 || bin.data == UIntPtr.Zero)
                            throw new StandardExceptions.InputDataRuleViolationException(
                                "The submitted WKT POLYGON is invalid and cannot be repaired");

                        // Map the response of the FFI into a Span
                        wkbData = new Span<byte>(bin.data.ToPointer(), (int)bin.len);

                        // Read the binary data into a Net Topology geometry and get the WKT representation
                        var wkbReader = new WKBReader();
                        var repairedPoly = wkbReader.Read(wkbData.ToArray()).AsText();

                        // Free the data allocated by Rust (we do it this way since we do not know the length of the response,
                        // so it would be just a guess if we passed Rust memory managed by C#)
                        c_bin_data_free(bin);

                        return repairedPoly;
                    }
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
                    throw new StandardExceptions.InputDataRuleViolationException("The submitted WKT POLYGON is invalid, try using the API's repair-wkt-polygon endpoint to fix it");
            }

            // Try repairing as a WKT (there may have been some text formatting errors)
            var repairedWkt = repair_wkt(Marshal.StringToHGlobalAnsi(_repairPolygon(wktPolygon)));
            var returnPoly = Marshal.PtrToStringAnsi(repairedWkt);
            c_char_free(repairedWkt); // We need to let Rust free the memory it was using
            if (string.IsNullOrEmpty(returnPoly) || returnPoly == "INVALIDGEOMETRY")
                throw new StandardExceptions.InputDataRuleViolationException("The submitted WKT POLYGON is invalid, try using the API's repair-wkt-polygon endpoint to fix it");
            return returnPoly;
        }

        /// <summary>
        /// This makes a quick pass of the geometry and fixes unclosed polygons as well
        /// as trying to fix any simple text formatting errors.
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
                throw new StandardExceptions.InputDataRuleViolationException("The submitted POLYGON has an improperly nested ring");

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

            return ($"POLYGON({string.Join(",", polyStrings)})");
        }
    }
}