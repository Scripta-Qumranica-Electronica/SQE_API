using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using OfficeOpenXml;
using sqe_api;

namespace from_goettingen.Source

{
	public class ExcelData
	{
		private readonly List<SourceRoi>  _rois  = new List<SourceRoi>();
		private readonly List<SourceLine> _lines = new List<SourceLine>();

		public ExcelData(string dir, SourceFileInfo fileInfo)
		{
			Console.WriteLine(fileInfo.FileName);
			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
			var ep = new ExcelPackage(new FileInfo(dir+fileInfo.FileName));
			var signsSheet = ep.Workbook.Worksheets[1];
			var lastRow = signsSheet.Dimension.End.Row;
			var lastLine = "";
			SourceLine currLine = null;

			for (var row = 2; row <= lastRow; row++)
			{
				var roiId = signsSheet.Cells[row, 1].Value?.ToString()?.Trim();

				if (roiId == null)
					continue;

				var x_ofset = signsSheet.Cells[row, 9].Value?.ToString()?.Trim();
				var y_ofset = signsSheet.Cells[row, 10].Value?.ToString()?.Trim();
				var width = signsSheet.Cells[row, 11].Value?.ToString()?.Trim();
				var height = signsSheet.Cells[row, 12].Value?.ToString()?.Trim();

				if (x_ofset == null
					|| x_ofset.Contains("."))
				{
					Console.WriteLine($"{roiId} - {fileInfo.FileName}");

					break;
				}

				_rois.Add(
						new SourceRoi(
								roiId
								, x_ofset
								, y_ofset
								, width
								, height
								, fileInfo
						));
			}

			var charSheet = ep.Workbook.Worksheets[0];
			lastRow = charSheet.Dimension.End.Row;
			var sequenceOffSet = 0;
			var lastReadingOrder = "";
			SourceSign currSign = null;

			for (var row = 2; row <= lastRow; row++)
			{
				var line = SourceData.GetCellString(charSheet, "line_id", row);

				if (line == null)
					continue;

				if (line != lastLine)
				{
					currLine = _lines.Find(x => x.LineName == line);

					if (currLine == null)
					{
						currLine = new SourceLine(line);
						_lines.Add(currLine);
					}

					lastLine = line;
				}

				var roiId = SourceData.GetCellString(charSheet, "roi_id", row);

				if (roiId == null)
					continue;

				var rois = _rois.FindAll(x => x.roi_id == int.Parse(roiId));
				var commentary = SourceData.GetCellString(charSheet, "commentary", row);

				if (_rois.Count == 0)
				{
					Console.WriteLine($"{row} no rois found");

					continue;
				}

				var human0 = SourceData.GetCellString(charSheet, "he_human_0", row);
				var readingOrder = SourceData.GetCellString(charSheet, "reading_order", row);

				if (!readingOrder.Equals(lastReadingOrder) || readingOrder.Equals("0"))
				{
					currSign = new SourceSign(
							int.Parse(readingOrder) + sequenceOffSet
							, readingOrder.Equals("0") ? "×" : human0
							, commentary);

					currLine.addSourceSign(currSign);

					lastReadingOrder = readingOrder;
				}
				else
					currSign.addCommentary(commentary);

				currSign.addAttributes(SourceData.GetAttributeValues(charSheet, row));
				currSign.addRois(rois);

				Debug.Assert(currLine != null, nameof(currLine) + " != null");

				for (var i = 1; i <= 3; i++)
				{
					var varReading = SourceData.GetCellString(charSheet, $"he_human_{i}", row);

					if (varReading != null && !human0.Contains(varReading))
					{
						currSign.addVarReading(varReading);
						human0 += varReading;
					}
				}


				var additionalSigns = currSign.GetAdditionalSigns();
				currLine.addSourceSigns(additionalSigns);
				sequenceOffSet += additionalSigns.Count;
			}

			_lines.Sort();
		}

		public List<SourceLine> getLines() => _lines;
	}
}
