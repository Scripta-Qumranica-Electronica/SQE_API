using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using MoreLinq;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Simplify;
using SQE.DatabaseAccess.Helpers;
using static SqeUtils.GeoRepairPolygonMethods;

// ReSharper disable ArrangeRedundantParentheses

namespace SQE.API.Server.Helpers;

public static class GeometryValidation
{
	/// <summary>
	///  Validate that the wkt polygon is indeed correct. If it is not, the method will
	///  throw an error. When the fix parameter is set to true, it will try to repair it and
	///  send the repaired version of the polygon back with the error.  If the polygon cannot be
	///  repaired at all, then a descriptive error about the polygon is thrown.
	/// </summary>
	/// <param name="wktPolygon">A wkt polygon</param>
	/// <param name="entityName">The type of object being validated (used in formulating useful exception error messages)</param>
	/// <param name="fix">
	///  Invalid polygons always throw an error, this flag determines whether to try to return a repaired
	///  version of the polygon with that error
	/// </param>
	/// <returns>A WKT string with the cleaned polygon</returns>
	public static string ValidatePolygon(string wktPolygon, string entityName, bool fix = false) =>

			// Previously we wrapped the private validation method in a Task so we can asynchronously call this non-trivial method
			// But see the docs: https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices?view=aspnetcore-3.1
			// Those docs explicitly warn against doing `await Task.Run`, so this is now synchronous.
			_validatePolygon(wktPolygon, entityName, fix);

	private static bool _hasValidDirection(Geometry geom)
	{
		var isPolygon = geom.OgcGeometryType == OgcGeometryType.Polygon;
		var isMultiPolygon = geom.OgcGeometryType == OgcGeometryType.MultiPolygon;

		if (!isPolygon
			&& !isMultiPolygon)
			return false;

		return isMultiPolygon
				? _multiPolygonHasValidDirection((MultiPolygon)geom)
				: _polygonHasValidDirection((Polygon)geom);
	}

	private static bool _multiPolygonHasValidDirection(MultiPolygon geom)
	{
		return geom.Geometries.All(x => _polygonHasValidDirection((Polygon)x));
	}

	private static bool _polygonHasValidDirection(Polygon polygon)
	{
		// Exterior ring should be CCW
		return Orientation.IsCCW(polygon.ExteriorRing.Coordinates)
			   &&

			   // Interior rings (holes) should be CW (i.e., NOT CCW)
			   polygon.InteriorRings.All(innerRing => !Orientation.IsCCW(innerRing.Coordinates));
	}

	private static Geometry _normalizeGeometry(Geometry geom)
	{
		var isPolygon = geom.OgcGeometryType == OgcGeometryType.Polygon;
		var isMultiPolygon = geom.OgcGeometryType == OgcGeometryType.MultiPolygon;

		if (!isPolygon
			&& !isMultiPolygon)
			return geom;

		if (isMultiPolygon)
			return _normalizeMultiPolygon((MultiPolygon)geom);

		return _normalizePolygon((Polygon)geom);
	}

	private static MultiPolygon _normalizeMultiPolygon(MultiPolygon mp)
	{
		var factory = mp.Factory;
		var polygons = mp.Geometries.Select(x => _normalizePolygon((Polygon)x)).ToArray();

		return factory.CreateMultiPolygon(polygons);
	}

	private static Polygon _normalizePolygon(Polygon polygon)
	{
		var factory = polygon.Factory;

		// Fix exterior ring - should be CCW
		var exteriorRing = polygon.ExteriorRing;

		if (!Orientation.IsCCW(exteriorRing.Coordinates))
			exteriorRing = (LinearRing)(((Geometry) exteriorRing).Reverse());

		// Fix interior rings (holes) - should be CW (i.e., NOT CCW)
		var interiorRings = polygon.InteriorRings.Select(x => Orientation.IsCCW(x.Coordinates)
																 ? (LinearRing)(((Geometry) x).Reverse())
																 : (LinearRing)x).ToArray();

		// Create a new polygon with corrected rings
		return factory.CreatePolygon((LinearRing)exteriorRing, interiorRings);
	}

	/// <summary>
	///  Private method to validate that the wkt polygon is indeed correct. If it is not, the method will
	///  throw an error. When the fix parameter is set to true, it will try to repair it and
	///  send the repaired version of the polygon back with the error.  If the polygon cannot be
	///  repaired at all, then a descriptive error about the polygon is thrown.
	/// </summary>
	/// <param name="wktPolygon">A wkt polygon</param>
	/// <param name="entityName">The type of object being validated (used in formulating useful exception error messages)</param>
	/// <param name="fix">
	///  Invalid polygons always throw an error, this flag determines whether to try to return a repaired
	///  version of the polygon with that error
	/// </param>
	/// <returns>A WKT string with the cleaned polygon</returns>
	/// <exception cref="StandardExceptions.InputDataRuleViolationException"></exception>
	private static string _validatePolygon(string wktPolygon, string entityName, bool fix)
	{
		var wkr = new WKTReader();

		// Bail immediately on null/blank input
		if (string.IsNullOrEmpty(wktPolygon))
		{
			throw new StandardExceptions.InputDataRuleViolationException(
					"The submitted WKT POLYGON is empty");
		}

		// Try loading the polygon
		try
		{
			var polygon = wkr.Read(wktPolygon);

			// If it is valid, return it
			if (polygon.IsValid)
			{
				// Remove any completely unnecessary points
				var simplifier = new DouglasPeuckerSimplifier(polygon) { DistanceTolerance = 0 };

				var simplifiedPolygon = simplifier.GetResultGeometry();

				return _hasValidDirection(simplifiedPolygon)
						? simplifiedPolygon.ToString()
						: _normalizeGeometry(simplifiedPolygon).ToString();
			}

			// It is invalid, but could be repaired as a binary representation
			// Throw an error if no request to fix it has been made
			if (!fix)
			{
				throw new StandardExceptions.InputDataRuleViolationException(
						"The submitted WKT POLYGON is invalid, try using the API's repair-wkt-polygon endpoint to fix it");
			}
		}
		catch
		{
			// Throw an error if no request to fix it has been made.
			if (!fix)
			{
				throw new StandardExceptions.InputDataRuleViolationException(
						"The submitted WKT POLYGON is invalid, try using the API's repair-wkt-polygon endpoint to fix it");
			}
		}

		try
		{
			return RepairWkt(wktPolygon);
		}
		catch
		{
			throw new StandardExceptions.InputDataRuleViolationException(
					"The submitted WKT POLYGON is invalid and cannot be repaired");
		}
	}

	/// <summary>
	///  This makes a quick pass of the geometry and fixes unclosed polygons as well
	///  as trying to fix any simple text formatting errors.
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
		{
			throw new StandardExceptions.InputDataRuleViolationException(
					"The submitted POLYGON has an improperly nested ring");
		}

		// We break the string into the individual paths the loop over them, repairing each path as we go.
		var polyStrings = wktSplit.Split(wkt)
								  .Select(z =>
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
										  })
								  .ToList();

		return $"POLYGON({string.Join(",", polyStrings)})";
	}

	// You must declare the layout of your C struct (this is for any array)
	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
	public struct BinaryData
	{
		public UIntPtr len;
		public IntPtr  data;
	}
}
