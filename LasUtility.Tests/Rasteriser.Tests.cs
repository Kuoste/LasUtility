﻿using System.IO;
using Xunit;
using LasUtility.ShapefileRasteriser;
using System.Diagnostics;
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