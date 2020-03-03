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
            //var result = await GeometryValidation.CleanPolygonAsync("POLYGON ((0 0, 15 20, 20 30, 25 45, 30 100, 45 40, 47 0, 50 1, 55 5, 50 10, 45 5, 0 0))", "artefact");
            var result = await GeometryValidation.CleanPolygonAsync("POLYGON ((0 0, 30 110, 95 109, 146 64, 195 127, 150 210, 280 240, 150 170, 144 105, 75 84, 63 25, 0 0))", "artefact");
            Assert.Equal("POLYGON ((0 0, 30 110, 95 109, 110.8625162144635 95.003662163708682, 111.03316738804288 94.966616161578273, 144 105, 150 170, 166.69817957580403 178.99132746389449, 166.73856425377144 179.12664815415488, 150 210, 280 240, 166.8742737957885 179.08614742850148, 166.83388911782109 178.95082673824109, 195 127, 146 64, 111.0124837855365 94.871337836291318, 110.84183261195712 94.908383838421727, 75 84, 63 25, 0 0))", result);

            result = await GeometryValidation.CleanPolygonAsync("POLYGON ((0 0, 15 20, 20 30, 25 45, 30 100, 45 40, 47 0, 50 1, 55 5, 50 10, 45 5, 0 0))", "artefact");
            Assert.Equal("POLYGON ((0 0, 15 20, 20 30, 25 45, 30 100, 45 40, 46.661672904972278 6.7665419005544516, 46.737377344785322 6.7373773447853216, 50 10, 55 5, 50 1, 47 0, 46.671660428361051 6.5667914327788823, 46.595955988548006 6.5959559885480123, 45 5, 0 0))", result);

            result = await GeometryValidation.CleanPolygonAsync("POLYGON ((0 0, 30 110, 95 109, 146 64, 195 127, 150 210, 280 240, 150 170, 144 105, 75 84, 63 25, 0 0), (40 60, 60 50, 30 40, 40 60)", "artefact");
            Assert.Equal("POLYGON ((0 0, 30 110, 95 109, 110.8625162144635 95.003662163708682, 111.03316738804288 94.966616161578273, 144 105, 150 170, 166.69817957580403 178.99132746389449, 166.73856425377144 179.12664815415488, 150 210, 280 240, 166.8742737957885 179.08614742850148, 166.83388911782109 178.95082673824109, 195 127, 146 64, 111.0124837855365 94.871337836291318, 110.84183261195712 94.908383838421727, 75 84, 63 25, 0 0), (30 40, 60 50, 40 60, 30 40))", result);
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