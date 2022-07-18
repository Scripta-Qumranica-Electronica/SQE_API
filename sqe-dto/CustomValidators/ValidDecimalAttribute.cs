using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace SQE.API.DTO.Validators
{
	public class ValidDecimalAttribute : ValidationAttribute
	{
		private readonly byte precision;
		private readonly byte scale;

		/// <summary>
		///  This validation attribute checks a decimal type to ensure
		///  it has the expected precision and scale.
		///  The decimal type in c# is a large 128bit type with massive support
		///  for decimal type numbers. When inputting values into a DECIMAL column
		///  in a database, however, it is usually necessary to use a more limited
		///  form of the decimal type. This validation attribute enables that
		///  functionality so that, for example, a column of the SQL DECIMAL(19,9)
		///  type in the database can be safeguarded by using [ValidDecimalAttribute(19,9)]
		///  on the c# attribute that will go in to or be read from the database.
		/// </summary>
		/// <param name="precision">The total number of digits in the decimal number</param>
		/// <param name="scale">The number of digits to the right of the decimal point</param>
		/// <exception cref="ArgumentException"></exception>
		public ValidDecimalAttribute(byte precision, byte scale)
		{
			if (scale > precision)
			{
				throw new ArgumentException(
						"The scale must be less than or equal to the precision");
			}

			this.precision = precision;
			this.scale = scale;
		}

		public override bool IsValid(object value)
		{
			// Get the testing value as a decimal, bail immediately on failure
			if (!(value is decimal decimalNumber))
				return false;

			// find the first number that is too large for the precision and scale
			var ceiling = (decimal) BigInteger.Pow(10, precision - scale);

			// Make sure that the number of digits to the left of the decimal point
			// do not exceed the desired precision.
			if (!(Math.Floor(Math.Abs(decimalNumber)) < ceiling))
				return false;

			// For finding the number of digits after the decimal point,
			// see: https://stackoverflow.com/questions/42264514/get-number-of-significant-digits-to-the-right-of-decimal-point-in-c-sharp-decima

			// Get the bit representation of the decimal number.
			// Dividing by 1.00... (to 28 places) removes all trailing zeros from the number.
			// Then get the bit representation (an array of 4 32bit ints)
			var bits = decimal.GetBits(decimalNumber / 1.0000000000000000000000000000m);

			// Bits 16 to 23 of the fourth byte (bits[3]) must contain an exponent between 0 and 28,
			// which indicates the power of 10 to divide the integer number component of the decimal.
			// Shifting 16 bits to the right and using AND 255 (= 11111111) extracts the value of (what is now)
			// the first 8 bits, which are the exponent (i.e., the number of decimal digits). Check to
			// see that this number is less than or equal to the scale.
			return ((bits[3] >> 16) & 255) <= scale;
		}
	}
}
