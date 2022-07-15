using System;
using System.Collections.Generic;
using OfficeOpenXml;

namespace from_goettingen.Source
{
    public class SourceData
    {
        public static Dictionary<string, int> DataColumns = new Dictionary<string, int>()
        {
            {"roi_id", 3},
            {"reading_order", 7},
            {"readding_order_alt", 8},
            {"attr", 9},
            {"related_to", 10},
            {"is_joined", 11},
            {"kerning", 12},
            {"damaged_sm", 13},
            {"damaged_vis", 14},
            {"damaged_legacy", 15},
            {"angle", 16},
            {"he_human_0", 17},
            {"he_human_1", 18},
            {"he_human_2", 19},
            {"he_human_3", 20},
            {"line_id", 21},
            {"commentary", 25},
        };

        public static Dictionary<string, Dictionary<string, int>> AttributeValues =
            new Dictionary<string, Dictionary<string, int>>()
            {
                {
                    "attr", new Dictionary<string, int>()
                    {
                        {"transformed", 24},
                        {"reinked", 45},
                        {"reinked?", 46},
                        {"retraced", 47},
                        {"retraced?", 48},
                        {"interlinear", 49},
                        {"creased", 50},
                        {"erased", 33},
                    }
                },
                {
                    "damaged_sm", new Dictionary<string, int>()
                    {
                        {"true", 54},
                    //    {"false", 0},
                        {"relevant_x", 52},
                        {"relevant_w", 52},
                        {"relevant_y", 53},
                        {"relevant_h", 53},
                    }
                },
                {
                    "damaged_legacy", new Dictionary<string, int>()
                    {
                        {"certain", 18},
                        {"probable_letter", 55},
                        {"possible_letter", 19},
                    }
                },
			};

        public static string GetCellString(ExcelWorksheet ws, string columnName, int row)
        {
            var value = ws.Cells[row, DataColumns[columnName]].Value;
            return value?.ToString().Trim();
        }

        public static List<int> GetAttributeValues(ExcelWorksheet ws, int row)
        {
            var attributeIds = new List<int>();
            foreach (var columnName in AttributeValues.Keys)
            {
                var valueString = GetCellString(ws, columnName, row);
                if (!String.IsNullOrWhiteSpace(valueString) && valueString != "null")
                {
                    var normalizedString=valueString.ToLower().Trim();
                    if (AttributeValues[columnName].ContainsKey(normalizedString))
						attributeIds.Add(AttributeValues[columnName][normalizedString]);
					else
						Console.WriteLine($"{columnName} : {valueString}");
				}
            }

            return attributeIds;
        }

    }
}
