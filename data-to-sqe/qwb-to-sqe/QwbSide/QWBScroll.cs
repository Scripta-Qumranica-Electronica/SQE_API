using System.Collections.Generic;

namespace qwb_to_sqe;

public class QWBScroll
{
	public readonly List<QWBFragment> fragments = new();
	public          uint              Id;
	public          string            Name;
}
