using System.Collections.Generic;
using System.Linq;

namespace SQE.DatabaseAccess.Helpers
{
	public class SignStreamGraph
	{
		// This dictionary provides the data necessary to traverse the graph
		// from beginning to end. Each node points to a list of all nodes
		// directly following it.
		private readonly Dictionary<uint, HashSet<uint>> forward =
				new Dictionary<uint, HashSet<uint>>();

		// This dictionary provides the data necessary to traverse the graph
		// from end to beginning. Each node points to a list of all nodes
		// directly preceding it.
		private readonly Dictionary<uint, HashSet<uint>> reverse =
				new Dictionary<uint, HashSet<uint>>();

		/// <summary>
		///  Creates a directed acyclic graph object from a list of
		///  node tuples.  Then all leaves in the graph can be found,
		///  all paths through the graph can be accessed, and possible
		///  insertions can be tested to see if they would create a
		///  cycle.
		/// </summary>
		/// <param name="arr">
		///  A list of tuples representing two nodes
		///  connected by an edge directed from the first item to the
		///  second item.
		/// </param>
		public SignStreamGraph(IEnumerable<(uint, uint)> arr)
		{
			if (arr == null)
				return;

			// Populate the forward and reverse dictionaries
			foreach (var (node, nextNode) in arr)
				_addLink(node, nextNode);

			graphIsAltered = true;
			_findLeaves();
		}

		// These two fields provide a listing of all leaves in the graph:
		// nodes that have either no preceding node (initialLeaves) or
		// following node (endLeaves)
		/// <summary>
		///  All nodes in the graph with no incoming edges
		/// </summary>
		public IEnumerable<uint> initialLeaves { get; private set; }

		/// <summary>
		///  All nodes in the graph with no outgoing edges
		/// </summary>
		public IEnumerable<uint> endLeaves { get; private set; }

		public bool graphIsAltered { get; private set; }

		/// <summary>
		///  Calculates all possible paths from the specified node as a 2D
		///  List of ordered Lists of connected nodes.
		/// </summary>
		/// <param name="currentNode">
		///  The node from which to start the search
		///  through the graph
		/// </param>
		/// <returns>
		///  A 2D List of ordered Lists of connected nodes outbound from
		///  the specified node
		/// </returns>
		public List<List<uint>> FindAllPaths(uint currentNode, bool towardBeginning = false)
		{
			var directedList = towardBeginning
					? reverse
					: forward;

			var response = new List<List<uint>>();
			var stream = new List<uint> { currentNode };

			// Stop recursion when there are no new nodes available
			if (!directedList.TryGetValue(currentNode, out var currentNextNodes))
			{
				response.Add(stream);

				return response;
			}

			// Recurse on every next node
			foreach (var node in currentNextNodes)
			{
				var nextResponse = FindAllPaths(node);

				foreach (var resp in nextResponse)
					response.Add(stream.Concat(resp).ToList());
			}

			return response;
		}

		/// <summary>
		///  Safely add a new linked pair of nodes to the graph; it will
		///  fail if the add would create a cycle in the graph.
		/// </summary>
		/// <param name="node">The node with the outgoing edge</param>
		/// <param name="nextNode">The node with the incoming edge</param>
		/// <returns>
		///  True if the pair was inserted, false if insertion failed
		///  due to violation of a constraint (it introduces a cycle in the
		///  graph)
		/// </returns>
		public bool AddLink(uint node, uint nextNode)
		{
			// Reject the request if it would result in a cycle
			if (_pathExists(nextNode, node))
				return false;

			UnsafeAddLink(node, nextNode);
			_findLeaves();
			graphIsAltered = false;

			return true;
		}

		/// <summary>
		///  Add a new linked pair of nodes to the graph, no checks against
		///  cycles are performed, no updating of th list of leaves is performed.
		/// </summary>
		/// <param name="node">The node with the outgoing edge</param>
		/// <param name="nextNode">The node with the incoming edge</param>
		public void UnsafeAddLink(uint node, uint nextNode)
		{
			_addLink(node, nextNode);
		}

		/// <summary>
		///  Calculate the leaves in the graph based on the current state
		///  of the forward and reverse graph dictionaries.
		/// </summary>
		public void FindLeaves()
		{
			_findLeaves();
			graphIsAltered = false;
		}

		/// <summary>
		///  Calculate the leaves in the graph based on the current state
		///  of the forward and reverse graph dictionaries.
		/// </summary>
		private void _findLeaves()
		{
			if (!graphIsAltered)
				return;

			// Any key in forward that does not occur in reverse is a leaf
			// with no inbound edge
			initialLeaves = forward.Keys.Except(reverse.Keys).ToList();

			// Any key in reverse that does not occur in forward is a leaf
			// with no outbound edge
			endLeaves = reverse.Keys.Except(forward.Keys).ToList();
			graphIsAltered = false;
		}

		private void _addLink(uint node, uint nextNode)
		{
			if (forward.ContainsKey(node))
				forward[node].Add(nextNode);
			else
				forward.Add(node, new HashSet<uint> { nextNode });

			if (reverse.ContainsKey(nextNode))
				reverse[nextNode].Add(node);
			else
				reverse.Add(nextNode, new HashSet<uint> { node });
		}

		/// <summary>
		///  Checks if any path exists in the graph from the starting
		///  node to the goal node.  It will return early the moment
		///  the goal node is found in a path emanating from the
		///  starting node.
		/// </summary>
		/// <param name="startNode">The starting node for searching the graph</param>
		/// <param name="goalNode">
		///  The node to be found in the paths emanating
		///  from the starting node
		/// </param>
		/// <returns>
		///  True if the goal node is present in any path emanating
		///  from the starting node.
		/// </returns>
		private bool _pathExists(uint startNode, uint goalNode)
		{
			var pathFound = false;

			// Stop recursion when there are no new nodes available,
			// the goal node has not been found
			if (!forward.TryGetValue(startNode, out var currentNextNodes))
				return false;

			// Stop recursion as soon as the goalNode has been found
			if (currentNextNodes.Contains(goalNode))
				return true;

			// Recurse on every next node
			foreach (var node in currentNextNodes)
			{
				pathFound = _pathExists(node, goalNode);

				// Stop recursion as soon as the goalNode has been found
				if (pathFound)
					return true;
			}

			return pathFound;
		}
	}
}
