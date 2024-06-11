using LasUtility.DEM;
using LasUtility.LAS;
using Xunit;


namespace LasUtility.Tests
{
    public class TriangulationTests
    {
        readonly string _sTestFoldername = Path.Combine("..", "..", "..", "TestFiles", "Triangulation");

        //[Fact]
        //public void AddPointCloudAndTriangulate_LasZipPort()
        //{
        //    string sTestName = "AddPointCloudAndTriangulate";
        //    string sTestInputFoldername = Path.Combine(_sTestFoldername, sTestName, "Input");
        //    string sTestOutputFoldername = Path.Combine(_sTestFoldername, sTestName, "Output");

        //    // Delete contents of output folder
        //    if (Directory.Exists(sTestOutputFoldername))
        //        Directory.Delete(sTestOutputFoldername, true);

        //    string sOutputShpFilename = Path.Combine(sTestOutputFoldername, "DEM.shp");

        //    // Create folders if they don't exist
        //    if (!Directory.Exists(sTestInputFoldername))
        //        Directory.CreateDirectory(sTestInputFoldername);

        //    if (!Directory.Exists(sTestOutputFoldername))
        //        Directory.CreateDirectory(sTestOutputFoldername);

        //    string sLasFullFileName = Path.Combine(sTestInputFoldername, "Q5232E1_cropped.laz");

        //    RunAddAndTriangulate(new LasZipFileReader(), sLasFullFileName, sOutputShpFilename);

        //    string sInputShpFilename = Path.Combine(sTestInputFoldername, "DEM.shp");
        //    Assert.True(File.Exists(sInputShpFilename), "Reference file does not exists in Input folder");
        //    Assert.True(Utils.FileCompare(sInputShpFilename, sOutputShpFilename), "File contents do not match");
        //}

        [Fact]
        public void AddPointCloudAndTriangulate_LasZipPInvoke()
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

            RunAddAndTriangulate(new LasZipNetReader(), sLasFullFileName, sOutputShpFilename);

            string sInputShpFilename = Path.Combine(sTestInputFoldername, "DEM.shp");
            Assert.True(File.Exists(sInputShpFilename), "Reference file does not exists in Input folder");
            Assert.True(Utils.FileCompare(sInputShpFilename, sOutputShpFilename), "File contents do not match");
        }

        private void RunAddAndTriangulate(ILasFileReader reader, string inputLasFullFilename, string outputShpFullFilename)
        {
            reader.ReadHeader(inputLasFullFilename);

            // Add padding because for some reason the triagulation is a little bit bigger. 
            // But is does not matter because it will be cropped to get rid of the unwanted huge triangles on the edges.
            double dPadding = 0.5;
            double minX = reader.MinX - dPadding;
            double minY = reader.MinY - dPadding;
            double maxX = reader.MaxX + dPadding;
            double maxY = reader.MaxY + dPadding;

            int iResolution = 100;

            SurfaceTriangulation tri = new(iResolution, iResolution,
                Math.Floor(minX), Math.Floor(minY),
                Math.Ceiling(maxX), Math.Ceiling(maxY));

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
    }
}
