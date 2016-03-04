using DotSpatial.Data;
using DotSpatial.Topology;
using LasUtility.DEM;
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasUtility.DEM
{
    internal class TriangleIndexGrid
    {
        List<int>[][] _grid;

        int _nRows, _nCols;
        public IRasterBounds Bounds { get; private set; }

        public TriangleIndexGrid(int nRows, int nCols, double minX, double minY, double maxX, double maxY)
        {
            _grid = new List<int>[nRows][];
            _nRows = nRows;
            _nCols = nCols;

            Extent extent = new Extent(minX, minY, maxX, maxY);
            Bounds = new RasterBounds(nRows, nCols, extent);

            for (int i = 0; i < nRows; i++)
                _grid[i] = new List<int>[nCols];
        }

        private bool GetGridIndexes(double x, double y, out int iRow, out int jCol)
        {
            RcIndex rc = Bounds.ProjToCell(new Coordinate(x, y));
            iRow = rc.Row;
            jCol = rc.Column;

            if ((jCol >= 0 && jCol < _nCols && iRow >= 0 && iRow < _nRows))
                return true;

            return false;
        }


        public List<int> GetTriangleIndexesInCell(double x, double y)
        {
            int iRow, jCol;

            if (!GetGridIndexes(x, y, out iRow, out jCol) ||
                _grid[iRow][jCol] == null)
            {
                return new List<int>();
            }

            return _grid[iRow][jCol];
        }


        public void AddIndex(IEnvelope e, int index)
        {
            int iRowMin, iColMin, iRowMax, iColMax;

            bool minInBounds = GetGridIndexes(e.Minimum.X, 
                e.Maximum.Y, out iRowMin, out iColMin);
            bool maxInBounds = GetGridIndexes(e.Maximum.X,
                e.Minimum.Y, out iRowMax, out iColMax);

            if (minInBounds && maxInBounds)
            {
                for (int iRow = iRowMin; iRow <= iRowMax; iRow++)
                {
                    for (int jCol = iColMin; jCol <= iColMax; jCol++)
                    {
                        if (_grid[iRow][jCol] == null)
                            _grid[iRow][jCol] = new List<int>();

                        if (!_grid[iRow][jCol].Contains(index))
                            _grid[iRow][jCol].Add(index);
                    }
                }
            }
            else
            {
                throw new IndexOutOfRangeException("Polygon envelope out of bounds");
            }
        }
    }
}
