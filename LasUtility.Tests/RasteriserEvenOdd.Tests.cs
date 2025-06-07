using System.IO;
using Xunit;
using LasUtility.Shapefile;
using System.Diagnostics;
using LasUtility.Nls;
using LasUtility.Common;
using NetTopologySuite.Geometries;
#if OPEN_CV
using OpenCvSharp;
#endif

namespace LasUtility.Tests
{
    public class RasteriserEvenOddTests
    {
        readonly string _sTestFoldername = Path.Combine("..", "..", "..", "TestFiles", "RasteriserEvenOdd");

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

            //string sFilename = "terraintype" + ByteRaster.FileExtension
            string sFilename = "terraintype" + ByteRaster.FileExtensionCompressed;
            string sOutputAscFilename = Path.Combine(sTestOutputFoldername, sFilename);

            // Create folders if they don't exist
            if (!Directory.Exists(sTestInputFoldername))
                Directory.CreateDirectory(sTestInputFoldername);

            if (!Directory.Exists(sTestOutputFoldername))
                Directory.CreateDirectory(sTestOutputFoldername);

            IShapefileRasteriser rasteriser = new RasteriserEvenOdd();
            string[] shpFullFilenames = Directory.GetFiles(sTestInputFoldername, "*.shp");

            rasteriser.InitializeRaster(shpFullFilenames);

            Envelope bounds = new();
            foreach (string filename in shpFullFilenames)
            {
                string[] splitNames = Path.GetFileName(filename).Split('_');

                if (splitNames.Length < 3)
                    throw new Exception("Filename not recognised as NLS shapefile");

                TileNamer.Decode(splitNames[1], out Envelope b);
                bounds.ExpandToInclude(b);
            }

            rasteriser.InitializeRaster(bounds);

            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.WaterPolygonClassesToRasterValues);
            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.WaterLineClassesToRasterValues);
            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.SwampPolygonClassesToRasterValues);
            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.FieldPolygonClassesToRasterValues);
            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.RockPolygonClassesToRasterValues);
            rasteriser.AddRasterizedClassesWithRasterValues(TopographicDb.SandPolygonClassesToRasterValues);

            foreach (string filename in shpFullFilenames)
                rasteriser.RasteriseShapefile(filename);

            rasteriser.WriteAsAscii(sOutputAscFilename);
            Assert.True(File.Exists(sOutputAscFilename));
            string sInputAscFilename = Path.Combine(sTestInputFoldername, sFilename);
            Assert.True(File.Exists(sInputAscFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputAscFilename, sOutputAscFilename), "ASP file contents do not match");

        }
    }
}
