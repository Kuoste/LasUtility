using System.IO;
using Xunit;
using LasUtility.ShapefileRasteriser;
using System.Diagnostics;
using LasUtility.Nls;
using LasUtility.Common;

namespace LasUtility.Tests
{
    public class HeightMapTests
    {
        readonly string _sTestFoldername = Path.Combine("..", "..", "..", "TestFiles", "HeightMap");


        [Fact]
        public void ReadRaster_ShouldContainBuilding()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            string sTestName = "ReadRaster_ShouldContainBuilding";
            string sTestInputFoldername = Path.Combine(_sTestFoldername, sTestName, "Input");

            var rasteriser = Rasteriser.CreateFromAscii(Path.Combine(sTestInputFoldername, "buildings_roads.asc"));

            byte classification = (byte)rasteriser.GetHeight(518550, 7044465);
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

            var rasteriser = Rasteriser.CreateFromAscii(Path.Combine(sTestInputFoldername, "buildings_roads.asc"));

            rasteriser.WriteAsAscii(sOutputAscFilename);

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

            var rasteriser = Rasteriser.CreateFromAscii(Path.Combine(sTestInputFoldername, "buildings_roads.asc"));

            int iCropInMeters = 200;
            int iMinX = (int)rasteriser.Bounds.MinX + iCropInMeters;
            int iMinY = (int)rasteriser.Bounds.MinY + iCropInMeters;
            int iMaxX = (int)rasteriser.Bounds.MaxX - iCropInMeters;
            int iMaxY = (int)rasteriser.Bounds.MaxY - iCropInMeters;

            rasteriser.WriteAsAscii(sOutputAscFilename, iMinX, iMinY, iMaxX, iMaxY);

            Assert.True(File.Exists(sOutputAscFilename));

            // Compare file contents with files in test folder
            string sInputAscFilename = Path.Combine(sTestInputFoldername, "buildings_roads_smaller.asc");
            Assert.True(File.Exists(sInputAscFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputAscFilename, sOutputAscFilename), "File contents do not match");
        }

    }
}
