using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO;
using NetTopologySuite.LinearReferencing;
using NetTopologySuite.Operation.Buffer.Validate;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Operation.Overlay;
using Newtonsoft.Json;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Helpers
{
    public static class GeometryValidation
    {

        private static readonly WKTReader _wkr = new WKTReader();
        private static readonly Regex _asymmetricNesting = new Regex(@"\),\s(?!\()");
        private static readonly Regex _wktSplit = new Regex(@"\)\s?,\s?\(");

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
            var fixedPoly = false;
            // Bail immediately on null/blank input
            if (string.IsNullOrEmpty(wktPolygon))
                return null;

            Geometry polygon;
            // Load Polygon
            try
            {
                polygon = _wkr.Read(wktPolygon);
            }
            catch
            {
                if (!fix)
                    throw new StandardExceptions.InputDataRuleViolationException("The submitted WKT POLYGON is invalid, try using the repair API's validate-wkt endpoint to fix it");

                polygon = _repairPolygon(wktPolygon);
                fixedPoly = true;
            }

            if (!polygon.IsValid)
            {
                if (!fix)
                    throw new StandardExceptions.InputDataRuleViolationException("The submitted WKT POLYGON is invalid, try using the repair API's validate-wkt endpoint to fix it");

                polygon = _repairPolygon(wktPolygon);
                fixedPoly = true;
            }

            if (fixedPoly)
                throw new StandardExceptions.MalformedDataException("wktPolygon", polygon.Normalized().ToString());

            return polygon.Normalized().ToString();
        }

        /// <summary>
        /// This makes a quick pass of the geometry and fixes unclosed polygons as well
        /// as trying to fix improperly ordered path elements within the Polygon
        /// </summary>
        /// <param name="wkt">A Wkt Polygon string</param>
        /// <returns></returns>
        /// <exception cref="StandardExceptions.InputDataRuleViolationException"></exception>
        private static Geometry _repairPolygon(string wkt)
        {
            if (_asymmetricNesting.Matches(wkt).Count > 0)
                throw new StandardExceptions.InputDataRuleViolationException("The submitted POLYGON has an improperly nested ring");

            // We break the string into the individual paths the loop over them, repairing each path as we go.
            var polyStrings = _wktSplit.Split(wkt).Select(z =>
            {
                // Strip extraneous character
                var saniString = z.Replace("POLYGON", "")
                    .Replace("MULTI", "")
                    .Replace("(", "")
                    .Replace(")", "");
                var coords = saniString.Split(",").ToList();

                // Make sure the path is explicitly closed
                if (coords.First() != coords.Last())
                    coords.Add(coords.First());

                // Convert the path into a single wkt Polygon
                var singlePath = @$"POLYGON(({string.Join(",", coords)}))";

                // Read the single path into net topology suite
                var geom = _wkr.Read(singlePath);

                // Check if geom is valid
                if (geom.IsValid) return geom;
                var bufferedGeom = geom.Buffer(0);
                if (!bufferedGeom.IsValid
                    || !bufferedGeom.Area.Equals(geom.Area))
                {
                    // We have a function to fix self-intersecting polys,
                    // Should we check here also for multipolys and
                    // run the algorithm to fix that?
                    if (geom.GeometryType == "MultiPolygon")
                        geom = _repairMultiPolygon(geom);
                    else
                        geom = _repairSelfInterectingPolygon(geom);

                    // If still invalid, we give up
                    if (!geom.IsValid)
                        throw new StandardExceptions.InputDataRuleViolationException($"The new repair method couldn't fix the poly: {geom}");
                }
                else
                {
                    geom = bufferedGeom;
                }

                // The Polygon is now valid
                return geom;
            }).ToList();

            // Try rebuilding the polygon from its individual paths, grab the first path
            var fullGeom = polyStrings[0];
            for (var i = 1; i < polyStrings.Count(); i++)
            {
                // Grab a new path
                var newGeom = polyStrings[i];
                // Create an overlay operation
                var op = new OverlayOp(fullGeom, newGeom);
                // Perform the overlay
                fullGeom = op.GetResultGeometry(SpatialFunction.SymDifference);
            }

            // If the result is a multipolygon try to repair that
            if (fullGeom.GetType() == typeof(MultiPolygon))
                fullGeom = _repairMultiPolygon(fullGeom);

            // Throw an error if we still couldn't compose a single polygon from the input 
            if (fullGeom.GetType() != typeof(Polygon))
                throw new StandardExceptions.InputDataRuleViolationException($"The new repair method couldn't fix the poly, it does not appear possible to compose it into a single POLYGON: {fullGeom}");

            return fullGeom;
        }

        /// <summary>
        /// Fixes a self-intersecting polygon by changing the point of intersection to
        /// very thin open area. 
        /// </summary>
        /// <param name="poly">The self-intersecting polygon to fix</param>
        /// <returns>A repaired version of the self-intersecting polygon</returns>
        private static Polygon _repairSelfInterectingPolygon(Geometry poly)
        {
            // Separate the geom into its discrete polygon entities
            var discretePolys = GeometryExtracter.Extract<Polygon>(poly);
            // If there is only one, our job is done
            if (discretePolys.Count != 1)
                throw new StandardExceptions.ImproperInputDataException("polygon");


            if (discretePolys[0].IsValid)
                return (Polygon)discretePolys[0];

            // Probably the polygon is invalid due to self intersection
            // Let's grab every outer line in the polygon
            var coords = poly.Boundary.Coordinates;
            var lines = coords.Select(
                (t, i) => new LineString(new[] { t, coords[i + 1 == coords.Length ? 0 : i + 1] })
            ).ToList();

            // Now group the lines into linestrings that start and stop at the points of intersection
            var intersectedLines = new List<LineString>();
            var points = new List<Coordinate>();
            foreach (var line in lines)
            {
                if (!points.Any())
                    points.Add(line.Coordinates[0]);

                foreach (var compLine in lines)
                {
                    if (line.Coordinates.Contains(compLine.Coordinates[0])
                        || line.Coordinates.Contains(compLine.Coordinates[1])
                        || !line.Intersects(compLine))
                        continue;

                    var point = line.Intersection(compLine);
                    points.Add(point.Coordinate);
                    intersectedLines.Add(new LineString(points.ToArray()));
                    points = new List<Coordinate>();
                    points.Add(point.Coordinate);
                }

                points.Add(line.Coordinates[1]);
                if (lines.Last().ToText() == line.ToText())
                    intersectedLines.Add(new LineString(points.ToArray()));
            }

            // Build the polygon out of the line segments
            var fixedIntersecting = intersectedLines.First().Coordinates.ToList();
            intersectedLines.RemoveAt(0);
            var counterpart = intersectedLines.Where(
                x => x.Coordinates.Last().Equals2D(fixedIntersecting.Last())
            ).LastOrDefault();

            while (counterpart != null)
            {
                intersectedLines.Remove(counterpart);
                // every other matched line should be reversed
                var rev = intersectedLines.Count % 2 == 0 ? counterpart.Coordinates.ToList() : counterpart.Coordinates.Reverse().ToList();
                var intersectionPoint = rev.First();

                // Delete the point of intersection from both line strings
                rev.RemoveAt(0);
                fixedIntersecting.RemoveAt(fixedIntersecting.Count - 1);

                //calculate the replacement coordinates for the point of intersection
                var distCalc = new PointPairDistance();
                distCalc.Initialize(intersectionPoint, fixedIntersecting.Last());
                var point1Dist = distCalc.Distance;
                distCalc.Initialize(intersectionPoint, rev.First());
                var point2Dist = distCalc.Distance;
                var newMidPoint1 = LinearLocation.PointAlongSegmentByFraction(intersectionPoint, fixedIntersecting.Last(), 0.1 * (1 / point1Dist));
                var newMidPoint2 = LinearLocation.PointAlongSegmentByFraction(intersectionPoint, rev.First(), 0.1 * (1 / point2Dist));

                // Add the replacements for the intersection point and concatenate the following line
                fixedIntersecting.Add(newMidPoint1);
                fixedIntersecting.Add(newMidPoint2);
                fixedIntersecting.AddRange(rev);

                // Find the next line segment to attach
                counterpart = intersectedLines.Where(
                    x => x.Coordinates.Last().Equals2D(fixedIntersecting.Last())
                ).LastOrDefault();

                // If a match is found continue building the line string
                if (counterpart != null
                    || intersectedLines.Count != 1) continue;

                // If we are down to the last segment, grab the last line segment available
                counterpart = intersectedLines.First();
            }

            // // Close the line string if necessary
            if (!fixedIntersecting.First().Equals2D(fixedIntersecting.Last()))
                fixedIntersecting.Add(fixedIntersecting.First());

            // Delete doubled endpoint
            if (fixedIntersecting.Last().Equals2D(fixedIntersecting[^2]))
                fixedIntersecting.RemoveAt(fixedIntersecting.Count - 1);

            // Return the line string as a polygon
            return new Polygon(new LinearRing(fixedIntersecting.ToArray()));
        }

        /// <summary>
        /// Repairs a self-intersecting polygon or a multipolygon by converting it
        /// into a single polygon that most closely resembles the input polygon
        /// while still remaining valid.
        /// </summary>
        /// <param name="poly">A self-intersecting polygon or a multi-polygon</param>
        /// <returns>A single valid polygon most closely approximating the input</returns>
        /// <exception cref="StandardExceptions.InputDataRuleViolationException"></exception>
        private static Polygon _repairMultiPolygon(Geometry poly)
        {
            // Separate the geom into its discrete polygon entities
            var discretePolys = GeometryExtracter.Extract<Polygon>(poly);

            // Firstly let us find pairs of the closest polygon to each polygon in cleanedPoly.
            var repairedPoly = discretePolys[0];
            var polyMovePairs = new List<int>();
            for (var i = 0; i < discretePolys.Count; ++i)
            {
                // Skip polys that we've already matched or are empty
                if (polyMovePairs.Contains(i) || discretePolys[i].Area.Equals(0))
                    continue;

                double distance = 0;
                var nearestPoly = -1;
                for (var j = 0; j < discretePolys.Count; ++j)
                {
                    // Skip the same polygon or empty polygons
                    if (j == i || discretePolys[j].Area.Equals(0))
                        continue;

                    var currentDistance = DistanceOp.Distance(discretePolys[i], discretePolys[j]);
                    // Check if this poly is closer than the closest so far
                    if (nearestPoly != -1
                        && distance < currentDistance) continue;

                    // Since this is the closest, set this as the new closest poly
                    distance = currentDistance;
                    nearestPoly = j;
                }

                // Bail out if no close poly was found
                if (nearestPoly == -1)
                    continue;

                // Now that we know the closest poly, attempt to join the two
                var points = DistanceOp.NearestPoints(discretePolys[i], discretePolys[nearestPoly]);
                var joined = _joinPolys(
                    (Polygon)discretePolys[i],
                    (Polygon)discretePolys[nearestPoly],
                    points[0],
                    points[1]
                );

                // Merge joined polys into the return poly
                repairedPoly = repairedPoly.Union(joined);

                // Record the match so we don't attempt to do it again
                polyMovePairs.Add(i);
                polyMovePairs.Add(nearestPoly);
            }

            // Throw an error if we still could not produce a valid Polygon type
            if (repairedPoly.GetType() != typeof(Polygon))
                throw new StandardExceptions.InputDataRuleViolationException($"Combined poly is invalid: {repairedPoly}");

            // If we are really valid, return the poly
            if (repairedPoly.IsValid)
                return (Polygon)repairedPoly;

            // Finally try to buffer it
            var bufferedPoly = repairedPoly.Buffer(0);
            // Throw an error if buffering changed the dimensions significantly
            if (Math.Abs(bufferedPoly.Area - repairedPoly.Area) > 1)
                throw new StandardExceptions.InputDataRuleViolationException($"");

            return (Polygon)bufferedPoly;
        }



        /// <summary>
        /// Join two polygons into a single polygon (not a multi-polygon) by altering them
        /// to intersect.
        /// </summary>
        /// <param name="poly1">First polygon</param>
        /// <param name="poly2">Second polygon</param>
        /// <param name="poly1Point">Point in first polygon that is closest to the second polygon</param>
        /// <param name="poly2Point">Point in second polygon that is closest to the first polygon</param>
        /// <returns>A single polygon union of the two input polygons</returns>
        /// <exception cref="StandardExceptions.InputDataRuleViolationException"></exception>
        private static Polygon _joinPolys(Polygon poly1, Polygon poly2, Coordinate poly1Point, Coordinate poly2Point)
        {
            // If the two closest points are the same, then we need to do something fancy
            if (poly1Point.Equals(poly2Point))
            {
                // TODO: maybe we can rewrite this to be a little better
                // We can grab each closest point, remove each from the polygon and add
                // two points instead, each a little closer to the previous/next point.
                // Then make a thin bridge joining the two sets of new points.
                var point = new Point(poly1Point.X, poly1Point.Y + 1);
                var (biggerPoly, smallerPoly) = poly1.Area > poly2.Area
                    ? (poly1, poly2)
                    : (poly2, poly1);
                // Try nudging the point in 8 cardinal directions to find a point inside the larger poly
                Point fittingPoint = null;
                // North
                if (biggerPoly.Contains(point))
                    fittingPoint = point;

                // Northeast
                point = new Point(point.X + 0.5, point.Y - 0.5);
                if (biggerPoly.Contains(point))
                    fittingPoint = point;

                // East
                point = new Point(point.X + 0.5, point.Y - 0.5);
                if (biggerPoly.Contains(point))
                    fittingPoint = point;

                // SouthEast
                point = new Point(point.X - 0.5, point.Y - 0.5);
                point = new Point(poly1Point.X, poly1Point.Y - 2);
                if (biggerPoly.Contains(point))
                    fittingPoint = point;
                point = new Point(poly1Point.X + 1, poly1Point.Y + 1);

                // South
                point = new Point(point.X - 0.5, point.Y - 0.5);
                if (biggerPoly.Contains(point))
                    fittingPoint = point;

                // SouthWest
                point = new Point(point.X - 0.5, point.Y + 0.5);
                if (biggerPoly.Contains(point))
                    fittingPoint = point;

                // West
                point = new Point(point.X - 0.5, point.Y + 0.5);
                if (biggerPoly.Contains(point))
                    fittingPoint = point;

                // NorthWest
                point = new Point(point.X + 0.5, point.Y - 0.5);
                if (biggerPoly.Contains(point))
                    fittingPoint = point;

                // What magic could make an interior point unfindable??? (it really could happen)
                if (fittingPoint == null)
                    throw new StandardExceptions.InputDataRuleViolationException($"");

                // Alter to smaller polygon so it intersects the larger one
                // TODO: find a way to do this without string manipulation
                var adjSmallerPoly = _wkr.Read(smallerPoly
                    .ToText()
                    .Replace(
                        $"{poly2Point.X} {poly2Point.Y}",
                        $"{fittingPoint.X} {fittingPoint.Y}"
                    ));

                // Join the two polygons and validate
                var joined = biggerPoly.Union(adjSmallerPoly);
                if (!joined.IsValid || joined.GetType() != typeof(Polygon))
                    throw new StandardExceptions.InputDataRuleViolationException($"");
                return (Polygon)joined;
            }

            // Apparently the two polygons just have close points, but don't share the same point.
            // So let's just swap the two closest points between the polygons and call it a day...
            // TODO: Find a way to do this without string manipulation.
            var repaired1 = _wkr.Read(poly1
                .ToText()
                .Replace(
                    $"{poly1Point.X} {poly1Point.Y}",
                    $"{poly2Point.X} {poly2Point.Y}"));
            if (!repaired1.IsValid)
            {
                // If it is invalid, that is almost certainly due to self intersection, so recurse to fix that
                repaired1 = _repairSelfInterectingPolygon(repaired1);
            }
            if (!repaired1.IsValid)
                // If we still aren't valid, then give up
                throw new StandardExceptions.InputDataRuleViolationException($"");

            var repaired2 = _wkr.Read(poly2
                .ToText()
                .Replace(
                    $"{poly2Point.X} {poly2Point.Y}",
                    $"{poly1Point.X} {poly1Point.Y}"));
            if (!repaired2.IsValid)
            {
                // If it is invalid, that is almost certainly due to self intersection, so recurse to fix that
                repaired2 = _repairSelfInterectingPolygon(repaired2);
            }
            if (!repaired2.IsValid)
                // If we still aren't valid, then give up
                throw new StandardExceptions.InputDataRuleViolationException($"");

            // Join our two new polygons, which should now intersect (but possibly might not)
            var combined = repaired1.Union(repaired2);

            // If combined is no good, give up and return the larger geometry (Buffered at 0 for safety).
            // This algorithm usually only fails when one or both polygons is really tiny or has very few points
            // (since polygons with very few points [triangle/square] are incredibly rare in our system
            // of worn material fragments and calligraphic fonts, we assume that such instances are noise). 
            if (!combined.IsValid
                || combined.GetType() != typeof(Polygon))
                return repaired1.Area > repaired2.Area
                    ? (Polygon)repaired1.Buffer(0)
                    : (Polygon)repaired2.Buffer(0);
            return (Polygon)combined;
        }
        private class LinePair
        {
            public LinePair(int line1, int? line2, int line3)
            {
                this.line1 = line1;
                this.line2 = line2;
                this.line3 = line3;
            }

            public int line1 { get; set; }
            public int? line2 { get; set; }
            public int line3 { get; set; }
        }
    }
}