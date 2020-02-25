using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO;
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
        /// Validate that the wkt polygon is indeed valid. If it is not, the method will
        /// try to repair it. If the polygon cannot be repaired, then an error is thrown.
        /// The method always returns a List of wkt Polygons, since the process of repairing
        /// a geometry may result in more than one polygon.
        /// </summary>
        /// <param name="wktPolygon">A wkt polygon</param>
        /// <param name="entityName">The type of object being validated (used in formulating useful exception error messages)</param>
        /// <returns>A WKT string with the cleaned polygon</returns>
        public static async Task<string> CleanPolygonAsync(string wktPolygon, string entityName)
        {
            return await Task.Run(() => _cleanPolygon(wktPolygon, entityName));
        }

        /// <summary>
        /// Validate that the wkt polygon is indeed valid. If it is not, the method will
        /// try to repair it. If the polygon cannot be repaired, then an error is thrown.
        /// The method always returns a List of wkt Polygons, since the process of repairing
        /// a geometry may result in more than one polygon.
        /// </summary>
        /// <param name="wktPolygon">A wkt polygon</param>
        /// <param name="entityName">The type of object being validated (used in formulating useful exception error messages)</param>
        /// <returns>A WKT string with the cleaned polygon</returns>
        /// <exception cref="StandardExceptions.InputDataRuleViolationException"></exception>
        private static string _cleanPolygon(string wktPolygon, string entityName)
        {
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
                polygon = _simpleClean(wktPolygon);
            }

            // Check that the submitted mask is a proper WKT Polygon geometry
            if (polygon.GetType() != typeof(Polygon))
                throw new StandardExceptions.InputDataRuleViolationException(
                    $"The {entityName} shape must be a well-formed WKT Polygon geometry."
                );

            // If it is valid, send it back
            if (polygon.IsValid)
                return polygon.Normalized().ToText();

            // The polygon is apparently invalid, first try a simple fix and check if it is acceptable
            var bufferedPoly = polygon.Buffer(0);
            if (bufferedPoly.IsValid && bufferedPoly.GetType() == typeof(Polygon) && Math.Abs(bufferedPoly.Area - polygon.Area) < 1)
                return bufferedPoly.Normalized().ToText();

            // The polygon rather seriously invalid, so let's try to clean it up with more serious intervention.
            var fullGeom = _simpleClean(polygon.ToText());

            // Check to see if we get a single polygon extracted from the operation
            var cleanedPoly = GeometryExtracter.Extract<Polygon>(fullGeom);
            if (cleanedPoly.Count == 1 && cleanedPoly.First().IsValid)
                return cleanedPoly.First().Normalized().ToText();

            // If not we make a last ditch effort to union the resulting polygons. This operation
            // could result in some (small but) noticeable change in the polygon.
            var repairedPoly = default(Polygon);
            try
            {
                repairedPoly = _repairIntersectingOrMultiPolygon(fullGeom);

                // Apparently all attempts to repair the polygon have failed
                if (!repairedPoly.IsValid
                    || repairedPoly.GetType() != typeof(Polygon))
                    throw new StandardExceptions.InputDataRuleViolationException(
                        $"the {entityName} {(repairedPoly.IsValid ? $"has {cleanedPoly.Count} separate polygons" : "is invalid")} and could not be repaired: {wktPolygon}"
                    );

                return repairedPoly.Normalized().ToText();
            }
            catch (StandardExceptions.InputDataRuleViolationException)
            {
                throw new StandardExceptions.InputDataRuleViolationException(
                    $"the {entityName} {(repairedPoly.IsValid ? $"has {cleanedPoly.Count} separate polygons" : "is invalid")} and could not be repaired: {wktPolygon}"
                );
            }
        }

        /// <summary>
        /// This makes a quick pass of the geometry and fixes unclosed polygons as well
        /// as trying to fix improperly ordered path elements within the Polygon
        /// </summary>
        /// <param name="wkt">A Wkt Polygon string</param>
        /// <returns></returns>
        /// <exception cref="StandardExceptions.InputDataRuleViolationException"></exception>
        private static Geometry _simpleClean(string wkt)
        {
            // Verify the request is declared as "POLYGON"
            if (wkt.Substring(0, 7) != "POLYGON")
                throw new StandardExceptions.InputDataRuleViolationException("A shape path must be a POLYGON type");

            if (_asymmetricNesting.Matches(wkt).Count > 0)
                throw new StandardExceptions.InputDataRuleViolationException("The submitted POLYGON has an improperly nested ring");

            // We break the string into the individual paths the loop over them, repairing each path as we go.
            var polyStrings = wkt.Split("),(").Select(z =>
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
                    // Last ditch attempt to repair the path using buffer 0 (usually fixes self intersection)
                    geom = _repairIntersectingOrMultiPolygon(geom);

                    // If still invalid, we give up
                    if (!geom.IsValid)
                        throw new StandardExceptions.InputDataRuleViolationException($"");
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

            // Return the reconstructed geometry
            return fullGeom;
        }

        /// <summary>
        /// Repairs a self-intersecting polygon or a multipolygon by converting it
        /// into a single polygon that most closely resembles the input polygon
        /// while still remaining valid.
        /// </summary>
        /// <param name="poly">A self-intersecting polygon or a multi-polygon</param>
        /// <returns>A single valid polygon most closely approximating the input</returns>
        /// <exception cref="StandardExceptions.InputDataRuleViolationException"></exception>
        private static Polygon _repairIntersectingOrMultiPolygon(Geometry poly)
        {
            // Separate the geom into its discrete polygon entities
            var discretePolys = GeometryExtracter.Extract<Polygon>(poly);
            // If there is only one, our job is done
            if (discretePolys.Count == 1)
            {
                if (discretePolys[0].IsValid)
                    return (Polygon)discretePolys[0];

                // Probably the polygon is invalid due to self intersection
                // Let's grab every outer line in the polygon
                var coords = poly.Boundary.Coordinates;
                var lines = coords.Select(
                    (t, i) => new LineString(new[] { t, coords[i + 1 == coords.Length ? 0 : i + 1] })
                ).ToList();

                // Now let's build n polygons by looking for intersecting lines
                var intersectionPoints = new Dictionary<Coordinate, int>();
                var intersectedLines = new List<LineString>();
                var lineCount = 0;
                foreach (var line in lines)
                {
                    var points = new List<Coordinate>();
                    if (lineCount == intersectedLines.Count)
                        intersectedLines.Add(new LineString(null));
                    else
                        points = intersectedLines[lineCount].Coordinates.ToList();

                    points.Add(line.Coordinates[0]);


                    var foundIntersect = false;
                    foreach (var compLine in lines)
                    {
                        if (line.Coordinates.Contains(compLine.Coordinates[0])
                            || line.Coordinates.Contains(compLine.Coordinates[1])
                            || !line.Intersects(compLine))
                            continue;

                        var point = line.Intersection(compLine);
                        points.Add(point.Coordinate);
                        intersectedLines[lineCount] = new LineString(points.ToArray());
                        foundIntersect = true;

                        if (intersectionPoints.TryGetValue(point.Coordinate, out var val))
                        {
                            // Let's continue building this preexisting polygon
                            lineCount = val;
                            continue;
                        }

                        // Record where the old polygon stopped
                        intersectionPoints[point.Coordinate] = lineCount;

                        // Start a new polygon
                        lineCount = intersectedLines.Count;
                        intersectedLines.Add(new LineString(new Coordinate[] { point.Coordinate, line.Coordinates[1] }));
                    }
                    if (foundIntersect) continue;
                    // If an intersection was not found, add the endpoint to the points list and update intersectedLines
                    points.Add(line.Coordinates[1]);
                    intersectedLines[lineCount] = new LineString(points.ToArray());
                }

                // Convert the line strings into polygons
                discretePolys = intersectedLines.Select(x =>
                {
                    var coordsList = x.Coordinates.ToList();
                    if (!coordsList.First().Equals(coordsList.Last()))
                        coordsList.Add(coordsList.First());

                    var coords = coordsList.ToArray();
                    return (Geometry)new Polygon(new LinearRing(coords));
                }).ToList();
            }

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

            // Make sure our combined poly is valid and return it
            if (repairedPoly.GetType() != typeof(Polygon))
                throw new StandardExceptions.InputDataRuleViolationException($"");

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
                var point = new Point(poly1Point.X, poly1Point.Y + 1);
                var (biggerPoly, smallerPoly) = poly1.Area > poly2.Area
                    ? (poly1, poly2)
                    : (poly2, poly1);
                // Try nudging the point in 4 cardinal directions to find a point inside the larger poly
                Point fittingPoint = null;
                // Up
                if (biggerPoly.Contains(point))
                    fittingPoint = point;
                // Down
                point = new Point(poly1Point.X, poly1Point.Y - 2);
                if (biggerPoly.Contains(point))
                    fittingPoint = point;
                point = new Point(poly1Point.X + 1, poly1Point.Y + 1);
                // Right
                if (biggerPoly.Contains(point))
                    fittingPoint = point;
                // Left
                point = new Point(poly1Point.X - 2, poly1Point.Y);
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
                repaired1 = _repairIntersectingOrMultiPolygon(repaired1);
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
                repaired2 = _repairIntersectingOrMultiPolygon(repaired2);
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
    }
}