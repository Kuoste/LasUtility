using System.IO;
using Xunit;
using LasUtility.Shapefile;
using System.Diagnostics;

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

            rasteriser.InitializeRaster(shpFullFilenames);

            foreach (string filename in shpFullFilenames)
                rasteriser.AddShapefile(filename);

            rasteriser.WriteAsAscii(sOutputAscFilename);
            rasteriser.WriteAsPng(sOutputPngFilename);

            Assert.True(File.Exists(sOutputAscFilename));
            Assert.True(File.Exists(sOutputPngFilename));

            // Compare file contents with files in test folder
            string sInputAscFilename = Path.Combine(sTestInputFoldername, "buildings_roads.asc");
            string sInputPngFilename = Path.Combine(sTestInputFoldername, "buildings_roads.png");
            Assert.True(File.Exists(sInputAscFilename), "Reference file does not exists in Input folder");
            Assert.True(File.Exists(sInputPngFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputAscFilename, sOutputAscFilename), "File contents do not match");
            Assert.True(Utils.FileCompare(sInputPngFilename, sOutputPngFilename), "File contents do not match");
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

            string sOutputAscFilename = Path.Combine(sTestOutputFoldername, "buildings_roads.asc");

            // Create folders if they don't exist
            if (!Directory.Exists(sTestInputFoldername))
                Directory.CreateDirectory(sTestInputFoldername);

            if (!Directory.Exists(sTestOutputFoldername))
                Directory.CreateDirectory(sTestOutputFoldername);

            var rasteriser = Rasteriser.CreateFromAscii(Path.Combine(sTestInputFoldername, "buildings_roads.asc"));

            rasteriser.WriteAsAscii(sOutputAscFilename);

            Assert.True(File.Exists(sOutputAscFilename));

            // Compare file contents with files in test folder
            string sInputAscFilename = Path.Combine(sTestInputFoldername, "buildings_roads.asc");
            Assert.True(File.Exists(sInputAscFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputAscFilename, sOutputAscFilename), "File contents do not match");
        }
    }
}
