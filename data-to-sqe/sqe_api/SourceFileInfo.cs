namespace sqe_api
{
	public class SourceFileInfo
	{
		public uint      ArtefactId;
		public string    ColName;
		public string    FileName;
		public string    ImageFileName;
		public imagePart ImagePart = imagePart.Full;
		public int       MidpointX = 0;
		public int       Midpointy = 0;
		public int       OffsetX   = 0;
		public int       OffsetY   = 0;
		public double    Rotate    = 0.0;
		public decimal   Scale     = decimal.One;
		public uint      SqeImageId;
	}

	public enum imagePart
	{
		Full
		, Left
		, Right
		,
	}
}
