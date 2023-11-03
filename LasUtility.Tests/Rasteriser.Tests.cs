using System.IO;
using Xunit;
using LasUtility.ShapefileRasteriser;
using System.Diagnostics;
using LasUtility.Nls;
using LasUtility.Common;
#if OPEN_CV
using OpenCvSharp;
#endif

namespace LasUtility.Tests
{
    public class RasteriserTests
    {
        readonly string _sTestFoldername = Path.Combine("..", "..", "..", "TestFiles", "Rasteriser");

        [Fact]
        public void AddShapefileAndSave()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            string sTestName = "AddShapefileAndSave";
            string sTestInputFoldername = Path.Combine(_sTestFoldername, sTestName, "Input");
            string sTestOutputFoldername = Path.Combine(_sTestFoldername, sTestName, "Output");

            // Delete contents of output folder
            if (Directory.Exists(sTestOutputFoldername))
                Directory.Delete(sTestOutputFoldername, true);

            string sOutputAscFilename = Path.Combine(sTestOutputFoldername, "buildings_roads.asc");
            string sOutputPngFilename = Path.Combine(sTestOutputFoldername, "buildings_roads.png");

            // Create folders if they don't exist
            if (!Directory.Exists(sTestInputFoldername))
                Directory.CreateDirectory(sTestInputFoldername);

            if (!Directory.Exists(sTestOutputFoldername))
                Directory.CreateDirectory(sTestOutputFoldername);

            var rasteriser = new Rasteriser();
            string[] shpFullFilenames = Directory.GetFiles(sTestInputFoldername, "*.shp");

            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.BuildingPolygonClassesToRasterValues);
            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.RoadLineClassesToRasterValues);

            rasteriser.InitializeRaster(shpFullFilenames);

            foreach (string filename in shpFullFilenames)
                rasteriser.AddShapefile(filename);

            rasteriser.WriteAsAscii(sOutputAscFilename);
            Assert.True(File.Exists(sOutputAscFilename));
            string sInputAscFilename = Path.Combine(sTestInputFoldername, "buildings_roads.asc");
            Assert.True(File.Exists(sInputAscFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputAscFilename, sOutputAscFilename), "ASC file contents do not match");

#if OPEN_CV
            rasteriser.WriteAsPng(sOutputPngFilename);
            Assert.True(File.Exists(sOutputPngFilename));
            string sInputPngFilename = Path.Combine(sTestInputFoldername, "buildings_roads.png");
            Assert.True(File.Exists(sInputPngFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputPngFilename, sOutputPngFilename), "SHP file contents do not match");
#endif

        }

        [Fact]
        public void AddShapefileAndSaveAsCompressed()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            string sTestName = "AddShapefileAndSaveAsCompressed";
            string sTestInputFoldername = Path.Combine(_sTestFoldername, sTestName, "Input");
            string sTestOutputFoldername = Path.Combine(_sTestFoldername, sTestName, "Output");

            // Delete contents of output folder
            if (Directory.Exists(sTestOutputFoldername))
                Directory.Delete(sTestOutputFoldername, true);

            string sFileName = "buildings_roads" + HeightMap.FileExtensionCompressed;
            string sOutputFilename = Path.Combine(sTestOutputFoldername, sFileName);

            // Create folder if needed
            if (!Directory.Exists(sTestOutputFoldername))
                Directory.CreateDirectory(sTestOutputFoldername);

            var rasteriser = new Rasteriser();
            string[] shpFullFilenames = Directory.GetFiles(sTestInputFoldername, "*.shp");

            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.BuildingPolygonClassesToRasterValues);
            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.RoadLineClassesToRasterValues);

            rasteriser.InitializeRaster(shpFullFilenames);

            foreach (string filename in shpFullFilenames)
                rasteriser.AddShapefile(filename);

            rasteriser.WriteAsAscii(sOutputFilename);
            Assert.True(File.Exists(sOutputFilename));
            string sInputFilename = Path.Combine(sTestInputFoldername, sFileName);
            Assert.True(File.Exists(sInputFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputFilename, sOutputFilename), "ASC file contents do not match");
        }
    }
}
