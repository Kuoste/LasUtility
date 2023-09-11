using LasUtility.Common;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace LasUtility.DEM
{
    internal class TriangleIndexGrid
    {
        List<int>[][] _grid;
        private readonly int _iRowCount;
        private readonly int _iColCount;

        public IRasterBounds Bounds { get; private set; }

        public TriangleIndexGrid(int iRowCount, int iColCount, double minX, double minY, double maxX, double maxY)
        {
            _iRowCount = iRowCount;
            _iColCount = iColCount;

            Envelope extent = new (minX, maxX, minY, maxY);
            Bounds = new RasterBounds(iRowCount, iColCount, extent);
        }

        public void ResetGrid()
        {
            _grid = new List<int>[_iRowCount][];
            for (int i = 0; i < _iRowCount; i++)
                _grid[i] = new List<int>[_iColCount];
        }

        private void CreateGrid()
        {
            ResetGrid();
        }

        private bool GetGridIndexes(double x, double y, out int iRow, out int jCol)
        {
            RcIndex rc = Bounds.ProjToCell(new Coordinate(x, y));
            iRow = rc.Row;
            jCol = rc.Column;

            if ((jCol >= 0 && jCol < _iColCount && iRow >= 0 && iRow < _iRowCount))
                return true;

            return false;
        }


        public List<int> GetTriangleIndexesInCell(double x, double y)
        {
            if (!GetGridIndexes(x, y, out int iRow, out int jCol) ||
                _grid[iRow][jCol] == null)
            {
                return new List<int>();
            }

            return _grid[iRow][jCol];
        }

        public void AddIndex(Envelope e, int index)
        {
            if (_grid == null)
                CreateGrid();

            bool minInBounds = GetGridIndexes(e.MinX, 
                e.MinY, out int iRowMin, out int iColMin);
            bool maxInBounds = GetGridIndexes(e.MaxX,
                e.MaxY, out int iRowMax, out int iColMax);

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
