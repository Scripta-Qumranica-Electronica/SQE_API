using System;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Windows;
using sqe_api;
using SQE.DatabaseAccess.Models;

namespace from_goettingen
{
	internal class RotatedVector
	{
		private readonly double _x;
		private readonly double _y;

		public int XasInt => Convert.ToInt32(_x);
		public int YasInt => Convert.ToInt32(_y);

		public string PointAsString => $"{_x} {_y}";

		public RotatedVector(double x, double y, double r)
		{
			var rot = Math.PI * r / 180.0;
			_x = (x * Math.Cos(rot)) - (y * Math.Sin(rot));
			_y = (x * Math.Sin(rot)) + (y * Math.Cos(rot));
		}
	}

	public class SourceRoi
	{
		public int     roi_id  { get; }
		public int     x_ofset { get; }
		public int     y_ofset { get; }
		public int     width   { get; }
		public int     height  { get; }
		public decimal scale   { get; }
		public int     xOffset { get; }
		public int     yOffset { get; }

		public double rotation { get; }

		public int MidpointX { get; }
		public int MidpointY { get; }

		public uint ArtefactId { get; }

		public SourceRoi(
				string           id
				, string         x
				, string         y
				, string         w
				, string         h
				, SourceFileInfo fileInfo)
		{
			roi_id = int.Parse(id);
			x_ofset = int.Parse(x);
			y_ofset = int.Parse(y);
			width = int.Parse(w);
			height = int.Parse(h);
			scale = fileInfo.Scale;
			xOffset = fileInfo.OffsetX;
			yOffset = fileInfo.OffsetY;
			rotation = fileInfo.Rotate;
			MidpointX = fileInfo.MidpointX;
			MidpointY = fileInfo.Midpointy;
			ArtefactId = fileInfo.ArtefactId;

		}

		public SignInterpretationRoiData getRoiData()
		{
			var roiData = new SignInterpretationRoiData();

			var translateXY = new RotatedVector(
					(double) ((x_ofset + xOffset - MidpointX) * scale)
					, (double) ((y_ofset + yOffset - MidpointY) * scale)
					, rotation);

			roiData.TranslateX = translateXY.XasInt + Convert.ToInt32(MidpointX * scale);
			roiData.TranslateY = translateXY.YasInt + Convert.ToInt32(MidpointY * scale);

			var x = (double) (width * scale);
			var y = (double) (height * scale);

			var pointA = new RotatedVector(x, 0, rotation);
			var pointB = new RotatedVector(x, y, rotation);
			var pointC = new RotatedVector(0, y, rotation);

			roiData.Shape = "POLYGON(("
							+ $"0 0,"
							+ $"{pointA.PointAsString},"
							+ $"{pointB.PointAsString},"
							+ $"{pointC.PointAsString},"
							+ $"0 0"
							+ "))";

			roiData.ArtefactId = ArtefactId;

			return roiData;
		}
	}
}
