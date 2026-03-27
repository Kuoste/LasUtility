using LasUtility.Common;
using LasUtility.DEM;
using LasUtility.LAS;
using NetTopologySuite.Geometries;
using Xunit;


namespace LasUtility.Tests
{
    public class TriangulationTests
    {
        readonly string _sTestFoldername = Path.Combine("..", "..", "..", "TestFiles", "Triangulation");

        [Fact]
        public void AddPointCloudAndTriangulate()
        {
            string sTestName = "AddPointCloudAndTriangulate";
            string sTestInputFoldername = Path.Combine(_sTestFoldername, sTestName, "Input");
            string sTestOutputFoldername = Path.Combine(_sTestFoldername, sTestName, "Output");

            PrepareOutputFolder(sTestOutputFoldername);

            string sLasFullFileName = Path.Combine(sTestInputFoldername, "Q5232E1_cropped.laz");
            string sOutputShpFilename = Path.Combine(sTestOutputFoldername, "DEM.shp");

            var reader = new LasZipNetReader();
            reader.ReadHeader(sLasFullFileName);

            ITriangulation tri = CreateTriangulation(reader);
            RunAddAndTriangulate(reader, sLasFullFileName, sOutputShpFilename, tri);

            Assert.True(tri.GetTriangleCount() > 0);

            string sInputShpFilename = Path.Combine(sTestInputFoldername, "DEM.shp");
            Assert.True(File.Exists(sInputShpFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputShpFilename, sOutputShpFilename), "File contents do not match");
        }

        private static ITriangulation CreateTriangulation(ILasFileReader reader)
        {
            double dPadding = 0.5;
            double minX = reader.MinX - dPadding;
            double minY = reader.MinY - dPadding;
            double maxX = reader.MaxX + dPadding;
            double maxY = reader.MaxY + dPadding;

            int iResolution = 100;

            return new SurfaceTriangulation(iResolution, iResolution,
                Math.Floor(minX), Math.Floor(minY),
                Math.Ceiling(maxX), Math.Ceiling(maxY));
        }

        private static void RunAddAndTriangulate(ILasFileReader reader, string inputLasFullFilename, string outputShpFullFilename, ITriangulation tri)
        {
            reader.OpenReader(inputLasFullFilename);

            foreach (LasPoint p in reader.Points())
            {
                tri.AddPoint(p);
            }

            tri.Create();

            tri.ExportToShp(outputShpFullFilename);

            reader.CloseReader();

            reader.Dispose();
        }

        private static void PrepareOutputFolder(string outputFoldername)
        {
            if (Directory.Exists(outputFoldername))
                Directory.Delete(outputFoldername, true);

            if (!Directory.Exists(outputFoldername))
                Directory.CreateDirectory(outputFoldername);
        }

        [Fact]
        public void RasteriseDem_ShouldNotOverwriteExistingValues()
        {
            ITriangulation tri = CreateFlatPlaneTriangulation();
            AssertRasteriseDoesNotOverwrite(tri);
        }

        /// <summary>
        /// Creates a triangulation from a 5x5 grid of points on z = 100.
        /// </summary>
        private static ITriangulation CreateFlatPlaneTriangulation()
        {
            const double minX = 0;
            const double minY = 0;
            const double maxX = 10;
            const double maxY = 10;
            const double flatZ = 100.0;
            const int gridRes = 10;

            ITriangulation tri = new SurfaceTriangulation(gridRes, gridRes, minX, minY, maxX, maxY);

            // 5x5 grid of points covering the interior
            for (int ix = 1; ix <= 9; ix += 2)
            {
                for (int iy = 1; iy <= 9; iy += 2)
                {
                    tri.AddPoint(new LasPoint { x = ix, y = iy, z = flatZ, classification = 2 });
                }
            }

            tri.Create();
            return tri;
        }

        private static void AssertRasteriseDoesNotOverwrite(ITriangulation tri)
        {
            const int nRows = 10;
            const int nCols = 10;
            IRasterBounds bounds = new RasterBounds(nRows, nCols, 0, 0, 10, 10);

            float[,] dem = new float[nRows, nCols];
            for (int r = 0; r < nRows; r++)
                for (int c = 0; c < nCols; c++)
                    dem[r, c] = float.NaN;

            // Pre-fill one cell with a known value
            const float existingValue = 999.0f;
            dem[5, 5] = existingValue;

            // Lock the cell to prevent overwriting
            bool[,] lockedCells = new bool[nRows, nCols];
            lockedCells[5, 5] = true;

            RasteriseDemRequest request = new(dem, bounds)
            {
                LockedCells = lockedCells
            };

            tri.RasteriseDem(request);

            Assert.Equal(existingValue, dem[5, 5]);
        }

        [Fact]
        public void RasteriseDem_WhenClassificationMetadataRequested_ShouldFillClassificationRaster()
        {
            const int nRows = 10;
            const int nCols = 10;

            ITriangulation tri = CreateFlatPlaneTriangulation();
            IRasterBounds bounds = new RasterBounds(nRows, nCols, 0, 0, 10, 10);

            float[,] dem = new float[nRows, nCols];
            byte[,] classification = new byte[nRows, nCols];

            for (int r = 0; r < nRows; r++)
                for (int c = 0; c < nCols; c++)
                    dem[r, c] = float.NaN;

            var request = new RasteriseDemRequest(dem, bounds);
            request.ByteMetadata[RasteriseDemRequest.ClassificationMetadataName] = classification;

            tri.RasteriseDem(request);

            int classifiedCount = 0;
            for (int r = 0; r < nRows; r++)
            {
                for (int c = 0; c < nCols; c++)
                {
                    if (!float.IsNaN(dem[r, c]))
                    {
                        Assert.Equal((byte)2, classification[r, c]);
                        classifiedCount++;
                    }
                }
            }

            Assert.True(classifiedCount > 0, "RasteriseDem should fill classification metadata for rasterised cells");
        }
    }
}
