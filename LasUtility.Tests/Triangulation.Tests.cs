using LasUtility.DEM;
using LasUtility.LAS;
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

            // Delete contents of output folder
            if (Directory.Exists(sTestOutputFoldername))
                Directory.Delete(sTestOutputFoldername, true);

            string sOutputShpFilename = Path.Combine(sTestOutputFoldername, "DEM.shp");

            // Create folders if they don't exist
            if (!Directory.Exists(sTestInputFoldername))
                Directory.CreateDirectory(sTestInputFoldername);

            if (!Directory.Exists(sTestOutputFoldername))
                Directory.CreateDirectory(sTestOutputFoldername);

            string sLasFullFileName = Path.Combine(sTestInputFoldername, "Q5232E1_cropped.laz");
            ILasFileReader reader = new LasZipFileReader();
            reader.ReadHeader(sLasFullFileName);

            // Add padding because for some reason the triagulation is a little bit bigger. 
            // But is does not matter because it will be cropped to get rid of the unwanted huge triangles on the edges.
            double dPadding = 0.5;
            double minX = reader.MinX - dPadding;
            double minY = reader.MinY - dPadding;
            double maxX = reader.MaxX + dPadding;
            double maxY = reader.MaxY + dPadding;

            int iResolution = 100;

            SurfaceTriangulation tri = new (iResolution, iResolution, minX, minY, maxX, maxY);

            reader.OpenReader(sLasFullFileName);

            LasPoint p;

            while ((p = reader.ReadPoint()) != null)
            {
                tri.AddPoint(p);
            }

            tri.Create();

            tri.ExportToShp(sOutputShpFilename);

            reader.CloseReader();

            string sInputShpFilename = Path.Combine(sTestInputFoldername, "DEM.shp");
            Assert.True(File.Exists(sInputShpFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputShpFilename, sOutputShpFilename), "File contents do not match");
        }
    }
}
