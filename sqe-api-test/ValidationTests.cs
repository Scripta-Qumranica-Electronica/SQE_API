using System.Threading.Tasks;
using Xunit;
using SQE.API.Server.Helpers;

namespace SQE.ApiTest
{
    public class ValidationTests
    {
        [Fact]
        public async Task FixesSelfIntersectingErrorPoly()
        {
            var result = await GeometryValidation.CleanPolygonAsync("POLYGON ((0 0, 15 20, 20 30, 25 45, 30 100, 45 40, 47 0, 50 1, 55 5, 50 10, 45 5, 0 0))", "artefact");
            Assert.Equal("POLYGON ((0 0, 15 20, 20 30, 25 45, 30 100, 45 40, 46.6060606060606 7.878787878787878, 50 10, 55 5, 50 1, 47 0, 45.185185185185183 5.1851851851851842, 45 5, 0 0))", result);
        }

        [Fact]
        public async Task FixesInnerOuterEdgeErrorPoly()
        {
            var result = await GeometryValidation.CleanPolygonAsync("POLYGON((0 0, 10 0, 10 10, 0 10, 0 0),(5 2,5 7,10 7, 10 2, 5 2))", "artefact");
            Assert.Equal("POLYGON ((0 0, 0 10, 10 10, 10 7, 5 7, 5 2, 10 2, 10 0, 0 0))", result);
        }

        [Fact]
        public async Task FixesDanglingEdgeErrorPoly()
        {
            var result = await GeometryValidation.CleanPolygonAsync("POLYGON((0 0, 10 0, 15 5, 10 0, 10 10, 0 10, 0 0))", "artefact");
            Assert.Equal("POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0))", result);
        }

        [Fact]
        public async Task FixesOpenPoly()
        {
            var result = await GeometryValidation.CleanPolygonAsync("POLYGON((0 0, 10 0, 10 10, 0 10))", "artefact");
            Assert.Equal("POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0))", result);
        }

        [Fact]
        public async Task FixesAdjacentInnerRingsPolys()
        {
            var result = await GeometryValidation.CleanPolygonAsync(
                "POLYGON((0 0, 10 0, 10 10, 0 10, 0 0), (1 1, 1 8, 3 8, 3 1, 1 1), (3 1, 3 8, 5 8, 5 1, 3 1))",
                "artefact"
            );
            Assert.Equal("POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0), (1 1, 3 1, 5 1, 5 8, 3 8, 1 8, 1 1))", result);
        }

        [Fact]
        public async Task FixesNestedInnerPolys()
        {
            var result = await GeometryValidation.CleanPolygonAsync(
                "POLYGON((0 0, 10 0, 10 10, 0 10, 0 0), (2 8, 5 8, 5 2, 2 2, 2 8), (3 3, 4 3, 3 4, 3 3))",
                "artefact"
            );
            Assert.Equal("POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0), (2 2, 5 2, 5 8, 2 8, 2 2))", result);
        }

        [Fact]
        public async Task FixesWrongOrientationPolys()
        {
            var result = await GeometryValidation.CleanPolygonAsync(
                "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))",
                "artefact"
            );
            Assert.Equal("POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0))", result);
        }

        [Fact]
        public async Task MergesVeryClosePolys()
        {
            var result = await GeometryValidation.CleanPolygonAsync(
                "POLYGON ((0 0, 0 10, 10 10, 10 0, 0 0),(11 11, 11 20, 20 20, 20 11, 11 11))",
                "artefact"
            );
            Assert.Equal("POLYGON ((0 0, 0 10, 10.091743119266056 10.917431192660551, 11 20, 20 20, 20 11, 10.917431192660551 10.091743119266056, 10 0, 0 0))", result);
        }

        // TODO: Add maybe a few more complex example we have encountered
    }
}