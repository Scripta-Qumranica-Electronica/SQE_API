using System.Collections.Generic;

namespace qwb_to_sqe;

public class QWBLine
{
	public readonly List<QWBWord> words = new();
	public          string        Name;
}
