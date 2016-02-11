using DotSpatial.Data;
using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasUtility.VoxelGrid
{


    public class VoxelGrid
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

            for (int i = 0; i < grid.Count(); i++)
                grid[i] = new Bin[nCols];

            voxelGrid._grid = grid;

            return voxelGrid;
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public bool AddPoint(double x, double y, double z, byte classification)
        {
            Coordinate coord = new Coordinate(x, y);
            RcIndex rc = _bounds.ProjToCell(coord);

            int iRow = rc.Row;
            int jCol = rc.Column;

            bool IsAdded = false;

            if ((jCol >= 0 && jCol < nCols && iRow >= 0 && iRow < nRows))
            {
                if (_grid[iRow][jCol] == null)
                    _grid[iRow][jCol] = new Bin();

                _grid[iRow][jCol].AddPoint(z, classification);

                IsAdded = true;
            }

            return IsAdded;
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

    }

    internal class Bin
    {
        int _referenceHeight;
        List<BinPoint> groundPoints;
        List<BinPoint> otherPoints;

        public Bin()
        {
            groundPoints = new List<BinPoint>();
            otherPoints = new List<BinPoint>();
        }

        public void AddPoint(double z, byte classification)
        {
            if (classification == 2)
                groundPoints.Add(new BinPoint() { Z = z });
            else
                otherPoints.Add(new BinPoint() { Z = z });
        }

        public double ReferenceHeight
        {
            get { return _referenceHeight / 100D; }
            set { _referenceHeight = (int)(ReferenceHeight * 100); }
        }

        public void OrderPointsFromHighestToLowest()
        {

            groundPoints.Sort();
            otherPoints.Sort();

            groundPoints.Reverse();
            otherPoints.Reverse();
        }

        public double GetGroundMedian()
        {
            return groundPoints[groundPoints.Count / 2].Z;
        }
    }

    internal class BinPoint : IComparable<BinPoint>
    {
        private int _z;

        //public byte Class { get; set; }

        public double Z
        {
            get { return _z / 100D; }
            set { _z = (int)(Z * 100); }
        }

        public int CompareTo(BinPoint b)
        {
            return _z.CompareTo(b._z);
        }
    }
}
