using System.IO;
using Xunit;
using LasUtility.ShapefileRasteriser;
using System.Diagnostics;
using LasUtility.Nls;
using LasUtility.Common;
using NetTopologySuite.Geometries;

namespace LasUtility.Tests
{
    public class HeightMapTests
    {
        readonly string _sTestFoldername = Path.Combine("..", "..", "..", "TestFiles", "HeightMap");


        [Fact]
        public void CreateRaster_NonMetric_ShouldWorkOnEdges()
        {
            var hm = new HeightMap();

            int iRowCount = 70;
            int iColumnCount = 160;

            int iCoordinateMinX = 97;
            int iCoordinateMaxX = 257;
            int iCoordinateMinY = 667;
            int iCoordinateMaxY = 757;

            double dEps = 0.0001;

            Envelope extent = new(iCoordinateMinX, iCoordinateMaxX, iCoordinateMinY, iCoordinateMaxY);

            hm.InitializeRaster(iRowCount, iColumnCount, extent);

            // Fill raster with column index
            for (int iRow = 0; iRow < iRowCount; iRow++)
            {
                for (int iColumn = 0; iColumn < iColumnCount; iColumn++)
                {
                    hm.Raster[iRow][iColumn] = (byte)iColumn;
                }
            }

            double d = hm.GetValue(new Coordinate(iCoordinateMaxX, iCoordinateMinY));

            // Should return NaN since Max coordinates are outside of the raster
            Assert.Equal(double.NaN, d);

            d = hm.GetValue(new Coordinate(iCoordinateMaxX - dEps, iCoordinateMinY));

            // Should return 159 since MaxX - dEps is just inside the raster
            Assert.Equal(iColumnCount - 1, d);
        }

        [Fact]
        public void CreateRaster_NonMetric_ShouldWorkOnMiddle()
        {
            var hm = new HeightMap();

            int iRowCount = 2222;
            int iColumnCount = 4488;

            int iCoordinateMinX = 3752;
            int iCoordinateMaxX = 3811;
            int iCoordinateMinY = 144033;
            int iCoordinateMaxY = 144045;

            double dCellWidth = (iCoordinateMaxX - iCoordinateMinX) / (double)iColumnCount; 
            double dCellHeight = (iCoordinateMaxY - iCoordinateMinY) / (double)iRowCount;

            Envelope extent = new(iCoordinateMinX, iCoordinateMaxX, iCoordinateMinY, iCoordinateMaxY);
            hm.InitializeRaster(iRowCount, iColumnCount, extent);

            byte iTestValue = 123;

            double dTestCoordinateY = 144036.334;
            int iTestIndexRow = (int)((dTestCoordinateY - iCoordinateMinY) / dCellHeight);

            double dTestCoordinateX = 3768.99;
            int iTestIndexColumn = (int)((dTestCoordinateX - iCoordinateMinX) / dCellWidth);

            hm.Raster[iTestIndexRow][iTestIndexColumn] = iTestValue;

            double d = hm.GetValue(new Coordinate(dTestCoordinateX, dTestCoordinateY));

            Assert.Equal(iTestValue, d);

        }

        [Fact]
        public void ReadRaster_ShouldContainBuilding()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            string sTestName = "ReadRaster_ShouldContainBuilding";
            string sTestInputFoldername = Path.Combine(_sTestFoldername, sTestName, "Input");

            var hm = HeightMap.CreateFromAscii(Path.Combine(sTestInputFoldername, "buildings_roads.asc"));

            byte classification = (byte)hm.GetValue(new Coordinate(518550, 7044465));
            byte inputClassification = 101;

            Assert.Equal(inputClassification, classification);
        }

        [Fact]
        public void AddRasterAndSave()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            string sTestName = "AddRasterAndSave";
            string sTestInputFoldername = Path.Combine(_sTestFoldername, sTestName, "Input");
            string sTestOutputFoldername = Path.Combine(_sTestFoldername, sTestName, "Output");

            // Delete contents of output folder
            if (Directory.Exists(sTestOutputFoldername))
                Directory.Delete(sTestOutputFoldername, true);

            Directory.CreateDirectory(sTestOutputFoldername);

            string sOutputAscFilename = Path.Combine(sTestOutputFoldername, "buildings_roads.asc");

            var hm = HeightMap.CreateFromAscii(Path.Combine(sTestInputFoldername, "buildings_roads.asc"));

            hm.WriteAsAscii(sOutputAscFilename);

            Assert.True(File.Exists(sOutputAscFilename));

            // Compare file contents with files in test folder
            string sInputAscFilename = Path.Combine(sTestInputFoldername, "buildings_roads.asc");
            Assert.True(File.Exists(sInputAscFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputAscFilename, sOutputAscFilename), "File contents do not match");
        }

        [Fact]
        public void AddRasterAndSaveAsSmaller()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            string sTestName = "AddRasterAndSaveAsSmaller";
            string sTestInputFoldername = Path.Combine(_sTestFoldername, sTestName, "Input");
            string sTestOutputFoldername = Path.Combine(_sTestFoldername, sTestName, "Output");

            // Delete contents of output folder
            if (Directory.Exists(sTestOutputFoldername))
                Directory.Delete(sTestOutputFoldername, true);

            Directory.CreateDirectory(sTestOutputFoldername);

            string sOutputAscFilename = Path.Combine(sTestOutputFoldername, "buildings_roads_smaller.asc");

            var hm = HeightMap.CreateFromAscii(Path.Combine(sTestInputFoldername, "buildings_roads.asc"));

            int iCropInMeters = 200;
            int iMinX = (int)hm.Bounds.MinX + iCropInMeters;
            int iMinY = (int)hm.Bounds.MinY + iCropInMeters;
            int iMaxX = (int)hm.Bounds.MaxX - iCropInMeters;
            int iMaxY = (int)hm.Bounds.MaxY - iCropInMeters;

            hm.WriteAsAscii(sOutputAscFilename, iMinX, iMinY, iMaxX, iMaxY);

            Assert.True(File.Exists(sOutputAscFilename));

            // Compare file contents with files in test folder
            string sInputAscFilename = Path.Combine(sTestInputFoldername, "buildings_roads_smaller.asc");
            Assert.True(File.Exists(sInputAscFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputAscFilename, sOutputAscFilename), "File contents do not match");
        }

        [Fact]
        public void AddRasterAndCrop()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            string sTestName = "AddRasterAndCrop";
            string sTestInputFoldername = Path.Combine(_sTestFoldername, sTestName, "Input");
            string sTestOutputFoldername = Path.Combine(_sTestFoldername, sTestName, "Output");

            // Delete contents of output folder
            if (Directory.Exists(sTestOutputFoldername))
                Directory.Delete(sTestOutputFoldername, true);

            Directory.CreateDirectory(sTestOutputFoldername);

            string sOutputAscFilename = Path.Combine(sTestOutputFoldername, "buildings_roads_smaller.asc");

            var hm = HeightMap.CreateFromAscii(Path.Combine(sTestInputFoldername, "buildings_roads.asc"));

            int iCropInMeters = 200;
            int iMinX = (int)hm.Bounds.MinX + iCropInMeters;
            int iMinY = (int)hm.Bounds.MinY + iCropInMeters;
            int iMaxX = (int)hm.Bounds.MaxX - iCropInMeters;
            int iMaxY = (int)hm.Bounds.MaxY - iCropInMeters;

            var hmSmaller = hm.Crop(iMinX, iMinY, iMaxX, iMaxY);
            hmSmaller.WriteAsAscii(sOutputAscFilename);

            Assert.True(File.Exists(sOutputAscFilename));

            // Compare file contents with files in test folder
            string sInputAscFilename = Path.Combine(sTestInputFoldername, "buildings_roads_smaller.asc");
            Assert.True(File.Exists(sInputAscFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputAscFilename, sOutputAscFilename), "File contents do not match");
        }

        [Fact]
        public void CreateRasterNonMetricAndCrop()
        {
            HeightMap hm = new();
            hm.InitializeRaster(55, 66, new Envelope(1000, 1010, 500, 550));

            RcIndex rc = hm.Bounds.ProjToCell(new Coordinate(1005, 505));

            hm.Raster[rc.Row][rc.Column] = 100;

            HeightMap hmSmaller = hm.Crop(1003, 501, 1008, 520);

            Assert.Equal(100, hmSmaller.Raster[4][14]);
        }

        [Fact]
        public void CreateRasterAndSaveAsCompressed()
        {
            string sTestName = "CreateRasterAndSaveAsCompressed";
            string sTestOutputFoldername = Path.Combine(_sTestFoldername, sTestName, "Output");

            // Delete contents of output folder
            if (Directory.Exists(sTestOutputFoldername))
                Directory.Delete(sTestOutputFoldername, true);

            Directory.CreateDirectory(sTestOutputFoldername);

            HeightMap hm = new();
            hm.InitializeRaster(10, 10, new Envelope(0, 10, 0, 10));

            hm.Raster[5][5] = 1;
            hm.Raster[5][6] = 1;

            string sOutputFilename = Path.Combine(sTestOutputFoldername, "test" + HeightMap.FileExtensionCompressed);

            hm.WriteAsAscii(sOutputFilename);
            Assert.True(File.Exists(sOutputFilename));

            // Read back
            var hmRead = HeightMap.CreateFromAscii(sOutputFilename);
            Assert.Equal(1, hmRead.Raster[5][5]);
            Assert.Equal(1, hmRead.Raster[5][6]);
        }
    }
}
