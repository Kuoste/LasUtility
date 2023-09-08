using LasUtility.LAS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LasUtility.Tests
{
    public class VoxelGridTests
    {
        readonly string _sTestFoldername = Path.Combine("..", "..", "..", "TestFiles", "VoxelGrid");

        [Fact]
        public void AddPoints_WhenOnEdges_AndSaveAndLoad_ShouldBeTheSame()
        {
            int iGridSize = 10;
            double dMinX = 0.0;
            double dMinY = 10.0;
            double dMaxX = 10.0; // 10.0 is outside the area, but 9.999999 is inside.
            double dMaxY = 20.0; // 20.0 is outside the area, but 19.999999 is inside.
            double dEps = 0.00000001;

            LasPoint p1 = new LasPoint() { x = dMinX, y = dMinY, z = 100 };
            LasPoint p2 = new LasPoint() { x = dMaxX - dEps, y = dMaxY - dEps, z = 10 };
            LasPoint p3 = new LasPoint() { x = dMinX + 5, y = dMaxY - dEps, z = 5 };

            VoxelGrid.VoxelGrid grid = VoxelGrid.VoxelGrid.CreateGrid(iGridSize, iGridSize, dMinX, dMinY, dMaxX, dMaxY);

            Assert.True(grid.AddPoint(p1.x, p1.y, p1.z, p1.classification, true));
            Assert.True(grid.AddPoint(p2.x, p2.y, p2.z, p1.classification, true));
            Assert.True(grid.AddPoint(p3.x, p3.y, p3.z, p1.classification, true));

            int iRow, jCol;

            grid.GetGridIndexes(p1.x, p1.y, out iRow, out jCol);
            Assert.Equal(p1.z, grid.GetGroundMedian(iRow, jCol));

            grid.GetGridIndexes(p2.x, p2.y, out iRow, out jCol);
            Assert.Equal(p2.z, grid.GetGroundMedian(iRow, jCol));

            grid.GetGridIndexes(p3.x, p3.y, out iRow, out jCol);
            Assert.Equal(p3.z, grid.GetGroundMedian(iRow, jCol));
        }

        [Fact]
        public void AddPointsAndSave()
        {
            string sTestName = "AddPointsAndSave";
            string sTestInputFoldername = Path.Combine(_sTestFoldername, sTestName, "Input");
            string sTestOutputFoldername = Path.Combine(_sTestFoldername, sTestName, "Output");

            string sInputFilename = Path.Combine(sTestInputFoldername, "points.obj");
            string sOutputFilename = Path.Combine(sTestOutputFoldername, "points.obj");

            int iGridSize = 10;
            double dMinX = 0.0;
            double dMinY = 100000.0;
            double dMaxX = 10.0; // 10.0 is outside the area, but 9.999999 is inside.
            double dMaxY = 200000.0; // 200000.0 is outside the area, but 199999.999999 is inside.
            double dEps = 0.000001;

            LasPoint p1 = new LasPoint() { x = dMinX, y = dMinY, z = 100 };
            LasPoint p2 = new LasPoint() { x = dMaxX - dEps, y = dMaxY - dEps, z = 10 };
            LasPoint p3 = new LasPoint() { x = dMinX + 5, y = dMinY + 55000, z = 5 };

            VoxelGrid.VoxelGrid grid = VoxelGrid.VoxelGrid.CreateGrid(iGridSize, iGridSize, dMinX, dMinY, dMaxX, dMaxY);

            Assert.True(grid.AddPoint(p1.x, p1.y, p1.z, p1.classification, true));
            Assert.True(grid.AddPoint(p2.x, p2.y, p2.z, p1.classification, true));
            Assert.True(grid.AddPoint(p3.x, p3.y, p3.z, p1.classification, true));

            grid.Serialize(sOutputFilename);

            Assert.True(File.Exists(sOutputFilename));
            Assert.True(File.Exists(sInputFilename));

            Assert.True(Utils.FileCompare(sOutputFilename, sInputFilename));
        }

        [Fact]
        public void LoadPoints()
        {
            string sTestName = "LoadPoints";
            string sTestInputFoldername = Path.Combine(_sTestFoldername, sTestName, "Input");
            string sTestOutputFoldername = Path.Combine(_sTestFoldername, sTestName, "Output");

            string sInputFilename = Path.Combine(sTestInputFoldername, "points.obj");

            double dMinX = 0.0;
            double dMinY = 100000.0;
            double dMaxX = 10.0; // 10.0 is outside the area, but 9.999999 is inside.
            double dMaxY = 200000.0; // 200000.0 is outside the area, but 199999.999999 is inside.
            double dEps = 0.000001;

            LasPoint p1 = new LasPoint() { x = dMinX, y = dMinY, z = 100 };
            LasPoint p2 = new LasPoint() { x = dMaxX - dEps, y = dMaxY - dEps, z = 10 };
            LasPoint p3 = new LasPoint() { x = dMinX + 5, y = dMinY + 55000, z = 5 };

            int iRow, jCol;

            VoxelGrid.VoxelGrid grid = VoxelGrid.VoxelGrid.Deserialize(sInputFilename);

            grid.GetGridIndexes(p1.x, p1.y, out iRow, out jCol);
            Assert.Equal(p1.z, grid.GetGroundMedian(iRow, jCol));

            grid.GetGridIndexes(p2.x, p2.y, out iRow, out jCol);
            Assert.Equal(p2.z, grid.GetGroundMedian(iRow, jCol));

            grid.GetGridIndexes(p3.x, p3.y, out iRow, out jCol);
            Assert.Equal(p3.z, grid.GetGroundMedian(iRow, jCol));
        }
    }
}
