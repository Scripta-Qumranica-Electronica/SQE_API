using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace SQE.SqeHttpApi.Server.Helpers
{
    public static class InputValidation
    {
        
    }

    public static class GeometryValidation
    {
        public static bool ValidateTransformMatrix(string transformMatrix)
        {
            try
            {
                _ = JsonConvert.DeserializeObject<transformMatrix>(transformMatrix);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private class transformMatrix
        {
            [Required]
            public double[][] matrix { get; set; }
            
            public transformMatrix(double[][] matrix)
            {
                if (!IsValidRows(matrix))
                    throw new SystemException("Improper number of rows or columns.");
                if (!IsValidValues(matrix[0]) || !IsValidValues(matrix[1]))
                    throw new SystemException("Invalid numerical values");
            }
            
            bool IsValidRows(double[][] mat)
            {
                if (mat.Length != 2)
                    return false;
                if (mat[0].Length != 3)
                    return false;
                if (mat[1].Length != 3)
                    return false;
                return true;
            }

            bool IsValidValues(double[] row)
            {
                if (row[0] < -1 && row[0] > 1)
                    return false;
                if (row[1] < -1 && row[1] > 1)
                    return false;
                return (int)row[2] == row[2];
            }
        }
    }
}