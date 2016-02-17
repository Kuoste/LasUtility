using DotSpatial.Data;
using DotSpatial.Topology;
using LasReader.DEM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LasUtility.DEM;

namespace LasUtility.VoxelGrid
{
    public class VoxelGrid : IHeightMap
    {
        IRasterBounds _bounds;
        Bin[][] _grid;

        public int nRows { get; private set; }
        public int nCols { get; private set; }

        public static VoxelGrid CreateGrid(int nRows, int nCols, double minX, double minY, double maxX, double maxY)
        {
            Extent extent = new Extent(minX, minY, maxX, maxY);
            VoxelGrid voxelGrid = new VoxelGrid();

            voxelGrid.nRows = nRows;
            voxelGrid.nCols = nCols;
            voxelGrid._bounds= new RasterBounds(nRows, nCols, extent);

            Bin[][] grid = new Bin[nRows][];

            for (int iRow = 0; iRow < grid.Count(); iRow++)
            {
                grid[iRow] = new Bin[nCols];

                for (int jCol = 0; jCol < nCols; jCol++)
                    grid[iRow][jCol] = new Bin();
            }        

            voxelGrid._grid = grid;

            return voxelGrid;
        }

        private bool GetIndexes(double x, double y, out int iRow, out int jCol)
        {
            Coordinate coord = new Coordinate(x, y);
            RcIndex rc = _bounds.ProjToCell(coord);


            iRow = rc.Row;
            jCol = rc.Column;

            bool InBounds = false;

            if ((jCol >= 0 && jCol < nCols && iRow >= 0 && iRow < nRows))
            {
                InBounds = true;
            }

            return InBounds;

        }

        public bool AddPoint(double x, double y, double z, byte classification)
        {
            int iRow, jCol;
            bool IsAdded = false;

            if (GetIndexes(x, y, out iRow, out jCol))
            {
                _grid[iRow][jCol].AddPoint(z, classification);
                IsAdded = true;
            }

            return IsAdded;
        }

        public void SetMissingHeights(IHeightMap tri)
        {
            int nMissingBefore = 0;
            int nMissingAfter = 0;

            for (int iRow = 0; iRow < nRows; iRow++)
            {
                for (int jCol = 0; jCol < nCols; jCol++)
                {

                    double median = GetGroundMedian(iRow, jCol);

                    if (double.IsNaN(median))
                    {
                        nMissingBefore++;
                        Coordinate center = _bounds.CellCenter_ToProj(iRow, jCol);
                        median = tri.GetHeight(center.X, center.Y);
                        _grid[iRow][jCol].ReferenceHeight = median;

                        if (double.IsNaN(median))
                            nMissingAfter++;
                    }
                }
            }
        }

        public void SortFromHighestToLowest()
        {
            foreach (Bin[] row in _grid)
            {
                foreach (Bin b in row)
                {
                    b.OrderPointsFromHighestToLowest();
                }
            }
        }

        public double GetGroundMedian(int iRow, int jCol)
        {
            return _grid[iRow][jCol].GetGroundMedian();
        }

        public double GetHeight(double x, double y)
        {
            int iRow, jCol;
            double ret = double.NaN;

            if (GetIndexes(x, y, out iRow, out jCol))
            {
                ret = GetGroundMedian(iRow, jCol);
            }

            return ret;
        }
    }
}
