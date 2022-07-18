using System.Collections.Generic;
using System.Linq;
using DeepEqual.Syntax;
using SQE.DatabaseAccess.Helpers;
using Xunit;

namespace SQE.ApiTest
{
	public class GraphTests
	{
		[Fact]
		[Trait("Category", "Graph Analysis")]
		public void CanTraverseGraph()
		{
			// Arrange
			var initialGraphData = new List<(uint, uint)>
			{
					(1, 2)
					, (2, 3)
					, (3, 4)
					, (4, 5)
					, (2, 7)
					, (7, 8)
					, (8, 9)
					, (9, 10)
					, (5, 10)
					, (10, 11)
					, (11, 12)
					,
			};

			var expectedResult = new List<List<uint>>
			{
					new List<uint>
					{
							1
							, 2
							, 3
							, 4
							, 5
							, 10
							, 11
							, 12
							,
					}
					, new List<uint>
					{
							1
							, 2
							, 7
							, 8
							, 9
							, 10
							, 11
							, 12
							,
					}
					,
			};

			// Act
			var graph = new SignStreamGraph(initialGraphData);
			var initLeaves = graph.initialLeaves;

			// Assert
			Assert.Equal(1u, initLeaves.FirstOrDefault());
			var streams = graph.FindAllPaths(initLeaves.FirstOrDefault());
			streams.ShouldDeepEqual(expectedResult);

			Assert.False(graph.AddLink(3, 1)); // Reject creation of a cycle
			Assert.True(graph.AddLink(1, 3));  // Accept a valid addition
		}
	}
}
