using System;

namespace sqe_api
{
	public class SourceFileInfo
	{
		public string    FileName;
		public string    ColName;
		public string    ImageFileName;
		public uint      SqeImageId;
		public int       OffsetX   = 0;
		public int       OffsetY   = 0;
		public decimal   Scale     = decimal.One;
		public double    Rotate    = 0.0;
		public int       MidpointX = 0;
		public int       Midpointy = 0;
		public uint      ArtefactId;
		public imagePart ImagePart = imagePart.Full;
	}

	public enum imagePart
	{
		Full
		, Left
		, Right
		,
	}
}
