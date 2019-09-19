using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SQE.SqeHttpApi.DataAccess.Helpers;

namespace SQE.SqeHttpApi.DataAccess.Models
{
	public class TransformMatrix
	{
		/// <summary>
		///     This class provides a model and validator for the JSON transform matrices stored in the SQE
		///     database. The transform matrix is a 2D array with exactly 2 rows of 3 columns each. The values
		///     of the third column should be a whole number (e.g., 657 or 657.000), since there is no point
		///     in subpixel translations with the high resolution images we use.
		/// </summary>
		/// <param name="matrix">
		///     A 2d array containing a transform matrix in the format:
		///     [ [ ScaleX, ShearX, TranslateX ], [ ScaleY, ShearY, TranslateY ] ].
		/// </param>
		/// <exception cref="StandardExceptions.ImproperInputDataException"></exception>
		public TransformMatrix(IReadOnlyList<double[]> matrix)
		{
			if (!IsValidRows(matrix))
				throw new StandardExceptions.ImproperInputDataException("position");
			if (!IsValidValues(matrix[0])
				|| !IsValidValues(matrix[1]))
				throw new StandardExceptions.ImproperInputDataException("position");
		}

		// The object must have a "matrix" property that is a 2d array of doubles
		[Required] public double[][] matrix { get; set; }

		private static bool IsValidRows(IReadOnlyList<double[]> mat)
		{
			// The 1st dimension of the array must have 2 elements
			if (mat.Count != 2)
				return false;
			// Each of those elements must have exactly three elements
			return mat[0].Length == 3 && mat[1].Length == 3;
		}

		private static bool IsValidValues(IReadOnlyList<double> row)
		{
			// Check that the third element in the 2nd dimension of the array is equivalent to a whole number.
			return (int)row[2] == row[2];
		}
	}
}