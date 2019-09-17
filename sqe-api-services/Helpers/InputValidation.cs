using Newtonsoft.Json;
using SQE.API.DATA.Models;

namespace SQE.API.SERVICES.Helpers
{
	public static class GeometryValidation
	{
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
	}
}