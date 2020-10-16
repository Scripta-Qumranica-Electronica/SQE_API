using System.Collections.Generic;

namespace qwb_to_sqe
{
	public class Word
	{
		protected static bool isSuperScript   = false;
		protected static bool IsSubScript     = false;
		protected static bool IsReconstructed = false;
		protected static bool IsDeleted       = false;
		protected static bool WrongAddition   = false;
		protected static bool Forgotten       = false;
		protected static bool IsOriginal      = false;
		protected static bool IsInserted      = false;
		protected static bool IsCorrected     = false;
		protected static bool IsMarginVariant = false;
		protected static bool IsQuestionable  = false;
		protected static bool IsConjecture    = false;

		//    protected static readonly SQEConn SqeConnection = new SQEConn();

		protected readonly int           qwbId;
		protected readonly List<QwbSign> signs     = new List<QwbSign>();
		protected          int           IsVariant = 0;

		protected Word() { }

		protected Word(int id) => qwbId = id;
	}
}
