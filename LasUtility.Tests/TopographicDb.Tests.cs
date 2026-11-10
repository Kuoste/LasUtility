using System.Collections.Generic;
using LasUtility.Nls;
using LasUtility.Shapefile;
using Xunit;

namespace LasUtility.Tests
{
    public class TopographicDbTests
    {
        [Fact]
        public void ClassMapsCannotBeModified()
        {
            IReadOnlyDictionary<int, byte>[] maps =
            {
                TopographicDb.WaterLineClassesToRasterValues,
                TopographicDb.RoadLineClassesToRasterValues,
                TopographicDb.BuildingPolygonClassesToRasterValues,
                TopographicDb.WaterPolygonClassesToRasterValues,
                TopographicDb.SwampPolygonClassesToRasterValues,
                TopographicDb.FieldPolygonClassesToRasterValues,
                TopographicDb.RockPolygonClassesToRasterValues,
                TopographicDb.RockLineClassesToRasterValues,
                TopographicDb.SandPolygonClassesToRasterValues,
            };

            foreach (IReadOnlyDictionary<int, byte> map in maps)
            {
                Assert.NotEmpty(map);

                // Casting back to a mutable interface must not allow changes
                IDictionary<int, byte> mutable = Assert.IsAssignableFrom<IDictionary<int, byte>>(map);
                Assert.True(mutable.IsReadOnly);
                Assert.Throws<System.NotSupportedException>(() => mutable[1] = 1);
            }
        }

        [Fact]
        public void AddingSameClassesTwiceDoesNotThrow()
        {
            RasteriserEvenOdd rasteriser = new();

            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.WaterPolygonClassesToRasterValues);
            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.WaterPolygonClassesToRasterValues);

            // A plain Dictionary is still accepted
            rasteriser.AddRasterizedClassesWithRasterValues(new Dictionary<int, byte> { { 36200, 200 } });
            rasteriser.RemoveRasterizedClassesWithRasterValues(TopographicDb.WaterPolygonClassesToRasterValues);
        }
    }
}
